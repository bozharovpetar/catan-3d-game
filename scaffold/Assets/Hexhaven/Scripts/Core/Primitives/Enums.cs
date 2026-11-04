// SCAFFOLD — complete. Spec: docs/03-CORE-API.md §1.
using System;

namespace Hexhaven.Core
{
    public enum Resource { Timber = 0, Clay = 1, Fiber = 2, Grain = 3, Iron = 4 }
    public enum Terrain { Forest = 0, ClayPit = 1, FlaxField = 2, Farmland = 3, Mine = 4, Ruins = 5 }
    public enum BuildingType { None = 0, Outpost = 1, Keep = 2 }
    public enum PieceType { Path = 0, Outpost = 1, Keep = 2 }
    public enum HarborType { None = 0, Generic = 1, Timber = 2, Clay = 3, Fiber = 4, Grain = 5, Iron = 6 }
    public enum CharterType { Warden = 0, Monument = 1, RoadCrew = 2, Bounty = 3, Decree = 4 }
    public enum Phase { RollForOrder, SetupOutpost, SetupPath, TurnStart, Discard, MoveStorm, ChooseVictim, Main, RoadCrew, GameOver }
    public enum BoardMode { BalancedRandom = 0, Random = 1, Beginner = 2 }
    public enum BotDifficulty { Easy = 0, Normal = 1, Hard = 2 }
    public enum TradeResponse { Pending = 0, Accepted = 1, Declined = 2, Countered = 3 }
    public enum AwardType { LongestPath = 0, StrongestGuard = 1 }

    /// <summary>Static information about resources.</summary>
    public static class ResourceInfo
    {
        public const int Count = 5;
        public static readonly Resource[] All = { Resource.Timber, Resource.Clay, Resource.Fiber, Resource.Grain, Resource.Iron };
        static readonly string[] names = { "Timber", "Clay", "Fiber", "Grain", "Iron" };
        static readonly char[] letters = { 'T', 'C', 'F', 'G', 'I' };
        public static string Name(Resource r) { return names[(int)r]; }
        public static char Letter(Resource r) { return letters[(int)r]; }
    }

    /// <summary>Static information about terrains.</summary>
    public static class TerrainInfo
    {
        /// <summary>Resource produced by a terrain; false for Ruins.</summary>
        public static bool TryGetResource(Terrain t, out Resource r)
        {
            switch (t)
            {
                case Terrain.Forest: r = Resource.Timber; return true;
                case Terrain.ClayPit: r = Resource.Clay; return true;
                case Terrain.FlaxField: r = Resource.Fiber; return true;
                case Terrain.Farmland: r = Resource.Grain; return true;
                case Terrain.Mine: r = Resource.Iron; return true;
                default: r = Resource.Timber; return false;
            }
        }

        public static string Name(Terrain t)
        {
            switch (t)
            {
                case Terrain.Forest: return "Forest";
                case Terrain.ClayPit: return "Clay Pit";
                case Terrain.FlaxField: return "Flax Field";
                case Terrain.Farmland: return "Farmland";
                case Terrain.Mine: return "Mine";
                default: return "Ruins";
            }
        }
    }

    /// <summary>Static information about harbors.</summary>
    public static class HarborInfo
    {
        public static bool IsSpecific(HarborType h) { return h >= HarborType.Timber && h <= HarborType.Iron; }

        public static Resource ToResource(HarborType h)
        {
            if (!IsSpecific(h)) throw new ArgumentException("Not a specific harbor: " + h);
            return (Resource)((int)h - (int)HarborType.Timber);
        }

        public static HarborType FromResource(Resource r) { return (HarborType)((int)r + (int)HarborType.Timber); }

        public static string Label(HarborType h)
        {
            if (h == HarborType.Generic) return "3:1";
            if (IsSpecific(h)) return "2:1 " + ResourceInfo.Name(ToResource(h));
            return "";
        }
    }

    /// <summary>Build costs (R-6.1).</summary>
    public static class Costs
    {
        public static readonly ResourceBag Path = ResourceBag.Of(1, 1, 0, 0, 0);
        public static readonly ResourceBag Outpost = ResourceBag.Of(1, 1, 1, 1, 0);
        public static readonly ResourceBag Keep = ResourceBag.Of(0, 0, 0, 2, 3);
        public static readonly ResourceBag Charter = ResourceBag.Of(0, 0, 1, 1, 1);
    }

    /// <summary>Component counts and thresholds (R-1, R-7, R-11).</summary>
    public static class Limits
    {
        public const int BankPerResource = 19, Paths = 15, Outposts = 5, Keeps = 4;
        public const int HandLimit = 7, LongestPathMin = 5, StrongestGuardMin = 3, AwardPoints = 2;
        public const int Wardens = 14, Monuments = 5, RoadCrews = 2, Bounties = 2, Decrees = 2;
        public const int DeckSize = Wardens + Monuments + RoadCrews + Bounties + Decrees; // 25
    }
}
