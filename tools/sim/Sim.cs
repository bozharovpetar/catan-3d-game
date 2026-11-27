// Headless simulation harness — the executable spec of the Core API (docs/09-TESTING.md §3).
// Build: tools/sim/run-sim.sh  (Mono mcs or .NET SDK). Compiles ONLY game/Assets/Hexhaven/Scripts/Core/**.cs + this file.
//
// Modes:
//   Sim.exe [games=1000] [--seed0 N]       AI games with all gates (04-AI §6, 09-TESTING §3). Exit 0 only if all pass.
//   Sim.exe --random-legal [games=2000]     random legal-action driver (no AI): invariants, replay, save round-trip.
//   Sim.exe --selftest board                board/geometry/generator checks (works from milestone M1).
//   Sim.exe --one SEED [--verbose]          single AI game, prints the log.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Hexhaven.Core;
using Hexhaven.Core.AI;

static class Sim
{
    static int failures;
    static long totalDecisions;
    static readonly List<string> failureNotes = new List<string>();

    static void Fail(string msg)
    {
        failures++;
        if (failureNotes.Count < 40) failureNotes.Add(msg);
    }

    static int Main(string[] args)
    {
        try
        {
            if (args.Length >= 2 && args[0] == "--selftest" && args[1] == "board") return SelfTestBoard();
            if (args.Length >= 1 && args[0] == "--random-legal") return RandomLegal(args.Length > 1 ? int.Parse(args[1]) : 2000);
            if (args.Length >= 2 && args[0] == "--one") return One(int.Parse(args[1]), args.Contains("--verbose"));
            int games = args.Length > 0 && !args[0].StartsWith("--") ? int.Parse(args[0]) : 1000;
            int seed0 = 1;
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--seed0") seed0 = int.Parse(args[i + 1]);
            return AiGames(games, seed0);
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine("NOT IMPLEMENTED YET: " + ex.Message + "\n" + ex.StackTrace);
            return 3;
        }
    }

    // ===================================================================== board self-test (M1)

    static int SelfTestBoard()
    {
        var g = Board.CreateGeometry();
        Check(g.Tiles.Count == 19 && g.Vertices.Count == 54 && g.Edges.Count == 72 && g.Harbors.Count == 9, "counts");
        var byTiles = new int[4];
        foreach (var v in g.Vertices) byTiles[v.Tiles.Count]++;
        Check(byTiles[3] == 24 && byTiles[2] == 12 && byTiles[1] == 18, "vertex tile distribution " + string.Join(",", byTiles));
        Check(g.Edges.Count(e => e.Tiles.Count == 2) == 42 && g.Edges.Count(e => e.Tiles.Count == 1) == 30, "edge tile distribution");
        foreach (var v in g.Vertices) Check(v.Neighbors.Count == 2 || v.Neighbors.Count == 3, "vertex degree");
        int[,] order = { { -2, 0 }, { -2, 1 }, { -2, 2 }, { -1, -1 }, { -1, 0 }, { -1, 1 }, { -1, 2 }, { 0, -2 }, { 0, -1 }, { 0, 0 }, { 0, 1 }, { 0, 2 }, { 1, -2 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 2, -2 }, { 2, -1 }, { 2, 0 } };
        for (int i = 0; i < 19; i++) Check(g.Tiles[i].Q == order[i, 0] && g.Tiles[i].R == order[i, 1], "tile order " + i);
        foreach (var h in g.Harbors) Check(g.Edges[h.EdgeId].Tiles.Count == 1, "harbor coastal " + h.Slot);
        var harborVerts = new HashSet<int>();
        foreach (var h in g.Harbors)
        {
            var e = g.Edges[h.EdgeId];
            Check(harborVerts.Add(e.A) && harborVerts.Add(e.B), "corner in two harbors (slot " + h.Slot + ")");
        }

        var beg = BoardGenerator.Generate(BoardMode.Beginner, new Pcg32(1));
        Check(BoardGenerator.SatisfiesC1(beg) && BoardGenerator.SatisfiesC2(beg) && BoardGenerator.SatisfiesC3(beg) && BoardGenerator.SatisfiesC4(beg) && BoardGenerator.SatisfiesC5(beg), "beginner satisfies C1-C5");
        Check(beg.Tiles[10].Terrain == Terrain.Ruins && beg.RuinsTile == 10, "beginner ruins at tile 10");

        var sw = Stopwatch.StartNew();
        long worst = 0;
        for (int seed = 1; seed <= 1000; seed++)
        {
            var t0 = sw.ElapsedTicks;
            var b = BoardGenerator.Generate(BoardMode.BalancedRandom, new Pcg32((ulong)seed));
            worst = Math.Max(worst, sw.ElapsedTicks - t0);
            Check(BoardGenerator.SatisfiesC1(b) && BoardGenerator.SatisfiesC2(b) && BoardGenerator.SatisfiesC3(b) && BoardGenerator.SatisfiesC4(b) && BoardGenerator.SatisfiesC5(b), "balanced C1-C5 seed " + seed);
            CheckMultisets(b, seed);
            var r = BoardGenerator.Generate(BoardMode.Random, new Pcg32((ulong)seed));
            Check(BoardGenerator.SatisfiesC1(r), "random C1 seed " + seed);
            CheckMultisets(r, seed);
        }
        double worstMs = worst * 1000.0 / Stopwatch.Frequency;
        Check(worstMs < 200, "balanced generation worst " + worstMs.ToString("F1") + " ms");
        var b1 = BoardGenerator.Generate(BoardMode.BalancedRandom, new Pcg32(77));
        var b2 = BoardGenerator.Generate(BoardMode.BalancedRandom, new Pcg32(77));
        for (int i = 0; i < 19; i++) Check(b1.Tiles[i].Terrain == b2.Tiles[i].Terrain && b1.Tiles[i].Number == b2.Tiles[i].Number, "deterministic");

        Console.WriteLine("board selftest: " + (failures == 0 ? "PASS" : "FAIL") + " (worst balanced gen " + worstMs.ToString("F1") + " ms)");
        foreach (var n in failureNotes) Console.WriteLine("  - " + n);
        return failures == 0 ? 0 : 1;
    }

