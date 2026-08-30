using System.Collections;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class SecuritySwarmerPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SwarmerPresentation_CommunicatesPressureContactAndPurgeWithoutMovingCombatRoot()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            game.DebugApplyScenario(DebugScenario.EasternRoomCombat);
            yield return null;
            yield return null;

            var swarmer = game.transform.Find("Security Swarmer 1");
            var controller = swarmer.GetComponent<SecuritySwarmerPresentation>();
            var body = swarmer.Find("Swarmer Body");
            var needle = swarmer.Find("Swarmer Needle");

            Assert.That(game.HasSwarmerAssets, Is.True);
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(3));
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(body.GetComponent<MeshFilter>().sharedMesh.name, Does.StartWith("SecuritySwarmer"),
                "The Swarmer must use its purpose-built mesh rather than a Unity primitive.");
            Assert.That(body.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored Swarmer material must retain its original texture atlas.");
            Assert.That(swarmer.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Presentation geometry must remain independent from the deterministic collision radius.");

            game.DebugSetThreatsFrozen(true);
            var rootPosition = swarmer.position;
            var rootScale = swarmer.localScale;
            var restNeedlePosition = needle.localPosition;
            controller.SetPressure(1f);
            yield return new WaitForSeconds(0.12f);
            Assert.That(controller.Pressure, Is.GreaterThan(0f));
            Assert.That(needle.localPosition, Is.Not.EqualTo(restNeedlePosition),
                "Convergence pressure should extend the contact silhouette without moving gameplay collision.");
            Assert.That(swarmer.position, Is.EqualTo(rootPosition));
            Assert.That(swarmer.localScale, Is.EqualTo(rootScale));

            controller.PlayContact();
            yield return null;
            Assert.That(controller.IsContactReacting, Is.True);
            Assert.That(swarmer.position, Is.EqualTo(rootPosition));

            game.DebugPurgeSwarmers();
            Assert.That(game.ActiveSwarmerCount, Is.Zero);
            Assert.That(controller.IsPurgeVisible, Is.True);
            Assert.That(swarmer.gameObject.activeSelf, Is.True,
                "The non-authoritative root may remain visible only for the bounded purge collapse.");
            yield return new WaitForSeconds(0.32f);
            Assert.That(controller.IsPurgeVisible, Is.False);
            Assert.That(swarmer.gameObject.activeSelf, Is.False);
        }
    }
}
