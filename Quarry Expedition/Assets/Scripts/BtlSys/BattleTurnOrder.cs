using System.Collections.Generic;

namespace BtlSys
{
    public sealed class BattleTurnOrder : IComparer<BattleAction>
    {
        public int Compare(BattleAction left, BattleAction right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int priority = right.HasPriority.CompareTo(left.HasPriority);
            if (priority != 0) return priority;

            int speed = right.Speed.CompareTo(left.Speed);
            if (speed != 0) return speed;

            int total = right.StatTotal.CompareTo(left.StatTotal);
            if (total != 0) return total;

            // The design has no final tie rule yet; keep the queued order stable.
            return left.Sequence.CompareTo(right.Sequence);
        }
    }
}