    static void CheckMultisets(Board b, int seed)
    {
        var t = b.Tiles.GroupBy(x => x.Terrain).ToDictionary(x => x.Key, x => x.Count());
        Check(t[Terrain.Forest] == 4 && t[Terrain.ClayPit] == 3 && t[Terrain.FlaxField] == 4 && t[Terrain.Farmland] == 4 && t[Terrain.Mine] == 3 && t[Terrain.Ruins] == 1, "terrain multiset " + seed);
        var nums = b.Tiles.Where(x => x.Terrain != Terrain.Ruins).Select(x => x.Number).OrderBy(x => x).ToArray();
        Check(string.Join(",", nums) == "2,3,3,4,4,5,5,6,6,8,8,9,9,10,10,11,11,12", "token multiset " + seed);
        Check(b.Tiles[b.RuinsTile].Number == 0, "ruins number " + seed);
        var types = b.Harbors.Select(h => h.Type).ToList();
        Check(types.Count(x => x == HarborType.Generic) == 4 && types.Distinct().Count() == 6, "harbor types " + seed);
    }

    static void Check(bool ok, string what) { if (!ok) Fail(what); }

    // ===================================================================== AI games (M3+)

    sealed class Mix { public string Name; public BotDifficulty[] Seats; }

    static readonly Mix[] Mixes =
    {
        new Mix { Name = "4xNormal", Seats = new[] { BotDifficulty.Normal, BotDifficulty.Normal, BotDifficulty.Normal, BotDifficulty.Normal } },
        new Mix { Name = "Hard+3Easy", Seats = new[] { BotDifficulty.Hard, BotDifficulty.Easy, BotDifficulty.Easy, BotDifficulty.Easy } },
        new Mix { Name = "Normal+3Easy", Seats = new[] { BotDifficulty.Normal, BotDifficulty.Easy, BotDifficulty.Easy, BotDifficulty.Easy } },
        new Mix { Name = "Hard+3Normal", Seats = new[] { BotDifficulty.Hard, BotDifficulty.Normal, BotDifficulty.Normal, BotDifficulty.Normal } },
    };

