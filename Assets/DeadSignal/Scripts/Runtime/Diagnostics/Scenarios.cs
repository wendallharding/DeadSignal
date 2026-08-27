using OOI.AutoUI;

namespace DeadSignal.Diagnostics
{
    public sealed class DebugScenariosPage : DebugMenuPage
    {
        [BeginLayoutGroup("Curated Scenarios", width: 1120, layoutGroupType: LayoutGroupType.Grid, numGridColumns: 3,
            gridCellHeight: 58, backgroundColor: 0x102B35FA)]
        [Button("Fresh Run")] public void FreshRun() => _apply(DebugScenario.FreshRun);
        [Button("Tower Activation")] public void TowerActivation() => _apply(DebugScenario.TowerActivation);
        [Button("First Overclock")] public void FirstOverclock() => _apply(DebugScenario.FirstOverclock);
        [Button("Active Salvage Chain")] public void ActiveSalvageChain() => _apply(DebugScenario.ActiveSalvageChain);
        [Button("Sapper Pulse")] public void SapperPulse() => _apply(DebugScenario.SapperPulse);
        [Button("Interceptor Charge")] public void InterceptorCharge() => _apply(DebugScenario.InterceptorCharge);
        [Button("Suppressor Extraction")] public void SuppressorExtraction() => _apply(DebugScenario.SuppressorExtraction);
        [Button("Critical Recovery")] public void CriticalRecovery() => _apply(DebugScenario.CriticalRecovery);
        [Button("Optional Cache")] public void OptionalCache() => _apply(DebugScenario.OptionalCache);
        [Button("Stable Extraction")] public void StableExtraction() => _apply(DebugScenario.StableExtraction);
        [Button("Overdrive Extraction")] public void OverdriveExtraction() => _apply(DebugScenario.OverdriveExtraction);
        [Button("Victory")] public void Victory() => _apply(DebugScenario.Victory);
        [Button("Failure")] public void Failure() => _apply(DebugScenario.Failure);
        [Button("Eastern Combat — Swarmers On")]
        public void EasternRoomCombat() => _apply(DebugScenario.EasternRoomCombat);
        [Button("Eastern Combat — Swarmers Off")]
        public void EasternRoomCombatNoSwarmers() => _apply(DebugScenario.EasternRoomCombatNoSwarmers);
        [Button("All Effects")] public void AllEffects() => _apply(DebugScenario.AllEffects);
        [EndLayoutGroup] public void EndPage() { }

        private void _apply(DebugScenario scenario) => Run(() => Game.DebugApplyScenario(scenario), $"Scenario loaded: {scenario}");
    }
}
