using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class SecurityTrialLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator SecurityTrialLighting_SeparatesWarningLockdownClearAndRecoveryWithinBudget()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var cookie = Resources.Load<Texture2D>("Environment/SecurityTrialContainmentCookie");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            Assert.That(tuning, Is.Not.Null);
            Assert.That(cookie, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(chamber, Is.Not.Null);

            var securityProfiles = tuning.LandmarkLights.Where(profile =>
                profile.PowerSource is >= EnvironmentLightPowerSource.SecurityCommitment and
                    <= EnvironmentLightPowerSource.SecurityCapacitor).ToArray();
            Assert.That(securityProfiles, Has.Length.EqualTo(4));
            var lights = securityProfiles.ToDictionary(
                profile => profile.PowerSource,
                profile => GameObject.Find(profile.Name).GetComponent<Light>());
            Assert.That(lights[EnvironmentLightPowerSource.SecurityLockdown].cookie, Is.SameAs(cookie));

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            var initialHighContrast = game.IsHighContrastEnabled;
            if (initialReducedFlashes)
            {
                game.DebugToggleReducedFlashes();
            }
            if (initialHighContrast)
            {
                game.DebugToggleHighContrast();
            }

            game.DebugStabilizeCore();
            scene.Player.position = chamber.CommitmentSwitch.position + Vector3.back * 1.4f;
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Available));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.SecurityCommitment, true);
            _captureIfRequested(scene.PlayerCamera, "P21-Room-A-Warning-1280x720.png", 1280, 720);

            game.DebugCommitSecurityTrial();
            scene.Player.position = chamber.ArenaPosition + Vector3.back * 1.8f;
            Assert.That(chamber.TryBeginLockdown(
                chamber.LockdownThreshold.TransformPoint(new Vector3(0f, 0f, 1f))), Is.True);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(chamber.LockdownPresentationState,
                Is.EqualTo(LockdownChamberPresentationState.LockedActive));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.SecurityLockdown, true);
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.SecurityClear, false);
            var phaseOneIntensity = lights[EnvironmentLightPowerSource.SecurityLockdown].intensity;

            Assert.That(chamber.AdvancePhase(), Is.True);
            Assert.That(chamber.AdvancePhase(), Is.True);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(lights[EnvironmentLightPowerSource.SecurityLockdown].intensity,
                Is.GreaterThan(phaseOneIntensity));
            _captureIfRequested(scene.PlayerCamera, "P21-Room-B-Phase-Three-1600x900.png", 1600, 900);

            chamber.Complete();
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(chamber.LockdownPresentationState, Is.EqualTo(LockdownChamberPresentationState.Cleared));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.SecurityLockdown, false);
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.SecurityClear, true);
            _captureIfRequested(scene.PlayerCamera, "P21-Room-B-Cleared-1600x900.png", 1600, 900);

            scene.Player.position = chamber.RewardPosition + Vector3.back * 1.5f;
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(chamber.CapacitorPresentationState, Is.EqualTo(TrialCapacitorPresentationState.Available));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.SecurityCapacitor, true);
            _captureIfRequested(scene.PlayerCamera, "P21-Room-C-Recovery-1600x900.png", 1600, 900);

            if (!game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (!game.IsHighContrastEnabled)
            {
                game.DebugToggleHighContrast();
            }
            yield return new WaitForSecondsRealtime(1.6f);
            _captureIfRequested(scene.PlayerCamera, "P21-Room-C-Accessible-1600x900.png", 1600, 900);
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
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P21_CAPTURE_DIR");
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
