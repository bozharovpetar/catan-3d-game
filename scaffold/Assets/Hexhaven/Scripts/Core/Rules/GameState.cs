// SCAFFOLD — complete data model. Spec: docs/03-CORE-API.md §3–§4.
using System;
using System.Collections.Generic;

namespace Hexhaven.Core
{
    public sealed class PlayerConfig
    {
        public string Name;
        public bool IsBot;
        public BotDifficulty Difficulty = BotDifficulty.Normal;

        public PlayerConfig Clone() { return new PlayerConfig { Name = Name, IsBot = IsBot, Difficulty = Difficulty }; }
    }

    public sealed class GameConfig
    {
        public int Seed;
        public BoardMode BoardMode = BoardMode.BalancedRandom;
        public int VictoryTarget = 10;
        public bool FriendlyStorm;
        public bool ClassicPhases;
        public List<PlayerConfig> Players = new List<PlayerConfig>();

        static readonly string[] DefaultNames = { "You", "Bryn", "Corra", "Dain" };

        /// <summary>Seat 0 human "You", others Normal bots.</summary>
        public static GameConfig Default(int seed, int playerCount = 4)
        {
            var c = new GameConfig { Seed = seed };
            for (int i = 0; i < playerCount; i++)
                c.Players.Add(new PlayerConfig { Name = DefaultNames[i], IsBot = i != 0, Difficulty = BotDifficulty.Normal });
            return c;
        }

        /// <summary>null if valid, otherwise a message.</summary>
        public string Validate()
        {
            if (Players == null || Players.Count < 3 || Players.Count > 4) return "Player count must be 3 or 4.";
            if (VictoryTarget < 8 || VictoryTarget > 15) return "Victory target must be 8..15.";
            foreach (var p in Players)
                if (p == null || string.IsNullOrEmpty(p.Name) || p.Name.Length > 16) return "Player names must be 1..16 characters.";
            return null;
        }

        public GameConfig Clone()
        {
            var c = new GameConfig { Seed = Seed, BoardMode = BoardMode, VictoryTarget = VictoryTarget, FriendlyStorm = FriendlyStorm, ClassicPhases = ClassicPhases };
            foreach (var p in Players) c.Players.Add(p.Clone());
            return c;
        }
    }

    public sealed class CharterCard
    {
        public CharterType Type;
        public int BoughtOnTurn;
        public CharterCard Clone() { return new CharterCard { Type = Type, BoughtOnTurn = BoughtOnTurn }; }
    }

    public sealed class PlayerStats
    {
        public ResourceBag Produced, Spent, TradedAway, TradedFor;
        public ResourceBag LostToDiscard, LostToPlunder, GainedFromPlunder, GainedFromCharters;
        public int TradesCompleted, PlundersMade, TimesPlundered, ChartersBought;
        public PlayerStats Clone() { return (PlayerStats)MemberwiseClone(); }
    }

    public sealed class PlayerState
    {
        public int Seat;
        public string Name;
        public bool IsBot;
        public BotDifficulty Difficulty;
        public ResourceBag Hand;
        public readonly List<CharterCard> Charters = new List<CharterCard>();
        public int WardensPlayed;
        public int PathsLeft = Limits.Paths, OutpostsLeft = Limits.Outposts, KeepsLeft = Limits.Keeps;
        public int PathLength;
        public int PendingDiscard;
        public int RollOffTotal;
        public PlayerStats Stats = new PlayerStats();

        public PlayerState Clone()
        {
            var copy = new PlayerState
            {
                Seat = Seat, Name = Name, IsBot = IsBot, Difficulty = Difficulty, Hand = Hand, WardensPlayed = WardensPlayed,
                PathsLeft = PathsLeft, OutpostsLeft = OutpostsLeft, KeepsLeft = KeepsLeft, PathLength = PathLength,
                PendingDiscard = PendingDiscard, RollOffTotal = RollOffTotal, Stats = Stats.Clone()
            };
            foreach (var c in Charters) copy.Charters.Add(c.Clone());
            return copy;
        }
    }

    public sealed class TradeOffer
    {
        public int Id;
        public int Proposer;
        public ResourceBag Give;     // proposer gives
        public ResourceBag Get;      // proposer receives
        public int TargetMask;
        public readonly TradeResponse[] Responses;
        public readonly ResourceBag[] CounterGive;   // proposer perspective
        public readonly ResourceBag[] CounterGet;

        public TradeOffer(int playerCount)
        {
            Responses = new TradeResponse[playerCount];
            CounterGive = new ResourceBag[playerCount];
            CounterGet = new ResourceBag[playerCount];
        }

        public bool IsTarget(int seat) { return (TargetMask & (1 << seat)) != 0; }

        public TradeOffer Clone()
        {
            var o = new TradeOffer(Responses.Length) { Id = Id, Proposer = Proposer, Give = Give, Get = Get, TargetMask = TargetMask };
            Array.Copy(Responses, o.Responses, Responses.Length);
            Array.Copy(CounterGive, o.CounterGive, CounterGive.Length);
            Array.Copy(CounterGet, o.CounterGet, CounterGet.Length);
            return o;
        }
    }

    public sealed class GameState
    {
        public GameConfig Config;
        public Board Board;
        public PlayerState[] Players;
        public ResourceBag Bank;
        public List<CharterType> Deck = new List<CharterType>();   // last element = top
        public Pcg32 Rng;

        public Phase Phase = Phase.RollForOrder;
        public int Current;
        public int StartPlayer = -1;
        public int TurnNumber;
        public int Die1, Die2;
        public int StormTile;
        public int LongestPathHolder = -1, StrongestGuardHolder = -1;
        public int Winner = -1;

        public List<int> RollOffQueue = new List<int>();
        public int[] SetupOrder;
        public int SetupIndex;
        public int LastSetupOutpost = -1;

        public Phase StormReturnPhase = Phase.Main;
        public bool CharterPlayedThisTurn;
        public int PlayedOtherCharters;
        public int RoadCrewRemaining;
        public Phase RoadCrewReturnPhase = Phase.Main;
        public bool TradingClosedThisTurn;
        public TradeOffer ActiveTrade;
        public int NextTradeId = 1;

        public readonly List<GameAction> ActionLog = new List<GameAction>();
        public int EventSeq;
        public readonly int[] DiceHistogram = new int[13];

        public PlayerState CurrentPlayer { get { return Players[Current]; } }
        public int PlayerCount { get { return Players.Length; } }
    }
}
