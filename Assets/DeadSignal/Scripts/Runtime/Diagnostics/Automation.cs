using OOI.AutoUI;

namespace DeadSignal.Diagnostics
{
    public sealed class DebugAutomationPage : DebugMenuPage
    {
        private bool m_captureEachStep;
        private bool m_stepByStep;

        [BeginLayoutGroup("Route Sequencer/Sequence", backgroundColor: 0x30220EFA)]
        [Dropdown] public DebugRoutePreset RoutePreset { get; set; } = DebugRoutePreset.OpeningLoop;
        [Dropdown] public DebugAutomationMode AutomationMode { get; set; } = DebugAutomationMode.AssistedPlaythrough;
        [Dropdown] public DebugAutomationProfile AutomationProfile { get; set; } = DebugAutomationProfile.SafeNavigation;
        [Toggle(onChanged: nameof(_toggleStepMode))]
        public bool StepByStep { get => m_stepByStep; set => m_stepByStep = value; }
        [Toggle(onChanged: nameof(_toggleCaptureEachStep))]
        public bool CaptureEachStep { get => m_captureEachStep; set => m_captureEachStep = value; }
        [Label, HideLabel] public string SequenceStatus => Game != null ? Game.DebugRouteSequenceStatus : "Runtime unavailable";
        [Button] public void StartSequence() => Run(
            () => Game.DebugStartRouteSequence(RoutePreset, AutomationMode, AutomationProfile), $"Sequence started: {RoutePreset}");
        [Button] public void PauseSequence() => Run(Game.DebugPauseRouteSequence, "Route sequence paused");
        [Button] public void ResumeSequence() => Run(Game.DebugResumeRouteSequence, "Route sequence resumed");
        [Button] public void RetryStep() => Run(Game.DebugRetryRouteStep, "Current route step retried");
        [Button] public void SkipStep() => Run(Game.DebugSkipRouteStep, "Current route step skipped");
        [Button] public void AbortSequence() => Run(Game.DebugAbortRouteSequence, "Route sequence aborted");
        [EndLayoutGroup] public void EndRouteSequencer() { }

        [BeginLayoutGroup("Route Sequencer/Recording and Report", backgroundColor: 0x102535FA)]
        [Button] public void RecordCurrentPosition() => Run(Game.DebugRecordRouteNode, "Current position added to recorded route");
        [Button] public void ClearRecordedRoute() => Run(Game.DebugClearRecordedRoute, "Recorded route cleared");
        [Button] public void RunRecordedRoute() => Run(
            () => Game.DebugStartRouteSequence(DebugRoutePreset.RecordedRoute, AutomationMode, AutomationProfile),
            "Recorded route started");
        [Button] public void CopyRouteReport() => Run(Game.DebugCopyRouteReport, "Route report copied");
        [Label, HideLabel] public string RouteReport => Game != null ? Game.DebugRouteSequenceReport : "Runtime unavailable";
        [EndLayoutGroup] public void EndRecording() { }

        [BeginLayoutGroup("Manual Automation/Time and Route Driver", backgroundColor: 0x152035FA)]
        [Dropdown(onChanged: nameof(_applyTimeScale))] public DebugTimeScale TimeScale { get; set; } = DebugTimeScale.Normal;
        [Button] public void StepOneFrame() => Run(Game.DebugStepFrame, "Advanced one simulation frame");
        [Dropdown] public DebugLocation RouteDestination { get; set; } = DebugLocation.CentralTower;
        [Button] public void DriveToDestination() =>
            Run(() => Game.DebugStartRouteDriver(RouteDestination), $"Route driver started: {RouteDestination}");
        [Button] public void StopRouteDriver() => Run(Game.DebugStopRouteDriver, "Route driver stopped");
        [EndLayoutGroup] public void EndTimeAndRoute() { }

        [BeginLayoutGroup("Manual Automation/Camera and Visualization", backgroundColor: 0x102535FA)]
        [Button] public void ToggleFreeCamera() => Run(Game.DebugToggleFreeCamera, "Free camera toggled");
        [Button] public void ShowRoomOverview() => Run(Game.DebugCameraOverview, "Room overview shown");
        [Button] public void ToggleCollisionAndEntries() =>
            Run(Game.DebugToggleWorldVisualization, "Collision and entry visualization toggled");
        [Button] public void VisitCameraBoundaries() => Run(Game.DebugVisitCameraBoundaries, "Camera boundary visit started");
        [EndLayoutGroup] public void EndPage() { }

        private void _applyTimeScale()
        {
            var scale = TimeScale switch
            {
                DebugTimeScale.Paused => 0f,
                DebugTimeScale.Quarter => 0.25f,
                DebugTimeScale.Half => 0.5f,
                DebugTimeScale.Double => 2f,
                _ => 1f
            };
            Run(() => Game.DebugSetTimeScale(scale), $"Time scale set to {scale:0.00}x");
        }

        private void _toggleStepMode() => Run(() => Game.DebugSetRouteStepMode(m_stepByStep), $"Step-by-step mode {m_stepByStep}");

        private void _toggleCaptureEachStep() => Run(
            () => Game.DebugSetRouteCaptureEachStep(m_captureEachStep), $"Capture each step {m_captureEachStep}");
    }
}
