using System;

namespace DeadSignal.Missions
{
    public enum CoolantSealThreadingPhase
    {
        AwaitingFirstBaffle,
        AwaitingSecondBaffle,
        SealAvailable,
        Releasing,
        Complete
    }

    /// <summary>
    /// Deterministic ordered-waypoint state for threading the Coolant Reclamation baffles.
    /// </summary>
    public sealed class CoolantSealThreading
    {
        public CoolantSealThreadingPhase Phase { get; private set; }

        public CoolantSealThreading(
            float firstBaffleX,
            float firstBaffleY,
            float secondBaffleX,
            float secondBaffleY,
            float waypointRadius)
        {
            if (waypointRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(waypointRadius));
            }

            m_firstBaffleX = firstBaffleX;
            m_firstBaffleY = firstBaffleY;
            m_secondBaffleX = secondBaffleX;
            m_secondBaffleY = secondBaffleY;
            m_waypointRadiusSquared = waypointRadius * waypointRadius;
            Reset();
        }

        public void Observe(float x, float y, bool objectiveAvailable)
        {
            if (!objectiveAvailable)
            {
                return;
            }

            if (Phase == CoolantSealThreadingPhase.AwaitingFirstBaffle &&
                _distanceSquared(x, y, m_firstBaffleX, m_firstBaffleY) <= m_waypointRadiusSquared)
            {
                Phase = CoolantSealThreadingPhase.AwaitingSecondBaffle;
                return;
            }

            if (Phase == CoolantSealThreadingPhase.AwaitingSecondBaffle &&
                _distanceSquared(x, y, m_secondBaffleX, m_secondBaffleY) <= m_waypointRadiusSquared)
            {
                Phase = CoolantSealThreadingPhase.SealAvailable;
            }
        }

        public bool TryReleaseSeal(bool objectiveAvailable)
        {
            if (!objectiveAvailable || Phase != CoolantSealThreadingPhase.SealAvailable)
            {
                return false;
            }

            Phase = CoolantSealThreadingPhase.Releasing;
            return true;
        }

        public bool CanCompleteRelease(bool atReleaseThreshold, bool objectiveAvailable)
        {
            return atReleaseThreshold && objectiveAvailable && Phase == CoolantSealThreadingPhase.Releasing;
        }

        public void CompleteRelease()
        {
            if (Phase != CoolantSealThreadingPhase.Releasing)
            {
                throw new InvalidOperationException("The coolant seal must be released before the line can stabilize.");
            }

            Phase = CoolantSealThreadingPhase.Complete;
        }

        public void Reset()
        {
            Phase = CoolantSealThreadingPhase.AwaitingFirstBaffle;
        }

        private static float _distanceSquared(float x, float y, float targetX, float targetY)
        {
            var deltaX = x - targetX;
            var deltaY = y - targetY;
            return deltaX * deltaX + deltaY * deltaY;
        }

        private readonly float m_firstBaffleX;
        private readonly float m_firstBaffleY;
        private readonly float m_secondBaffleX;
        private readonly float m_secondBaffleY;
        private readonly float m_waypointRadiusSquared;
    }
}
