using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Presentation;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class StationStateFeedbackPlayModeTests
    {
        [UnityTest]
        public IEnumerator TowerInstallationAndShortcut_UseOneBoundedReusableFeedbackOwner()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<StationStateFeedbackController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.HasTexture, Is.True);
            Assert.That(feedback.PoolSize, Is.EqualTo(4));

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                if (!game.IsReducedFlashesEnabled)
                {
                    game.ToggleReducedFlashes();
                }

                game.DebugActivateTower();
                Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Tower));
                Assert.That(feedback.LastPosition, Is.EqualTo(game.TowerPosition));
                yield return null;
                Assert.That(feedback.CurrentAlpha, Is.InRange(0.01f, 0.28f));
                Assert.That(feedback.CurrentColor.r, Is.GreaterThan(feedback.CurrentColor.b),
                    "A resolved station mutation should begin in the established amber state.");
                yield return new WaitForSeconds(0.45f);
                Assert.That(feedback.CurrentColor.b, Is.GreaterThan(feedback.CurrentColor.r),
                    "The same fixed-owner glyph should resolve into the established cyan state.");

                feedback.Play(game.CentralInstallationPosition, StationStateFeedbackKind.Installation);
                Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Installation));
                Assert.That(feedback.LastPosition, Is.EqualTo(game.CentralInstallationPosition));

                feedback.Play(game.ShortcutPosition, StationStateFeedbackKind.Passage);
                Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Passage));
                Assert.That(feedback.LastPosition, Is.EqualTo(game.ShortcutPosition));
                Assert.That(feedback.ActiveCount, Is.LessThanOrEqualTo(feedback.PoolSize));
                Assert.That(feedback.PlayCount, Is.EqualTo(3));

                var pooledChildCount = feedback.transform.childCount;
                for (var index = 0; index < 8; index++)
                {
                    feedback.Play(game.TowerPosition + Vector3.right * index, StationStateFeedbackKind.Machinery);
                }
                Assert.That(feedback.transform.childCount, Is.EqualTo(pooledChildCount),
                    "Saturated station-state feedback should reuse its warmed objects.");
                Assert.That(feedback.ActiveCount, Is.EqualTo(feedback.PoolSize));
                Assert.That(feedback.PlayCount, Is.EqualTo(11));

                feedback.SetPaused(true);
                Assert.That(feedback.ActiveCount, Is.Zero);
                Assert.That(feedback.CurrentAlpha, Is.Zero);
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
    }
}
