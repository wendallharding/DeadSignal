using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class WithdrawalDockLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator WithdrawalDockLighting_RelightsPursuitSurgeAndUplinkWithinBudget()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var cookie = Resources.Load<Texture2D>("Environment/ExtractionUplinkLockOnCookie");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var departure = Object.FindFirstObjectByType<AuthoredDepartureChannelReadability>();
            var dock = Object.FindFirstObjectByType<AuthoredExtractionDockReadability>();
            Assert.That(tuning, Is.Not.Null);
            Assert.That(cookie, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(departure, Is.Not.Null);
            Assert.That(dock, Is.Not.Null);

            var profiles = tuning.LandmarkLights.Where(profile =>
                profile.PowerSource is >= EnvironmentLightPowerSource.WithdrawalWardenBay and
                    <= EnvironmentLightPowerSource.ExtractionUplink).ToArray();
            Assert.That(profiles, Has.Length.EqualTo(4));
            var lights = profiles.ToDictionary(
                profile => profile.PowerSource,
                profile => GameObject.Find(profile.Name).GetComponent<Light>());
            Assert.That(lights[EnvironmentLightPowerSource.ExtractionUplink].cookie, Is.SameAs(cookie));

            game.DebugPrepareDepartureSurge();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(game.CurrentPoweredWithdrawalPhase, Is.EqualTo(PoweredWithdrawalPhase.DepartureSurge));
            Assert.That(departure.PresentationState, Is.EqualTo(DepartureChannelPresentationState.OpenSurgeAvailable),
                "The debug route advances through the released direct channel before the lighting comparison.");
            scene.Player.position = game.WardenBayPursuitPosition + Vector3.back * 2.2f;
            yield return new WaitForSecondsRealtime(0.8f);
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.WithdrawalWardenBay, true);
            _captureIfRequested(scene.PlayerCamera, "P22-Warden-Bay-Pursuit-1600x900.png", 1600, 900);
            scene.Player.position = game.SapperCradlePursuitPosition + Vector3.back * 2.2f;
            yield return new WaitForSecondsRealtime(0.8f);
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.WithdrawalSapperCradle, true);
            _captureIfRequested(scene.PlayerCamera, "P22-Sapper-Cradle-Pursuit-1600x900.png", 1600, 900);
            scene.Player.position = departure.transform.position + new Vector3(-2.4f, 0f, -2.4f);
            yield return new WaitForSecondsRealtime(0.8f);
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.DepartureSurge, true);
            _captureIfRequested(scene.PlayerCamera, "P22-Departure-Surge-Available-1280x720.png", 1280, 720);

            game.DebugMakeExtractionReady();
            scene.Player.position = dock.transform.position + Vector3.back * 2.8f;
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(dock.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Available));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.ExtractionUplink, true);
            _captureIfRequested(scene.PlayerCamera, "P22-Dock-Available-1600x900.png", 1600, 900);

            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(dock.PresentationState, Is.EqualTo(ExtractionDockPresentationState.ActiveProgress));
            _captureIfRequested(scene.PlayerCamera, "P22-Dock-Active-1600x900.png", 1600, 900);

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            var initialHighContrast = game.IsHighContrastEnabled;
            if (!initialReducedFlashes)
            {
                game.DebugToggleReducedFlashes();
            }
            if (!initialHighContrast)
            {
                game.DebugToggleHighContrast();
            }
            yield return new WaitForSecondsRealtime(1.6f);
            _captureIfRequested(scene.PlayerCamera, "P22-Dock-Active-Accessible-1600x900.png", 1600, 900);
            if (!initialHighContrast)
            {
                game.DebugToggleHighContrast();
            }
            if (!initialReducedFlashes)
            {
                game.DebugToggleReducedFlashes();
            }

            var enabledLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(light => light.enabled).ToArray();
            Assert.That(enabledLights, Has.Length.LessThanOrEqualTo(tuning.MaximumVisibleRealtimeLights));
            Assert.That(enabledLights.Count(light => light.shadows != LightShadows.None),
                Is.LessThanOrEqualTo(tuning.MaximumShadowedRealtimeLights));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
        }

        private static void _assertVisibleState(
            EnvironmentLightingTuning tuning,
            System.Collections.Generic.IReadOnlyDictionary<EnvironmentLightPowerSource, Light> lights,
            EnvironmentLightPowerSource powerSource,
            bool powered)
        {
            var profile = tuning.LandmarkLights.Single(candidate => candidate.PowerSource == powerSource);
            var light = lights[powerSource];
            Assert.That(light.enabled, Is.True);
            Assert.That(light.color, Is.EqualTo(profile.GetColor(powered)));
            Assert.That(light.intensity, Is.GreaterThan(profile.GetIntensity(powered) * 0.7f));
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P22_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                return;
            }

            Directory.CreateDirectory(captureDirectory);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
