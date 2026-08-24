using OOI.AutoUI;
using DeadSignal.Missions;

namespace DeadSignal.Diagnostics
{
    public sealed class DebugCapturePage : DebugMenuPage
    {
        [BeginLayoutGroup("Capture and Validation", width: 560, backgroundColor: 0x282035FA)]
        [Button] public void CaptureScreenshot() => Run(Game.DebugCaptureScreenshot, "Screenshot capture queued");
        [Button] public void CaptureCombatFrame() => Run(Game.DebugCaptureCombatSequence, "Combat capture sequence started");
        [Button] public void ExerciseAllCombatEffects() => Run(Game.DebugExerciseCombatFeedback, "All combat effects exercised");
        [Button] public void ExerciseAllThreatTelegraphs() => Run(Game.DebugExerciseThreatTelegraphs, "All threat telegraphs exercised");
        [Button] public void CopyReplayData() => Run(Game.DebugCopyReplayData, "Replay data copied");
        [EndLayoutGroup] public void EndCapture() { }

        [BeginLayoutGroup("Extraction and Presentation", width: 560, backgroundColor: 0x082A2AFA)]
        [Button] public void MakeExtractionReady() => Run(Game.DebugMakeExtractionReady, "Extraction made ready");
        [Button] public void BeginStableUplink() =>
            Run(() => Game.DebugBeginExtraction(ExtractionUplinkMode.Stable), "Stable uplink started");
        [Button] public void BeginOverdriveUplink() =>
            Run(() => Game.DebugBeginExtraction(ExtractionUplinkMode.Overdrive), "Overdrive uplink started");
        [Button] public void CompleteUplink() => Run(Game.DebugCompleteExtraction, "Uplink completed");
        [Button] public void PlayTowerSweep() => Run(Game.DebugPlayTowerSweep, "Tower sweep played");
        [Button] public void PlaySignalImpact() => Run(Game.DebugPlaySignalImpact, "Signal impact played");
        [Button] public void PlaySignalRecovery() => Run(Game.DebugPlaySignalRecovery, "Signal recovery played");
        [Button] public void PlaySalvageChain() => Run(Game.DebugPlaySalvageChain, "Salvage chain played");
        [EndLayoutGroup] public void EndPage() { }
    }

    public sealed class DebugSettingsPage : DebugMenuPage
    {
        private bool m_runWhileOpen;

        [BeginLayoutGroup("Capture and Validation/Menu and Accessibility", backgroundColor: 0x20252AFA)]
        [Toggle(onChanged: nameof(_toggleRunWhileOpen))]
        public bool RunWhileMenuOpen { get => m_runWhileOpen; set => m_runWhileOpen = value; }
        [Button] public void ToggleSteadyCamera() => Run(Game.DebugToggleCameraImpulse, "Steady camera toggled");
        [Button] public void ToggleReducedFlashes() => Run(Game.DebugToggleReducedFlashes, "Reduced flashes toggled");
        [Button] public void ToggleHighContrast() => Run(Game.DebugToggleHighContrast, "High contrast toggled");
        [Button] public void ToggleAudio() => Run(Game.DebugToggleAudio, "Audio toggled");
        [EndLayoutGroup] public void EndAccessibility() { }

        [BeginLayoutGroup("Capture and Validation/Composition and Replay", backgroundColor: 0x101820FA)]
        [Label, HideLabel] public string Composition => Game != null ? Game.DebugComposition : "Runtime unavailable";
        [Label, HideLabel] public string Replay => Game != null ? Game.DebugReplayInfo : "Runtime unavailable";
        [EndLayoutGroup] public void EndPage() { }

        private void _toggleRunWhileOpen() => Menu.SetRunWhileOpen(m_runWhileOpen);
    }
}
