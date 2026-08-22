namespace DeadSignal
{
    public enum SignalOverclock
    {
        None,
        ChainArc,
        OverdriveThrusters
    }

    /// <summary>
    /// Owns the single temporary build choice awarded during a run. Input and presentation remain external.
    /// </summary>
    public sealed class SignalOverclockChoice
    {
        public bool IsPending { get; private set; }
        public SignalOverclock Selected { get; private set; }

        public void NotifySalvageCollected(int salvage)
        {
            if (salvage == 1 && Selected == SignalOverclock.None)
            {
                IsPending = true;
            }
        }

        public bool TrySelect(SignalOverclock overclock)
        {
            if (!IsPending || overclock == SignalOverclock.None)
            {
                return false;
            }

            Selected = overclock;
            IsPending = false;
            return true;
        }
    }
}
