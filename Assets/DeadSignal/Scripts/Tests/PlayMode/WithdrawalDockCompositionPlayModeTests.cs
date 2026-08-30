using System.Collections;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class WithdrawalDockCompositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator WithdrawalDockComposition_FramesFinalRouteWithoutChangingTraversal()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var composition = Object.FindFirstObjectByType<AuthoredWithdrawalDockComposition>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(composition.IsConfigured, Is.True);
            Assert.That(composition.SectionCount, Is.EqualTo(3));
            Assert.That(composition.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Withdrawal and Dock composition must remain presentation-only.");
            Assert.That(composition.transform.Find("Withdrawal and Dock Underdeck Aprons"), Is.Not.Null);
            Assert.That(composition.transform.Find("Withdrawal and Dock Shadow Backs"), Is.Not.Null);
            Assert.That(composition.transform.Find("Withdrawal and Dock Edge Frames"), Is.Not.Null);
            Assert.That(composition.Sections[0].sharedMaterial.name, Is.EqualTo("StationSteel"));
            Assert.That(composition.Sections[1].sharedMaterial.name, Is.EqualTo("StationBlack"));
            Assert.That(composition.Sections[2].sharedMaterial.name, Is.EqualTo("MaintenanceBulkhead"));

            var apronBounds = composition.Sections[0].bounds;
            Assert.That(apronBounds.Contains(new Vector3(-9.2f, -0.71f, -5.6f)), Is.True,
                "The Extraction Dock must own an apron.");
            Assert.That(apronBounds.Contains(new Vector3(-7.2f, -0.71f, -4.2f)), Is.True,
                "The Departure Channel must remain visually connected to the Dock.");
        }

        [UnityTest]
        public IEnumerator WithdrawalDockComposition_PreservesShutterSurgeAndUplinkAuthority()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var departure = Object.FindFirstObjectByType<AuthoredDepartureChannelReadability>();
            var dock = Object.FindFirstObjectByType<AuthoredExtractionDockReadability>();
            Assert.That(departure, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(dock, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(departure.transform.Find("Departure Cargo Shutter"), Is.Not.Null);
            Assert.That(departure.transform.Find("Departure Capacitor Surge Signal"), Is.Not.Null);
            Assert.That(dock.transform.Find("Extraction Uplink Status"), Is.Not.Null);
            Assert.That(departure.GetComponentsInChildren<AuthoredMapObstacle>(true).Length, Is.EqualTo(3));
            Assert.That(dock.GetComponentsInChildren<Collider>(true), Is.Empty);
        }
    }
}
