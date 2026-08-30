using System.Collections;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class SecurityTrialCompositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SecurityTrialComposition_FramesEveryTrialRoomWithoutChangingTraversal()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var composition = Object.FindFirstObjectByType<AuthoredSecurityTrialComposition>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(composition.IsConfigured, Is.True);
            Assert.That(composition.SectionCount, Is.EqualTo(3));
            Assert.That(composition.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Security Trial composition must remain presentation-only.");
            Assert.That(composition.transform.Find("Security Trial Underdeck Aprons"), Is.Not.Null);
            Assert.That(composition.transform.Find("Security Trial Shadow Backs"), Is.Not.Null);
            Assert.That(composition.transform.Find("Security Trial Threshold Frames"), Is.Not.Null);
            Assert.That(composition.Sections[0].sharedMaterial.name, Is.EqualTo("StationSteel"));
            Assert.That(composition.Sections[1].sharedMaterial.name, Is.EqualTo("StationBlack"));
            Assert.That(composition.Sections[2].sharedMaterial.name, Is.EqualTo("MaintenanceBulkhead"));

            var apronBounds = composition.Sections[0].bounds;
            Assert.That(apronBounds.Contains(new Vector3(42.5f, -0.71f, 33f)), Is.True,
                "Room A must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(42.5f, -0.71f, 54f)), Is.True,
                "Room B must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(42.5f, -0.71f, 75f)), Is.True,
                "The Reward Vault must own an apron.");
        }

        [UnityTest]
        public IEnumerator SecurityTrialComposition_PreservesTrialDoorsPopulationAndRewardAuthority()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            Assert.That(chamber, Is.Not.Null.And.Property("IsComplete").True);
            Assert.That(chamber.transform.Find("Lockdown Threshold"), Is.Not.Null);
            Assert.That(chamber.transform.Find("Lockdown Entry Door/Entry Door Slab"), Is.Not.Null);
            Assert.That(chamber.transform.Find("Reward Vault Door/Reward Door Slab"), Is.Not.Null);
            Assert.That(chamber.transform.Find("Lockdown Arena/Lockdown Chamber Status"), Is.Not.Null);
            Assert.That(chamber.transform.Find("Reward Vault/Capacitor Vault Status"), Is.Not.Null);
            Assert.That(chamber.GetComponentsInChildren<AuthoredMapObstacle>(true).Length, Is.EqualTo(15));
            Assert.That(chamber.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(chamber.RewardSignal, Is.EqualTo(20f));
        }
    }
}
