using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    public enum Phase { Setup, Roll, Main, MoveStorm, GameOver }

    public class Player
    {
        public int Id;
        public string Name;
        public bool IsBot;
        public readonly ResourceBag Hand = new ResourceBag();
        public int PathsLeft = 15, OutpostsLeft = 5, KeepsLeft = 4;
        public int LongestPath;
    }

    /// <summary>
    /// Complete rules engine. Pure C# with no Unity dependency so it can be unit-tested and simulated headless.
    /// </summary>
    public class Game
    {
        public const int PointsToWin = 10;
        public const int LongestPathMin = 5;
        public const int HandLimit = 7;

        public readonly Board Board;
        public readonly List<Player> Players = new List<Player>();
        public readonly List<string> Log = new List<string>();
        public readonly Random Rng;

        public int Current;
        public Phase Phase = Phase.Setup;
        public int StormTile;
        public int Die1, Die2;
        public int Winner = -1;
        public int LongestPathHolder = -1;
        public int TurnNumber = 1;
        public int TradesThisTurn;
        /// <summary>Incremented on every state change so views can cheaply detect updates.</summary>
        public int Version;

        // Setup (snake draft: 0,1,2,3,3,2,1,0)
        int[] setupOrder;
        public int SetupIndex;
        public bool SetupNeedsPath;
        public int SetupLastVertex = -1;

        public event Action<string> Logged;

        public Game(int seed, IList<string> names, IList<bool> bots)
        {
            Rng = new Random(seed);
            Board = Board.Generate(Rng);
            StormTile = Board.RuinsTile();
            for (int i = 0; i < names.Count; i++)
                Players.Add(new Player { Id = i, Name = names[i], IsBot = bots[i] });
            int n = Players.Count;
            setupOrder = new int[n * 2];
            for (int i = 0; i < n; i++) { setupOrder[i] = i; setupOrder[2 * n - 1 - i] = i; }
            Current = setupOrder[0];
            Say(CurrentPlayer.Name + ": place your first outpost.");
        }

        public Player CurrentPlayer { get { return Players[Current]; } }
        public bool InSetup { get { return Phase == Phase.Setup; } }
        public bool SetupSecondRound { get { return SetupIndex >= Players.Count; } }
        public int DiceTotal { get { return Die1 + Die2; } }

        void Say(string msg)
        {
            Log.Add(msg);
            if (Log.Count > 200) Log.RemoveAt(0);
            if (Logged != null) Logged(msg);
        }

        // ------------------------------------------------------------------ scoring

        public int VictoryPoints(int p)
        {
            int vp = 0;
            foreach (var v in Board.Vertices)
            {
                if (v.Owner != p) continue;
                vp += v.Building == Building.Keep ? 2 : 1;
            }
            if (LongestPathHolder == p) vp += 2;
            return vp;
        }

        void CheckWin()
        {
            if (Phase == Phase.GameOver || InSetup) return;
            if (VictoryPoints(Current) >= PointsToWin)
            {
                Winner = Current;
                Phase = Phase.GameOver;
                Say(CurrentPlayer.Name + " wins with " + VictoryPoints(Current) + " points!");
            }
        }

        // ------------------------------------------------------------------ outposts

        public bool SiteIsOpen(int v)
        {
            var vx = Board.Vertices[v];
            if (vx.Owner >= 0) return false;
            foreach (int n in vx.Neighbors) if (Board.Vertices[n].Owner >= 0) return false;
            return true;
        }

        public bool CanPlaceOutpost(int p, int v, bool ignoreCost = false)
        {
            if (p != Current || !SiteIsOpen(v)) return false;
            var pl = Players[p];
            if (InSetup) return !SetupNeedsPath;
            if (Phase != Phase.Main || pl.OutpostsLeft <= 0) return false;
            if (!ignoreCost && !pl.Hand.Contains(Costs.Outpost)) return false;
            foreach (int e in Board.Vertices[v].Edges) if (Board.Edges[e].Owner == p) return true;
            return false;
        }

        public bool PlaceOutpost(int v)
        {
            int p = Current;
            if (!CanPlaceOutpost(p, v)) return false;
            var pl = Players[p];
            var vx = Board.Vertices[v];
            vx.Owner = p;
            vx.Building = Building.Outpost;
            pl.OutpostsLeft--;
            if (InSetup)
            {
                if (SetupSecondRound)
                {
                    var gained = new ResourceBag();
                    foreach (int t in vx.Tiles)
                    {
                        int r = Board.Tiles[t].Resource;
                        if (r >= 0) gained[r]++;
                    }
                    pl.Hand.Add(gained);
                    Say(pl.Name + " founds an outpost and gathers " + gained + ".");
                }
                else Say(pl.Name + " founds an outpost.");
                SetupNeedsPath = true;
                SetupLastVertex = v;
                Say(pl.Name + ": place a path next to it.");
            }
            else
            {
                pl.Hand.Subtract(Costs.Outpost);
                Say(pl.Name + " builds an outpost.");
                UpdateLongestPath();   // an outpost can cut an opponent's path
            }
            Version++;
            CheckWin();
            return true;
        }

        // ------------------------------------------------------------------ paths

        public bool CanPlacePath(int p, int e, bool ignoreCost = false)
        {
            if (p != Current) return false;
            var ed = Board.Edges[e];
            if (ed.Owner >= 0) return false;
            var pl = Players[p];
            if (InSetup) return SetupNeedsPath && (ed.A == SetupLastVertex || ed.B == SetupLastVertex);
            if (Phase != Phase.Main || pl.PathsLeft <= 0) return false;
            if (!ignoreCost && !pl.Hand.Contains(Costs.Path)) return false;
            return ConnectsAt(p, ed.A, e) || ConnectsAt(p, ed.B, e);
        }

        bool ConnectsAt(int p, int v, int exceptEdge)
        {
            var vx = Board.Vertices[v];
            if (vx.Owner == p) return true;
            if (vx.Owner >= 0) return false; // opponent building blocks the connection
            foreach (int e in vx.Edges) if (e != exceptEdge && Board.Edges[e].Owner == p) return true;
            return false;
        }

        public bool PlacePath(int e)
        {
            int p = Current;
            if (!CanPlacePath(p, e)) return false;
            var pl = Players[p];
            Board.Edges[e].Owner = p;
            pl.PathsLeft--;
            if (InSetup)
            {
                SetupNeedsPath = false;
                SetupIndex++;
                if (SetupIndex >= setupOrder.Length)
                {
                    Phase = Phase.Roll;
                    Current = 0;
                    Say("Setup complete. " + CurrentPlayer.Name + ", roll the dice.");
                }
                else
                {
                    Current = setupOrder[SetupIndex];
                    Say(CurrentPlayer.Name + ": place your " + (SetupSecondRound ? "second" : "first") + " outpost.");
                }
            }
            else
            {
                pl.Hand.Subtract(Costs.Path);
                Say(pl.Name + " builds a path.");
            }
            UpdateLongestPath();
            Version++;
            CheckWin();
            return true;
        }

        // ------------------------------------------------------------------ keeps

        public bool CanUpgrade(int p, int v, bool ignoreCost = false)
        {
            if (p != Current || Phase != Phase.Main) return false;
            var vx = Board.Vertices[v];
            var pl = Players[p];
            if (vx.Owner != p || vx.Building != Building.Outpost || pl.KeepsLeft <= 0) return false;
            return ignoreCost || pl.Hand.Contains(Costs.Keep);
        }

        public bool Upgrade(int v)
        {
            int p = Current;
            if (!CanUpgrade(p, v)) return false;
            var pl = Players[p];
            pl.Hand.Subtract(Costs.Keep);
            Board.Vertices[v].Building = Building.Keep;
            pl.KeepsLeft--;
            pl.OutpostsLeft++;
            Say(pl.Name + " raises a keep.");
            Version++;
            CheckWin();
            return true;
        }

        // ------------------------------------------------------------------ dice, production, storm

        public bool RollDice()
        {
            if (Phase != Phase.Roll) return false;
            Die1 = Rng.Next(1, 7);
            Die2 = Rng.Next(1, 7);
            int sum = DiceTotal;
            Say(CurrentPlayer.Name + " rolls " + sum + ".");
            if (sum == 7)
            {
                foreach (var pl in Players)
                {
                    int total = pl.Hand.Total;
                    if (total <= HandLimit) continue;
                    var lost = new ResourceBag();
                    for (int i = 0; i < total / 2; i++) lost[pl.Hand.TakeRandom(Rng)]++;
                    Say(pl.Name + " loses " + lost + " to the storm.");
                }
                Phase = Phase.MoveStorm;
                Say(CurrentPlayer.Name + ": move the storm.");
            }
            else
            {
                Produce(sum);
                Phase = Phase.Main;
            }
            Version++;
            return true;
        }

        void Produce(int number)
        {
            var gains = new ResourceBag[Players.Count];
            for (int i = 0; i < gains.Length; i++) gains[i] = new ResourceBag();
            foreach (var t in Board.Tiles)
            {
                if (t.Number != number || t.Id == StormTile || t.Resource < 0) continue;
                foreach (int v in t.Corners)
                {
                    var vx = Board.Vertices[v];
                    if (vx.Owner < 0) continue;
                    gains[vx.Owner][t.Resource] += vx.Building == Building.Keep ? 2 : 1;
                }
            }
            for (int i = 0; i < gains.Length; i++)
            {
                if (gains[i].Total == 0) continue;
                Players[i].Hand.Add(gains[i]);
                Say(Players[i].Name + " gathers " + gains[i] + ".");
            }
        }

        public bool CanMoveStorm(int tile) { return Phase == Phase.MoveStorm && tile != StormTile; }

        /// <summary>Opponents with cards who have a building on the given tile.</summary>
        public List<int> StealTargets(int tile)
        {
            var list = new List<int>();
            foreach (int v in Board.Tiles[tile].Corners)
            {
                int o = Board.Vertices[v].Owner;
                if (o >= 0 && o != Current && !list.Contains(o) && Players[o].Hand.Total > 0) list.Add(o);
            }
            return list;
        }

        public bool MoveStorm(int tile, int victim = -1)
        {
            if (!CanMoveStorm(tile)) return false;
            StormTile = tile;
            var targets = StealTargets(tile);
            if (targets.Count > 0)
            {
                if (!targets.Contains(victim)) victim = targets[Rng.Next(targets.Count)];
                int r = Players[victim].Hand.TakeRandom(Rng);
                CurrentPlayer.Hand[r]++;
                Say(CurrentPlayer.Name + " moves the storm and plunders 1 card from " + Players[victim].Name + ".");
            }
            else Say(CurrentPlayer.Name + " moves the storm.");
            Phase = Phase.Main;
            Version++;
            return true;
        }

        // ------------------------------------------------------------------ trading

        public int TradeRate(int p, int resource)
        {
            int rate = 4;
            foreach (var v in Board.Vertices)
            {
                if (v.Owner != p || v.Harbor < 0) continue;
                if (v.Harbor == resource) return 2;
                if (v.Harbor == HarborInfo.Generic) rate = 3;
            }
            return rate;
        }

        public bool CanBankTrade(int p, int give, int get)
        {
            return p == Current && Phase == Phase.Main && give != get && Players[p].Hand[give] >= TradeRate(p, give);
        }

        public bool BankTrade(int give, int get)
        {
            int p = Current;
            if (!CanBankTrade(p, give, get)) return false;
            int rate = TradeRate(p, give);
            Players[p].Hand[give] -= rate;
            Players[p].Hand[get] += 1;
            TradesThisTurn++;
            Say(CurrentPlayer.Name + " trades " + rate + " " + Res.Names[give] + " for 1 " + Res.Names[get] + ".");
            Version++;
            return true;
        }

        // ------------------------------------------------------------------ turn flow

        public bool EndTurn()
        {
            if (Phase != Phase.Main) return false;
            Current = (Current + 1) % Players.Count;
            if (Current == 0) TurnNumber++;
            TradesThisTurn = 0;
            Phase = Phase.Roll;
            Say(CurrentPlayer.Name + ", roll the dice.");
            Version++;
            return true;
        }

        // ------------------------------------------------------------------ longest path

        void UpdateLongestPath()
        {
            int best = 0;
            foreach (var pl in Players)
            {
                pl.LongestPath = ComputeLongestPath(pl.Id);
                if (pl.LongestPath > best) best = pl.LongestPath;
            }
            int holder = LongestPathHolder;
            if (holder >= 0 && Players[holder].LongestPath >= best && best >= LongestPathMin) return; // holder keeps ties

            int newHolder = -1, count = 0;
            foreach (var pl in Players)
                if (pl.LongestPath == best && best >= LongestPathMin) { newHolder = pl.Id; count++; }
            if (count != 1) newHolder = -1;
            if (newHolder != holder)
            {
                LongestPathHolder = newHolder;
                if (newHolder >= 0) Say(Players[newHolder].Name + " now holds the Longest Path (" + best + ").");
                else Say("Nobody holds the Longest Path.");
            }
        }

        public int ComputeLongestPath(int p)
        {
            int best = 0;
            var visited = new bool[Board.Edges.Count];
            foreach (var e in Board.Edges)
            {
                if (e.Owner != p) continue;
                visited[e.Id] = true;
                best = Math.Max(best, Walk(p, e.B, visited, 1));
                best = Math.Max(best, Walk(p, e.A, visited, 1));
                visited[e.Id] = false;
            }
            return best;
        }

        int Walk(int p, int v, bool[] visited, int length)
        {
            var vx = Board.Vertices[v];
            if (vx.Owner >= 0 && vx.Owner != p) return length;
            int best = length;
            foreach (int e in vx.Edges)
            {
                if (visited[e] || Board.Edges[e].Owner != p) continue;
                visited[e] = true;
                best = Math.Max(best, Walk(p, Board.Edges[e].Other(v), visited, length + 1));
                visited[e] = false;
            }
            return best;
        }

        // ------------------------------------------------------------------ queries for UI / bots

        public List<int> LegalOutposts(int p, bool ignoreCost = false)
        {
            var list = new List<int>();
            for (int v = 0; v < Board.Vertices.Count; v++) if (CanPlaceOutpost(p, v, ignoreCost)) list.Add(v);
            return list;
        }

        public List<int> LegalPaths(int p, bool ignoreCost = false)
        {
            var list = new List<int>();
            for (int e = 0; e < Board.Edges.Count; e++) if (CanPlacePath(p, e, ignoreCost)) list.Add(e);
            return list;
        }

        public List<int> LegalUpgrades(int p, bool ignoreCost = false)
        {
            var list = new List<int>();
            for (int v = 0; v < Board.Vertices.Count; v++) if (CanUpgrade(p, v, ignoreCost)) list.Add(v);
            return list;
        }
    }
}
