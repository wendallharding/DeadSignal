using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Player;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class ExtractionOutcomeFeedbackPlayModeTests
    {
        [UnityTest]
        public IEnumerator ExtractionLifecycle_UsesOneFixedOwnerAndAccessibilityBounds()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<ExtractionOutcomeFeedbackController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.HasTexture, Is.True);
            Assert.That(feedback.EventPoolSize, Is.EqualTo(2));
            Assert.That(feedback.OwnedObjectCount, Is.EqualTo(3),
                "One progress owner and two terminal event owners should be warmed once.");
            Assert.That(feedback.OwnedLineCount, Is.EqualTo(3),
                "Progress and terminal outcomes should each own one warmed hierarchy shape.");

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }

                game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
                yield return null;

                Assert.That(game.IsExtractionUplinkActive, Is.True);
                Assert.That(feedback.IsProgressActive, Is.True);
                Assert.That(feedback.LastKind, Is.EqualTo(ExtractionOutcomeFeedbackKind.ExtractionStart));
                var extractionPosition = feedback.LastPosition;
                Assert.That(extractionPosition, Is.EqualTo(game.DebugExtractionPosition));
                Assert.That(feedback.ProgressNormalized, Is.GreaterThanOrEqualTo(0f));
                Assert.That(feedback.CurrentMaximumAlpha, Is.LessThanOrEqualTo(0.14f));
                var progressLine = feedback.transform.Find("Extraction Uplink Progress/Uplink Progress Route")
                    ?.GetComponent<LineRenderer>();
                Assert.That(progressLine, Is.Not.Null);
                Assert.That(progressLine.enabled, Is.True);
                Assert.That(progressLine.positionCount, Is.EqualTo(5));

                var progressBefore = feedback.ProgressNormalized;
                yield return new WaitForSeconds(0.15f);
                Assert.That(feedback.ProgressNormalized, Is.GreaterThan(progressBefore));

                game.DebugCompleteExtraction();
                Assert.That(feedback.IsProgressActive, Is.False);
                Assert.That(feedback.LastKind, Is.EqualTo(ExtractionOutcomeFeedbackKind.ExtractionComplete));
                yield return null;

                Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
                Assert.That(feedback.LastOutcome, Is.EqualTo(RunOutcome.Victory));
                Assert.That(feedback.LastKind, Is.EqualTo(ExtractionOutcomeFeedbackKind.Victory));
                Assert.That(feedback.ActiveEventCount, Is.EqualTo(2),
                    "Extraction completion and victory should use the two warmed event layers.");
                Assert.That(feedback.CurrentMaximumAlpha, Is.LessThanOrEqualTo(0.26f));
                var activeOutcomeLines = System.Array.FindAll(
                    feedback.GetComponentsInChildren<LineRenderer>(true),
                    line => line.enabled && line.name == "Outcome Hierarchy Shape");
                Assert.That(activeOutcomeLines, Has.Length.EqualTo(2));
                Assert.That(System.Array.Exists(activeOutcomeLines, line => line.positionCount == 4), Is.True);
                Assert.That(System.Array.Exists(activeOutcomeLines, line => line.positionCount == 5), Is.True);

                var warmedObjectCount = feedback.OwnedObjectCount;
                for (var index = 0; index < 6; index++)
                {
                    feedback.PlayOutcome(RunOutcome.Victory, extractionPosition);
                }
                Assert.That(feedback.OwnedObjectCount, Is.EqualTo(warmedObjectCount));
                Assert.That(feedback.ActiveEventCount, Is.LessThanOrEqualTo(feedback.EventPoolSize));

                feedback.SetPaused(true);
                Assert.That(feedback.ActiveEventCount, Is.Zero);
                Assert.That(feedback.IsProgressActive, Is.False);
            }
            finally
            {
                feedback.SetPaused(false);
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
            }
        }

        [UnityTest]
        public IEnumerator Defeat_UsesPlayerLocalContractingOutcomeCue()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<ExtractionOutcomeFeedbackController>();
            var playerPosition = game.DebugPlayerPosition;

            game.DebugApplyScenario(DebugScenario.Failure);
            yield return null;

            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Destroyed));
            Assert.That(feedback.LastOutcome, Is.EqualTo(RunOutcome.Destroyed));
            Assert.That(feedback.LastKind, Is.EqualTo(ExtractionOutcomeFeedbackKind.Defeat));
            Assert.That(feedback.LastPosition, Is.EqualTo(playerPosition));
            Assert.That(feedback.ActiveEventCount, Is.EqualTo(1));
            var defeatLine = System.Array.Find(
                feedback.GetComponentsInChildren<LineRenderer>(true),
                line => line.enabled && line.name == "Outcome Hierarchy Shape");
            Assert.That(defeatLine, Is.Not.Null);
            Assert.That(defeatLine.positionCount, Is.EqualTo(4));
            Assert.That(defeatLine.loop, Is.False);
        }

        [UnityTest]
        public IEnumerator ExtractionProgress_RendersAtTheAuthoredDockWithReducedFlashes()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<ExtractionOutcomeFeedbackController>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            Assert.That(game, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }

                game.DebugTeleport(DebugLocation.Extraction);
                Object.FindFirstObjectByType<PlayerFollowCamera>().SnapToFocus(references.Player.position);
                yield return new WaitForSecondsRealtime(0.8f);
                game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
                yield return new WaitForSecondsRealtime(0.15f);

                Assert.That(feedback.IsProgressActive, Is.True);
                Assert.That(feedback.CurrentMaximumAlpha, Is.LessThanOrEqualTo(0.14f));
                Object.FindFirstObjectByType<PlayerFollowCamera>().SnapToFocus(game.DebugExtractionPosition);
                _captureIfRequested(
                    references.PlayerCamera,
                    "P51-Extraction-Uplink-Reduced-Flashes-1280x720.png",
                    1280,
                    720);
            }
            finally
            {
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
            }
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_P51_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                return;
            }

            Directory.CreateDirectory(captureDirectory);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(texture);
                Object.Destroy(renderTexture);
            }
        }
    }
}
