namespace DeadSignal.Missions
{
    public readonly struct RunDebrief
    {
        public RunDebrief(string grade, string signal, string combat, string exposure, string route, string coaching = "")
        {
            Grade = grade; Signal = signal; Combat = combat; Exposure = exposure; Route = route; Coaching = coaching;
        }

        public string Grade { get; }
        public string Signal { get; }
        public string Combat { get; }
        public string Exposure { get; }
        public string Route { get; }
        public string Coaching { get; }

        public static RunDebrief Evaluate(RunModel model, RunMetrics metrics)
        {
            var signalRatio = model.Signal / RunModel.MaximumSignal;
            var deadZoneRatio = metrics.ElapsedSeconds > 0f ? metrics.DeadZoneSeconds / metrics.ElapsedSeconds : 0f;
            var pressure = metrics.SecurityHits + metrics.SapperPulses;
            var score = (model.Outcome == RunOutcome.Victory ? 2 : 0) + (signalRatio >= 0.4f ? 1 : 0) +
                        (pressure == 0 ? 1 : 0) + (deadZoneRatio <= 0.45f ? 1 : 0);
            return new RunDebrief(
                score >= 5 ? "S" : score == 4 ? "A" : score == 3 ? "B" : score == 2 ? "C" : "D",
                signalRatio >= 0.4f ? "RESERVE SECURE" : signalRatio >= 0.2f ? "RESERVE TIGHT" : "RESERVE CRITICAL",
                pressure == 0 ? "NO SECURITY DRAINS" : pressure == 1 ? "1 SECURITY DRAIN" : $"{pressure} SECURITY DRAINS",
                deadZoneRatio <= 0.3f ? "EXPOSURE CONTROLLED" : deadZoneRatio <= 0.55f ? "EXPOSURE ELEVATED" : "EXPOSURE SEVERE",
                model.ShortcutOpen ? "SHORTCUT ROUTE" : "CONSERVATION ROUTE",
                _coaching(model, metrics));
        }

        private static string _coaching(RunModel model, RunMetrics metrics)
        {
            var traversal = metrics.PassiveSignalSpent + metrics.MovementSignalSpent;
            var breakdown = $"SIGNAL SPENT  TRAVEL {traversal:0}  |  FIRE {metrics.WeaponSignalSpent:0}  |  " +
                            $"RECOVERED {metrics.SignalRecovered + metrics.SalvageSignalRecovered:0}";
            if (model.Salvage < RunModel.SalvageRequired && metrics.DeadZoneSeconds > metrics.ElapsedSeconds * 0.5f)
            {
                return $"{breakdown}\nNEXT RUN: FOLLOW THE AMBER CACHE MARKER AND CROSS CYAN POWER BETWEEN RAIDS.";
            }
            if (metrics.ShotsFired > metrics.ThreatsPurged * 5 + 5)
            {
                return $"{breakdown}\nNEXT RUN: FIRE IN SHORT BURSTS; MARKED THREAT PURGES REFUND SIGNAL.";
            }
            if (metrics.SecurityHits + metrics.SapperPulses > 1)
            {
                return $"{breakdown}\nNEXT RUN: DASH ACROSS RED TELEGRAPHS AND BREAK THE SAPPER LINK FIRST.";
            }
            return $"{breakdown}\nNEXT RUN: CHAIN CACHES QUICKLY, THEN USE THE CYAN EXTRACTION APPROACH.";
        }
    }
}
