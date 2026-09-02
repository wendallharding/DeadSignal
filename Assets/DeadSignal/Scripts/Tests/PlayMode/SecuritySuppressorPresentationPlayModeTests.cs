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
            var reducedFlashesWasEnabled = game.IsReducedFlashesEnabled;
            var highContrastWasEnabled = game.IsHighContrastEnabled;
            if (reducedFlashesWasEnabled) game.DebugToggleReducedFlashes();
            if (highContrastWasEnabled) game.DebugToggleHighContrast();

            Assert.That(game.HasSecuritySuppressorAssets, Is.True);
            Assert.That(game.HasSecuritySuppressorPresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(suppressor.gameObject.activeSelf, Is.False);
            Assert.That(chassis.GetComponent<MeshFilter>().sharedMesh.name, Does.StartWith("SecuritySuppressor"),
                "The Suppressor must use its purpose-built mesh rather than a Unity primitive.");
            Assert.That(chassis.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored Suppressor material must retain its original texture atlas.");
            var ownedEffects = game.GetComponentsInChildren<LineRenderer>(true)
                .Where(renderer => renderer.gameObject.name.StartsWith("Security Suppressor", StringComparison.Ordinal))
                .ToArray();
            Assert.That(ownedEffects, Has.Length.EqualTo(4),
                "Model, field, response, and purge effects should be prewarmed once by the Suppressor owner.");
            Assert.That(ownedEffects.Select(renderer => renderer.sharedMaterial).Distinct().Count(), Is.EqualTo(1),
                "Every Suppressor event should reuse one owner-scoped tintable material.");

            game.DebugSpawnThreat(SecurityReinforcement.Suppressor);
            yield return null;
            Assert.That(controller.IsWaking, Is.True);

            game.DebugSetThreatsFrozen(true);
            var rootPosition = suppressor.position;
            var rootScale = suppressor.localScale;
            var restEmitterPosition = emitter.localPosition;
            suppressor.position = game.DebugPlayerPosition + Vector3.right * 1.8f;
            rootPosition = suppressor.position;
            controller.SetFieldState(true, false, 2.3f, game.DebugPlayerPosition);
            yield return null;
            Assert.That(controller.IsWarning, Is.True);
            Assert.That(controller.IsStateEffectVisible, Is.True);
            Assert.That(controller.IsFieldEffectVisible, Is.True,
                "Forecast should reinforce only the field edge and leave the tactical center transparent.");
            Assert.That(emitter.localPosition, Is.Not.EqualTo(restEmitterPosition),
                "The warning should deploy the field projectors without moving gameplay collision.");
            Assert.That(suppressor.position, Is.EqualTo(rootPosition));
            Assert.That(suppressor.localScale, Is.EqualTo(rootScale));

            controller.SetFieldState(true, true, 2.3f, game.DebugPlayerPosition);
            yield return null;
            Assert.That(controller.IsProjecting, Is.True);
            Assert.That(controller.IsPlayerCaught, Is.True);
            Assert.That(controller.IsResponseEffectVisible, Is.True,
                "A caught player should receive a bounded directional response without filling the field center.");
            Assert.That(suppressor.position, Is.EqualTo(rootPosition));
            _captureWhenRequested("P48-Suppressor-Projection-1600x900.png", 1600, 900);
            yield return null;

            controller.SetFieldState(false, false, 2.3f, game.DebugPlayerPosition);
            yield return null;
            Assert.That(controller.IsShuttingDown, Is.True);
            Assert.That(controller.IsPlayerCaught, Is.False);
            Assert.That(controller.IsResponseEffectVisible, Is.True,
                "Leaving or shutting down the field should retain a short outward exit confirmation.");

            var health = game.SuppressorHealth;
            game.DebugDamageThreat(SecurityReinforcement.Suppressor);
            yield return null;
            Assert.That(game.SuppressorHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);

            game.DebugPurgeThreat(SecurityReinforcement.Suppressor);
            Assert.That(suppressor.gameObject.activeSelf, Is.False);
            Assert.That(controller.IsPurgeVisible, Is.True);
            Assert.That(controller.IsPurgeEffectVisible, Is.True);
            yield return new WaitForSeconds(0.45f);
            Assert.That(controller.IsPurgeVisible, Is.False);
            Assert.That(controller.IsPurgeEffectVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Suppressor);
            yield return null;
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False);

            game.DebugToggleReducedFlashes();
            game.DebugSetThreatsFrozen(true);
            suppressor.position = game.DebugPlayerPosition + Vector3.right * 1.8f;
            controller.SetFieldState(true, false, 2.3f, game.DebugPlayerPosition);
            yield return null;
            Assert.That(controller.MaximumEffectAlpha, Is.LessThanOrEqualTo(0.31f),
                "Reduced Flashes must preserve Suppressor shape and direction while capping opacity.");
            _captureWhenRequested("P48-Suppressor-Reduced-Flashes-1280x720.png", 1280, 720);

            if (!reducedFlashesWasEnabled) game.DebugToggleReducedFlashes();
            if (highContrastWasEnabled) game.DebugToggleHighContrast();
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalSuppressorCaptureDir=";
            var argument = Environment.GetCommandLineArgs()
                .FirstOrDefault(value => value.StartsWith(ARGUMENT_PREFIX, StringComparison.OrdinalIgnoreCase));
            if (argument == null) return;

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
