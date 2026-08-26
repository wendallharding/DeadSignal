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
            var urgent = sapperAlive && sapperLatched
                ? $"INTERRUPT: SAPPER DRAIN IN {sapperPulseCooldown:0.0}s"
                : string.Empty;
            switch (model.CurrentMissionStage)
            {
                case MissionStage.CentralTower:
                    return new MissionGuidanceState(1, "RESTORE CENTRAL", "ACTIVATE THE CENTRAL SIGNAL TOWER",
                        $"SIGNAL -{RunModel.TowerCost:0}  //  REFILL +{RunModel.TowerRefill:0}");
                case MissionStage.CentralPayload:
                    return new MissionGuidanceState(2, "CENTRAL PAYLOAD", "CHOOSE ONE LOCAL AMBER ROUTE",
                        string.IsNullOrEmpty(urgent) ? "ANNEX OR COOLANT CACHE  //  ONE REQUIRED" : urgent);
                case MissionStage.RelayTower:
                    return new MissionGuidanceState(3, "EXTEND THE NETWORK", "RESTORE THE RELAY FOUNDRY TOWER",
                        string.IsNullOrEmpty(urgent) ? $"SIGNAL -{RunModel.RelayTowerCost:0}  //  WEAPON CALIBRATION" : urgent);
                case MissionStage.RelayPayload:
                    return new MissionGuidanceState(4, "RELAY PAYLOAD", "CHOOSE FOUNDRY OR COOLING GANTRY",
                        string.IsNullOrEmpty(urgent) ? "INNER COVER OR EXCHANGER LOOP  //  ONE REQUIRED" : urgent);
                case MissionStage.SpineTower:
                    return new MissionGuidanceState(5, "POWER THE SPINE", "RESTORE THE CAPACITOR SPINE TOWER",
                        string.IsNullOrEmpty(urgent) ? $"SIGNAL -{RunModel.SpineTowerCost:0}  //  EVOLVE WEAPON" : urgent);
                case MissionStage.SpinePayload:
                    return new MissionGuidanceState(6, "FINAL PAYLOAD", "SECURE ONE SPINE PAYLOAD",
                        string.IsNullOrEmpty(urgent) ? "GALLERY OR FURNACE-SIDE ROUTE  //  ONE REQUIRED" : urgent);
            }

            var extractionAdvisory = sapperAlive && sapperLatched
                ? $"SAPPER DRAIN IN {sapperPulseCooldown:0.0}s  //  EXTRACTION READY"
                : "THREE TOWERS + THREE PAYLOADS SECURED  //  QUENCH CACHE OPTIONAL";
            return new MissionGuidanceState(7, "EXTRACT OR GREED", "RETURN TO THE CYAN DOCK", extractionAdvisory);
        }
    }
}
