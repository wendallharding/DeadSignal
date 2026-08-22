using System;

namespace DeadSignal
{
    public enum SecurityReinforcement
    {
        None,
        Warden,
        Sapper
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

        public SecurityEscalationDirector(float entryDelay, float safeEntryDistance)
        {
            m_entryDelay = Math.Max(0f, entryDelay);
            m_safeEntryDistance = Math.Max(0f, safeEntryDistance);
        }

        public int EscalationTier => m_observedSalvage;
        public int ReinforcementsRemaining => Math.Max(0, m_observedSalvage - m_nextReinforcement);
        public float EntryCountdown => m_entryCountdown;
        public SecurityReinforcement PendingReinforcement => m_entryCountdown > 0f
            ? m_nextReinforcement % 2 == 0 ? SecurityReinforcement.Warden : SecurityReinforcement.Sapper
            : SecurityReinforcement.None;

        public SecurityReinforcement Tick(
            float seconds,
            bool towerOnline,
            int salvage,
            bool wardenAlive,
            bool sapperAlive,
            float wardenEntryDistance,
            float sapperEntryDistance)
        {
            if (!towerOnline)
            {
                return SecurityReinforcement.None;
            }

            m_observedSalvage = Math.Min(RunModel.SalvageRequired, Math.Max(m_observedSalvage, salvage));
            if (m_nextReinforcement >= m_observedSalvage)
            {
                m_entryCountdown = 0f;
                return SecurityReinforcement.None;
            }

            var reinforcement = m_nextReinforcement % 2 == 0
                ? SecurityReinforcement.Warden
                : SecurityReinforcement.Sapper;
            var roleAlive = reinforcement == SecurityReinforcement.Warden ? wardenAlive : sapperAlive;
            var entryDistance = reinforcement == SecurityReinforcement.Warden ? wardenEntryDistance : sapperEntryDistance;
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
    }
}
