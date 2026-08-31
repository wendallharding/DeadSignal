using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class EnvironmentLightingFoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredTuning_OwnsGlobalGradeAndBoundedPracticalLights()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(light => light.enabled).ToArray();
            var grade = GameObject.Find("Dead Signal Global Grade").GetComponent<Volume>();

            Assert.That(tuning, Is.Not.Null);
            Assert.That(game.HasEnvironmentLightingTuning, Is.True);
            Assert.That(scene.KeyLight.color, Is.EqualTo(tuning.KeyLightColor));
            Assert.That(scene.KeyLight.intensity, Is.EqualTo(tuning.KeyLightIntensity).Within(0.001f));
            Assert.That(scene.KeyLight.shadows, Is.EqualTo(tuning.KeyLightShadows));
            Assert.That(scene.KeyLight.shadowStrength, Is.EqualTo(tuning.KeyLightShadowStrength).Within(0.001f));
            Assert.That(scene.KeyLight.shadowBias, Is.EqualTo(tuning.KeyLightShadowBias).Within(0.001f));
            Assert.That(scene.KeyLight.shadowNormalBias,
                Is.EqualTo(tuning.KeyLightShadowNormalBias).Within(0.001f));
            Assert.That(scene.KeyLight.shadowNearPlane,
                Is.EqualTo(tuning.KeyLightShadowNearPlane).Within(0.001f));
            Assert.That(lights.Length, Is.LessThanOrEqualTo(tuning.MaximumVisibleRealtimeLights));
            Assert.That(lights.Count(light => light.shadows != LightShadows.None),
                Is.LessThanOrEqualTo(tuning.MaximumShadowedRealtimeLights));
            Assert.That(RenderSettings.ambientLight, Is.EqualTo(game.IsHighContrastEnabled
                ? tuning.HighContrastAmbientFloor
                : tuning.AmbientFloor));
            Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(tuning.AmbientIntensity).Within(0.001f));
            Assert.That(RenderSettings.defaultReflectionMode, Is.EqualTo(DefaultReflectionMode.Skybox));
            Assert.That(RenderSettings.reflectionIntensity,
                Is.EqualTo(tuning.ReflectionIntensity).Within(0.001f));
            Assert.That(RenderSettings.reflectionBounces, Is.EqualTo(tuning.ReflectionBounces));
            Assert.That(Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Empty,
                "The compact runtime station uses one bounded sky reflection rather than discontinuous room probes.");
            Assert.That(RenderSettings.fog, Is.EqualTo(tuning.FogEnabled));
            Assert.That(RenderSettings.fogColor, Is.EqualTo(tuning.FogColor));
            Assert.That(RenderSettings.fogDensity, Is.EqualTo(tuning.FogDensity).Within(0.0001f));

            Assert.That(grade.isGlobal, Is.True);
            Assert.That(grade.profile.TryGet(out Bloom bloom), Is.True);
            Assert.That(bloom.intensity.value, Is.EqualTo(game.IsReducedFlashesEnabled
                ? tuning.ReducedFlashesBloomIntensity
                : tuning.BloomIntensity).Within(0.001f));
            Assert.That(bloom.threshold.value, Is.EqualTo(tuning.BloomThreshold).Within(0.001f));
            Assert.That(grade.profile.TryGet(out ColorAdjustments color), Is.True);
            Assert.That(color.postExposure.value, Is.EqualTo(tuning.PostExposure).Within(0.001f));

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            var initialHighContrast = game.IsHighContrastEnabled;
            game.DebugToggleReducedFlashes();
            yield return null;
            Assert.That(bloom.intensity.value, Is.EqualTo(initialReducedFlashes
                ? tuning.BloomIntensity
                : tuning.ReducedFlashesBloomIntensity).Within(0.001f));
            game.DebugToggleReducedFlashes();
            yield return null;
            Assert.That(game.IsReducedFlashesEnabled, Is.EqualTo(initialReducedFlashes));

            foreach (var profile in tuning.LandmarkLights)
            {
                var lightObject = GameObject.Find(profile.Name);
                Assert.That(lightObject, Is.Not.Null, $"Missing practical-light owner {profile.Name}.");
                var light = lightObject.GetComponent<Light>();
                Assert.That(light.color, Is.EqualTo(profile.Color));
                Assert.That(light.range, Is.EqualTo(profile.Range).Within(0.001f));
                Assert.That(light.shadows, Is.EqualTo(LightShadows.None));
            }

            game.DebugTeleport(DebugLocation.CentralTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(scene.PlayerCamera, "P23-Central-Dormant-1600x900.png", 1600, 900);
            game.DebugActivateTower();
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(scene.PlayerCamera, "P23-Central-Powered-1600x900.png", 1600, 900);

            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            game.DebugTeleport(DebugLocation.Extraction);
            if (!game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (!game.IsHighContrastEnabled)
            {
                game.DebugToggleHighContrast();
            }
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(scene.PlayerCamera, "P23-Dock-Active-Accessible-1280x720.png", 1280, 720);
            if (!initialHighContrast)
            {
                game.DebugToggleHighContrast();
            }
            if (!initialReducedFlashes)
            {
                game.DebugToggleReducedFlashes();
            }
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P23_CAPTURE_DIR");
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
