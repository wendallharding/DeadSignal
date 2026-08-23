using System;

namespace DeadSignal.Salvage
{
    /// <summary>Deterministic momentum state for consecutive salvage recoveries.</summary>
    public sealed class SalvageChain
    {
        public int Count { get; private set; }
        public int BestCount { get; private set; }
        public float SecondsRemaining { get; private set; }

        public void Advance(float seconds)
        {
            if (seconds <= 0f || SecondsRemaining <= 0f)
            {
                return;
            }

            SecondsRemaining = Math.Max(0f, SecondsRemaining - seconds);
            if (SecondsRemaining <= 0f)
            {
                Count = 0;
            }
        }

        public float RecordCollection(float windowSeconds, float secondReward, float thirdReward)
        {
            Count = SecondsRemaining > 0f ? Count + 1 : 1;
            BestCount = Math.Max(BestCount, Count);
            SecondsRemaining = Math.Max(0f, windowSeconds);

            if (Count >= 3)
            {
                return Math.Max(0f, thirdReward);
            }

            return Count == 2 ? Math.Max(0f, secondReward) : 0f;
        }
    }
}
