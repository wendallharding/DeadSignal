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
    public sealed class SecurityWardenPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator WardenPresentation_CommunicatesLifecycleWithoutMovingCombatRoot()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var controller = Object.FindFirstObjectByType<SecurityWardenPresentation>();
            var warden = game.transform.Find("Security Warden");
            var chassis = warden.Find("Warden Chassis");
            var crown = warden.Find("Warden Crown");

            Assert.That(game.HasSecurityWardenPresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(warden.gameObject.activeSelf, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            yield return null;
            Assert.That(controller.IsWaking, Is.True,
                "Deployment should visibly wake the existing Warden model without delaying combat authority.");

            game.DebugSetThreatsFrozen(true);
            var rootPosition = warden.position;
            var rootScale = warden.localScale;
            var restChassisRotation = chassis.localRotation;
            var restCrownScale = crown.localScale;
            controller.SetThreatState(true, 1.2f);
            warden.position = game.DebugPlayerPosition + Vector3.forward * 1.35f;
            rootPosition = warden.position;
            yield return null;

            Assert.That(controller.IsScreening, Is.True);
            Assert.That(Quaternion.Angle(restChassisRotation, chassis.localRotation), Is.GreaterThan(0.1f),
                "Closing into contact range should brace the chassis before the authoritative strike.");
            Assert.That(crown.localScale.x, Is.GreaterThan(restCrownScale.x),
                "Warden-Sapper screening should widen the existing armor crown rather than add gameplay collision.");
            Assert.That(warden.position, Is.EqualTo(rootPosition));
            Assert.That(warden.localScale, Is.EqualTo(rootScale));

            game.DebugSetThreatsFrozen(false);
            game.DebugForceThreatAttack(SecurityReinforcement.Warden);
            warden.position = game.DebugPlayerPosition + Vector3.right * 0.5f;
            yield return null;
            Assert.That(controller.IsStriking, Is.True,
                "A resolved contact should begin bounded commit and recovery motion on the authored parts.");

            var health = game.WardenHealth;
            game.DebugDamageThreat(SecurityReinforcement.Warden);
            yield return null;
            Assert.That(game.WardenHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);

            game.DebugPurgeThreat(SecurityReinforcement.Warden);
            Assert.That(warden.gameObject.activeSelf, Is.False,
                "Purge presentation must not leave the gameplay target active after health reaches zero.");
            Assert.That(controller.IsPurgeVisible, Is.True,
                "The reusable mesh echo should carry the short purge motion after gameplay deactivation.");
            yield return new WaitForSeconds(0.5f);
            Assert.That(controller.IsPurgeVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            yield return null;
            Assert.That(warden.gameObject.activeSelf, Is.True);
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False,
                "Redeploying the Warden should reset every transient presentation layer.");
        }
    }
}