    sealed class Stats
    {
        public int Games, Finished, Stalls, IllegalActions, InvariantViolations, ReplayMismatches, SaveMismatches;
        public long TotalTurns; public List<int> Turns = new List<int>();
        public Dictionary<string, int[]> WinsByMix = new Dictionary<string, int[]>();
        public Dictionary<string, int> GamesByMix = new Dictionary<string, int>();
        public int[] WinsBySeat4xNormal = new int[4]; public int Games4xNormal4p;
        public double DecideTotalMs; public long Decides; public double DecideMaxMs;
        public long[] DecideHist = new long[10001];   // 0.01 ms buckets up to 100 ms (last = overflow); first 1,000 decisions skipped
        public int GamesWithLongestPath, GamesWithStrongestGuard, GamesWithDecree, GamesWithRoadCrew;
        public long DomesticTrades;
    }

    static GameConfig MakeConfig(int gameIndex, int seed, Mix mix, Pcg32 cfgRng)
    {
        int players = gameIndex % 5 == 4 ? 3 : 4;
        var c = new GameConfig { Seed = seed };
        c.BoardMode = (BoardMode)(gameIndex % 3);
        c.FriendlyStorm = cfgRng.NextInt(10) == 0;
        c.ClassicPhases = cfgRng.NextInt(10) == 1;
        int t = cfgRng.NextInt(20);
        c.VictoryTarget = t == 0 ? 8 : t == 1 ? 12 : 10;
        string[] names = { "A", "B", "C", "D" };
        for (int i = 0; i < players; i++)
            c.Players.Add(new PlayerConfig { Name = names[i], IsBot = true, Difficulty = mix.Seats[i] });
        return c;
    }

    static int AiGames(int games, int seed0)
    {
        var st = new Stats();
        var cfgRng = new Pcg32(12345);
        for (int g = 0; g < games; g++)
        {
            var mix = Mixes[g % Mixes.Length];
            var cfg = MakeConfig(g / Mixes.Length, seed0 + g, mix, cfgRng);
            RunAiGame(cfg, mix, g, st, false);
        }
        return Report(st);
    }

    static int One(int seed, bool verbose)
    {
        var st = new Stats();
        var cfg = MakeConfig(0, seed, Mixes[0], new Pcg32(1));
        cfg.VictoryTarget = 10; cfg.FriendlyStorm = false; cfg.ClassicPhases = false;
        RunAiGame(cfg, Mixes[0], 0, st, verbose);
        return Report(st);
    }

