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
    public sealed class SecurityInterceptorPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator InterceptorPresentation_CommunicatesChargeCrashAndPurgeWithoutMovingCombatRoot()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var controller = Object.FindFirstObjectByType<SecurityInterceptorPresentation>();
            var interceptor = game.transform.Find("Security Interceptor");
            var chassis = interceptor.Find("Interceptor Chassis");
            var blade = interceptor.Find("Interceptor Blade Left");

            Assert.That(game.HasSecurityInterceptorAssets, Is.True);
            Assert.That(game.HasSecurityInterceptorPresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(interceptor.gameObject.activeSelf, Is.False);
            Assert.That(chassis.GetComponent<MeshFilter>().sharedMesh.name, Does.StartWith("SecurityInterceptor"),
                "The Interceptor must use its purpose-built mesh rather than a Unity primitive.");
            Assert.That(chassis.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored Interceptor material must retain its original texture atlas.");

            game.DebugSpawnThreat(SecurityReinforcement.Interceptor);
            yield return null;
            Assert.That(controller.IsWaking, Is.True);

            game.DebugSetThreatsFrozen(true);
            var rootPosition = interceptor.position;
            var rootScale = interceptor.localScale;
            var restBladePosition = blade.localPosition;
            controller.SetThreatState(true, false);
            yield return null;
            Assert.That(controller.IsChargeLocked, Is.True);
            Assert.That(blade.localPosition, Is.Not.EqualTo(restBladePosition),
                "Charge lock should spread the rail silhouette without moving gameplay collision.");
            Assert.That(interceptor.position, Is.EqualTo(rootPosition));
            Assert.That(interceptor.localScale, Is.EqualTo(rootScale));

            controller.SetThreatState(false, true);
            yield return null;
            Assert.That(controller.IsDashCommitted, Is.True);
            Assert.That(interceptor.position, Is.EqualTo(rootPosition));

            controller.PlayRecovery(true, 0.5f);
            yield return null;
            Assert.That(controller.IsRecovering, Is.True);
            Assert.That(controller.IsCoverCrash, Is.True,
                "A cover collision should use the stronger recovery grammar while gameplay timing remains authoritative.");

            var health = game.InterceptorHealth;
            game.DebugDamageThreat(SecurityReinforcement.Interceptor);
            yield return null;
            Assert.That(game.InterceptorHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);

            game.DebugPurgeThreat(SecurityReinforcement.Interceptor);
            Assert.That(interceptor.gameObject.activeSelf, Is.False);
            Assert.That(controller.IsPurgeVisible, Is.True);
            yield return new WaitForSeconds(0.45f);
            Assert.That(controller.IsPurgeVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Interceptor);
            yield return null;
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False);
        }
    }
}
