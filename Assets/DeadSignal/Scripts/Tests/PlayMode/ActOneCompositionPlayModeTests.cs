using System.Collections;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class ActOneCompositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ActOneComposition_FramesEveryRequiredRoomWithoutChangingTraversal()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var composition = Object.FindFirstObjectByType<AuthoredActOneComposition>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(composition.IsConfigured, Is.True);
            Assert.That(composition.SectionCount, Is.EqualTo(3));
            Assert.That(composition.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Act I composition must remain presentation-only.");
            Assert.That(composition.transform.Find("Underdeck Aprons"), Is.Not.Null);
            Assert.That(composition.transform.Find("Shadow Backs"), Is.Not.Null);
            Assert.That(composition.transform.Find("Ceramic Braces"), Is.Not.Null);
            Assert.That(composition.Sections[0].sharedMaterial.name, Is.EqualTo("StationSteel"));
            Assert.That(composition.Sections[1].sharedMaterial.name, Is.EqualTo("StationBlack"));
            Assert.That(composition.Sections[2].sharedMaterial.name, Is.EqualTo("MaintenanceBulkhead"));

            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(-0.6f, -0.72f, 0.4f)), Is.True,
                "The Central Tower must own an underdeck apron.");
            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(-5.8f, -0.71f, 7.2f)), Is.True,
                "Relay Fork must own an underdeck apron.");
            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(9.7f, -0.71f, 6.3f)), Is.True,
                "Cargo Annex must own an underdeck apron.");
            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(10.4f, -0.71f, -4.8f)), Is.True,
                "Coolant Reclamation must own an underdeck apron.");
            Assert.That(composition.Sections[0].bounds.Contains(new Vector3(16.7f, -0.71f, 0f)), Is.True,
                "Transfer Vault must own an underdeck apron.");
        }

        [UnityTest]
        public IEnumerator ActOneMarkers_StayLocalToMachineryAfterCompositionPass()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var cargo = Object.FindFirstObjectByType<AuthoredCargoAnnexObjective>(FindObjectsInactive.Include);
            var coolant = Object.FindFirstObjectByType<AuthoredCoolantReclamationObjective>(FindObjectsInactive.Include);
            var relay = Object.FindFirstObjectByType<AuthoredRelayForkObjective>(FindObjectsInactive.Include);
            var vault = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            var installation = Object.FindFirstObjectByType<AuthoredCentralInstallationObjective>(FindObjectsInactive.Include);

            Assert.That(cargo.transform.Find("Cargo Commitment Marker").localScale.z, Is.LessThanOrEqualTo(1.5f));
            Assert.That(cargo.transform.Find("Power Coupling Secured Marker").localScale.x, Is.LessThanOrEqualTo(0.7f));
            Assert.That(coolant.transform.Find("Coolant Release Marker").localScale.x, Is.LessThanOrEqualTo(2.6f));
            Assert.That(coolant.transform.Find("Coolant Line Stable Marker").localScale.x, Is.LessThanOrEqualTo(0.7f));
            Assert.That(relay.transform.Find("Relay Feeds Routed").localScale.x, Is.LessThanOrEqualTo(2.5f));
            Assert.That(vault.transform.Find("Transfer Assembly Available").localScale.z, Is.LessThanOrEqualTo(1.4f));
            Assert.That(vault.transform.Find("Central Payload Assembled").localScale.z, Is.LessThanOrEqualTo(1.2f));
            Assert.That(installation.transform.Find("Central Payload Install Available/North Rail").localScale.x,
                Is.LessThanOrEqualTo(1.9f));
        }
    }
}
