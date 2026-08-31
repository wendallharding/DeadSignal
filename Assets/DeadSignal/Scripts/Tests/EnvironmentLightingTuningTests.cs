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
            Assert.That(tuning.LandmarkLights, Has.Count.EqualTo(8));
            Assert.That(tuning.LandmarkLights.Select(profile => profile.Name), Is.Unique);
            Assert.That(tuning.LandmarkLights.Count(profile => profile.Role == EnvironmentLightRole.DominantTask),
                Is.EqualTo(3));
            Assert.That(tuning.LandmarkLights, Has.Some.Property("Role").EqualTo(EnvironmentLightRole.SecondaryTask));
            Assert.That(tuning.LandmarkLights, Has.Some.Property("Role").EqualTo(EnvironmentLightRole.Practical));
            Assert.That(tuning.LandmarkLights, Has.Some.Property("Role").EqualTo(EnvironmentLightRole.Navigation));
            Assert.That(tuning.ReducedFlashesBloomIntensity, Is.LessThanOrEqualTo(tuning.BloomIntensity));
            Assert.That(tuning.ReducedFlashesPulseDepth, Is.LessThan(tuning.PracticalPulseDepth));
            Assert.That(tuning.DeadZoneVignette, Is.GreaterThanOrEqualTo(tuning.PoweredVignette));
            Assert.That(tuning.MaximumPoweredRouteEmission, Is.LessThan(tuning.MaximumEmission));
            Assert.That(tuning.MaximumVisibleRealtimeLights, Is.EqualTo(5));
            Assert.That(tuning.MaximumShadowedRealtimeLights, Is.EqualTo(1));
            var central = tuning.LandmarkLights.Single(profile => profile.RespondsToCentralPower);
            Assert.That(central.Role, Is.EqualTo(EnvironmentLightRole.DominantTask));
            Assert.That(central.GetColor(false), Is.Not.EqualTo(central.GetColor(true)));
            Assert.That(central.GetIntensity(false), Is.LessThan(central.GetIntensity(true)));
            var branchProfiles = tuning.LandmarkLights.Where(profile =>
                profile.PowerSource is EnvironmentLightPowerSource.CargoCoupling or
                    EnvironmentLightPowerSource.CoolantSeal or
                    EnvironmentLightPowerSource.RelayFeeds or
                    EnvironmentLightPowerSource.TransferAssembly).ToArray();
            Assert.That(branchProfiles, Has.Length.EqualTo(4));
            Assert.That(branchProfiles.Select(profile => profile.PowerSource), Is.Unique);
            Assert.That(branchProfiles.Count(profile => profile.LightType == LightType.Spot), Is.EqualTo(2));
            Assert.That(branchProfiles.Count(profile => profile.LightType == LightType.Point), Is.EqualTo(2));
            Assert.That(branchProfiles.All(profile => profile.GetIntensity(false) < profile.GetIntensity(true)), Is.True);
            Assert.That(branchProfiles.All(profile => profile.GetColor(false) != profile.GetColor(true)), Is.True);
            var relayRegionProfiles = tuning.LandmarkLights.Where(profile =>
                profile.PowerSource is EnvironmentLightPowerSource.RelayTower or
                    EnvironmentLightPowerSource.RelayPayload).ToArray();
            Assert.That(relayRegionProfiles, Has.Length.EqualTo(2));
            Assert.That(relayRegionProfiles.Select(profile => profile.LightType), Is.Unique);
            Assert.That(relayRegionProfiles.Select(profile => profile.Color.maxColorComponent), Is.Unique);
            Assert.That(relayRegionProfiles.All(profile => profile.GetIntensity(false) < profile.GetIntensity(true)),
                Is.True);
            var foundry = relayRegionProfiles.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.RelayTower);
            Assert.That(foundry.CookieResource, Is.EqualTo("Environment/RelayFoundryInductionCookie"));
            Assert.That(Resources.Load<Texture2D>(foundry.CookieResource), Is.Not.Null);
            Assert.That(tuning.CentralPoweredFixtureEmission, Is.GreaterThan(tuning.CentralDormantFixtureEmission));
            Assert.That(tuning.OpeningFixtureEmission, Is.LessThan(tuning.CentralPoweredFixtureEmission));
            Assert.That(tuning.OpeningTerritoryBase.a, Is.LessThan(0.2f));
            Assert.That(tuning.OpeningTerritoryEdge.a, Is.LessThan(0.5f));
            Assert.That(tuning.RelayTerritoryBase.a, Is.LessThan(tuning.OpeningTerritoryBase.a));
            Assert.That(tuning.RelayTerritoryEdge.a, Is.LessThan(tuning.OpeningTerritoryEdge.a));

            var clamped = tuning.ClampEmission(new Color(6f, 2f, 1f));
            Assert.That(clamped.maxColorComponent, Is.EqualTo(tuning.MaximumEmission).Within(0.001f));
        }
    }
}