    static void RunAiGame(GameConfig cfg, Mix mix, int gameIndex, Stats st, bool verbose)
    {
        var engine = GameEngine.NewGame(cfg);
        int n = cfg.Players.Count;
        var memories = new BotMemory[n];
        for (int i = 0; i < n; i++) memories[i] = new BotMemory(i, n);
        var initial = engine.TakeInitialEvents();
        Feed(memories, initial, engine.State, verbose);

        bool usedLp = false, usedSg = false, usedDecree = false, usedRoadCrew = false;
        int domestic = 0;
        int steps = 0;
        while (engine.State.Phase != Phase.GameOver && engine.State.TurnNumber < 400 && steps < 200000)
        {
            steps++;
            var waiting = engine.AwaitingInput();
            if (waiting.Count == 0) { Fail("No one awaiting input in phase " + engine.State.Phase + " seed " + cfg.Seed); break; }
            int seat = waiting[0];
            var sw = Stopwatch.StartNew();
            var action = BotBrain.Decide(engine, seat, memories[seat]);
            double ms = sw.Elapsed.TotalMilliseconds;
            totalDecisions++;
            if (totalDecisions > 1000)
            {
                st.DecideTotalMs += ms; st.Decides++; if (ms > st.DecideMaxMs) st.DecideMaxMs = ms;
                st.DecideHist[Math.Min(10000, (int)(ms * 100))]++;
            }

            string err = engine.Validate(action);
            if (err != null) { st.IllegalActions++; Fail("Illegal bot action " + action + " -> " + err + " (seed " + cfg.Seed + ", phase " + engine.State.Phase + ")"); break; }
            var res = engine.Apply(action);
            if (!res.Ok) { st.IllegalActions++; Fail("Apply failed after Validate ok: " + action + " -> " + res.Error); break; }
            Feed(memories, res.Events, engine.State, verbose);
            foreach (var e in res.Events)
            {
                if (e.Type == EventType.AwardChanged && e.Player >= 0) { if (e.Award == AwardType.LongestPath) usedLp = true; else usedSg = true; }
                if (e.Type == EventType.CharterPlayed && e.Charter == CharterType.Decree) usedDecree = true;
                if (e.Type == EventType.CharterPlayed && e.Charter == CharterType.RoadCrew) usedRoadCrew = true;
                if (e.Type == EventType.TradeCompleted) domestic++;
            }
            var inv = GameInvariants.Check(engine.State);
            if (inv.Count > 0) { st.InvariantViolations++; Fail("Invariant (seed " + cfg.Seed + "): " + string.Join(" | ", inv)); break; }
        }

        st.Games++;
        if (!st.GamesByMix.ContainsKey(mix.Name)) { st.GamesByMix[mix.Name] = 0; st.WinsByMix[mix.Name] = new int[4]; }
        if (engine.State.Phase == Phase.GameOver)
        {
            st.Finished++;
            st.TotalTurns += engine.State.TurnNumber;
            st.Turns.Add(engine.State.TurnNumber);
            if (n == 4)
            {
                st.GamesByMix[mix.Name]++;
                st.WinsByMix[mix.Name][engine.State.Winner]++;
                if (mix.Name == "4xNormal") { st.Games4xNormal4p++; st.WinsBySeat4xNormal[engine.State.Winner]++; }
            }
            if (engine.VictoryPoints(engine.State.Winner, true) < cfg.VictoryTarget) Fail("Winner below target, seed " + cfg.Seed);
        }
        else if (engine.State.TurnNumber >= 400) st.Stalls++;

        if (usedLp) st.GamesWithLongestPath++;
        if (usedSg) st.GamesWithStrongestGuard++;
        if (usedDecree) st.GamesWithDecree++;
        if (usedRoadCrew) st.GamesWithRoadCrew++;
        st.DomesticTrades += domestic;

        if (gameIndex % 10 == 0)
        {
            var replay = GameEngine.Replay(cfg, engine.State.ActionLog);
            if (StateHasher.Hash(replay.State) != StateHasher.Hash(engine.State)) { st.ReplayMismatches++; Fail("Replay hash mismatch seed " + cfg.Seed); }
        }
        if (gameIndex % 25 == 0)
        {
            string text = SaveCodec.Write(engine.State, "sim", "2026-01-01T00:00:00Z");
            string error; SaveMeta meta;
            var loaded = SaveCodec.Read(text, out error, out meta);
            if (loaded == null || StateHasher.Hash(loaded.State) != StateHasher.Hash(engine.State)) { st.SaveMismatches++; Fail("Save round-trip failed seed " + cfg.Seed + ": " + error); }
        }
    }

    static void Feed(BotMemory[] memories, List<GameEvent> events, GameState s, bool verbose)
    {
        foreach (var e in events)
        {
            for (int i = 0; i < memories.Length; i++) memories[i].Observe(e.RedactFor(i), s);
            if (verbose) Console.WriteLine("  [" + e.Seq + "] " + e.Type + " p=" + e.Player + " o=" + e.Other + " v=" + e.Value + " bag=" + e.Bag + " bag2=" + e.Bag2);
        }
    }

