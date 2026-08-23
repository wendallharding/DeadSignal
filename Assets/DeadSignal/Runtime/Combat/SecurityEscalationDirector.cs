using System;
using DeadSignal.Missions;

namespace DeadSignal.Combat
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
        private readonly float m_deadZoneTraceRecoveryRate;
        private readonly SecurityReinforcement m_bothPurgedPreference;

        private int m_observedSalvage;
        private int m_nextSalvageReinforcement;
        private float m_entryCountdown;
        private float m_deadZoneTraceProgress;
        private bool m_deadZoneTraceCompleted;
        private bool m_deadZoneTraceCooling;
        private bool m_extractionPressure;
        private bool m_extractionResponseDeployed;
        private SecurityReinforcement m_firstCoreResponse;
        private SecurityReinforcement m_firstSalvageResponse;
        private SecurityReinforcement m_pendingReinforcement;

        public SecurityEscalationDirector(
            float entryDelay,
            float safeEntryDistance,
            bool preferSapperWhenBothPurged = false,
            float deadZoneTraceDuration = 8f,
            float deadZoneTraceRecoveryRate = 0.5f)
        {
            m_entryDelay = Math.Max(0f, entryDelay);
            m_safeEntryDistance = Math.Max(0f, safeEntryDistance);
            m_deadZoneTraceDuration = Math.Max(0.1f, deadZoneTraceDuration);
            m_deadZoneTraceRecoveryRate = Math.Max(0f, deadZoneTraceRecoveryRate);
            m_bothPurgedPreference = preferSapperWhenBothPurged
                ? SecurityReinforcement.Sapper
                : SecurityReinforcement.Warden;
        }

        public int EscalationTier => m_observedSalvage;
        public int ReinforcementsRemaining =>
            Math.Max(0, _salvageReinforcementBudget() - m_nextSalvageReinforcement) +
            (m_extractionPressure && !m_extractionResponseDeployed ? 1 : 0);
        public float EntryCountdown => m_entryCountdown;
        public bool IsDeadZoneTraceActive => !m_deadZoneTraceCompleted && m_observedSalvage == 0 && m_deadZoneTraceProgress > 0f;
        public bool IsDeadZoneTraceCompleted => m_deadZoneTraceCompleted;
        public bool IsDeadZoneTraceCooling => IsDeadZoneTraceActive && m_deadZoneTraceCooling;
        public float DeadZoneTraceSecondsRemaining => IsDeadZoneTraceActive
            ? Math.Max(0f, m_deadZoneTraceDuration - m_deadZoneTraceProgress)
            : 0f;
        public SecurityReinforcement PendingReinforcement => m_pendingReinforcement;

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
                m_deadZoneTraceCooling = false;
                return SecurityReinforcement.None;
            }

            m_observedSalvage = Math.Min(RunModel.SalvageRequired, Math.Max(m_observedSalvage, salvage));
            _updateDeadZoneTrace(seconds, playerPowered);
            m_extractionPressure |= extractionPressure && m_observedSalvage >= RunModel.SalvageRequired;
            _chooseFirstSalvageResponse(wardenAlive, sapperAlive);
            _chooseCoreResponse(wardenAlive, sapperAlive);
            var reinforcement = _getNextReinforcement();
            if (reinforcement == SecurityReinforcement.None)
            {
                _clearPendingEntry();
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
                _clearPendingEntry();
                return SecurityReinforcement.None;
            }

            if (m_pendingReinforcement != reinforcement)
            {
                m_pendingReinforcement = reinforcement;
                m_entryCountdown = m_entryDelay;
                if (m_entryCountdown > 0f)
                {
                    return SecurityReinforcement.None;
                }
            }

            m_entryCountdown = Math.Max(0f, m_entryCountdown - Math.Max(0f, seconds));
            if (m_entryCountdown > 0f)
            {
                return SecurityReinforcement.None;
            }

            if (reinforcement == SecurityReinforcement.Suppressor)
            {
                m_extractionResponseDeployed = true;
            }
            else
            {
                m_nextSalvageReinforcement++;
            }

            _clearPendingEntry();
            return reinforcement;
        }

        private void _updateDeadZoneTrace(float seconds, bool playerPowered)
        {
            if (m_deadZoneTraceCompleted || m_observedSalvage > 0 || m_nextSalvageReinforcement > 0)
            {
                m_deadZoneTraceCooling = false;
                return;
            }

            if (playerPowered)
            {
                m_deadZoneTraceProgress = Math.Max(
                    0f,
                    m_deadZoneTraceProgress - Math.Max(0f, seconds) * m_deadZoneTraceRecoveryRate);
                m_deadZoneTraceCooling = m_deadZoneTraceProgress > 0f;
                return;
            }

            m_deadZoneTraceCooling = false;
            m_deadZoneTraceProgress = Math.Min(
                m_deadZoneTraceDuration,
                m_deadZoneTraceProgress + Math.Max(0f, seconds));
            m_deadZoneTraceCompleted = m_deadZoneTraceProgress >= m_deadZoneTraceDuration;
        }

        private int _salvageReinforcementBudget()
        {
            return Math.Max(m_observedSalvage, m_deadZoneTraceCompleted ? 1 : 0);
        }

        private void _chooseCoreResponse(bool wardenAlive, bool sapperAlive)
        {
            if (m_firstSalvageResponse != SecurityReinforcement.Interceptor ||
                m_nextSalvageReinforcement != 1 ||
                m_firstCoreResponse != SecurityReinforcement.None)
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

        private void _chooseFirstSalvageResponse(bool wardenAlive, bool sapperAlive)
        {
            if (m_firstSalvageResponse != SecurityReinforcement.None || _salvageReinforcementBudget() == 0)
            {
                return;
            }

            if (m_deadZoneTraceCompleted && m_observedSalvage == 0)
            {
                m_firstSalvageResponse = SecurityReinforcement.Interceptor;
                return;
            }

            if (!wardenAlive && sapperAlive)
            {
                m_firstSalvageResponse = SecurityReinforcement.Warden;
                m_firstCoreResponse = SecurityReinforcement.Warden;
            }
            else if (wardenAlive && !sapperAlive)
            {
                m_firstSalvageResponse = SecurityReinforcement.Sapper;
                m_firstCoreResponse = SecurityReinforcement.Sapper;
            }
            else if (!wardenAlive && !sapperAlive)
            {
                m_firstSalvageResponse = m_bothPurgedPreference;
                m_firstCoreResponse = m_bothPurgedPreference;
            }
            else
            {
                m_firstSalvageResponse = SecurityReinforcement.Interceptor;
            }
        }

        private SecurityReinforcement _getNextReinforcement()
        {
            if (m_extractionPressure && !m_extractionResponseDeployed)
            {
                return SecurityReinforcement.Suppressor;
            }

            return m_nextSalvageReinforcement < _salvageReinforcementBudget()
                ? _getSalvageReinforcement(m_nextSalvageReinforcement)
                : SecurityReinforcement.None;
        }

        private SecurityReinforcement _getSalvageReinforcement(int index)
        {
            if (m_firstSalvageResponse != SecurityReinforcement.Interceptor)
            {
                return index switch
                {
                    0 => m_firstSalvageResponse,
                    1 => SecurityReinforcement.Interceptor,
                    2 => m_firstCoreResponse == SecurityReinforcement.Warden
                        ? SecurityReinforcement.Sapper
                        : SecurityReinforcement.Warden,
                    _ => SecurityReinforcement.Suppressor
                };
            }

            return index switch
            {
                0 => m_firstSalvageResponse,
                1 => m_firstCoreResponse,
                2 => m_firstCoreResponse == SecurityReinforcement.Warden
                    ? SecurityReinforcement.Sapper
                    : SecurityReinforcement.Warden,
                _ => SecurityReinforcement.Suppressor
            };
        }

        private void _clearPendingEntry()
        {
            m_entryCountdown = 0f;
            m_pendingReinforcement = SecurityReinforcement.None;
        }
    }
}
