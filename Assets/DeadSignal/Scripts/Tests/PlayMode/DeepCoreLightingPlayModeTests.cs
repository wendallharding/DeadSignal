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
    public sealed class DeepCoreLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator DeepCoreLighting_ProgressesFromDeadMachineryToHotAndColdProcessPoolsWithinBudget()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var cookie = Resources.Load<Texture2D>("Environment/DeepCoreCalibrationApertureCookie");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var induction = Object.FindFirstObjectByType<AuthoredInductionLatticeObjective>();
            var flux = Object.FindFirstObjectByType<AuthoredFluxShuntObjective>();
            var convergence = Object.FindFirstObjectByType<AuthoredConvergenceCalibrationObjective>();
            var breaker = Object.FindFirstObjectByType<AuthoredBreakerResetObjective>();
            var furnace = Object.FindFirstObjectByType<AuthoredFurnaceForgeObjective>();
            var quench = Object.FindFirstObjectByType<AuthoredQuenchStabilizationObjective>();
            Assert.That(tuning, Is.Not.Null);
            Assert.That(cookie, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(induction, Is.Not.Null);
            Assert.That(flux, Is.Not.Null);
            Assert.That(convergence, Is.Not.Null);
            Assert.That(breaker, Is.Not.Null);
            Assert.That(furnace, Is.Not.Null);
            Assert.That(quench, Is.Not.Null);

            var deepCoreProfiles = tuning.LandmarkLights.Where(profile =>
                profile.PowerSource is >= EnvironmentLightPowerSource.InductionLattice and
                    <= EnvironmentLightPowerSource.QuenchStabilization).ToArray();
            Assert.That(deepCoreProfiles, Has.Length.EqualTo(6));
            var lights = deepCoreProfiles.ToDictionary(
                profile => profile.PowerSource,
                profile => GameObject.Find(profile.Name).GetComponent<Light>());
            Assert.That(lights[EnvironmentLightPowerSource.ConvergenceCalibration].cookie, Is.SameAs(cookie));
            Assert.That(lights.Values.Count(light => light.type == LightType.Spot), Is.EqualTo(3));
            Assert.That(lights.Values.Count(light => light.type == LightType.Point), Is.EqualTo(3));

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

            game.DebugActivateSpineTower();
            scene.Player.position = game.InductionLatticePosition + new Vector3(0f, 0f, -1.8f);
            yield return new WaitForSecondsRealtime(0.6f);
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.InductionLattice, false);
            _captureIfRequested(scene.PlayerCamera, "P20-Induction-Available-1280x720.png", 1280, 720);

            game.DebugChargeInductionLattice();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(induction.PresentationState,
                Is.EqualTo(InductionLatticePresentationState.Charged));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.InductionLattice, true);

            scene.Player.position = game.FluxShuntPosition + new Vector3(0f, 0f, -1.8f);
            game.DebugRouteFluxShunt();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(flux.PresentationState, Is.EqualTo(FluxShuntPresentationState.Routed));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.FluxShunt, true);
            _captureIfRequested(scene.PlayerCamera, "P20-Flux-Routed-1600x900.png", 1600, 900);

            scene.Player.position = game.ConvergenceCalibrationPosition + new Vector3(0f, 0f, -1.6f);
            game.DebugCompleteConvergenceCalibration();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(convergence.PresentationState,
                Is.EqualTo(ConvergenceCalibrationPresentationState.Complete));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.ConvergenceCalibration, true);
            _captureIfRequested(scene.PlayerCamera, "P20-Convergence-Calibrated-1600x900.png", 1600, 900);

            scene.Player.position = game.BreakerResetPosition + new Vector3(0f, 0f, -1.6f);
            game.DebugResetBreakerDistribution();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(breaker.PresentationState,
                Is.EqualTo(BreakerResetPresentationState.ResetComplete));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.BreakerDistribution, true);

            scene.Player.position = game.FurnaceForgePosition + new Vector3(0f, 0f, -1.8f);
            game.DebugForgeLattice();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(furnace.PresentationState,
                Is.EqualTo(CoreProcessingPresentationState.Complete));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.FurnaceForge, true);
            _captureIfRequested(scene.PlayerCamera, "P20-Furnace-Forged-1600x900.png", 1600, 900);

            scene.Player.position = game.QuenchStabilizationPosition + new Vector3(0f, 0f, -1.8f);
            game.DebugStabilizeCore();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(quench.PresentationState,
                Is.EqualTo(CoreProcessingPresentationState.Complete));
            _assertVisibleState(tuning, lights, EnvironmentLightPowerSource.QuenchStabilization, true);

            if (!game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (!game.IsHighContrastEnabled)
            {
                game.DebugToggleHighContrast();
            }
            yield return new WaitForSecondsRealtime(1.6f);
            _captureIfRequested(scene.PlayerCamera, "P20-Quench-Stabilized-Accessible-1600x900.png", 1600, 900);
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
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P20_CAPTURE_DIR");
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
