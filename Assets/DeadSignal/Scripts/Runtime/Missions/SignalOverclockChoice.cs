namespace DeadSignal.Missions
{
    public enum SignalOverclock
    {
        None,
        ChainArc,
        OverdriveThrusters
    }

    public enum SignalAuxiliaryOverclock
    {
        None,
        EmergencyCapacitor,
        FeedbackShield
    }

    public enum SignalWeaponOverclock
    {
        None,
        PiercingPulse,
        ControlledRicochet
    }

    public enum SignalOverclockSynergy
    {
        None,
        ArcOverload,
        ReactiveArc,
        CapacitorSurge,
        ShieldSurge
    }

    /// <summary>
    /// Owns the two temporary build choices awarded during a run. Input and presentation remain external.
    /// </summary>
    public sealed class SignalOverclockChoice
    {
        public bool IsPending => m_pendingStage != ChoiceStage.None || m_weaponPending;
        public bool IsPrimaryPending => m_pendingStage == ChoiceStage.Primary;
        public bool IsAuxiliaryPending => m_pendingStage == ChoiceStage.Auxiliary;
        public bool IsWeaponPending => m_weaponPending;
        public SignalOverclock Selected { get; private set; }
        public SignalAuxiliaryOverclock SelectedAuxiliary { get; private set; }
        public SignalWeaponOverclock SelectedWeapon { get; private set; }
        public bool IsWeaponEvolved { get; private set; }
        public bool IsEmergencyCapacitorAvailable { get; private set; }
        public bool IsFeedbackShieldCharged { get; private set; }
        public bool IsChainArcOverloadReady { get; private set; }
        public bool IsOverdriveSurgeActive => m_overdriveSurgeRemaining > 0f;
        public float OverdriveSurgeSecondsRemaining => m_overdriveSurgeRemaining;
        public SignalOverclockSynergy Synergy => (Selected, SelectedAuxiliary) switch
        {
            (SignalOverclock.ChainArc, SignalAuxiliaryOverclock.EmergencyCapacitor) => SignalOverclockSynergy.ArcOverload,
            (SignalOverclock.ChainArc, SignalAuxiliaryOverclock.FeedbackShield) => SignalOverclockSynergy.ReactiveArc,
            (SignalOverclock.OverdriveThrusters, SignalAuxiliaryOverclock.EmergencyCapacitor) =>
                SignalOverclockSynergy.CapacitorSurge,
            (SignalOverclock.OverdriveThrusters, SignalAuxiliaryOverclock.FeedbackShield) => SignalOverclockSynergy.ShieldSurge,
            _ => SignalOverclockSynergy.None
        };

        public void Tick(float dt)
        {
            m_overdriveSurgeRemaining = System.Math.Max(0f, m_overdriveSurgeRemaining - System.Math.Max(0f, dt));
        }

        public void NotifySalvageCollected(int salvage)
        {
            m_salvageCollected = System.Math.Max(m_salvageCollected, salvage);
            if (m_salvageCollected >= 1 && Selected == SignalOverclock.None)
            {
                m_pendingStage = ChoiceStage.Primary;
            }
            else if (m_salvageCollected >= 2 && SelectedAuxiliary == SignalAuxiliaryOverclock.None)
            {
                m_pendingStage = ChoiceStage.Auxiliary;
            }
        }

        public void NotifyRelayActivated()
        {
            if (SelectedWeapon == SignalWeaponOverclock.None)
            {
                m_weaponPending = true;
            }
        }

        public bool NotifySpineActivated()
        {
            if (SelectedWeapon == SignalWeaponOverclock.None || IsWeaponEvolved)
            {
                return false;
            }

            IsWeaponEvolved = true;
            return true;
        }

        public bool TrySelect(SignalOverclock overclock)
        {
            if (!IsPrimaryPending || overclock == SignalOverclock.None)
            {
                return false;
            }

            Selected = overclock;
            m_pendingStage = m_salvageCollected >= 2 ? ChoiceStage.Auxiliary : ChoiceStage.None;
            return true;
        }

        public bool TrySelect(SignalAuxiliaryOverclock overclock)
        {
            if (!IsAuxiliaryPending || overclock == SignalAuxiliaryOverclock.None)
            {
                return false;
            }

            SelectedAuxiliary = overclock;
            IsEmergencyCapacitorAvailable = overclock == SignalAuxiliaryOverclock.EmergencyCapacitor;
            IsFeedbackShieldCharged = overclock == SignalAuxiliaryOverclock.FeedbackShield;
            m_pendingStage = ChoiceStage.None;
            return true;
        }

        public bool TrySelect(SignalWeaponOverclock overclock)
        {
            if (!IsWeaponPending || overclock == SignalWeaponOverclock.None)
            {
                return false;
            }

            SelectedWeapon = overclock;
            m_weaponPending = false;
            return true;
        }

        public float TryTriggerEmergencyCapacitor(RunModel model, SignalOverclockTuning tuning)
        {
            if (!IsEmergencyCapacitorAvailable || model.Outcome != RunOutcome.Running ||
                model.Signal > tuning.EmergencyCapacitorThreshold)
            {
                return 0f;
            }

            var restored = model.RestoreSignal(tuning.EmergencyCapacitorRestore);
            if (restored > 0f)
            {
                IsEmergencyCapacitorAvailable = false;
                if (Selected == SignalOverclock.ChainArc)
                {
                    IsChainArcOverloadReady = true;
                }
                else if (Selected == SignalOverclock.OverdriveThrusters)
                {
                    m_overdriveSurgeRemaining = tuning.OverdriveSynergySurgeDuration;
                }
            }

            return restored;
        }

        public bool TryAbsorbThreatDamage(SignalOverclockTuning tuning)
        {
            if (SelectedAuxiliary != SignalAuxiliaryOverclock.FeedbackShield || !IsFeedbackShieldCharged)
            {
                return false;
            }

            IsFeedbackShieldCharged = false;
            if (Selected == SignalOverclock.ChainArc)
            {
                IsChainArcOverloadReady = true;
            }
            else if (Selected == SignalOverclock.OverdriveThrusters)
            {
                m_overdriveSurgeRemaining = tuning.OverdriveSynergySurgeDuration;
            }

            return true;
        }

        public bool TryConsumeChainArcOverload()
        {
            if (!IsChainArcOverloadReady)
            {
                return false;
            }

            IsChainArcOverloadReady = false;
            return true;
        }

        public bool NotifyThreatPurged()
        {
            if (SelectedAuxiliary != SignalAuxiliaryOverclock.FeedbackShield || IsFeedbackShieldCharged)
            {
                return false;
            }

            IsFeedbackShieldCharged = true;
            return true;
        }

        private enum ChoiceStage
        {
            None,
            Primary,
            Auxiliary
        }

        private ChoiceStage m_pendingStage;
        private bool m_weaponPending;
        private int m_salvageCollected;
        private float m_overdriveSurgeRemaining;
    }
}
