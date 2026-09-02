using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Combat;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class CombatFeedbackPoolingPlayModeTests
    {
        [UnityTest]
        public IEnumerator ImpactPurgeAndChainFeedback_ReusesBoundedPoolsAndClearsOnPause()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<CombatFeedbackController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.ImpactPoolSize, Is.EqualTo(12));
            Assert.That(feedback.SparkPoolSize, Is.EqualTo(12));
            Assert.That(feedback.ChainArcPoolSize, Is.EqualTo(6));
            Assert.That(feedback.CreatedPooledObjectCount, Is.EqualTo(30));
            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            var initialHighContrast = game.IsHighContrastEnabled;

            var origin = game.transform.position + Vector3.up * 0.5f;
            if (game.IsHighContrastEnabled)
            {
                game.DebugToggleHighContrast();
            }
            if (game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            _playImpactLineup(feedback, origin);
            _captureWhenRequested("P44-Hit-Purge-Language-1600x900.png", 1600, 900);
            feedback.SetPaused(true);
            feedback.SetPaused(false);

            for (var index = 0; index < 20; index++)
            {
                feedback.PlayEnvironmentImpact(origin + Vector3.right * index * 0.01f);
            }

            for (var index = 0; index < 12; index++)
            {
                feedback.PlayChainArc(origin, origin + Vector3.right * (1f + index * 0.05f));
            }

            Assert.That(feedback.ImpactPoolSize, Is.EqualTo(16));
            Assert.That(feedback.SparkPoolSize, Is.EqualTo(16));
            Assert.That(feedback.ChainArcPoolSize, Is.EqualTo(8));
            Assert.That(feedback.ActiveImpactCount, Is.EqualTo(16));
            Assert.That(feedback.ActiveSparkCount, Is.EqualTo(16));
            Assert.That(feedback.ActiveChainArcCount, Is.EqualTo(8));
            Assert.That(feedback.CreatedPooledObjectCount, Is.EqualTo(40));

            var threat = new GameObject("Purge Reaction Target");
            try
            {
                threat.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);
                feedback.PlaySignalImpact(origin, true);
                feedback.PlayThreatReaction(threat.transform);
                Assert.That(feedback.LastImpactLanguage, Is.EqualTo(CombatFeedbackController.ImpactLanguage.Purge));
                Assert.That(_findActiveGlyph(feedback, "Combat Impact Burst").positionCount, Is.EqualTo(8),
                    "A purge should use the starburst silhouette instead of reading as another ordinary hit.");
                Assert.That(feedback.ActiveImpactCount, Is.EqualTo(16),
                    "A decisive purge should replace ordinary saturated visuals without exceeding the cap.");
                Assert.That(feedback.ActiveThreatReactionCount, Is.EqualTo(1));

                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }

                feedback.SetPaused(true);
                Assert.That(feedback.ActiveImpactCount, Is.Zero);
                Assert.That(feedback.ActiveSparkCount, Is.Zero);
                Assert.That(feedback.ActiveChainArcCount, Is.Zero);
                Assert.That(feedback.ActiveThreatReactionCount, Is.Zero);
                Assert.That(threat.transform.localScale, Is.EqualTo(new Vector3(1.2f, 1.1f, 1.2f)));
                feedback.SetPaused(false);

                feedback.PlayShieldImpact(origin);
                Assert.That(feedback.LastImpactLanguage, Is.EqualTo(CombatFeedbackController.ImpactLanguage.ShieldHit));
                var activeShieldSprites = 0;
                for (var index = 0; index < feedback.transform.childCount; index++)
                {
                    var child = feedback.transform.GetChild(index);
                    if (!child.gameObject.activeSelf || child.name != "Combat Impact Burst" ||
                        !child.TryGetComponent<SpriteRenderer>(out var renderer))
                    {
                        continue;
                    }

                    activeShieldSprites++;
                    Assert.That(renderer.color.a, Is.LessThanOrEqualTo(0.31f),
                        "Reduced Flashes must cap both layers of the shield read.");
                    var glyph = child.GetComponent<LineRenderer>();
                    Assert.That(glyph.enabled, Is.True);
                    Assert.That(glyph.loop, Is.True);
                    Assert.That(glyph.positionCount, Is.EqualTo(6),
                        "Shield blocks should retain a closed hexagonal silhouette independent of color.");
                    Assert.That(glyph.startColor.a, Is.LessThanOrEqualTo(0.31f));
                }

                Assert.That(activeShieldSprites, Is.EqualTo(2),
                    "The shield read should remain a distinct two-layer cyan/white confirmation.");
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                _playImpactLineup(feedback, origin);
                _captureWhenRequested("P44-Hit-Purge-Reduced-Flashes-1280x720.png", 1280, 720);

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.PlaySecurityImpact(origin);
                var securityTint = _findActiveSprite(feedback, "Combat Impact Burst").color;
                Assert.That(securityTint.r, Is.GreaterThan(securityTint.b + 0.5f),
                    "Player damage should retain a dominant red read.");

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.PlaySapperImpact(origin);
                var sapperTint = _findActiveSprite(feedback, "Combat Impact Burst").color;
                Assert.That(sapperTint.b, Is.GreaterThan(securityTint.b + 0.5f),
                    "Sapper damage should remain magenta rather than reading as ordinary red damage.");

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.PlayArmorImpact(origin);
                Assert.That(feedback.LastImpactLanguage, Is.EqualTo(CombatFeedbackController.ImpactLanguage.ArmorHit));
                Assert.That(_findActiveGlyph(feedback, "Combat Impact Burst").positionCount, Is.EqualTo(5),
                    "Surviving specialist armor should use a bracketed impact silhouette.");

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.PlayEnvironmentImpact(origin);
                Assert.That(feedback.LastImpactLanguage, Is.EqualTo(CombatFeedbackController.ImpactLanguage.WallHit));
                var wallTint = _findActiveSprite(feedback, "Bulkhead Signal Impact").color;
                Assert.That(wallTint.g, Is.GreaterThan(securityTint.g + 0.3f));
                Assert.That(wallTint.b, Is.LessThan(0.3f),
                    "Blocked bolts should retain an amber bulkhead read distinct from threats and shields.");
                Assert.That(_findActiveGlyph(feedback, "Bulkhead Signal Impact").positionCount, Is.EqualTo(4));

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.PlaySignalRecovery(origin);
                Assert.That(feedback.LastImpactLanguage, Is.EqualTo(CombatFeedbackController.ImpactLanguage.BountyRecovery));
                Assert.That(_findActiveGlyph(feedback, "Signal Recovery Burst").positionCount, Is.EqualTo(5));

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.PlaySalvageChain(origin, 3);
                Assert.That(feedback.LastImpactLanguage, Is.EqualTo(CombatFeedbackController.ImpactLanguage.ChainRecovery));
                Assert.That(_findActiveGlyph(feedback, "Salvage Chain Burst").positionCount, Is.EqualTo(5));

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                for (var index = 0; index < 40; index++)
                {
                    feedback.PlayEnvironmentImpact(origin);
                }

                var createdAfterWarmup = feedback.CreatedPooledObjectCount;
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (var index = 0; index < 64; index++)
                {
                    feedback.PlayEnvironmentImpact(origin);
                }

                var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                Assert.That(feedback.CreatedPooledObjectCount, Is.EqualTo(createdAfterWarmup),
                    "Continuous impacts must reuse the saturated pool without creating Unity objects or components.");
                Assert.That(allocatedAfter - allocatedBefore, Is.Zero,
                    "Continuous impact playback must not allocate managed memory after pool warmup.");

                feedback.SetPaused(true);
                Assert.That(feedback.ActiveImpactCount, Is.Zero);
                Assert.That(feedback.ActiveSparkCount, Is.Zero);
                feedback.SetPaused(false);
            }
            finally
            {
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
                if (game.IsHighContrastEnabled != initialHighContrast)
                {
                    game.DebugToggleHighContrast();
                }

                Object.Destroy(threat);
            }

            var previousFeedback = feedback;
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var restartedFeedback = Object.FindFirstObjectByType<CombatFeedbackController>();
            Assert.That(restartedFeedback, Is.Not.Null);
            Assert.That(restartedFeedback, Is.Not.SameAs(previousFeedback));
            Assert.That(Object.FindObjectsByType<CombatFeedbackController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(restartedFeedback.ImpactPoolSize, Is.EqualTo(12));
            Assert.That(restartedFeedback.SparkPoolSize, Is.EqualTo(12));
            Assert.That(restartedFeedback.ChainArcPoolSize, Is.EqualTo(6));
            Assert.That(restartedFeedback.ActiveImpactCount, Is.Zero);
            Assert.That(restartedFeedback.ActiveSparkCount, Is.Zero);
            Assert.That(restartedFeedback.ActiveChainArcCount, Is.Zero);
        }

        private static SpriteRenderer _findActiveSprite(CombatFeedbackController feedback, string objectName)
        {
            for (var index = 0; index < feedback.transform.childCount; index++)
            {
                var child = feedback.transform.GetChild(index);
                if (child.gameObject.activeSelf && child.name == objectName &&
                    child.TryGetComponent<SpriteRenderer>(out var renderer))
                {
                    return renderer;
                }
            }

            Assert.Fail($"No active pooled sprite named '{objectName}' was found.");
            return null;
        }

        private static LineRenderer _findActiveGlyph(CombatFeedbackController feedback, string objectName)
        {
            for (var index = 0; index < feedback.transform.childCount; index++)
            {
                var child = feedback.transform.GetChild(index);
                if (child.gameObject.activeSelf && child.name == objectName &&
                    child.TryGetComponent<LineRenderer>(out var renderer) && renderer.enabled)
                {
                    return renderer;
                }
            }

            Assert.Fail($"No active pooled glyph named '{objectName}' was found.");
            return null;
        }

        private static void _playImpactLineup(CombatFeedbackController feedback, Vector3 origin)
        {
            feedback.PlayArmorImpact(origin + Vector3.left * 2.4f);
            feedback.PlayEnvironmentImpact(origin + Vector3.left * 0.8f);
            feedback.PlayShieldImpact(origin + Vector3.right * 0.8f);
            feedback.PlaySignalImpact(origin + Vector3.right * 2.4f, true);
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalHitPurgeCaptureDir=";
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
