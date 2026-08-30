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
    public sealed class SignalSapperPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SapperPresentation_CommunicatesLifecycleWithoutMovingCombatRoot()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var controller = Object.FindFirstObjectByType<SignalSapperPresentation>();
            var sapper = game.transform.Find("Signal Sapper");
            var leftFork = sapper.Find("Sapper Fork Left");
            var core = sapper.Find("Sapper Drain Core");

            Assert.That(game.HasSignalSapperPresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(sapper.gameObject.activeSelf, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            yield return null;
            Assert.That(controller.IsWaking, Is.True,
                "Deployment should visibly wake the existing Sapper model without delaying combat authority.");

            game.DebugSetThreatsFrozen(true);
            var rootPosition = sapper.position;
            var rootScale = sapper.localScale;
            var restForkPosition = leftFork.localPosition;
            controller.SetThreatState(true, 1f, 3f);
            yield return null;

            Assert.That(controller.IsLatchDeploying, Is.True);
            Assert.That(controller.IsTetherOwned, Is.True,
                "The latched actor pose should own the established Sapper-to-tower tether read.");
            Assert.That(leftFork.localPosition, Is.Not.EqualTo(restForkPosition),
                "Latch deployment should open the authored siphon fork without moving gameplay collision.");
            Assert.That(core.localScale, Is.Not.EqualTo(Vector3.zero));
            Assert.That(sapper.position, Is.EqualTo(rootPosition));
            Assert.That(sapper.localScale, Is.EqualTo(rootScale));

            game.DebugSetThreatsFrozen(false);
            game.DebugForceThreatAttack(SecurityReinforcement.Sapper);
            yield return null;
            Assert.That(controller.IsSiphonPulsing, Is.True,
                "A resolved drain should begin a bounded pulse on the authored parts.");

            var health = game.SapperHealth;
            game.DebugDamageThreat(SecurityReinforcement.Sapper);
            yield return null;
            Assert.That(game.SapperHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);
            Assert.That(controller.IsInterrupted, Is.True,
                "A nonlethal hit while latched should visibly interrupt siphon buildup.");

            game.DebugPurgeThreat(SecurityReinforcement.Sapper);
            Assert.That(sapper.gameObject.activeSelf, Is.False,
                "Purge presentation must not leave the gameplay target active after health reaches zero.");
            Assert.That(controller.IsPurgeVisible, Is.True,
                "The reusable four-part echo should carry the short purge motion after gameplay deactivation.");
            yield return new WaitForSeconds(0.5f);
            Assert.That(controller.IsPurgeVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            yield return null;
            Assert.That(sapper.gameObject.activeSelf, Is.True);
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False,
                "Redeploying the Sapper should reset every transient presentation layer.");
            Assert.That(controller.IsTetherOwned, Is.False);
        }
    }
}
