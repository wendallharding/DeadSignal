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
    public sealed class SpineRegionLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpineRegionLighting_SeparatesPressureTowerAndPoweredReturnWithinBudget()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var cookie = Resources.Load<Texture2D>("Environment/SpineHighVoltageLaneCookie");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var venting = Object.FindFirstObjectByType<AuthoredSpineVentingObjective>(FindObjectsInactive.Include);
            var tower = Object.FindFirstObjectByType<AuthoredSpineTowerReadability>(FindObjectsInactive.Include);
            var finishes = Object.FindObjectsByType<AuthoredSpineHeroFinish>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(tuning, Is.Not.Null);
            Assert.That(cookie, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(venting, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(tower, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(finishes, Has.Length.EqualTo(2));

            var trenchProfile = tuning.LandmarkLights.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.SpineVenting);
            var spineProfile = tuning.LandmarkLights.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.SpineTower);
            var trenchLight = GameObject.Find(trenchProfile.Name).GetComponent<Light>();
            var spineLight = GameObject.Find(spineProfile.Name).GetComponent<Light>();
            Assert.That(trenchLight.type, Is.EqualTo(LightType.Point));
            Assert.That(spineLight.type, Is.EqualTo(LightType.Spot));
            Assert.That(spineLight.cookie, Is.SameAs(cookie));

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

            game.DebugActivateRelayTower();
            game.DebugCollectNextCache();
            game.DebugInstallRelayPayload();
            game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
            scene.Player.position = venting.Position + new Vector3(0f, 0f, -1.4f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.VentAvailable));
            Assert.That(trenchLight.enabled, Is.True);
            Assert.That(trenchLight.color, Is.EqualTo(trenchProfile.GetColor(false)));
            _captureIfRequested(scene.PlayerCamera, "P19-Trench-Pressurized-1280x720.png", 1280, 720);

            game.DebugVentSpineBerth();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(venting.PresentationState, Is.EqualTo(SpineBerthPresentationState.Vented));
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.ActivationAvailable));
            Assert.That(trenchLight.color, Is.EqualTo(trenchProfile.GetColor(true)));
            Assert.That(trenchLight.intensity, Is.GreaterThan(trenchProfile.GetIntensity(false) * 0.75f));
            _captureIfRequested(scene.PlayerCamera, "P19-Trench-Vented-1600x900.png", 1600, 900);

            scene.Player.position = game.SpineTowerPosition + new Vector3(0f, 0f, -2.8f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(spineLight.enabled, Is.True);
            Assert.That(spineLight.color, Is.EqualTo(spineProfile.GetColor(false)));
            _captureIfRequested(scene.PlayerCamera, "P19-Spine-Available-1600x900.png", 1600, 900);

            game.DebugActivateSpineTower();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(tower.PresentationState, Is.EqualTo(SpineTowerPresentationState.Powered));
            Assert.That(spineLight.color, Is.EqualTo(spineProfile.GetColor(true)));
            Assert.That(spineLight.intensity, Is.GreaterThan(spineProfile.GetIntensity(false) * 0.75f));
            Assert.That(game.transform.Find("Capacitor Spine Region/Spine Return Threshold").gameObject.activeSelf,
                Is.True);
            var spineTerritory = game.transform.Find("Spine Power Territory").GetComponent<Renderer>().sharedMaterial;
            Assert.That(spineTerritory.GetColor("_BaseColor").a,
                Is.EqualTo(tuning.SpineTerritoryBase.a).Within(0.001f));
            Assert.That(spineTerritory.GetColor("_EdgeColor").a,
                Is.EqualTo(tuning.SpineTerritoryEdge.a).Within(0.001f));

            if (!game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (!game.IsHighContrastEnabled)
            {
                game.DebugToggleHighContrast();
            }
            yield return new WaitForSecondsRealtime(1.6f);
            _captureIfRequested(scene.PlayerCamera, "P19-Spine-Powered-Accessible-1600x900.png", 1600, 900);
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

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P19_CAPTURE_DIR");
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
