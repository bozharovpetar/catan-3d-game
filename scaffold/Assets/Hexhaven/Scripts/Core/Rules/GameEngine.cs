// SCAFFOLD — SIGNATURES ONLY. Implement in milestone M2 per docs/01-RULES.md and docs/03-CORE-API.md §5–§8.
// Split into partial files (GameEngine.Setup.cs, .Turn.cs, .Build.cs, .Charters.cs, .Trade.cs, .Awards.cs, .Queries.cs) as you implement.
using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    /// <summary>The rules engine. The only thing allowed to mutate GameState.</summary>
    public sealed partial class GameEngine
    {
        public GameState State { get; private set; }

        GameEngine(GameState state) { State = state; }

        // ------------------------------------------------------------------ lifecycle

        /// <summary>Validates config (throws ArgumentException), generates the board, shuffles the deck,
        /// Phase = RollForOrder with RollOffQueue = all seats; emits GameStarted (see TakeInitialEvents).</summary>
        public static GameEngine NewGame(GameConfig config) { throw new NotImplementedException("M2"); }

        /// <summary>Events produced by NewGame (GameStarted, PhaseChanged). Returns them once, then an empty list.</summary>
        public List<GameEvent> TakeInitialEvents() { throw new NotImplementedException("M2"); }

        /// <summary>Rebuilds a game from its config and action log. Throws InvalidOperationException if any action fails.</summary>
        public static GameEngine Replay(GameConfig config, IList<GameAction> actions) { throw new NotImplementedException("M2"); }

        /// <summary>Deep copy for AI lookahead.</summary>
        public GameEngine Clone() { throw new NotImplementedException("M2"); }

        // ------------------------------------------------------------------ actions

        /// <summary>Seats that must act now, in the order a single-threaded driver should ask them (03 §5).</summary>
        public List<int> AwaitingInput() { throw new NotImplementedException("M2"); }

        /// <summary>null if legal, else an Errors.* code. Pure.</summary>
        public string Validate(GameAction a) { throw new NotImplementedException("M2"); }

        /// <summary>Validates, mutates, logs the action, returns events. Never throws for illegal actions.</summary>
        public ApplyResult Apply(GameAction a) { throw new NotImplementedException("M2"); }

        // ------------------------------------------------------------------ queries (pure)

        public int VictoryPoints(int seat, bool includeHidden) { throw new NotImplementedException("M2"); }
        public int MonumentCount(int seat) { throw new NotImplementedException("M2"); }
        public int MaritimeRate(int seat, Resource give) { throw new NotImplementedException("M2"); }
        public bool HasHarbor(int seat, HarborType type) { throw new NotImplementedException("M2"); }
        public bool CanAfford(int seat, ResourceBag cost) { throw new NotImplementedException("M2"); }
        public bool IsOpenSite(int vertex) { throw new NotImplementedException("M2"); }
        public List<int> LegalSetupOutposts() { throw new NotImplementedException("M2"); }
        public List<int> LegalSetupPaths() { throw new NotImplementedException("M2"); }
        public List<int> LegalPaths(int seat, bool ignoreCost) { throw new NotImplementedException("M2"); }
        public List<int> LegalOutposts(int seat, bool ignoreCost) { throw new NotImplementedException("M2"); }
        public List<int> LegalKeeps(int seat, bool ignoreCost) { throw new NotImplementedException("M2"); }
        public List<int> LegalStormTiles(int seat) { throw new NotImplementedException("M2"); }
        public List<int> PlunderCandidates(int seat, int tile) { throw new NotImplementedException("M2"); }
        public bool CanPlayCharter(int seat, CharterType type) { throw new NotImplementedException("M2"); }
        public bool CanBuyCharter(int seat) { throw new NotImplementedException("M2"); }
        public int ComputePathLength(int seat) { throw new NotImplementedException("M2"); }
        public int TotalProductionPips(int seat) { throw new NotImplementedException("M2"); }

        // ------------------------------------------------------------------ test/debug hooks (09-TESTING §1)
        // Compiled in tests, the sim harness (define HEXHAVEN_TESTS) and Development builds (debug overlay).
#if HEXHAVEN_TESTS || UNITY_INCLUDE_TESTS || UNITY_EDITOR || DEVELOPMENT_BUILD
        public void Debug_TransferFromBank(int seat, ResourceBag bag) { throw new NotImplementedException("M2"); }
        public void Debug_SetPhase(Phase phase, int current) { throw new NotImplementedException("M2"); }
        /// <summary>Forces the dice of the next RollDice / RollForOrder.</summary>
        public void Debug_SetDice(int d1, int d2) { throw new NotImplementedException("M2"); }
        public void Debug_PlaceBuilding(int seat, int vertex, BuildingType type) { throw new NotImplementedException("M2"); }
        public void Debug_PlacePath(int seat, int edge) { throw new NotImplementedException("M2"); }
        public void Debug_GiveCharter(int seat, CharterType type, int boughtOnTurn) { throw new NotImplementedException("M2"); }
        public void Debug_SetStorm(int tile) { throw new NotImplementedException("M2"); }
#endif
    }

    /// <summary>Consistency checks run after every action in tests, the sim and Editor builds (03 §10).</summary>
    public static class GameInvariants
    {
        public static List<string> Check(GameState s) { throw new NotImplementedException("M2"); }
    }
}
