using OOI.AutoUI;

namespace DeadSignal.Diagnostics
{
    public sealed class DebugAutomationPage : DebugMenuPage
    {
        [BeginLayoutGroup("Time and Route Driver", width: 560, backgroundColor: 0x152035FA)]
        [Dropdown(onChanged: nameof(_applyTimeScale))] public DebugTimeScale TimeScale { get; set; } = DebugTimeScale.Normal;
        [Button] public void StepOneFrame() => Run(Game.DebugStepFrame, "Advanced one simulation frame");
        [Dropdown] public DebugLocation RouteDestination { get; set; } = DebugLocation.CentralTower;
        [Button] public void DriveToDestination() =>
            Run(() => Game.DebugStartRouteDriver(RouteDestination), $"Route driver started: {RouteDestination}");
        [Button] public void StopRouteDriver() => Run(Game.DebugStopRouteDriver, "Route driver stopped");
        [EndLayoutGroup] public void EndTimeAndRoute() { }

        [BeginLayoutGroup("Camera and Visualization", width: 560, backgroundColor: 0x102535FA)]
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
    }
}
