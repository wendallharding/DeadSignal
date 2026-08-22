using System;

namespace DeadSignal
{
    /// <summary>
    /// Deterministic countdown for the final extraction pursuit. Runtime input and presentation remain external.
    /// </summary>
    public sealed class ExtractionUplink
    {
        private readonly float m_duration;

        public ExtractionUplink(float duration)
        {
            m_duration = Math.Max(0.1f, duration);
        }

        public bool IsActive { get; private set; }
        public bool IsComplete { get; private set; }
        public float SecondsRemaining { get; private set; }

        public bool Begin()
        {
            if (IsActive || IsComplete)
            {
                return false;
            }

            IsActive = true;
            SecondsRemaining = m_duration;
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

        public float Accelerate(float seconds)
        {
            if (!IsActive || seconds <= 0f)
            {
                return 0f;
            }

            var previousRemaining = SecondsRemaining;
            _advance(seconds);
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
