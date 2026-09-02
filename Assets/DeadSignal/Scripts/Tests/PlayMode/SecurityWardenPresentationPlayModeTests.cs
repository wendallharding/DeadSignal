using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
            var ownedEffects = game.GetComponentsInChildren<LineRenderer>(true)
                .Where(renderer => renderer.gameObject.name.StartsWith("Security Warden", StringComparison.Ordinal))
                .ToArray();
            Assert.That(ownedEffects, Has.Length.EqualTo(3),
                "Wake, contact, and purge effects should be prewarmed once by the Warden presentation owner.");
            Assert.That(ownedEffects.Select(renderer => renderer.sharedMaterial).Distinct().Count(), Is.EqualTo(1),
                "Every Warden event should reuse one owner-scoped tintable material.");

            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            yield return null;
            Assert.That(controller.IsWaking, Is.True,
                "Deployment should visibly wake the existing Warden model without delaying combat authority.");
            Assert.That(controller.IsStrikeEffectVisible, Is.True,
                "Wake-up should frame the finished Warden model with a bounded red state effect.");

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
            Assert.That(controller.IsContactEffectVisible, Is.True,
                "The authoritative contact should add a short directional seam without moving the combat root.");

            yield return new WaitForSeconds(0.18f);
            Assert.That(controller.IsRecoveryOpeningVisible, Is.True,
                "The post-contact recovery should visibly open after the unchanged strike commit.");
            _captureWhenRequested("P45-Warden-Strike-Recovery-1600x900.png", 1600, 900);

            var health = game.WardenHealth;
            game.DebugDamageThreat(SecurityReinforcement.Warden);
            yield return null;
            Assert.That(game.WardenHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);
            Assert.That(controller.IsContactEffectVisible, Is.True,
                "Armor response should stay anchored to the finished model and retain hit direction.");

            game.DebugPurgeThreat(SecurityReinforcement.Warden);
            Assert.That(warden.gameObject.activeSelf, Is.False,
                "Purge presentation must not leave the gameplay target active after health reaches zero.");
            Assert.That(controller.IsPurgeVisible, Is.True,
                "The reusable mesh echo should carry the short purge motion after gameplay deactivation.");
            Assert.That(controller.IsPurgeEffectVisible, Is.True,
                "Purge should add a bounded red release ring around the existing collapse.");
            yield return new WaitForSeconds(0.5f);
            Assert.That(controller.IsPurgeVisible, Is.False);
            Assert.That(controller.IsPurgeEffectVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            yield return null;
            Assert.That(warden.gameObject.activeSelf, Is.True);
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False,
                "Redeploying the Warden should reset every transient presentation layer.");

            var reducedFlashesWasEnabled = game.IsReducedFlashesEnabled;
            if (!reducedFlashesWasEnabled)
            {
                game.DebugToggleReducedFlashes();
            }

            warden.position = game.DebugPlayerPosition + Vector3.right * 0.7f;
            game.DebugForceThreatAttack(SecurityReinforcement.Warden);
            yield return null;
            Assert.That(controller.MaximumEffectAlpha, Is.LessThanOrEqualTo(0.31f),
                "Reduced Flashes must retain Warden shape language while capping transient opacity.");
            _captureWhenRequested("P45-Warden-Reduced-Flashes-1280x720.png", 1280, 720);

            if (!reducedFlashesWasEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalWardenCaptureDir=";
            var argument = Environment.GetCommandLineArgs()
                .FirstOrDefault(value => value.StartsWith(ARGUMENT_PREFIX, StringComparison.OrdinalIgnoreCase));
            if (argument == null)
            {
                return;
            }

            var directory = argument.Substring(ARGUMENT_PREFIX.Length).Trim('"');
            Directory.CreateDirectory(directory);
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(candidate => candidate.enabled);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = camera.aspect;
            try
            {
                camera.aspect = (float)width / height;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                var capture = new Texture2D(width, height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply();
                File.WriteAllBytes(Path.Combine(directory, fileName), capture.EncodeToPNG());
                Object.DestroyImmediate(capture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
