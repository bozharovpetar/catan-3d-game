using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    /// <summary>
    /// Heuristic AI. Each call performs at most one action so the view can animate bot turns step by step.
    /// </summary>
    public static class Bot
    {
        /// <summary>Performs one action for the current (bot) player. Returns false when the bot has finished its turn.</summary>
        public static bool Step(Game g)
        {
            int p = g.Current;
            switch (g.Phase)
            {
                case Phase.Setup:
                    if (g.SetupNeedsPath) return g.PlacePath(ChooseSetupPath(g, p));
                    return g.PlaceOutpost(ChooseSetupOutpost(g, p));
                case Phase.Roll:
                    return g.RollDice();
                case Phase.MoveStorm:
                    return g.MoveStorm(ChooseStormTile(g, p), -1);
                case Phase.Main:
                    if (MainAction(g, p)) return true;
                    g.EndTurn();
                    return false;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ evaluation

        public static double SiteValue(Game g, int v, int p)
        {
            var vx = g.Board.Vertices[v];
            double score = 0;
            var seen = new bool[Res.Count];
            int own = 0;
            foreach (int t in vx.Tiles)
            {
                var tile = g.Board.Tiles[t];
                int r = tile.Resource;
                if (r < 0) continue;
                double pips = Res.Pips(tile.Number);
                if (tile.Id == g.StormTile) pips *= 0.5;
                score += pips;
                if (!seen[r]) { seen[r] = true; score += 1.0; }
            }
            // value resources the player currently lacks access to
            if (p >= 0)
            {
                foreach (var ov in g.Board.Vertices)
                {
                    if (ov.Owner != p) continue;
                    own++;
                    foreach (int t in ov.Tiles)
                    {
                        int r = g.Board.Tiles[t].Resource;
                        if (r >= 0 && seen[r]) score -= 0.3;
                    }
                }
            }
            if (vx.Harbor >= 0) score += own > 0 ? 1.0 : 0.5;
            return score;
        }

        static int ChooseSetupOutpost(Game g, int p)
        {
            int best = -1; double bestScore = double.MinValue;
            foreach (int v in g.LegalOutposts(p))
            {
                double s = SiteValue(g, v, p) + g.Rng.NextDouble() * 0.2;
                if (s > bestScore) { bestScore = s; best = v; }
            }
            return best;
        }

        static int ChooseSetupPath(Game g, int p)
        {
            int best = -1; double bestScore = double.MinValue;
            foreach (int e in g.LegalPaths(p))
            {
                int far = g.Board.Edges[e].Other(g.SetupLastVertex);
                double s = FrontierValue(g, far, p) + g.Rng.NextDouble() * 0.1;
                if (s > bestScore) { bestScore = s; best = e; }
            }
            return best;
        }

        /// <summary>How promising it is to extend a path to vertex v.</summary>
        static double FrontierValue(Game g, int v, int p)
        {
            var vx = g.Board.Vertices[v];
            if (vx.Owner >= 0 && vx.Owner != p) return -5;
            double best = g.SiteIsOpen(v) ? SiteValue(g, v, p) : 0;
            foreach (int n in vx.Neighbors)
                if (g.SiteIsOpen(n)) best = Math.Max(best, SiteValue(g, n, p) * 0.7);
            return best;
        }

        static int ChooseStormTile(Game g, int p)
        {
            int best = -1; double bestScore = double.MinValue;
            int leader = -1, leaderVp = -1;
            foreach (var pl in g.Players)
            {
                if (pl.Id == p) continue;
                int vp = g.VictoryPoints(pl.Id);
                if (vp > leaderVp) { leaderVp = vp; leader = pl.Id; }
            }
            foreach (var t in g.Board.Tiles)
            {
                if (!g.CanMoveStorm(t.Id)) continue;
                double s = 0;
                foreach (int v in t.Corners)
                {
                    var vx = g.Board.Vertices[v];
                    if (vx.Owner < 0) continue;
                    double w = vx.Building == Building.Keep ? 2 : 1;
                    if (vx.Owner == p) s -= 10 * w;
                    else s += w * (vx.Owner == leader ? 1.5 : 1.0) + (g.Players[vx.Owner].Hand.Total > 0 ? 0.5 : 0);
                }
                s *= Math.Max(1, Res.Pips(t.Number));
                s += g.Rng.NextDouble() * 0.1;
                if (s > bestScore) { bestScore = s; best = t.Id; }
            }
            return best;
        }

        // ------------------------------------------------------------------ main phase

        static bool MainAction(Game g, int p)
        {
            var pl = g.Players[p];

            var keeps = g.LegalUpgrades(p);
            if (keeps.Count > 0) return g.Upgrade(BestBy(g, keeps, p));

            var outposts = g.LegalOutposts(p);
            if (outposts.Count > 0) return g.PlaceOutpost(BestBy(g, outposts, p));

            bool hasSites = g.LegalOutposts(p, true).Count > 0;
            var paths = g.LegalPaths(p);
            if (paths.Count > 0 && pl.OutpostsLeft > 0 && (!hasSites || pl.Hand.Total >= 7 || g.LongestPathHolder != p && pl.LongestPath >= 4))
            {
                int bestPath = -1; double bestScore = double.MinValue;
                foreach (int e in paths)
                {
                    var ed = g.Board.Edges[e];
                    double s = Math.Max(FrontierValue(g, ed.A, p), FrontierValue(g, ed.B, p)) + g.Rng.NextDouble() * 0.1;
                    if (s > bestScore) { bestScore = s; bestPath = e; }
                }
                if (bestScore > 0 || pl.Hand.Total >= 7) return g.PlacePath(bestPath);
            }

            return TradeTowardGoal(g, p, hasSites);
        }

        static int BestBy(Game g, List<int> vertices, int p)
        {
            int best = vertices[0]; double bestScore = double.MinValue;
            foreach (int v in vertices)
            {
                double s = SiteValue(g, v, p);
                if (s > bestScore) { bestScore = s; best = v; }
            }
            return best;
        }

        static bool TradeTowardGoal(Game g, int p, bool hasSites)
        {
            var pl = g.Players[p];
            if (g.TradesThisTurn >= 3) return false;
            var goals = new List<ResourceBag>();
            bool canKeep = pl.KeepsLeft > 0 && g.LegalUpgrades(p, true).Count > 0;
            bool canOutpost = pl.OutpostsLeft > 0 && hasSites;
            bool canPath = pl.PathsLeft > 0 && g.LegalPaths(p, true).Count > 0;
            if (canKeep && pl.Hand[(int)Resource.Iron] + pl.Hand[(int)Resource.Grain] >= 3) goals.Add(Costs.Keep);
            if (canOutpost) goals.Add(Costs.Outpost);
            if (canKeep) goals.Add(Costs.Keep);
            if (canPath) goals.Add(Costs.Path);

            foreach (var goal in goals)
            {
                int missing = 0, need = -1;
                for (int r = 0; r < Res.Count; r++)
                {
                    int lack = goal[r] - pl.Hand[r];
                    if (lack > 0) { missing += lack; if (need < 0) need = r; }
                }
                if (need < 0) continue;

                // pay with resources not needed for this goal; only trade if the goal becomes reachable
                int spare = 0, give = -1, giveSurplus = 0;
                for (int r = 0; r < Res.Count; r++)
                {
                    int rate = g.TradeRate(p, r);
                    int surplus = pl.Hand[r] - goal[r];
                    if (surplus >= rate)
                    {
                        spare += surplus / rate;
                        if (surplus > giveSurplus) { giveSurplus = surplus; give = r; }
                    }
                }
                if (give >= 0 && spare >= missing) return g.BankTrade(give, need);
            }
            return false;
        }
    }
}
