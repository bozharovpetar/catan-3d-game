// SCAFFOLD — complete. Spec: docs/03-CORE-API.md §9.
using System.Globalization;
using System.Text;

namespace Hexhaven.Core
{
    /// <summary>FNV-1a 64 over a canonical text dump of the game state.</summary>
    public static class StateHasher
    {
        public static ulong Hash(GameState s)
        {
            string dump = CanonicalDump(s);
            ulong h = 14695981039346656037UL;
            foreach (byte b in Encoding.UTF8.GetBytes(dump))
            {
                h ^= b;
                h = unchecked(h * 1099511628211UL);
            }
            return h;
        }

        public static string HashHex(GameState s) { return Hash(s).ToString("x16", CultureInfo.InvariantCulture); }

        public static string CanonicalDump(GameState s)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("phase=").Append(s.Phase).Append(" current=").Append(s.Current.ToString(ci))
              .Append(" turn=").Append(s.TurnNumber.ToString(ci)).Append(" storm=").Append(s.StormTile.ToString(ci))
              .Append(" dice=").Append(s.Die1.ToString(ci)).Append(',').Append(s.Die2.ToString(ci))
              .Append(" lp=").Append(s.LongestPathHolder.ToString(ci)).Append(" sg=").Append(s.StrongestGuardHolder.ToString(ci))
              .Append(" winner=").Append(s.Winner.ToString(ci)).Append(" start=").Append(s.StartPlayer.ToString(ci)).Append('\n');
            sb.Append("flow=").Append(s.StormReturnPhase).Append(',').Append(s.CharterPlayedThisTurn ? '1' : '0').Append(',')
              .Append(s.RoadCrewRemaining.ToString(ci)).Append(',').Append(s.SetupIndex.ToString(ci)).Append(',')
              .Append(s.LastSetupOutpost.ToString(ci)).Append(',').Append(s.NextTradeId.ToString(ci)).Append(',')
              .Append(s.PlayedOtherCharters.ToString(ci)).Append(" rolloff=");
            for (int i = 0; i < s.RollOffQueue.Count; i++) { if (i > 0) sb.Append(';'); sb.Append(s.RollOffQueue[i].ToString(ci)); }
            sb.Append('\n');
            sb.Append("bank=").Append(s.Bank.ToCodec()).Append('\n');
            sb.Append("deck=");
            for (int i = 0; i < s.Deck.Count; i++) { if (i > 0) sb.Append(','); sb.Append(((int)s.Deck[i]).ToString(ci)); }
            sb.Append('\n');
            sb.Append("rng=").Append(s.Rng.State.ToString(ci)).Append(',').Append(s.Rng.Inc.ToString(ci)).Append('\n');
            foreach (var p in s.Players)
            {
                sb.Append('p').Append(p.Seat.ToString(ci)).Append(" hand=").Append(p.Hand.ToCodec()).Append(" charters=");
                for (int i = 0; i < p.Charters.Count; i++)
                {
                    if (i > 0) sb.Append(';');
                    sb.Append(((int)p.Charters[i].Type).ToString(ci)).Append(':').Append(p.Charters[i].BoughtOnTurn.ToString(ci));
                }
                sb.Append(" wardens=").Append(p.WardensPlayed.ToString(ci))
                  .Append(" left=").Append(p.PathsLeft.ToString(ci)).Append(',').Append(p.OutpostsLeft.ToString(ci)).Append(',').Append(p.KeepsLeft.ToString(ci))
                  .Append(" len=").Append(p.PathLength.ToString(ci)).Append(" pend=").Append(p.PendingDiscard.ToString(ci))
                  .Append(" roll=").Append(p.RollOffTotal.ToString(ci)).Append('\n');
            }
            sb.Append("v=");
            for (int i = 0; i < s.Board.Vertices.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var v = s.Board.Vertices[i];
                if (v.Owner < 0) sb.Append('-'); else sb.Append((v.Owner * 3 + (int)v.Building).ToString(ci));
            }
            sb.Append('\n').Append("e=");
            for (int i = 0; i < s.Board.Edges.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var e = s.Board.Edges[i];
                if (e.Owner < 0) sb.Append('-'); else sb.Append(e.Owner.ToString(ci));
            }
            sb.Append('\n');
            var t = s.ActiveTrade;
            if (t == null) sb.Append("trade=none\n");
            else
            {
                sb.Append("trade=").Append(t.Id.ToString(ci)).Append(',').Append(t.Proposer.ToString(ci)).Append(',')
                  .Append(t.Give.ToCodec()).Append(',').Append(t.Get.ToCodec()).Append(',').Append(t.TargetMask.ToString(ci));
                for (int i = 0; i < t.Responses.Length; i++)
                    sb.Append(',').Append(((int)t.Responses[i]).ToString(ci)).Append(':').Append(t.CounterGive[i].ToCodec()).Append(':').Append(t.CounterGet[i].ToCodec());
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
