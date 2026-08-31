using System.Linq;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class EnvironmentLightingTuningTests
    {
        [Test]
        public void AuthoredTuning_DefinesReadableRolesAccessibilityAndPerformanceCeilings()
        {
            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");

            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.LandmarkLights, Has.Count.EqualTo(4));
            Assert.That(tuning.LandmarkLights.Select(profile => profile.Name), Is.Unique);
            Assert.That(tuning.LandmarkLights.Count(profile => profile.Role == EnvironmentLightRole.DominantTask),
                Is.EqualTo(1));
            Assert.That(tuning.LandmarkLights, Has.Some.Property("Role").EqualTo(EnvironmentLightRole.SecondaryTask));
            Assert.That(tuning.LandmarkLights, Has.Some.Property("Role").EqualTo(EnvironmentLightRole.Practical));
            Assert.That(tuning.LandmarkLights, Has.Some.Property("Role").EqualTo(EnvironmentLightRole.Navigation));
            Assert.That(tuning.ReducedFlashesBloomIntensity, Is.LessThanOrEqualTo(tuning.BloomIntensity));
            Assert.That(tuning.ReducedFlashesPulseDepth, Is.LessThan(tuning.PracticalPulseDepth));
            Assert.That(tuning.DeadZoneVignette, Is.GreaterThanOrEqualTo(tuning.PoweredVignette));
            Assert.That(tuning.MaximumPoweredRouteEmission, Is.LessThan(tuning.MaximumEmission));
            Assert.That(tuning.MaximumVisibleRealtimeLights, Is.EqualTo(5));
            Assert.That(tuning.MaximumShadowedRealtimeLights, Is.EqualTo(1));

            var clamped = tuning.ClampEmission(new Color(6f, 2f, 1f));
            Assert.That(clamped.maxColorComponent, Is.EqualTo(tuning.MaximumEmission).Within(0.001f));
        }
    }
}
