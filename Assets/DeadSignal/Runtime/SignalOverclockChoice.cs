namespace DeadSignal
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

    /// <summary>
    /// Owns the two temporary build choices awarded during a run. Input and presentation remain external.
    /// </summary>
    public sealed class SignalOverclockChoice
    {
        public bool IsPending => m_pendingStage != ChoiceStage.None;
        public bool IsPrimaryPending => m_pendingStage == ChoiceStage.Primary;
        public bool IsAuxiliaryPending => m_pendingStage == ChoiceStage.Auxiliary;
        public SignalOverclock Selected { get; private set; }
        public SignalAuxiliaryOverclock SelectedAuxiliary { get; private set; }
        public bool IsEmergencyCapacitorAvailable { get; private set; }
        public bool IsFeedbackShieldCharged { get; private set; }

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
            }

            return restored;
        }

        public bool TryAbsorbThreatDamage()
        {
            if (SelectedAuxiliary != SignalAuxiliaryOverclock.FeedbackShield || !IsFeedbackShieldCharged)
            {
                return false;
            }

            IsFeedbackShieldCharged = false;
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
        private int m_salvageCollected;
    }
}
