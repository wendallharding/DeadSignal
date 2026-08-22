namespace DeadSignal
{
    public readonly struct RunDebrief
    {
        public RunDebrief(string grade, string signal, string combat, string exposure, string route)
        {
            Grade = grade; Signal = signal; Combat = combat; Exposure = exposure; Route = route;
        }

        public string Grade { get; }
        public string Signal { get; }
        public string Combat { get; }
        public string Exposure { get; }
        public string Route { get; }

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
                model.ShortcutOpen ? "SHORTCUT ROUTE" : "CONSERVATION ROUTE");
        }
    }
}