    static int Report(Stats st)
    {
        double meanTurns = st.Finished > 0 ? st.TotalTurns / (double)st.Finished : 0;
        st.Turns.Sort();
        int median = st.Turns.Count > 0 ? st.Turns[st.Turns.Count / 2] : 0;
        double finishedPct = st.Games > 0 ? 100.0 * st.Finished / st.Games : 0;
        Console.WriteLine("=== Hexhaven simulation ===");
        Console.WriteLine("games " + st.Games + ", finished " + st.Finished + " (" + finishedPct.ToString("F2") + "%), stalls " + st.Stalls);
        Console.WriteLine("turns mean " + meanTurns.ToString("F1") + ", median " + median);
        Console.WriteLine("illegal actions " + st.IllegalActions + ", invariant violations " + st.InvariantViolations + ", replay mismatches " + st.ReplayMismatches + ", save mismatches " + st.SaveMismatches);
        double p999 = Percentile(st.DecideHist, st.Decides, 0.999);
        Console.WriteLine("decide mean " + (st.Decides > 0 ? st.DecideTotalMs / st.Decides : 0).ToString("F3") + " ms, p99.9 " + p999.ToString("F2") + " ms, max " + st.DecideMaxMs.ToString("F1") + " ms (first 1,000 decisions excluded)");
        foreach (var kv in st.GamesByMix)
        {
            var w = st.WinsByMix[kv.Key];
            Console.WriteLine("mix " + kv.Key + " (4p games " + kv.Value + "): wins by seat " + string.Join(",", w) + (kv.Value > 0 ? "  seat0 rate " + (100.0 * w[0] / kv.Value).ToString("F1") + "%" : ""));
        }
        double g = Math.Max(1, st.Games);
        Console.WriteLine("awards: Longest Path in " + (100 * st.GamesWithLongestPath / g).ToString("F0") + "%, Strongest Guard in " + (100 * st.GamesWithStrongestGuard / g).ToString("F0") + "%");
        Console.WriteLine("charters: Decree played in " + (100 * st.GamesWithDecree / g).ToString("F0") + "%, Road Crew in " + (100 * st.GamesWithRoadCrew / g).ToString("F0") + "%");
        Console.WriteLine("domestic trades per game " + (st.DomesticTrades / g).ToString("F2"));

        // ---- gates (04-AI §6, 09-TESTING §3) — evaluated only with enough games for statistics
        Gate(st.IllegalActions == 0, "0 illegal actions");
        Gate(st.InvariantViolations == 0, "0 invariant violations");
        Gate(st.ReplayMismatches == 0 && st.SaveMismatches == 0, "replay/save hashes match");
        if (st.Games >= 400)
        {
            Gate(finishedPct >= 99.5, "≥ 99.5% games finish before turn 400");
            Gate(meanTurns >= 60 && meanTurns <= 110, "mean game length 60–110 turns");
            Gate(st.Decides == 0 || (st.DecideTotalMs / st.Decides < 2.0 && p999 < 30), "Decide mean < 2 ms, p99.9 < 30 ms");
            Gate(st.GamesWithLongestPath / g >= 0.40 && st.GamesWithLongestPath / g <= 0.90, "Longest Path awarded in 40–90% of games");
            Gate(st.GamesWithStrongestGuard / g >= 0.10 && st.GamesWithStrongestGuard / g <= 0.60, "Strongest Guard in 10–60%");
            Gate(st.GamesWithDecree / g >= 0.20, "Decree played in ≥ 20%");
            Gate(st.GamesWithRoadCrew / g >= 0.20, "Road Crew played in ≥ 20%");
            Gate(st.DomesticTrades / g >= 1.0, "≥ 1 domestic trade per game on average");
            RateGate(st, "Hard+3Easy", 0.40);
            RateGate(st, "Normal+3Easy", 0.33);
            RateGate(st, "Hard+3Normal", 0.28);
        }
        if (st.Games4xNormal4p >= 2000)
            for (int s = 0; s < 4; s++)
                Gate(Math.Abs(100.0 * st.WinsBySeat4xNormal[s] / st.Games4xNormal4p - 25) <= 5, "seat " + s + " win rate within 25 ± 5% (4xNormal)");

        Console.WriteLine(failures == 0 ? "RESULT: PASS" : "RESULT: FAIL (" + failures + ")");
        foreach (var n in failureNotes) Console.WriteLine("  - " + n);
        return failures == 0 ? 0 : 1;
    }

    static double Percentile(long[] hist, long count, double q)
    {
        if (count == 0) return 0;
        long target = (long)Math.Ceiling(count * q), acc = 0;
        for (int i = 0; i < hist.Length; i++) { acc += hist[i]; if (acc >= target) return i / 100.0; }
        return 100;
    }

    static void RateGate(Stats st, string mix, double minRate)
    {
        int games;
        if (!st.GamesByMix.TryGetValue(mix, out games) || games < 100) return;
        double rate = st.WinsByMix[mix][0] / (double)games;
        Gate(rate >= minRate, mix + " seat-0 win rate " + (rate * 100).ToString("F1") + "% ≥ " + (minRate * 100) + "%");
    }

    static void Gate(bool ok, string name)
    {
        Console.WriteLine((ok ? "  [PASS] " : "  [FAIL] ") + name);
        if (!ok) Fail("gate: " + name);
    }

    // ===================================================================== random legal driver (M2)

