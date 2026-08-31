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
    public sealed class RelayRegionLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator RelayRegionLighting_DistinguishesFoundryAndGantryRelightsAndStaysWithinBudget()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var cookie = Resources.Load<Texture2D>("Environment/RelayFoundryInductionCookie");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var readability = Object.FindFirstObjectByType<AuthoredRelayNetworkReadability>(FindObjectsInactive.Include);
            var foundryFinish = Object.FindFirstObjectByType<AuthoredRelayFoundryHeroFinish>(FindObjectsInactive.Include);
            var gantryFinish = Object.FindFirstObjectByType<AuthoredCoolingGantryHeroFinish>(FindObjectsInactive.Include);
            Assert.That(tuning, Is.Not.Null);
            Assert.That(cookie, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(readability, Is.Not.Null);
            Assert.That(foundryFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(gantryFinish, Is.Not.Null.And.Property("IsConfigured").True);

            var foundryProfile = tuning.LandmarkLights.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.RelayTower);
            var gantryProfile = tuning.LandmarkLights.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.RelayPayload);
            var foundryLight = GameObject.Find(foundryProfile.Name).GetComponent<Light>();
            var gantryLight = GameObject.Find(gantryProfile.Name).GetComponent<Light>();
            Assert.That(foundryLight.type, Is.EqualTo(LightType.Spot));
            Assert.That(foundryLight.cookie, Is.SameAs(cookie));
            Assert.That(gantryLight.type, Is.EqualTo(LightType.Point));
            Assert.That(foundryProfile.Color.maxColorComponent,
                Is.Not.EqualTo(gantryProfile.Color.maxColorComponent));

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

            scene.Player.position = game.RelayTowerPosition + new Vector3(-3.5f, 0f, -2.5f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
            Assert.That(foundryLight.enabled, Is.True);
            Assert.That(foundryLight.color, Is.EqualTo(foundryProfile.GetColor(false)));
            _captureIfRequested(scene.PlayerCamera, "P18-Foundry-Dormant-1600x900.png", 1600, 900);

            game.DebugActivateRelayTower();
            scene.Player.position = game.RelayTowerPosition + new Vector3(-3.5f, 0f, -2.5f);
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Powered));
            Assert.That(foundryLight.color, Is.EqualTo(foundryProfile.GetColor(true)));
            Assert.That(foundryLight.intensity, Is.GreaterThan(foundryProfile.GetIntensity(false) * 0.75f));
            var relayTerritory = game.transform.Find("Relay Power Territory").GetComponent<Renderer>().sharedMaterial;
            Assert.That(relayTerritory.GetColor("_BaseColor").a, Is.EqualTo(tuning.RelayTerritoryBase.a).Within(0.001f));
            Assert.That(relayTerritory.GetColor("_EdgeColor").a, Is.EqualTo(tuning.RelayTerritoryEdge.a).Within(0.001f));
            _captureIfRequested(scene.PlayerCamera, "P18-Foundry-Powered-1600x900.png", 1600, 900);

            scene.Player.position = gantryFinish.FinishRenderer.bounds.center + new Vector3(-1.8f, 0f, 0.5f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.ProcessingAvailable));
            Assert.That(gantryLight.enabled, Is.True);
            Assert.That(gantryLight.color, Is.EqualTo(gantryProfile.GetColor(false)));
            _captureIfRequested(scene.PlayerCamera, "P18-Gantry-Available-1280x720.png", 1280, 720);

            game.DebugCollectNextCache();
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.Stabilized));
            Assert.That(gantryLight.color, Is.EqualTo(gantryProfile.GetColor(true)));
            Assert.That(gantryLight.intensity, Is.GreaterThan(gantryProfile.GetIntensity(false) * 0.75f));

            if (!game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (!game.IsHighContrastEnabled)
            {
                game.DebugToggleHighContrast();
            }
            yield return new WaitForSecondsRealtime(0.6f);
            _captureIfRequested(scene.PlayerCamera, "P18-Gantry-Stabilized-Accessible-1600x900.png", 1600, 900);
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
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P18_CAPTURE_DIR");
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
