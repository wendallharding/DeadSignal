using System.Collections;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class SecuritySuppressorPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SuppressorPresentation_CommunicatesFieldLifecycleWithoutMovingCombatRoot()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var controller = Object.FindFirstObjectByType<SecuritySuppressorPresentation>();
            var suppressor = game.transform.Find("Security Suppressor");
            var chassis = suppressor.Find("Suppressor Chassis");
            var emitter = suppressor.Find("Suppressor Emitter Left");

            Assert.That(game.HasSecuritySuppressorAssets, Is.True);
            Assert.That(game.HasSecuritySuppressorPresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(suppressor.gameObject.activeSelf, Is.False);
            Assert.That(chassis.GetComponent<MeshFilter>().sharedMesh.name, Does.StartWith("SecuritySuppressor"),
                "The Suppressor must use its purpose-built mesh rather than a Unity primitive.");
            Assert.That(chassis.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored Suppressor material must retain its original texture atlas.");

            game.DebugSpawnThreat(SecurityReinforcement.Suppressor);
            yield return null;
            Assert.That(controller.IsWaking, Is.True);

            game.DebugSetThreatsFrozen(true);
            var rootPosition = suppressor.position;
            var rootScale = suppressor.localScale;
            var restEmitterPosition = emitter.localPosition;
            controller.SetFieldState(true, false);
            yield return null;
            Assert.That(controller.IsWarning, Is.True);
            Assert.That(emitter.localPosition, Is.Not.EqualTo(restEmitterPosition),
                "The warning should deploy the field projectors without moving gameplay collision.");
            Assert.That(suppressor.position, Is.EqualTo(rootPosition));
            Assert.That(suppressor.localScale, Is.EqualTo(rootScale));

            controller.SetFieldState(true, true);
            yield return null;
            Assert.That(controller.IsProjecting, Is.True);
            Assert.That(suppressor.position, Is.EqualTo(rootPosition));

            controller.SetFieldState(false, false);
            yield return null;
            Assert.That(controller.IsShuttingDown, Is.True);

            var health = game.SuppressorHealth;
            game.DebugDamageThreat(SecurityReinforcement.Suppressor);
            yield return null;
            Assert.That(game.SuppressorHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);

            game.DebugPurgeThreat(SecurityReinforcement.Suppressor);
            Assert.That(suppressor.gameObject.activeSelf, Is.False);
            Assert.That(controller.IsPurgeVisible, Is.True);
            yield return new WaitForSeconds(0.45f);
            Assert.That(controller.IsPurgeVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Suppressor);
            yield return null;
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False);
        }
    }
}
