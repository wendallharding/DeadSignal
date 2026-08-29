using System.Collections;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Presentation;
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

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                if (!game.IsReducedFlashesEnabled)
                {
                    game.ToggleReducedFlashes();
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
                    game.ToggleReducedFlashes();
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
        }
    }
}
