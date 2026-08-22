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
    /// Converts route exposure, salvage progress, and the player's purge order into a small, bounded reinforcement queue.
    /// </summary>
    public sealed class SecurityEscalationDirector
    {
        private readonly float m_entryDelay;
        private readonly float m_safeEntryDistance;
        private readonly float m_deadZoneTraceDuration;
        private readonly SecurityReinforcement m_bothPurgedPreference;

        private int m_observedSalvage;
        private int m_nextReinforcement;
        private float m_entryCountdown;
        private float m_deadZoneTraceProgress;
        private bool m_deadZoneTraceCompleted;
        private bool m_extractionPressure;
        private SecurityReinforcement m_firstCoreResponse;

        public SecurityEscalationDirector(
            float entryDelay,
            float safeEntryDistance,
            bool preferSapperWhenBothPurged = false,
            float deadZoneTraceDuration = 8f)
        {
            m_entryDelay = Math.Max(0f, entryDelay);
            m_safeEntryDistance = Math.Max(0f, safeEntryDistance);
            m_deadZoneTraceDuration = Math.Max(0.1f, deadZoneTraceDuration);
            m_bothPurgedPreference = preferSapperWhenBothPurged
                ? SecurityReinforcement.Sapper
                : SecurityReinforcement.Warden;
        }

        public int EscalationTier => m_observedSalvage;
        public int ReinforcementsRemaining => Math.Max(0, _reinforcementBudget() - m_nextReinforcement);
        public float EntryCountdown => m_entryCountdown;
        public bool IsDeadZoneTraceActive => !m_deadZoneTraceCompleted && m_observedSalvage == 0 && m_deadZoneTraceProgress > 0f;
        public bool IsDeadZoneTraceCompleted => m_deadZoneTraceCompleted;
        public float DeadZoneTraceSecondsRemaining => IsDeadZoneTraceActive
            ? Math.Max(0f, m_deadZoneTraceDuration - m_deadZoneTraceProgress)
            : 0f;
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
            return Tick(
                seconds,
                towerOnline,
                true,
                salvage,
                extractionPressure,
                interceptorAlive,
                wardenAlive,
                sapperAlive,
                suppressorAlive,
                interceptorEntryDistance,
                wardenEntryDistance,
                sapperEntryDistance,
                suppressorEntryDistance);
        }

        public SecurityReinforcement Tick(
            float seconds,
            bool towerOnline,
            bool playerPowered,
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
                m_deadZoneTraceProgress = 0f;
                return SecurityReinforcement.None;
            }

            m_observedSalvage = Math.Min(RunModel.SalvageRequired, Math.Max(m_observedSalvage, salvage));
            _updateDeadZoneTrace(seconds, playerPowered);
            m_extractionPressure |= extractionPressure && m_observedSalvage >= RunModel.SalvageRequired;
            var reinforcementBudget = _reinforcementBudget();
            if (m_nextReinforcement >= reinforcementBudget)
            {
                m_entryCountdown = 0f;
                return SecurityReinforcement.None;
            }

            _chooseCoreResponse(wardenAlive, sapperAlive);
            var reinforcement = _getReinforcement(m_nextReinforcement);
            if (reinforcement == SecurityReinforcement.None)
            {
                m_entryCountdown = 0f;
                return SecurityReinforcement.None;
            }

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

        private void _updateDeadZoneTrace(float seconds, bool playerPowered)
        {
            if (m_deadZoneTraceCompleted || m_observedSalvage > 0 || m_nextReinforcement > 0)
            {
                return;
            }

            if (playerPowered)
            {
                m_deadZoneTraceProgress = 0f;
                return;
            }

            m_deadZoneTraceProgress = Math.Min(
                m_deadZoneTraceDuration,
                m_deadZoneTraceProgress + Math.Max(0f, seconds));
            m_deadZoneTraceCompleted = m_deadZoneTraceProgress >= m_deadZoneTraceDuration;
        }

        private int _reinforcementBudget()
        {
            var openingBudget = Math.Max(m_observedSalvage, m_deadZoneTraceCompleted ? 1 : 0);
            return openingBudget + (m_extractionPressure ? 1 : 0);
        }

        private void _chooseCoreResponse(bool wardenAlive, bool sapperAlive)
        {
            if (m_nextReinforcement != 1 || m_firstCoreResponse != SecurityReinforcement.None)
            {
                return;
            }

            if (!wardenAlive && sapperAlive)
            {
                m_firstCoreResponse = SecurityReinforcement.Warden;
            }
            else if (wardenAlive && !sapperAlive)
            {
                m_firstCoreResponse = SecurityReinforcement.Sapper;
            }
            else if (!wardenAlive && !sapperAlive)
            {
                m_firstCoreResponse = m_bothPurgedPreference;
            }
        }

        private SecurityReinforcement _getReinforcement(int index)
        {
            return index switch
            {
                0 => SecurityReinforcement.Interceptor,
                1 => m_firstCoreResponse,
                2 => m_firstCoreResponse == SecurityReinforcement.Warden
                    ? SecurityReinforcement.Sapper
                    : SecurityReinforcement.Warden,
                _ => SecurityReinforcement.Suppressor
            };
        }
    }
}
