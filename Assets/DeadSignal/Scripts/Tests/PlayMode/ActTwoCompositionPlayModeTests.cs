using System.Collections;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class ActTwoCompositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ActTwoComposition_FramesEveryRequiredRoomWithoutChangingTraversal()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var composition = Object.FindFirstObjectByType<AuthoredActTwoComposition>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(composition.IsConfigured, Is.True);
            Assert.That(composition.SectionCount, Is.EqualTo(3));
            Assert.That(composition.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Act II composition must remain presentation-only.");
            Assert.That(composition.transform.Find("Underdeck Aprons"), Is.Not.Null);
            Assert.That(composition.transform.Find("Shadow Backs"), Is.Not.Null);
            Assert.That(composition.transform.Find("Ceramic Braces"), Is.Not.Null);
            Assert.That(composition.Sections[0].sharedMaterial.name, Is.EqualTo("StationSteel"));
            Assert.That(composition.Sections[1].sharedMaterial.name, Is.EqualTo("StationBlack"));
            Assert.That(composition.Sections[2].sharedMaterial.name, Is.EqualTo("MaintenanceBulkhead"));

            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(27.5f, -0.72f, 0f)), Is.True,
                "Relay Foundry must own an underdeck apron.");
            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(27.5f, -0.71f, -11.25f)), Is.True,
                "Cooling Gantry must own an underdeck apron.");
            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(42.5f, -0.71f, 0f)), Is.True,
                "Capacitor Spine must own an underdeck apron.");
            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(42.5f, -0.71f, -8f)), Is.True,
                "Discharge Trench must own an underdeck apron.");
        }

        [UnityTest]
        public IEnumerator ActTwoComposition_PreservesMachineryAndReturnGateAuthority()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var relay = Object.FindFirstObjectByType<AuthoredRelayPayloadObjective>(FindObjectsInactive.Include);
            var spine = Object.FindFirstObjectByType<AuthoredSpineVentingObjective>(FindObjectsInactive.Include);
            var gates = Object.FindObjectsByType<AuthoredRouteDoorReadability>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(relay, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(relay.HasReadabilityAssets, Is.True);
            Assert.That(spine, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(gates.Length, Is.GreaterThanOrEqualTo(2));
        }
    }
}
