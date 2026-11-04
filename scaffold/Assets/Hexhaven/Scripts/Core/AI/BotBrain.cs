// SCAFFOLD — BotParams complete; BotBrain/BotMemory/CardTracker SIGNATURES ONLY (implement in M3 per docs/04-AI.md).
// Split into BotBrain.cs, BotParams.cs, BotMemory.cs, CardTracker.cs, BotEvaluator.cs, BotTrade.cs as you implement.
using System;
using System.Collections.Generic;

namespace Hexhaven.Core.AI
{
    /// <summary>Tunables per difficulty (04-AI §1.1).</summary>
    public sealed class BotParams
    {
        public BotDifficulty Difficulty;
        public double Noise;
        public int SetupTopK;
        public bool ProposesDomesticTrades;
        public int MaxProposalsPerTurn;
        public int MaxMaritimePerTurn;
        public double AcceptThreshold;
        /// <summary>Refuse trades with players whose public VP is at least this. int.MaxValue = never refuse.</summary>
        public int RefuseLeaderAtVp;
        public bool UsesCardTracker;
        public bool UsesLookahead;
        public bool KeepsHandUnderLimit;

        public static BotParams For(BotDifficulty d, int victoryTarget)
        {
            switch (d)
            {
                case BotDifficulty.Easy:
                    return new BotParams
                    {
                        Difficulty = d, Noise = 2.0, SetupTopK = 5, ProposesDomesticTrades = false, MaxProposalsPerTurn = 0,
                        MaxMaritimePerTurn = 2, AcceptThreshold = -0.5, RefuseLeaderAtVp = int.MaxValue,
                        UsesCardTracker = false, UsesLookahead = false, KeepsHandUnderLimit = false
                    };
                case BotDifficulty.Hard:
                    return new BotParams
                    {
                        Difficulty = d, Noise = 0.1, SetupTopK = 1, ProposesDomesticTrades = true, MaxProposalsPerTurn = 3,
                        MaxMaritimePerTurn = 6, AcceptThreshold = 0.6, RefuseLeaderAtVp = victoryTarget - 3,
                        UsesCardTracker = true, UsesLookahead = true, KeepsHandUnderLimit = true
                    };
                default:
                    return new BotParams
                    {
                        Difficulty = BotDifficulty.Normal, Noise = 0.4, SetupTopK = 2, ProposesDomesticTrades = true, MaxProposalsPerTurn = 2,
                        MaxMaritimePerTurn = 4, AcceptThreshold = 0.0, RefuseLeaderAtVp = victoryTarget - 2,
                        UsesCardTracker = true, UsesLookahead = false, KeepsHandUnderLimit = true
                    };
            }
        }
    }

    /// <summary>Estimates opponents' hands from observed events (04-AI §4).</summary>
    public sealed class CardTracker
    {
        public CardTracker(int playerCount, int ownerSeat) { throw new NotImplementedException("M3"); }
        public void Observe(GameEvent e, GameState s) { throw new NotImplementedException("M3"); }
        public int KnownAtLeast(int seat, Resource r) { throw new NotImplementedException("M3"); }
        public double Estimate(int seat, Resource r) { throw new NotImplementedException("M3"); }
        /// <summary>Σ over opponents of the owner.</summary>
        public double EstimatedTotal(Resource r) { throw new NotImplementedException("M3"); }
    }

    /// <summary>Per-bot memory across decisions; owned by the driver, one per bot seat.</summary>
    public sealed class BotMemory
    {
        public readonly int Seat;
        public CardTracker Tracker;
        public int ProposalsThisTurn, MaritimeThisTurn, TurnSeen = -1;
        public int LastRejectedTradeHash;

        public BotMemory(int seat, int playerCount) { Seat = seat; Tracker = new CardTracker(playerCount, seat); }

        /// <summary>Feed every event, already redacted for this seat (GameEvent.RedactFor(Seat)).</summary>
        public void Observe(GameEvent e, GameState s) { throw new NotImplementedException("M3"); }
    }

    public static class BotBrain
    {
        /// <summary>Returns a legal action for seat (which must be in engine.AwaitingInput()). Never illegal.</summary>
        public static GameAction Decide(GameEngine engine, int seat, BotMemory memory) { throw new NotImplementedException("M3"); }
    }
}
