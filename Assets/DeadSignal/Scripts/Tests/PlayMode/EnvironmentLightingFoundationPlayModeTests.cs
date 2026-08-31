using System.Collections;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
            Assert.That(lights.Length, Is.LessThanOrEqualTo(tuning.MaximumVisibleRealtimeLights));
            Assert.That(lights.Count(light => light.shadows != LightShadows.None),
                Is.LessThanOrEqualTo(tuning.MaximumShadowedRealtimeLights));
            Assert.That(RenderSettings.ambientLight, Is.EqualTo(game.IsHighContrastEnabled
                ? tuning.HighContrastAmbientFloor
                : tuning.AmbientFloor));
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
        }
    }
}
