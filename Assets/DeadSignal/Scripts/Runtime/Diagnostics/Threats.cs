using OOI.AutoUI;
using DeadSignal.Combat;

namespace DeadSignal.Diagnostics
{
    public sealed class DebugThreatsPage : DebugMenuPage
    {
        private bool m_freezeThreats;

        [BeginLayoutGroup("Threat Deployment", width: 550, layoutGroupType: LayoutGroupType.Grid, numGridColumns: 2,
            gridCellHeight: 58, backgroundColor: 0x32151AFA)]
        [Button] public void SpawnWarden() => _spawn(SecurityReinforcement.Warden);
        [Button] public void PurgeWarden() => _purge(SecurityReinforcement.Warden);
        [Button] public void SpawnSapper() => _spawn(SecurityReinforcement.Sapper);
        [Button] public void PurgeSapper() => _purge(SecurityReinforcement.Sapper);
        [Button] public void SpawnInterceptor() => _spawn(SecurityReinforcement.Interceptor);
        [Button] public void PurgeInterceptor() => _purge(SecurityReinforcement.Interceptor);
        [Button] public void SpawnSuppressor() => _spawn(SecurityReinforcement.Suppressor);
        [Button] public void PurgeSuppressor() => _purge(SecurityReinforcement.Suppressor);
        [EndLayoutGroup] public void EndDeployment() { }

        [BeginLayoutGroup("Selected Threat", width: 550, backgroundColor: 0x27151AFA)]
        [Dropdown] public SecurityReinforcement SelectedThreat { get; set; } = SecurityReinforcement.Warden;
        [Button] public void DamageSelectedThreat() => Run(() => Game.DebugDamageThreat(SelectedThreat), $"Damaged {SelectedThreat}");
        [Button] public void RepositionSelectedThreat() => Run(() => Game.DebugRepositionThreat(SelectedThreat), $"Repositioned {SelectedThreat}");
        [Button] public void ForceSelectedAttack() => Run(() => Game.DebugForceThreatAttack(SelectedThreat), $"Forced {SelectedThreat} attack");
        [Toggle(onChanged: nameof(_toggleFreeze))]
        public bool FreezeThreats { get => m_freezeThreats; set => m_freezeThreats = value; }
        [EndLayoutGroup] public void EndPage() { }

        private void _spawn(SecurityReinforcement threat) => Run(() => Game.DebugSpawnThreat(threat), $"Spawned {threat}");
        private void _purge(SecurityReinforcement threat) => Run(() => Game.DebugPurgeThreat(threat), $"Purged {threat}");
        private void _toggleFreeze() => Run(() => Game.DebugSetThreatsFrozen(m_freezeThreats), $"Threat freeze {m_freezeThreats}");
    }
}
