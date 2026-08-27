namespace DeadSignal.Missions
{
    public readonly struct MissionGuidanceState
    {
        public MissionGuidanceState(int phase, string title, string action, string advisory)
        {
            Phase = phase;
            Title = title;
            Action = action;
            Advisory = advisory;
        }

        public int Phase { get; }
        public string Title { get; }
        public string Action { get; }
        public string Advisory { get; }
    }

    /// <summary>Translates the deterministic run state into compact, actionable mission guidance.</summary>
    public static class MissionGuidance
    {
        public static MissionGuidanceState Evaluate(RunModel model, bool sapperAlive, bool sapperLatched, float sapperPulseCooldown)
        {
            var objective = model.CurrentObjective;
            var guidance = objective.Guidance;
            var urgent = sapperAlive && sapperLatched
                ? $"INTERRUPT: SAPPER DRAIN IN {sapperPulseCooldown:0.0}s"
                : string.Empty;
            if (string.IsNullOrEmpty(urgent) || objective.Id == MissionObjectiveId.CentralTower)
            {
                return guidance;
            }

            var advisory = objective.Id == MissionObjectiveId.Extraction
                ? $"SAPPER DRAIN IN {sapperPulseCooldown:0.0}s  //  EXTRACTION READY"
                : urgent;
            return new MissionGuidanceState(guidance.Phase, guidance.Title, guidance.Action, advisory);
        }
    }
}
