using System;
using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class StationStateFeedbackPlayModeTests
    {
        [UnityTest]
        public IEnumerator SalvageCollection_UsesRecoveryFeedbackWithoutChangingRewardRules()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<StationStateFeedbackController>();
            game.DebugActivateTower();
            var signalBefore = game.CurrentSignal;

            game.DebugCollectNextCache();

            Assert.That(game.CurrentSalvage, Is.GreaterThan(0));
            Assert.That(game.CurrentSignal, Is.EqualTo(signalBefore),
                "Presentation must not award Signal above the existing capped salvage rule.");
            Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Recovery));
            Assert.That(feedback.ActiveCount, Is.LessThanOrEqualTo(feedback.PoolSize));
        }

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
            var ownedLines = Array.FindAll(
                feedback.GetComponentsInChildren<LineRenderer>(true),
                line => line.name is "Primary Transition Shape" or "Detail Transition Shape");
            Assert.That(ownedLines, Has.Length.EqualTo(8),
                "Each warmed slot should own two reusable transition-shape layers.");

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }

                game.DebugActivateTower();
                Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Tower));
                Assert.That(feedback.LastPosition, Is.EqualTo(game.TowerPosition));
                yield return null;
                Assert.That(feedback.CurrentAlpha, Is.InRange(0.01f, 0.28f));
                Assert.That(feedback.CurrentColor.r, Is.GreaterThan(feedback.CurrentColor.b),
                    "A resolved station mutation should begin in the established amber state.");
                _assertActiveShape(feedback, 12, 4);
                yield return new WaitForSeconds(0.45f);
                Assert.That(feedback.CurrentColor.b, Is.GreaterThan(feedback.CurrentColor.r),
                    "The same fixed-owner glyph should resolve into the established cyan state.");

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.CentralInstallationPosition, StationStateFeedbackKind.Installation);
                Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Installation));
                Assert.That(feedback.LastPosition, Is.EqualTo(game.CentralInstallationPosition));
                yield return null;
                _assertActiveShape(feedback, 8, 4);

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.ShortcutPosition, StationStateFeedbackKind.Passage);
                Assert.That(feedback.LastKind, Is.EqualTo(StationStateFeedbackKind.Passage));
                Assert.That(feedback.LastPosition, Is.EqualTo(game.ShortcutPosition));
                Assert.That(feedback.ActiveCount, Is.LessThanOrEqualTo(feedback.PoolSize));
                yield return null;
                _assertActiveShape(feedback, 5, 5);

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.TowerPosition, StationStateFeedbackKind.LockdownEntry);
                Assert.That(feedback.CurrentColor.r, Is.GreaterThan(feedback.CurrentColor.g + 0.5f));
                yield return null;
                _assertActiveShape(feedback, 8, 4);
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.TowerPosition, StationStateFeedbackKind.TrialCommitment);
                yield return null;
                _assertActiveShape(feedback, 5, 5);
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.TowerPosition, StationStateFeedbackKind.PhaseTransition);
                yield return null;
                _assertActiveShape(feedback, 6, 3);
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.TowerPosition, StationStateFeedbackKind.RoomClear);
                yield return null;
                _assertActiveShape(feedback, 5, 4);
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.TowerPosition, StationStateFeedbackKind.RewardRelease);
                Assert.That(feedback.CurrentColor.r, Is.GreaterThan(feedback.CurrentColor.b));
                yield return null;
                _assertActiveShape(feedback, 4, 4);
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.TowerPosition, StationStateFeedbackKind.Recovery);
                Assert.That(feedback.CurrentColor.g, Is.GreaterThan(feedback.CurrentColor.r));
                yield return null;
                _assertActiveShape(feedback, 4, 4);
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.TowerPosition, StationStateFeedbackKind.DepartureSurge);
                yield return null;
                _assertActiveShape(feedback, 6, 4);

                feedback.SetPaused(true);
                feedback.SetPaused(false);
                feedback.Play(game.BreakerResetPosition, StationStateFeedbackKind.Machinery);
                yield return null;
                _assertActiveShape(feedback, 6, 5);
                var playCountBeforeDuplicate = feedback.PlayCount;
                feedback.Play(game.BreakerResetPosition, StationStateFeedbackKind.Machinery);
                Assert.That(feedback.PlayCount, Is.EqualTo(playCountBeforeDuplicate),
                    "One resolved mutation must not stack duplicate transition layers at the same position.");
                Assert.That(feedback.SuppressedDuplicateCount, Is.EqualTo(1));

                var pooledChildCount = feedback.transform.childCount;
                for (var index = 0; index < 8; index++)
                {
                    feedback.Play(game.TowerPosition + Vector3.right * index, StationStateFeedbackKind.Machinery);
                }
                Assert.That(feedback.transform.childCount, Is.EqualTo(pooledChildCount),
                    "Saturated station-state feedback should reuse its warmed objects.");
                Assert.That(feedback.ActiveCount, Is.EqualTo(feedback.PoolSize));
                Assert.That(feedback.PlayCount, Is.EqualTo(playCountBeforeDuplicate + 8));

                feedback.SetPaused(true);
                Assert.That(feedback.ActiveCount, Is.Zero);
                Assert.That(feedback.CurrentAlpha, Is.Zero);
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
        public IEnumerator MachineryAndPassageTransitions_LeaveAuthoritativeCompletedStatesVisible()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<StationStateFeedbackController>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            Assert.That(game, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            var initialReducedFlashes = game.IsReducedFlashesEnabled;

            try
            {
                if (game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }

                game.DebugActivateTower();
                feedback.SetPaused(true);
                feedback.SetPaused(false);
                game.DebugTeleport(DebugLocation.Shortcut);
                yield return new WaitForSeconds(0.5f);
                game.DebugOpenShortcut();
                yield return new WaitForSeconds(0.2f);
                _assertActiveShape(feedback, 5, 5);
                _captureIfRequested(references.PlayerCamera, "P50-Door-Release-1600x900.png", 1600, 900);

                yield return new WaitForSeconds(0.7f);
                Assert.That(feedback.ActiveCount, Is.Zero,
                    "Short-lived passage effects must yield to the authored open-door state.");
                var shortcutDoor = Array.Find(
                    Object.FindObjectsByType<AuthoredRouteDoorReadability>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    door => Vector3.Distance(door.transform.position, game.ShortcutPosition) < 1f);
                Assert.That(shortcutDoor, Is.Not.Null);
                Assert.That(shortcutDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
                Assert.That(shortcutDoor.FrameKit.IsOpen, Is.True);

                game.DebugToggleReducedFlashes();
                feedback.Play(game.ShortcutPosition, StationStateFeedbackKind.Machinery);
                yield return new WaitForSeconds(0.2f);
                Assert.That(feedback.CurrentAlpha, Is.InRange(0.01f, 0.28f));
                _assertActiveShape(feedback, 6, 5);
                _captureIfRequested(references.PlayerCamera, "P50-Machinery-Reduced-Flashes-1280x720.png", 1280, 720);
            }
            finally
            {
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
            }
        }

        [UnityTest]
        public IEnumerator TrialPhaseTransition_RendersAtTheAuthoredArenaWithoutChangingState()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<StationStateFeedbackController>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            Assert.That(game, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            Assert.That(chamber, Is.Not.Null);
            var stateBefore = chamber.State;

            references.Player.position = chamber.ArenaPosition + Vector3.back * 1.8f;
            yield return new WaitForSecondsRealtime(0.8f);
            feedback.Play(chamber.ArenaPosition, StationStateFeedbackKind.PhaseTransition);
            yield return new WaitForSecondsRealtime(0.16f);

            Assert.That(chamber.State, Is.EqualTo(stateBefore),
                "The climax effect must not advance the authoritative Trial state.");
            _assertActiveShape(feedback, 6, 3);
            _captureIfRequested(
                references.PlayerCamera,
                "P51-Trial-Phase-1600x900.png",
                1600,
                900,
                "DEAD_SIGNAL_P51_CAPTURE_DIR");
        }

        private static void _assertActiveShape(
            StationStateFeedbackController feedback,
            int expectedPrimaryPositions,
            int expectedDetailPositions)
        {
            var activeLines = Array.FindAll(
                feedback.GetComponentsInChildren<LineRenderer>(true),
                line => line.enabled &&
                        (line.name == "Primary Transition Shape" || line.name == "Detail Transition Shape"));
            Assert.That(activeLines, Has.Length.EqualTo(2));
            var primary = Array.Find(activeLines, line => line.name == "Primary Transition Shape");
            var detail = Array.Find(activeLines, line => line.name == "Detail Transition Shape");
            Assert.That(primary, Is.Not.Null);
            Assert.That(detail, Is.Not.Null);
            Assert.That(primary.positionCount, Is.EqualTo(expectedPrimaryPositions));
            Assert.That(detail.positionCount, Is.EqualTo(expectedDetailPositions));
            Assert.That(primary.sharedMaterial, Is.SameAs(detail.sharedMaterial));
        }

        private static void _captureIfRequested(
            Camera camera,
            string fileName,
            int width,
            int height,
            string environmentVariable = "DEAD_SIGNAL_P50_CAPTURE_DIR")
        {
            var captureDirectory = Environment.GetEnvironmentVariable(environmentVariable);
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
                Object.Destroy(renderTexture);
                Object.Destroy(texture);
            }
        }
    }
}
