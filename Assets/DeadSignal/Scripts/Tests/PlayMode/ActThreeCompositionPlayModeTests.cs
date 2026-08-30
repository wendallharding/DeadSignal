using System.Collections;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class ActThreeCompositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ActThreeComposition_FramesEveryDeepCoreRoomWithoutChangingTraversal()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var composition = Object.FindFirstObjectByType<AuthoredActThreeComposition>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(composition.IsConfigured, Is.True);
            Assert.That(composition.SectionCount, Is.EqualTo(3));
            Assert.That(composition.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Act III deep-core composition must remain presentation-only.");
            Assert.That(composition.transform.Find("Deep-Core Underdeck Aprons"), Is.Not.Null);
            Assert.That(composition.transform.Find("Deep-Core Shadow Backs"), Is.Not.Null);
            Assert.That(composition.transform.Find("Deep-Core Ceramic Braces"), Is.Not.Null);
            Assert.That(composition.Sections[0].sharedMaterial.name, Is.EqualTo("StationSteel"));
            Assert.That(composition.Sections[1].sharedMaterial.name, Is.EqualTo("StationBlack"));
            Assert.That(composition.Sections[2].sharedMaterial.name, Is.EqualTo("MaintenanceBulkhead"));

            var apronBounds = composition.Sections[0].bounds;
            Assert.That(apronBounds.Contains(new Vector3(42.5f, -0.71f, 8.5f)), Is.True, "Induction must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(32f, -0.71f, 21.25f)), Is.True, "Flux must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(42.5f, -0.71f, 17f)), Is.True, "Convergence must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(53f, -0.71f, 17f)), Is.True, "Breaker must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(42.5f, -0.71f, 25.5f)), Is.True, "Furnace must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(53f, -0.71f, 25.5f)), Is.True, "Quench must own an apron.");
        }

        [UnityTest]
        public IEnumerator ActThreeComposition_PreservesMachineryAndOptionalCacheAuthority()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            Assert.That(Object.FindFirstObjectByType<AuthoredInductionLatticeObjective>(FindObjectsInactive.Include),
                Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(Object.FindFirstObjectByType<AuthoredFluxShuntObjective>(FindObjectsInactive.Include),
                Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(Object.FindFirstObjectByType<AuthoredConvergenceCalibrationObjective>(FindObjectsInactive.Include),
                Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(Object.FindFirstObjectByType<AuthoredBreakerResetObjective>(FindObjectsInactive.Include),
                Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(Object.FindFirstObjectByType<AuthoredFurnaceForgeObjective>(FindObjectsInactive.Include),
                Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(Object.FindFirstObjectByType<AuthoredQuenchStabilizationObjective>(FindObjectsInactive.Include),
                Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(Object.FindObjectsByType<AuthoredSalvageSocket>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.GreaterThanOrEqualTo(1), "The Furnace optional-cache socket must remain authored.");
        }
    }
}
