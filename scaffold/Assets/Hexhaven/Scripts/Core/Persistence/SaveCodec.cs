// SCAFFOLD — complete (depends on GameEngine.Replay from M2). Spec: docs/08-PERSISTENCE.md §2.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hexhaven.Core
{
    public sealed class SaveMeta
    {
        public string SavedAtUtc, Label;
        public int Round;
        public int[] PublicVp;
        public GameConfig Config;
    }

    public static class SaveCodec
    {
        public const string Header = "HEXHAVEN-SAVE 1";

        public static string Write(GameState state, string label, string savedAtUtcIso)
        {
            var ci = CultureInfo.InvariantCulture;
            var c = state.Config;
            int round = state.TurnNumber <= 0 ? 0 : (state.TurnNumber - 1) / state.PlayerCount + 1;
            var sb = new StringBuilder();
            sb.Append(Header).Append('\n');
            sb.Append("meta savedAtUtc=").Append(Enc(savedAtUtcIso)).Append(" round=").Append(round.ToString(ci)).Append(" label=").Append(Enc(label ?? "")).Append(" vp=");
            for (int i = 0; i < state.PlayerCount; i++) { if (i > 0) sb.Append(','); sb.Append(PublicVp(state, i).ToString(ci)); }
            sb.Append('\n');
            sb.Append("config seed=").Append(c.Seed.ToString(ci)).Append(" board=").Append(c.BoardMode)
              .Append(" target=").Append(c.VictoryTarget.ToString(ci)).Append(" friendly=").Append(c.FriendlyStorm ? "1" : "0").Append('\n');
            for (int i = 0; i < c.Players.Count; i++)
            {
                var p = c.Players[i];
                sb.Append("player seat=").Append(i.ToString(ci)).Append(" name=").Append(Enc(p.Name))
                  .Append(" bot=").Append(p.IsBot ? "1" : "0").Append(" diff=").Append(p.Difficulty).Append('\n');
            }
            foreach (var a in state.ActionLog) sb.Append(a.ToCodec()).Append('\n');
            sb.Append("hash ").Append(StateHasher.HashHex(state)).Append('\n');
            return sb.ToString();
        }

        /// <summary>Parses and replays. Returns null and sets error on any failure.</summary>
        public static GameEngine Read(string text, out string error, out SaveMeta meta)
        {
            error = null;
            meta = new SaveMeta();
            try
            {
                var lines = text.Replace("\r\n", "\n").Split('\n');
                if (lines.Length == 0 || lines[0].Trim() != Header) { error = "Not a Hexhaven save (bad header)."; return null; }
                var config = new GameConfig();
                var players = new SortedDictionary<int, PlayerConfig>();
                var actions = new List<GameAction>();
                string hash = null;
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("a ")) { actions.Add(GameAction.FromCodec(line)); continue; }
                    if (line.StartsWith("hash ")) { hash = line.Substring(5).Trim(); continue; }
                    var kv = ParseKv(line);
                    if (line.StartsWith("meta "))
                    {
                        meta.SavedAtUtc = Get(kv, "savedAtUtc", "");
                        meta.Label = Get(kv, "label", "");
                        meta.Round = int.Parse(Get(kv, "round", "0"), CultureInfo.InvariantCulture);
                        string vp = Get(kv, "vp", "");
                        if (vp.Length > 0) meta.PublicVp = Array.ConvertAll(vp.Split(','), x => int.Parse(x, CultureInfo.InvariantCulture));
                    }
                    else if (line.StartsWith("config "))
                    {
                        config.Seed = int.Parse(Get(kv, "seed", "0"), CultureInfo.InvariantCulture);
                        config.BoardMode = (BoardMode)Enum.Parse(typeof(BoardMode), Get(kv, "board", "BalancedRandom"));
                        config.VictoryTarget = int.Parse(Get(kv, "target", "10"), CultureInfo.InvariantCulture);
                        config.FriendlyStorm = Get(kv, "friendly", "0") == "1";
                    }
                    else if (line.StartsWith("player "))
                    {
                        int seat = int.Parse(Get(kv, "seat", "0"), CultureInfo.InvariantCulture);
                        players[seat] = new PlayerConfig
                        {
                            Name = Get(kv, "name", "Player"),
                            IsBot = Get(kv, "bot", "0") == "1",
                            Difficulty = (BotDifficulty)Enum.Parse(typeof(BotDifficulty), Get(kv, "diff", "Normal"))
                        };
                    }
                }
                foreach (var p in players.Values) config.Players.Add(p);
                meta.Config = config;
                string cfgError = config.Validate();
                if (cfgError != null) { error = cfgError; return null; }
                var engine = GameEngine.Replay(config, actions);
                if (hash != null && hash != StateHasher.HashHex(engine.State)) { error = "This save appears to be corrupted (hash mismatch)."; return null; }
                return engine;
            }
            catch (Exception ex)
            {
                error = "This save appears to be corrupted: " + ex.Message;
                return null;
            }
        }

        /// <summary>Buildings + held awards, without hidden Monuments (for the Load screen).</summary>
        public static int PublicVp(GameState s, int seat)
        {
            int vp = 0;
            foreach (var v in s.Board.Vertices)
                if (v.Owner == seat) vp += v.Building == BuildingType.Keep ? 2 : 1;
            if (s.LongestPathHolder == seat) vp += Limits.AwardPoints;
            if (s.StrongestGuardHolder == seat) vp += Limits.AwardPoints;
            return vp;
        }

        static Dictionary<string, string> ParseKv(string line)
        {
            var d = new Dictionary<string, string>();
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq > 0) d[parts[i].Substring(0, eq)] = Dec(parts[i].Substring(eq + 1));
            }
            return d;
        }

        static string Get(Dictionary<string, string> d, string key, string fallback)
        {
            string v;
            return d.TryGetValue(key, out v) ? v : fallback;
        }

        /// <summary>Minimal URL-style encoding for spaces, '=', '%' and newlines.</summary>
        public static string Enc(string s)
        {
            return s.Replace("%", "%25").Replace(" ", "%20").Replace("=", "%3D").Replace("\n", "%0A").Replace("\r", "%0D");
        }

        public static string Dec(string s)
        {
            return s.Replace("%20", " ").Replace("%3D", "=").Replace("%0A", "\n").Replace("%0D", "\r").Replace("%25", "%");
        }
    }
}
