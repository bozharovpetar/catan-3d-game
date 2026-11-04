// SCAFFOLD — complete (factories + codec). Spec: docs/03-CORE-API.md §6, docs/08-PERSISTENCE.md §2.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hexhaven.Core
{
    public enum ActionType
    {
        RollForOrder, PlaceSetupOutpost, PlaceSetupPath, RollDice, Discard, MoveStorm, ChooseVictim,
        BuildPath, BuildOutpost, BuildKeep, BuyCharter,
        PlayWarden, PlayRoadCrew, PlayBounty, PlayDecree, PlaceFreePath,
        MaritimeTrade, ProposeTrade, RespondTrade, ConfirmTrade, CancelTrade,
        EndTurn
    }

    /// <summary>One player intent. All state changes happen through GameEngine.Apply(GameAction).</summary>
    public sealed class GameAction
    {
        public ActionType Type;
        public int Player;
        public int Vertex = -1, Edge = -1, Tile = -1, Target = -1, TradeId = -1, TargetMask = 0;
        public Resource Resource, Resource2;
        public ResourceBag Give, Get;
        public TradeResponse Response;

        static GameAction Make(ActionType t, int p) { return new GameAction { Type = t, Player = p }; }

        public static GameAction RollForOrder(int p) { return Make(ActionType.RollForOrder, p); }
        public static GameAction PlaceSetupOutpost(int p, int vertex) { var a = Make(ActionType.PlaceSetupOutpost, p); a.Vertex = vertex; return a; }
        public static GameAction PlaceSetupPath(int p, int edge) { var a = Make(ActionType.PlaceSetupPath, p); a.Edge = edge; return a; }
        public static GameAction RollDice(int p) { return Make(ActionType.RollDice, p); }
        public static GameAction Discard(int p, ResourceBag cards) { var a = Make(ActionType.Discard, p); a.Give = cards; return a; }
        public static GameAction MoveStorm(int p, int tile) { var a = Make(ActionType.MoveStorm, p); a.Tile = tile; return a; }
        public static GameAction ChooseVictim(int p, int target) { var a = Make(ActionType.ChooseVictim, p); a.Target = target; return a; }
        public static GameAction BuildPath(int p, int edge) { var a = Make(ActionType.BuildPath, p); a.Edge = edge; return a; }
        public static GameAction BuildOutpost(int p, int vertex) { var a = Make(ActionType.BuildOutpost, p); a.Vertex = vertex; return a; }
        public static GameAction BuildKeep(int p, int vertex) { var a = Make(ActionType.BuildKeep, p); a.Vertex = vertex; return a; }
        public static GameAction BuyCharter(int p) { return Make(ActionType.BuyCharter, p); }
        public static GameAction PlayWarden(int p) { return Make(ActionType.PlayWarden, p); }
        public static GameAction PlayRoadCrew(int p) { return Make(ActionType.PlayRoadCrew, p); }
        public static GameAction PlayBounty(int p, ResourceBag take) { var a = Make(ActionType.PlayBounty, p); a.Get = take; return a; }
        public static GameAction PlayDecree(int p, Resource r) { var a = Make(ActionType.PlayDecree, p); a.Resource = r; return a; }
        public static GameAction PlaceFreePath(int p, int edge) { var a = Make(ActionType.PlaceFreePath, p); a.Edge = edge; return a; }
        public static GameAction MaritimeTrade(int p, Resource give, Resource get) { var a = Make(ActionType.MaritimeTrade, p); a.Resource = give; a.Resource2 = get; return a; }
        public static GameAction ProposeTrade(int p, ResourceBag give, ResourceBag get, int targetMask) { var a = Make(ActionType.ProposeTrade, p); a.Give = give; a.Get = get; a.TargetMask = targetMask; return a; }

        /// <summary>Counter terms are in the PROPOSER's perspective; ignored unless response == Countered.</summary>
        public static GameAction RespondTrade(int p, int tradeId, TradeResponse response, ResourceBag counterGive, ResourceBag counterGet)
        {
            var a = Make(ActionType.RespondTrade, p);
            a.TradeId = tradeId; a.Response = response;
            if (response == TradeResponse.Countered) { a.Give = counterGive; a.Get = counterGet; }
            return a;
        }

        public static GameAction ConfirmTrade(int p, int tradeId, int partner) { var a = Make(ActionType.ConfirmTrade, p); a.TradeId = tradeId; a.Target = partner; return a; }
        public static GameAction CancelTrade(int p, int tradeId) { var a = Make(ActionType.CancelTrade, p); a.TradeId = tradeId; return a; }
        public static GameAction EndTurn(int p) { return Make(ActionType.EndTurn, p); }

        public GameAction Clone() { return (GameAction)MemberwiseClone(); }

        static bool UsesResources(ActionType t) { return t == ActionType.MaritimeTrade || t == ActionType.PlayDecree; }

        /// <summary>"a &lt;Type&gt; p=.. v=.. e=.. t=.. target=.. trade=.. mask=.. r=.. r2=.. give=.. get=.. resp=.."; defaults omitted.</summary>
        public string ToCodec()
        {
            var sb = new StringBuilder("a ");
            sb.Append(Type.ToString());
            sb.Append(" p=").Append(Player.ToString(CultureInfo.InvariantCulture));
            if (Vertex >= 0) sb.Append(" v=").Append(Vertex.ToString(CultureInfo.InvariantCulture));
            if (Edge >= 0) sb.Append(" e=").Append(Edge.ToString(CultureInfo.InvariantCulture));
            if (Tile >= 0) sb.Append(" t=").Append(Tile.ToString(CultureInfo.InvariantCulture));
            if (Target >= 0) sb.Append(" target=").Append(Target.ToString(CultureInfo.InvariantCulture));
            if (TradeId >= 0) sb.Append(" trade=").Append(TradeId.ToString(CultureInfo.InvariantCulture));
            if (TargetMask != 0) sb.Append(" mask=").Append(TargetMask.ToString(CultureInfo.InvariantCulture));
            if (UsesResources(Type)) sb.Append(" r=").Append(Resource.ToString());
            if (Type == ActionType.MaritimeTrade) sb.Append(" r2=").Append(Resource2.ToString());
            if (Give != ResourceBag.Zero) sb.Append(" give=").Append(Give.ToCodec());
            if (Get != ResourceBag.Zero) sb.Append(" get=").Append(Get.ToCodec());
            if (Type == ActionType.RespondTrade) sb.Append(" resp=").Append(Response.ToString());
            return sb.ToString();
        }

        public static GameAction FromCodec(string line)
        {
            var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts[0] != "a") throw new FormatException("Not an action line: " + line);
            var a = new GameAction { Type = (ActionType)Enum.Parse(typeof(ActionType), parts[1]) };
            for (int i = 2; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0) continue;
                string key = parts[i].Substring(0, eq), val = parts[i].Substring(eq + 1);
                switch (key)
                {
                    case "p": a.Player = ParseInt(val); break;
                    case "v": a.Vertex = ParseInt(val); break;
                    case "e": a.Edge = ParseInt(val); break;
                    case "t": a.Tile = ParseInt(val); break;
                    case "target": a.Target = ParseInt(val); break;
                    case "trade": a.TradeId = ParseInt(val); break;
                    case "mask": a.TargetMask = ParseInt(val); break;
                    case "r": a.Resource = (Resource)Enum.Parse(typeof(Resource), val); break;
                    case "r2": a.Resource2 = (Resource)Enum.Parse(typeof(Resource), val); break;
                    case "give": a.Give = ResourceBag.FromCodec(val); break;
                    case "get": a.Get = ResourceBag.FromCodec(val); break;
                    case "resp": a.Response = (TradeResponse)Enum.Parse(typeof(TradeResponse), val); break;
                    default: break; // forward compatibility: ignore unknown keys
                }
            }
            return a;
        }

        static int ParseInt(string s) { return int.Parse(s, CultureInfo.InvariantCulture); }

        public override string ToString() { return ToCodec(); }
    }
}
