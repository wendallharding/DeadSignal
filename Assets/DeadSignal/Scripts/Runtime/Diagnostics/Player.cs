using OOI.AutoUI;
using UnityEngine;
using DeadSignal.Application;
using DeadSignal.Missions;

namespace DeadSignal.Diagnostics
{
    public sealed class DebugPlayerPage : DebugMenuPage
    {
        private bool m_invulnerable;
        private bool m_infiniteSignal;

        [BeginLayoutGroup("Run and Player", width: 560, backgroundColor: 0x17242CFA)]
        [EditBox(onChanged: nameof(_setSignal))] public float Signal { get; set; } = RunModel.StartingSignal;
        [EditBox] public int DesiredSalvage { get; set; }
        [Button] public void ApplySalvage() => _applySalvage();
        [Dropdown] public DebugLocation TeleportLocation { get; set; }
        [Button] public void Teleport() => Run(() => Game.DebugTeleport(TeleportLocation), $"Teleported to {TeleportLocation}");
        [Button] public void ResetDashCooldown() => Run(Game.DebugResetDashCooldown, "Dash cooldown reset");
        [Button] public void ActivateCentralTower() => Run(Game.DebugActivateTower, "Central tower activated");
        [Button] public void ActivateRelayTower() => Run(Game.DebugActivateRelayTower, "Relay tower activated");
        [Button] public void OpenShortcut() => Run(Game.DebugOpenShortcut, "Shortcut opened");
        [Toggle(onChanged: nameof(_toggleInvulnerable))]
        public bool Invulnerable { get => m_invulnerable; set => m_invulnerable = value; }
        [Toggle(onChanged: nameof(_toggleInfiniteSignal))]
        public bool InfiniteSignal { get => m_infiniteSignal; set => m_infiniteSignal = value; }
        [EndLayoutGroup] public void EndRunAndPlayer() { }

        [BeginLayoutGroup("Salvage and Upgrades", width: 560, backgroundColor: 0x33280EFA)]
        [Button] public void CollectNextCache() => Run(Game.DebugCollectNextCache, "Next cache collected");
        [Button] public void SelectChainArc() => _overclock(SignalOverclock.ChainArc);
        [Button] public void SelectOverdriveThrusters() => _overclock(SignalOverclock.OverdriveThrusters);
        [Button] public void SelectEmergencyCapacitor() =>
            Run(() => Game.DebugSelectAuxiliary(SignalAuxiliaryOverclock.EmergencyCapacitor), "Emergency Capacitor selected");
        [Button] public void SelectFeedbackShield() =>
            Run(() => Game.DebugSelectAuxiliary(SignalAuxiliaryOverclock.FeedbackShield), "Feedback Shield selected");
        [Button] public void SelectPiercingPulse() =>
            Run(() => Game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse), "Piercing Pulse selected");
        [Button] public void SelectControlledRicochet() =>
            Run(() => Game.DebugSelectWeapon(SignalWeaponOverclock.ControlledRicochet), "Controlled Ricochet selected");
        [EndLayoutGroup] public void EndPage() { }

        protected override void OnConfigured() => Signal = Game.CurrentSignal;

        private void _setSignal() => Run(() => Game.DebugSetSignal(Signal), $"Signal set to {Signal:0.0}");
        private void _toggleInvulnerable() => Run(() => Game.DebugSetInvulnerable(m_invulnerable), $"Invulnerability {m_invulnerable}");
        private void _toggleInfiniteSignal() => Run(() => Game.DebugSetInfiniteSignal(m_infiniteSignal), $"Infinite Signal {m_infiniteSignal}");
        private void _overclock(SignalOverclock overclock) => Run(() => Game.DebugSelectOverclock(overclock), $"{overclock} selected");

        private void _applySalvage()
        {
            DesiredSalvage = Mathf.Clamp(DesiredSalvage, 0, RunModel.SalvageRequired + 1);
            while (Game.CurrentSalvage < DesiredSalvage)
            {
                var before = Game.CurrentSalvage;
                Game.DebugCollectNextCache();
                if (Game.CurrentSalvage == before)
                {
                    break;
                }
            }
            Menu.Confirm($"Salvage set upward to {Game.CurrentSalvage}");
        }
    }
}