    static int RandomLegal(int games)
    {
        var rng = new Pcg32(999);
        int finished = 0, actions = 0;
        for (int g = 0; g < games; g++)
        {
            var cfg = GameConfig.Default(1000 + g, g % 4 == 3 ? 3 : 4);
            foreach (var p in cfg.Players) p.IsBot = true;
            cfg.BoardMode = (BoardMode)(g % 3);
            cfg.FriendlyStorm = g % 7 == 0;
            cfg.ClassicPhases = g % 5 == 0;
            var engine = GameEngine.NewGame(cfg);
            engine.TakeInitialEvents();
            int steps = 0;
            while (engine.State.Phase != Phase.GameOver && engine.State.TurnNumber < 300 && steps < 50000)
            {
                steps++;
                var waiting = engine.AwaitingInput();
                if (waiting.Count == 0) { Fail("no awaiting seat, phase " + engine.State.Phase); break; }
                int seat = waiting[rng.NextInt(waiting.Count)];
                var cands = Candidates(engine, seat, rng);
                GameAction chosen = null;
                for (int tries = 0; tries < cands.Count * 2 && chosen == null; tries++)
                {
                    var c = cands[rng.NextInt(cands.Count)];
                    if (engine.Validate(c) == null) chosen = c;
                }
                if (chosen == null) chosen = cands.FirstOrDefault(c => engine.Validate(c) == null);
                if (chosen == null) { Fail("no legal candidate for seat " + seat + " in " + engine.State.Phase + " (seed " + cfg.Seed + ")"); break; }
                ulong before = StateHasher.Hash(engine.State);
                // Validate purity
                foreach (var c in cands.Take(5)) engine.Validate(c);
                if (StateHasher.Hash(engine.State) != before) { Fail("Validate mutated state"); break; }
                var res = engine.Apply(chosen);
                actions++;
                if (!res.Ok) { Fail("Apply failed after Validate ok: " + chosen + " -> " + res.Error); break; }
                var inv = GameInvariants.Check(engine.State);
                if (inv.Count > 0) { Fail("Invariant (seed " + cfg.Seed + ", after " + chosen + "): " + string.Join(" | ", inv)); break; }
                // Illegal action must not mutate
                var bogus = GameAction.BuildKeep(seat, rng.NextInt(54));
                if (engine.Validate(bogus) != null)
                {
                    ulong h = StateHasher.Hash(engine.State);
                    var r2 = engine.Apply(bogus);
                    if (r2.Ok || StateHasher.Hash(engine.State) != h) { Fail("Illegal action changed state or succeeded"); break; }
                }
            }
            if (engine.State.Phase == Phase.GameOver) finished++;
            if (g % 20 == 0)
            {
                var replay = GameEngine.Replay(cfg, engine.State.ActionLog);
                if (StateHasher.Hash(replay.State) != StateHasher.Hash(engine.State)) Fail("replay mismatch seed " + cfg.Seed);
                string error; SaveMeta meta;
                var loaded = SaveCodec.Read(SaveCodec.Write(engine.State, "x", "t"), out error, out meta);
                if (loaded == null || StateHasher.Hash(loaded.State) != StateHasher.Hash(engine.State)) Fail("save round trip seed " + cfg.Seed + ": " + error);
            }
            if (failures > 0) break;
        }
        Console.WriteLine("random-legal: games " + games + ", finished " + finished + ", actions " + actions + " → " + (failures == 0 ? "PASS" : "FAIL"));
        foreach (var n in failureNotes) Console.WriteLine("  - " + n);
        return failures == 0 ? 0 : 1;
    }

    static ResourceBag RandomSubBag(ResourceBag hand, int count, Pcg32 rng)
    {
        var bag = new ResourceBag();
        var h = hand;
        for (int i = 0; i < count && h.Total > 0; i++)
        {
            int pick = rng.NextInt(h.Total);
            for (int r = 0; r < 5; r++) { if (pick < h[r]) { h[r]--; bag[r]++; break; } pick -= h[r]; }
        }
        return bag;
    }

