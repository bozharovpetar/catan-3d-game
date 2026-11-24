// SCAFFOLD — complete. Spec: docs/03-CORE-API.md §6.1 (Errors, ApplyResult) and §7 (events).
using System.Collections.Generic;

namespace Hexhaven.Core
{
    public enum EventType
    {
        GameStarted, RollOffRoll, RollOffTie, StartPlayerChosen, PhaseChanged, TurnStarted, TurnEnded,
        DiceRolled, Produced, ProductionBlocked, ShortageNoPayout, ShortagePartial, StartingResources,
        DiscardRequired, Discarded, StormMoved, Plundered, NoPlunder,
        Built, CharterBought, CharterPlayed, BountyTaken, DecreeResolved, RoadCrewStarted,
        MaritimeTraded, TradeProposed, TradeResponded, TradeCompleted, TradeCancelled,
        AwardChanged, MonumentsRevealed, GameWon
    }

    /// <summary>Something that happened. See the field-usage table in 03-CORE-API §7.</summary>
    public sealed class GameEvent
    {
        public int Seq;
        public EventType Type;
        public int Player = -1, Other = -1, Tile = -1, Vertex = -1, Edge = -1;
        public int Die1, Die2, Value, Mask;
        public ResourceBag Bag, Bag2;
        public Resource Resource;
        public CharterType Charter;
        public AwardType Award;
        public PieceType Piece;
        public Phase Phase;
        public TradeResponse Response;
        public bool Free;
        public int[] Amounts;
        public int VisibleMask = -1;
        public bool Redacted;

        public bool IsVisibleTo(int seat) { return VisibleMask == -1 || (seat >= 0 && (VisibleMask & (1 << seat)) != 0); }

        /// <summary>Copy with private fields blanked for viewers not entitled to them (seat −1 = spectator).</summary>
        public GameEvent RedactFor(int seat)
        {
            var e = (GameEvent)MemberwiseClone();
            if (Amounts != null) e.Amounts = (int[])Amounts.Clone();
            if (!IsVisibleTo(seat))
            {
                e.Redacted = true;
                e.Resource = Resource.Timber;     // meaningless once Redacted
                e.Charter = CharterType.Warden;   // meaningless once Redacted
            }
            return e;
        }
    }

    public static class Errors
    {
        public const string GameOver = "GameOver", NotYourTurn = "NotYourTurn", WrongPhase = "WrongPhase",
            BadId = "BadId", Occupied = "Occupied", DistanceRule = "DistanceRule", NotConnected = "NotConnected",
            NoPiecesLeft = "NoPiecesLeft", CannotAfford = "CannotAfford", NotYourOutpost = "NotYourOutpost",
            SameTile = "SameTile", StormProtected = "StormProtected", InvalidVictim = "InvalidVictim",
            InvalidDiscard = "InvalidDiscard", DeckEmpty = "DeckEmpty", CharterAlreadyPlayed = "CharterAlreadyPlayed",
            NoPlayableCharter = "NoPlayableCharter", NoLegalPlacement = "NoLegalPlacement", BankEmpty = "BankEmpty",
            InvalidBounty = "InvalidBounty", InvalidTrade = "InvalidTrade", TradePending = "TradePending",
            NoSuchTrade = "NoSuchTrade", PartnerNotReady = "PartnerNotReady", PartnerCannotPay = "PartnerCannotPay",
            TradingClosed = "TradingClosed";
    }

    public sealed class ApplyResult
    {
        public bool Ok;
        public string Error;
        public List<GameEvent> Events = new List<GameEvent>();

        public static ApplyResult Fail(string error) { return new ApplyResult { Ok = false, Error = error }; }
        public static ApplyResult Success(List<GameEvent> events, string warning = null) { return new ApplyResult { Ok = true, Error = warning, Events = events }; }
    }
}
