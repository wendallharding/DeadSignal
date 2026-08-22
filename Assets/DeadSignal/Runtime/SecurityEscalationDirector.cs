using System;

namespace DeadSignal
{
    public enum SecurityReinforcement
    {
        None,
        Interceptor,
        Warden,
        Sapper,
        Suppressor
    }

    /// <summary>
    /// Deterministically converts salvage progress into a small, bounded reinforcement queue.
    /// </summary>
    public sealed class SecurityEscalationDirector
    {
        private readonly float m_entryDelay;
        private readonly float m_safeEntryDistance;

        private int m_observedSalvage;
        private int m_nextReinforcement;
        private float m_entryCountdown;
        private bool m_extractionPressure;

        public SecurityEscalationDirector(float entryDelay, float safeEntryDistance)
        {
            m_entryDelay = Math.Max(0f, entryDelay);
            m_safeEntryDistance = Math.Max(0f, safeEntryDistance);
        }

        public int EscalationTier => m_observedSalvage;
        public int ReinforcementsRemaining => Math.Max(0, m_observedSalvage + (m_extractionPressure ? 1 : 0) - m_nextReinforcement);
        public float EntryCountdown => m_entryCountdown;
        public SecurityReinforcement PendingReinforcement => m_entryCountdown > 0f
            ? _getReinforcement(m_nextReinforcement)
            : SecurityReinforcement.None;

        public SecurityReinforcement Tick(
            float seconds,
            bool towerOnline,
            int salvage,
            bool extractionPressure,
            bool interceptorAlive,
            bool wardenAlive,
            bool sapperAlive,
            bool suppressorAlive,
            float interceptorEntryDistance,
            float wardenEntryDistance,
            float sapperEntryDistance,
            float suppressorEntryDistance)
        {
            if (!towerOnline)
            {
                return SecurityReinforcement.None;
            }

            m_observedSalvage = Math.Min(RunModel.SalvageRequired, Math.Max(m_observedSalvage, salvage));
            m_extractionPressure |= extractionPressure && m_observedSalvage >= RunModel.SalvageRequired;
            var reinforcementBudget = m_observedSalvage + (m_extractionPressure ? 1 : 0);
            if (m_nextReinforcement >= reinforcementBudget)
            {
                m_entryCountdown = 0f;
                return SecurityReinforcement.None;
            }

            var reinforcement = _getReinforcement(m_nextReinforcement);
            var roleAlive = reinforcement switch
            {
                SecurityReinforcement.Interceptor => interceptorAlive,
                SecurityReinforcement.Warden => wardenAlive,
                SecurityReinforcement.Sapper => sapperAlive,
                _ => suppressorAlive
            };
            var entryDistance = reinforcement switch
            {
                SecurityReinforcement.Interceptor => interceptorEntryDistance,
                SecurityReinforcement.Warden => wardenEntryDistance,
                SecurityReinforcement.Sapper => sapperEntryDistance,
                _ => suppressorEntryDistance
            };
            if (roleAlive || entryDistance < m_safeEntryDistance)
            {
                m_entryCountdown = 0f;
                return SecurityReinforcement.None;
            }

            if (m_entryCountdown <= 0f)
            {
                m_entryCountdown = m_entryDelay;
                return SecurityReinforcement.None;
            }

            m_entryCountdown = Math.Max(0f, m_entryCountdown - Math.Max(0f, seconds));
            if (m_entryCountdown > 0f)
            {
                return SecurityReinforcement.None;
            }

            m_nextReinforcement++;
            return reinforcement;
        }

        private static SecurityReinforcement _getReinforcement(int index)
        {
            return index switch
            {
                0 => SecurityReinforcement.Interceptor,
                1 => SecurityReinforcement.Warden,
                2 => SecurityReinforcement.Sapper,
                _ => SecurityReinforcement.Suppressor
            };
        }
    }
}
