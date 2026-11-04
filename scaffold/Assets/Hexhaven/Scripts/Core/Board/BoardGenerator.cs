// SCAFFOLD — complete. Spec: docs/01-RULES.md R-2, docs/03-CORE-API.md §2.
using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    /// <summary>Fills terrain, numbers and harbor types.</summary>
    public static class BoardGenerator
    {
        static readonly Terrain[] TerrainPool =
        {
            Terrain.Forest, Terrain.Forest, Terrain.Forest, Terrain.Forest,
            Terrain.ClayPit, Terrain.ClayPit, Terrain.ClayPit,
            Terrain.FlaxField, Terrain.FlaxField, Terrain.FlaxField, Terrain.FlaxField,
            Terrain.Farmland, Terrain.Farmland, Terrain.Farmland, Terrain.Farmland,
            Terrain.Mine, Terrain.Mine, Terrain.Mine,
            Terrain.Ruins
        };

        static readonly int[] TokenPool = { 2, 3, 3, 4, 4, 5, 5, 6, 6, 8, 8, 9, 9, 10, 10, 11, 11, 12 };

        static readonly HarborType[] HarborPool =
        {
            HarborType.Generic, HarborType.Generic, HarborType.Generic, HarborType.Generic,
            HarborType.Timber, HarborType.Clay, HarborType.Fiber, HarborType.Grain, HarborType.Iron
        };

        public static Board Generate(BoardMode mode, Pcg32 rng)
        {
            var b = Board.CreateGeometry();
            if (mode == BoardMode.Beginner)
            {
                BeginnerLayout.Apply(b);
                return b;
            }

            var terrains = new List<Terrain>(TerrainPool);
            var tokens = new List<int>(TokenPool);
            if (mode == BoardMode.Random)
            {
                while (true)
                {
                    rng.Shuffle(terrains);
                    ApplyTerrains(b, terrains);
                    bool ok = false;
                    for (int i = 0; i < 10000 && !ok; i++)
                    {
                        rng.Shuffle(tokens);
                        ApplyTokens(b, tokens);
                        ok = SatisfiesC1(b);
                    }
                    if (ok) break;
                }
            }
            else
            {
                while (true)
                {
                    bool terrainOk = false;
                    for (int i = 0; i < 5000 && !terrainOk; i++)
                    {
                        rng.Shuffle(terrains);
                        ApplyTerrains(b, terrains);
                        terrainOk = SatisfiesC3(b) && SatisfiesC5(b);
                    }
                    if (!terrainOk) continue;
                    bool tokenOk = false;
                    for (int i = 0; i < 2000 && !tokenOk; i++)
                    {
                        rng.Shuffle(tokens);
                        ApplyTokens(b, tokens);
                        tokenOk = SatisfiesC1(b) && SatisfiesC2(b) && SatisfiesC4(b);
                    }
                    if (tokenOk) break;
                }
            }

            var harbors = new List<HarborType>(HarborPool);
            rng.Shuffle(harbors);
            b.AssignHarborTypes(harbors);
            return b;
        }

        static void ApplyTerrains(Board b, List<Terrain> terrains)
        {
            for (int i = 0; i < b.Tiles.Count; i++) b.Tiles[i].Terrain = terrains[i];
        }

        static void ApplyTokens(Board b, List<int> tokens)
        {
            int k = 0;
            foreach (var t in b.Tiles) t.Number = t.Terrain == Terrain.Ruins ? 0 : tokens[k++];
        }

        static bool Hot(int n) { return n == 6 || n == 8; }

        /// <summary>C1: no two 6/8 tiles adjacent.</summary>
        public static bool SatisfiesC1(Board b)
        {
            foreach (var t in b.Tiles)
                if (Hot(t.Number))
                    foreach (int n in t.Neighbors) if (Hot(b.Tiles[n].Number)) return false;
            return true;
        }

        /// <summary>C2: no two adjacent tiles share a number.</summary>
        public static bool SatisfiesC2(Board b)
        {
            foreach (var t in b.Tiles)
                if (t.Number != 0)
                    foreach (int n in t.Neighbors) if (b.Tiles[n].Number == t.Number) return false;
            return true;
        }

        /// <summary>C3: no two adjacent tiles share a terrain.</summary>
        public static bool SatisfiesC3(Board b)
        {
            foreach (var t in b.Tiles)
                foreach (int n in t.Neighbors) if (b.Tiles[n].Terrain == t.Terrain) return false;
            return true;
        }

        /// <summary>C4: no corner's tiles exceed 11 pips in total.</summary>
        public static bool SatisfiesC4(Board b)
        {
            foreach (var v in b.Vertices)
            {
                int sum = 0;
                foreach (int t in v.Tiles) sum += HexMath.Pips(b.Tiles[t].Number);
                if (sum > 11) return false;
            }
            return true;
        }

        /// <summary>C5: Ruins is not the centre tile.</summary>
        public static bool SatisfiesC5(Board b)
        {
            var centre = b.TileAt(0, 0);
            return centre.Terrain != Terrain.Ruins;
        }
    }

    /// <summary>The fixed Beginner layout (01-RULES R-2.5).</summary>
    public static class BeginnerLayout
    {
        // index = tile id
        public static readonly Terrain[] Terrains =
        {
            Terrain.FlaxField, Terrain.Farmland, Terrain.Mine, Terrain.Forest, Terrain.Mine, Terrain.Forest, Terrain.ClayPit,
            Terrain.ClayPit, Terrain.FlaxField, Terrain.Farmland, Terrain.Ruins, Terrain.Farmland,
            Terrain.Farmland, Terrain.Mine, Terrain.FlaxField, Terrain.Forest, Terrain.FlaxField, Terrain.Forest, Terrain.ClayPit
        };

        public static readonly int[] Numbers = { 5, 8, 10, 6, 3, 11, 9, 4, 10, 12, 0, 8, 3, 5, 2, 9, 6, 11, 4 };

        public static readonly HarborType[] Harbors =
        {
            HarborType.Generic, HarborType.Timber, HarborType.Generic, HarborType.Clay, HarborType.Generic,
            HarborType.Fiber, HarborType.Grain, HarborType.Generic, HarborType.Iron
        };

        public static void Apply(Board b)
        {
            for (int i = 0; i < b.Tiles.Count; i++)
            {
                b.Tiles[i].Terrain = Terrains[i];
                b.Tiles[i].Number = Numbers[i];
            }
            b.AssignHarborTypes(Harbors);
        }
    }
}
