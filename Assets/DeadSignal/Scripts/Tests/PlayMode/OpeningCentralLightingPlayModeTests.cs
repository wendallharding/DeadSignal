using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class OpeningCentralLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator OpeningAndCentralLighting_PreserveFlanksAndRelightWithTowerState()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var centralFinish = Object.FindFirstObjectByType<AuthoredCentralHeroFinish>(FindObjectsInactive.Include);
            var dockFinishes = Object.FindObjectsByType<AuthoredDepartureDockHeroFinish>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var centralProfile = tuning.LandmarkLights.Single(profile => profile.RespondsToCentralPower);
            var openingProfile = tuning.LandmarkLights.Single(profile => profile.Role == EnvironmentLightRole.Navigation);
            var centralLight = GameObject.Find(centralProfile.Name).GetComponent<Light>();
            var openingLight = GameObject.Find(openingProfile.Name).GetComponent<Light>();
            var channel = GameObject.Find("Extraction Departure Channel");

            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(centralFinish, Is.Not.Null);
            Assert.That(dockFinishes, Has.Length.EqualTo(2));
            Assert.That(channel.GetComponentsInChildren<AuthoredMapObstacle>(), Has.Length.EqualTo(3));
            Assert.That(channel.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(centralLight.color, Is.EqualTo(centralProfile.GetColor(false)));
            Assert.That(openingLight.color, Is.EqualTo(openingProfile.Color));
            Assert.That(centralFinish.PracticalEmission.maxColorComponent,
                Is.EqualTo(tuning.CentralDormantFixtureEmission).Within(0.001f));
            foreach (var finish in dockFinishes)
            {
                Assert.That(finish.PracticalEmission.maxColorComponent,
                    Is.EqualTo(openingProfile.Color.maxColorComponent * tuning.OpeningFixtureEmission).Within(0.001f));
            }

            game.DebugTeleport(DebugLocation.Extraction);
            yield return null;
            _captureIfRequested(scene.PlayerCamera, "P16-Opening-1280x720.png", 1280, 720);
            game.DebugTeleport(DebugLocation.CentralTower);
            yield return null;
            _captureIfRequested(scene.PlayerCamera, "P16-Central-Dormant-1600x900.png", 1600, 900);

            game.DebugActivateTower();
            yield return null;
            Assert.That(centralLight.color, Is.EqualTo(centralProfile.GetColor(true)));
            Assert.That(centralLight.intensity, Is.GreaterThan(centralProfile.GetIntensity(false)));
            Assert.That(centralFinish.PracticalEmission.maxColorComponent,
                Is.EqualTo(centralProfile.GetColor(true).maxColorComponent *
                           tuning.CentralPoweredFixtureEmission).Within(0.001f));
            _captureIfRequested(scene.PlayerCamera, "P16-Central-Powered-1600x900.png", 1600, 900);

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
            yield return null;
            _captureIfRequested(scene.PlayerCamera, "P16-Central-Accessible-1600x900.png", 1600, 900);
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
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P16_CAPTURE_DIR");
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
