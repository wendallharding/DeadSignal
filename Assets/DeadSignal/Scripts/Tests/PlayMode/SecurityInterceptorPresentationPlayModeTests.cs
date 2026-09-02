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
            var reducedFlashesWasEnabled = game.IsReducedFlashesEnabled;
            var highContrastWasEnabled = game.IsHighContrastEnabled;
            if (reducedFlashesWasEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (highContrastWasEnabled)
            {
                game.DebugToggleHighContrast();
            }

            Assert.That(game.HasSecurityInterceptorAssets, Is.True);
            Assert.That(game.HasSecurityInterceptorPresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(interceptor.gameObject.activeSelf, Is.False);
            Assert.That(chassis.GetComponent<MeshFilter>().sharedMesh.name, Does.StartWith("SecurityInterceptor"),
                "The Interceptor must use its purpose-built mesh rather than a Unity primitive.");
            Assert.That(chassis.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored Interceptor material must retain its original texture atlas.");
            var ownedEffects = game.GetComponentsInChildren<LineRenderer>(true)
                .Where(renderer => renderer.gameObject.name.StartsWith("Security Interceptor", StringComparison.Ordinal))
                .ToArray();
            Assert.That(ownedEffects, Has.Length.EqualTo(3),
                "State, event, and purge effects should be prewarmed once by the Interceptor presentation owner.");
            Assert.That(ownedEffects.Select(renderer => renderer.sharedMaterial).Distinct().Count(), Is.EqualTo(1),
                "Every Interceptor event should reuse one owner-scoped tintable material.");

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
            Assert.That(controller.IsStateEffectVisible, Is.True,
                "Target lock should place directional rails around the model without replacing the floor charge line.");
            Assert.That(blade.localPosition, Is.Not.EqualTo(restBladePosition),
                "Charge lock should spread the rail silhouette without moving gameplay collision.");
            Assert.That(interceptor.position, Is.EqualTo(rootPosition));
            Assert.That(interceptor.localScale, Is.EqualTo(rootScale));

            controller.SetThreatState(false, true);
            yield return null;
            Assert.That(controller.IsDashCommitted, Is.True);
            Assert.That(controller.IsStateEffectVisible, Is.True,
                "The committed dash should retain a narrow forward point and trailing wake around the model.");
            Assert.That(interceptor.position, Is.EqualTo(rootPosition));

            interceptor.position = game.DebugPlayerPosition + Vector3.right * 1.4f;
            rootPosition = interceptor.position;
            controller.PlayRecovery(true, 0.5f);
            yield return null;
            Assert.That(controller.IsRecovering, Is.True);
            Assert.That(controller.IsCoverCrash, Is.True,
                "A cover collision should use the stronger recovery grammar while gameplay timing remains authoritative.");
            Assert.That(controller.IsRecoveryOpeningVisible, Is.True,
                "Recovery should open the directional rails into an amber counterattack window.");
            Assert.That(controller.IsEventEffectVisible, Is.True,
                "A cover crash should add a bounded break glyph without covering perpendicular escape space.");
            Assert.That(interceptor.position, Is.EqualTo(rootPosition));
            _captureWhenRequested("P47-Interceptor-Crash-Recovery-1600x900.png", 1600, 900);

            var health = game.InterceptorHealth;
            game.DebugDamageThreat(SecurityReinforcement.Interceptor);
            yield return null;
            Assert.That(game.InterceptorHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);
            Assert.That(controller.IsEventEffectVisible, Is.True,
                "Nonlethal armor contact should retain source direction around the finished model.");

            game.DebugPurgeThreat(SecurityReinforcement.Interceptor);
            Assert.That(interceptor.gameObject.activeSelf, Is.False);
            Assert.That(controller.IsPurgeVisible, Is.True);
            Assert.That(controller.IsPurgeEffectVisible, Is.True,
                "Purge should add a bounded release ring around the reusable mesh collapse.");
            yield return new WaitForSeconds(0.45f);
            Assert.That(controller.IsPurgeVisible, Is.False);
            Assert.That(controller.IsPurgeEffectVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Interceptor);
            yield return null;
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False);

            game.DebugToggleReducedFlashes();

            game.DebugSetThreatsFrozen(true);
            interceptor.position = game.DebugPlayerPosition + Vector3.right * 1.4f;
            controller.SetThreatState(true, false);
            yield return null;
            Assert.That(controller.MaximumEffectAlpha, Is.LessThanOrEqualTo(0.31f),
                "Reduced Flashes must retain Interceptor direction while capping transient opacity.");
            _captureWhenRequested("P47-Interceptor-Reduced-Flashes-1280x720.png", 1280, 720);

            if (!reducedFlashesWasEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (highContrastWasEnabled)
            {
                game.DebugToggleHighContrast();
            }
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalInterceptorCaptureDir=";
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