    static List<GameAction> Candidates(GameEngine e, int seat, Pcg32 rng)
    {
        var s = e.State;
        var list = new List<GameAction>();
        var me = s.Players[seat];
        switch (s.Phase)
        {
            case Phase.RollForOrder: list.Add(GameAction.RollForOrder(seat)); break;
            case Phase.SetupOutpost: foreach (int v in e.LegalSetupOutposts()) list.Add(GameAction.PlaceSetupOutpost(seat, v)); break;
            case Phase.SetupPath: foreach (int x in e.LegalSetupPaths()) list.Add(GameAction.PlaceSetupPath(seat, x)); break;
            case Phase.TurnStart:
                list.Add(GameAction.RollDice(seat));
                if (rng.NextInt(4) == 0)
                {   // any Charter may be played before the roll (R-4.2)
                    list.Add(GameAction.PlayWarden(seat));
                    list.Add(GameAction.PlayRoadCrew(seat));
                    list.Add(GameAction.PlayBounty(seat, ResourceBag.Single((Resource)rng.NextInt(5), 1) + ResourceBag.Single((Resource)rng.NextInt(5), 1)));
                    list.Add(GameAction.PlayDecree(seat, (Resource)rng.NextInt(5)));
                }
                break;
            case Phase.Discard: list.Add(GameAction.Discard(seat, RandomSubBag(me.Hand, me.PendingDiscard, rng))); break;
            case Phase.MoveStorm: foreach (int t in e.LegalStormTiles(seat)) list.Add(GameAction.MoveStorm(seat, t)); break;
            case Phase.ChooseVictim: foreach (int p in e.PlunderCandidates(seat, s.StormTile)) list.Add(GameAction.ChooseVictim(seat, p)); break;
            case Phase.RoadCrew: foreach (int x in e.LegalPaths(seat, true)) list.Add(GameAction.PlaceFreePath(seat, x)); break;
            case Phase.Main:
                var t0 = s.ActiveTrade;
                if (t0 != null)
                {
                    if (seat == t0.Proposer)
                    {
                        for (int p = 0; p < s.PlayerCount; p++)
                            if (t0.Responses[p] == TradeResponse.Accepted || t0.Responses[p] == TradeResponse.Countered) list.Add(GameAction.ConfirmTrade(seat, t0.Id, p));
                        list.Add(GameAction.CancelTrade(seat, t0.Id));
                    }
                    else
                    {
                        list.Add(GameAction.RespondTrade(seat, t0.Id, TradeResponse.Accepted, ResourceBag.Zero, ResourceBag.Zero));
                        list.Add(GameAction.RespondTrade(seat, t0.Id, TradeResponse.Declined, ResourceBag.Zero, ResourceBag.Zero));
                        var cg = RandomSubBag(s.Players[t0.Proposer].Hand, 1 + rng.NextInt(2), rng);
                        var cget = RandomSubBag(me.Hand, 1, rng);
                        list.Add(GameAction.RespondTrade(seat, t0.Id, TradeResponse.Countered, cg, cget));
                    }
                    break;
                }
                foreach (int x in e.LegalPaths(seat, false)) list.Add(GameAction.BuildPath(seat, x));
                foreach (int v in e.LegalOutposts(seat, false)) list.Add(GameAction.BuildOutpost(seat, v));
                foreach (int v in e.LegalKeeps(seat, false)) list.Add(GameAction.BuildKeep(seat, v));
                list.Add(GameAction.BuyCharter(seat));
                list.Add(GameAction.PlayWarden(seat));
                list.Add(GameAction.PlayRoadCrew(seat));
                list.Add(GameAction.PlayBounty(seat, ResourceBag.Single((Resource)rng.NextInt(5), 1) + ResourceBag.Single((Resource)rng.NextInt(5), 1)));
                list.Add(GameAction.PlayDecree(seat, (Resource)rng.NextInt(5)));
                list.Add(GameAction.MaritimeTrade(seat, (Resource)rng.NextInt(5), (Resource)rng.NextInt(5)));
                var give = RandomSubBag(me.Hand, 1 + rng.NextInt(2), rng);
                var get = ResourceBag.Single((Resource)rng.NextInt(5), 1);
                int mask = 0; for (int p = 0; p < s.PlayerCount; p++) if (p != seat && rng.NextInt(2) == 0) mask |= 1 << p;
                if (mask == 0) mask = 1 << ((seat + 1) % s.PlayerCount);
                list.Add(GameAction.ProposeTrade(seat, give, get, mask));
                for (int k = 0; k < 3; k++) list.Add(GameAction.EndTurn(seat));   // weight ending
                break;
        }
        if (list.Count == 0) list.Add(GameAction.EndTurn(seat));
        return list;
    }
}
