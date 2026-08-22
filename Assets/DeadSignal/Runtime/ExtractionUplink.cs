using System;

namespace DeadSignal
{
    public enum ExtractionUplinkMode
    {
        None,
        Stable,
        Overdrive
    }

    /// <summary>
    /// Deterministic countdown for the final extraction pursuit. Runtime input and presentation remain external.
    /// </summary>
    public sealed class ExtractionUplink
    {
        private readonly float m_stableDuration;
        private readonly float m_overdriveDuration;
        private readonly float m_overdriveSignalCost;
        private readonly float m_stablePurgeAcceleration;
        private readonly float m_overdrivePurgeAcceleration;

        public ExtractionUplink(
            float stableDuration,
            float overdriveDuration,
            float overdriveSignalCost,
            float stablePurgeAcceleration,
            float overdrivePurgeAcceleration)
        {
            m_stableDuration = Math.Max(0.1f, stableDuration);
            m_overdriveDuration = Math.Max(0.1f, Math.Min(overdriveDuration, m_stableDuration));
            m_overdriveSignalCost = Math.Max(0f, overdriveSignalCost);
            m_stablePurgeAcceleration = Math.Max(0f, stablePurgeAcceleration);
            m_overdrivePurgeAcceleration = Math.Max(0f, Math.Min(overdrivePurgeAcceleration, m_stablePurgeAcceleration));
        }

        public bool IsActive { get; private set; }
        public bool IsComplete { get; private set; }
        public float SecondsRemaining { get; private set; }
        public ExtractionUplinkMode Mode { get; private set; }
        public float StableDuration => m_stableDuration;
        public float OverdriveDuration => m_overdriveDuration;
        public float OverdriveSignalCost => m_overdriveSignalCost;
        public float StablePurgeAcceleration => m_stablePurgeAcceleration;
        public float OverdrivePurgeAcceleration => m_overdrivePurgeAcceleration;
        public float CurrentPurgeAcceleration => Mode == ExtractionUplinkMode.Overdrive
            ? m_overdrivePurgeAcceleration
            : m_stablePurgeAcceleration;

        public bool CanAffordOverdrive(float availableSignal)
        {
            return availableSignal > m_overdriveSignalCost;
        }

        public bool Begin(ExtractionUplinkMode mode)
        {
            if (IsActive || IsComplete || mode == ExtractionUplinkMode.None)
            {
                return false;
            }

            Mode = mode;
            IsActive = true;
            SecondsRemaining = mode == ExtractionUplinkMode.Overdrive ? m_overdriveDuration : m_stableDuration;
            return true;
        }

        public bool Tick(float seconds)
        {
            if (!IsActive || seconds <= 0f)
            {
                return false;
            }

            _advance(seconds);
            return IsComplete;
        }

        public float RewardPurge()
        {
            if (!IsActive || CurrentPurgeAcceleration <= 0f)
            {
                return 0f;
            }

            var previousRemaining = SecondsRemaining;
            _advance(CurrentPurgeAcceleration);
            return previousRemaining - SecondsRemaining;
        }

        private void _advance(float seconds)
        {
            SecondsRemaining = Math.Max(0f, SecondsRemaining - seconds);
            if (SecondsRemaining > 0f)
            {
                return;
            }

            IsActive = false;
            IsComplete = true;
        }
    }
}
