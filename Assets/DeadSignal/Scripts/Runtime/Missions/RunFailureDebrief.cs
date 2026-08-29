namespace DeadSignal.Missions
{
    public readonly struct RunFailureDebrief
    {
        public RunFailureDebrief(string cause, string progress, string summary, string coaching)
        {
            Cause = cause;
            Progress = progress;
            Summary = summary;
            Coaching = coaching;
        }

        public string Cause { get; }
        public string Progress { get; }
        public string Summary { get; }
        public string Coaching { get; }

        public static RunFailureDebrief Evaluate(RunModel model, RunMetrics metrics)
        {
            var objective = model.CurrentObjective;
            var totalSeconds = (int)metrics.ElapsedSeconds;
            var securityPressure = metrics.SecurityHits + metrics.SapperPulses + metrics.SwarmerContacts;
            var traversalSpend = metrics.PassiveSignalSpent + metrics.MovementSignalSpent;
            var recovered = metrics.SignalRecovered + metrics.SalvageSignalRecovered;
            var deadZoneRatio = metrics.ElapsedSeconds > 0f ? metrics.DeadZoneSeconds / metrics.ElapsedSeconds : 0f;

            var coaching = deadZoneRatio >= 0.5f
                ? "NEXT: CROSS CYAN POWER BETWEEN DEAD-ZONE COMMITMENTS."
                : securityPressure >= 2
                    ? "NEXT: BREAK RED TELEGRAPHS AND PURGE THE SAPPER LINK FIRST."
                    : metrics.WeaponSignalSpent > traversalSpend
                        ? "NEXT: BASIC FIRE IS FREE; SAVE SIGNAL-POWERED ATTACKS FOR OPENINGS."
                        : "NEXT: WITHDRAW EARLIER AND PROTECT A RESERVE FOR THE RETURN.";

            return new RunFailureDebrief(
                "SIGNAL DEPLETED — EMERGENCY RECOVERY EXPIRED",
                $"FAILED AT  {objective.Guidance.Title}   //   {objective.OwningRoom.ToUpperInvariant()}",
                $"RUN {totalSeconds / 60:00}:{totalSeconds % 60:00}  |  DEAD {metrics.DeadZoneSeconds:0}s  |  " +
                $"TRAVEL {traversalSpend:0}  |  " +
                $"CONTACTS {securityPressure}  |  PURGES {metrics.ThreatsPurged}  |  RECOVERED {recovered:0}",
                coaching);
        }
    }
}
