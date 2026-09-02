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
            var reducedFlashesWasEnabled = game.IsReducedFlashesEnabled;
            if (reducedFlashesWasEnabled)
            {
                game.DebugToggleReducedFlashes();
            }

            Assert.That(game.HasSignalSapperPresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(sapper.gameObject.activeSelf, Is.False);
            var ownedEffects = game.GetComponentsInChildren<LineRenderer>(true)
                .Where(renderer => renderer.gameObject.name.StartsWith("Signal Sapper", StringComparison.Ordinal))
                .ToArray();
            Assert.That(ownedEffects, Has.Length.EqualTo(3),
                "Acquisition, drain, and purge effects should be prewarmed once by the Sapper presentation owner.");
            Assert.That(ownedEffects.Select(renderer => renderer.sharedMaterial).Distinct().Count(), Is.EqualTo(1),
                "Every Sapper event should reuse one owner-scoped tintable material.");

            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            yield return null;
            Assert.That(controller.IsWaking, Is.True,
                "Deployment should visibly wake the existing Sapper model without delaying combat authority.");
            Assert.That(controller.IsStateEffectVisible, Is.True,
                "Emergence and target acquisition should retain a bounded directional bracket around the finished model.");

            game.DebugSetThreatsFrozen(true);
            sapper.position = game.DebugPlayerPosition + Vector3.right * 1.8f;
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
            sapper.position = game.DebugPlayerPosition + Vector3.right * 1.8f;
            yield return null;
            Assert.That(controller.IsSiphonPulsing, Is.True,
                "A resolved drain should begin a bounded pulse on the authored parts.");
            Assert.That(controller.IsDrainEffectVisible, Is.True,
                "A successful drain should add a closed release glyph distinct from interrupted buildup.");
            yield return new WaitForSeconds(0.12f);
            _captureWhenRequested("P46-Sapper-Latch-Drain-1600x900.png", 1600, 900);

            var health = game.SapperHealth;
            game.DebugDamageThreat(SecurityReinforcement.Sapper);
            yield return null;
            Assert.That(game.SapperHealth, Is.EqualTo(health - 1f));
            Assert.That(controller.IsHitReacting, Is.True);
            Assert.That(controller.IsInterrupted, Is.True,
                "A nonlethal hit while latched should visibly interrupt siphon buildup.");
            Assert.That(controller.IsInterruptedEffectVisible, Is.True,
                "Interrupted buildup should retain an open amber break shape around the Sapper model.");

            game.DebugPurgeThreat(SecurityReinforcement.Sapper);
            Assert.That(sapper.gameObject.activeSelf, Is.False,
                "Purge presentation must not leave the gameplay target active after health reaches zero.");
            Assert.That(controller.IsPurgeVisible, Is.True,
                "The reusable four-part echo should carry the short purge motion after gameplay deactivation.");
            Assert.That(controller.IsPurgeEffectVisible, Is.True,
                "Purge should add a bounded magenta release ring around the existing collapse.");
            yield return new WaitForSeconds(0.5f);
            Assert.That(controller.IsPurgeVisible, Is.False);
            Assert.That(controller.IsPurgeEffectVisible, Is.False);

            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            yield return null;
            Assert.That(sapper.gameObject.activeSelf, Is.True);
            Assert.That(controller.IsWaking, Is.True);
            Assert.That(controller.IsPurgeVisible, Is.False,
                "Redeploying the Sapper should reset every transient presentation layer.");
            Assert.That(controller.IsTetherOwned, Is.False);

            if (!game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }

            sapper.position = game.DebugPlayerPosition + Vector3.right * 1.8f;
            controller.SetThreatState(true, 0.3f, 3f);
            yield return null;
            Assert.That(controller.MaximumEffectAlpha, Is.LessThanOrEqualTo(0.31f),
                "Reduced Flashes must retain Sapper shape and direction while capping transient opacity.");
            _captureWhenRequested("P46-Sapper-Reduced-Flashes-1280x720.png", 1280, 720);

            if (!reducedFlashesWasEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalSapperCaptureDir=";
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
