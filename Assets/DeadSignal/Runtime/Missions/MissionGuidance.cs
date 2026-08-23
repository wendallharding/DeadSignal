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
            if (!model.TowerOnline)
            {
                return new MissionGuidanceState(1, "RESTORE NETWORK", "ACTIVATE THE CENTRAL SIGNAL TOWER",
                    $"SIGNAL -{RunModel.TowerCost:0}  //  REFILL +{RunModel.TowerRefill:0}");
            }

            if (!model.CanExtract)
            {
                var remaining = RunModel.SalvageRequired - model.Salvage;
                var advisory = sapperAlive && sapperLatched
                    ? $"INTERRUPT: SAPPER DRAIN IN {sapperPulseCooldown:0.0}s"
                    : $"{remaining} SALVAGE REMAINING  //  CHOOSE ANY {remaining} CACHE{(remaining == 1 ? string.Empty : "S")}";
                return new MissionGuidanceState(2, "RECOVER SALVAGE", "RAID THE AMBER CACHES", advisory);
            }

            var extractionAdvisory = sapperAlive && sapperLatched
                ? $"SAPPER DRAIN IN {sapperPulseCooldown:0.0}s  //  EXTRACTION READY"
                : "CARGO SECURED  //  EXTRACTION READY";
            return new MissionGuidanceState(3, "EXTRACT", "RETURN TO THE CYAN DOCK", extractionAdvisory);
        }
    }
}
