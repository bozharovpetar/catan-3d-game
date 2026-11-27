using System;

namespace Hexhaven.Core
{
    public enum Resource { Timber = 0, Clay = 1, Fiber = 2, Grain = 3, Iron = 4 }

    public enum HexTerrain { Forest, ClayPit, FlaxField, Farmland, Mine, Ruins }

    public enum Building { None, Outpost, Keep }

    public static class Res
    {
        public const int Count = 5;
        public static readonly string[] Names = { "Timber", "Clay", "Fiber", "Grain", "Iron" };

        /// <summary>Resource produced by a terrain, or -1 for none.</summary>
        public static int FromTerrain(HexTerrain t)
        {
            switch (t)
            {
                case HexTerrain.Forest: return (int)Resource.Timber;
                case HexTerrain.ClayPit: return (int)Resource.Clay;
                case HexTerrain.FlaxField: return (int)Resource.Fiber;
                case HexTerrain.Farmland: return (int)Resource.Grain;
                case HexTerrain.Mine: return (int)Resource.Iron;
                default: return -1;
            }
        }

        /// <summary>Number of dice combinations (out of 36) that roll this number.</summary>
        public static int Pips(int number)
        {
            if (number < 2 || number > 12 || number == 7) return 0;
            return 6 - Math.Abs(7 - number);
        }
    }

    [Serializable]
    public class ResourceBag
    {
        public readonly int[] Counts = new int[Res.Count];

        public int this[int i] { get { return Counts[i]; } set { Counts[i] = value; } }

        public int Total
        {
            get { int t = 0; for (int i = 0; i < Res.Count; i++) t += Counts[i]; return t; }
        }

        public static ResourceBag Of(int timber, int clay, int fiber, int grain, int iron)
        {
            var b = new ResourceBag();
            b.Counts[0] = timber; b.Counts[1] = clay; b.Counts[2] = fiber; b.Counts[3] = grain; b.Counts[4] = iron;
            return b;
        }

        public bool Contains(ResourceBag cost)
        {
            for (int i = 0; i < Res.Count; i++) if (Counts[i] < cost.Counts[i]) return false;
            return true;
        }

        public void Add(ResourceBag other)
        {
            for (int i = 0; i < Res.Count; i++) Counts[i] += other.Counts[i];
        }

        public void Subtract(ResourceBag other)
        {
            for (int i = 0; i < Res.Count; i++) Counts[i] -= other.Counts[i];
        }

        /// <summary>Removes one random card and returns its resource index, or -1 if empty.</summary>
        public int TakeRandom(Random rng)
        {
            int total = Total;
            if (total == 0) return -1;
            int pick = rng.Next(total);
            for (int i = 0; i < Res.Count; i++)
            {
                if (pick < Counts[i]) { Counts[i]--; return i; }
                pick -= Counts[i];
            }
            return -1;
        }

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < Res.Count; i++) if (Counts[i] > 0) parts.Add(Counts[i] + " " + Res.Names[i]);
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts.ToArray());
        }
    }

    public static class Costs
    {
        public static readonly ResourceBag Path = ResourceBag.Of(1, 1, 0, 0, 0);
        public static readonly ResourceBag Outpost = ResourceBag.Of(1, 1, 1, 1, 0);
        public static readonly ResourceBag Keep = ResourceBag.Of(0, 0, 0, 2, 3);
    }
}
