using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using UnityEngine.UI;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class DeadSignalDebugMenuPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_CreatesClosedAutoUiDebugMenuInEditor()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var menu = Object.FindFirstObjectByType<DeadSignalDebugMenu>(FindObjectsInactive.Include);
            Canvas debugCanvas = null;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.name == "DEAD SIGNAL — Debug Menu")
                {
                    debugCanvas = canvas;
                    break;
                }
            }

            Assert.That(game, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(debugCanvas, Is.Not.Null);
            Assert.That(debugCanvas.gameObject.activeSelf, Is.False);
            Assert.That(Resources.Load<GameObject>("UI/Debug/AutoUI"), Is.Not.Null);
            Assert.That(Resources.Load<GameObject>("UI/Debug/Canvas_DebugMenu"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Commands_CreatePlayableFeatureScenariosWithoutBypassingInvariants()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);

            game.DebugSetSignal(1f);
            Assert.That(game.CurrentSignal, Is.EqualTo(1f));

            game.DebugActivateTower();
            Assert.That(game.IsTowerOnline, Is.True);
            Assert.That(game.CurrentSignal, Is.GreaterThan(1f));

            game.DebugCollectNextCache();
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));
            Assert.That(game.IsOverclockChoicePending, Is.True);

            game.DebugSelectOverclock(SignalOverclock.ChainArc);
            Assert.That(game.SelectedOverclock, Is.EqualTo(SignalOverclock.ChainArc));

            game.DebugMakeExtractionReady();
            Assert.That(game.CurrentSalvage, Is.EqualTo(RunModel.SalvageRequired));

            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            Assert.That(game.IsExtractionUplinkActive, Is.True);

            game.DebugCompleteExtraction();
            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
        }

        [UnityTest]
        public IEnumerator Harness_ControlsStateAutomationCameraVisualizationAndTelemetry()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);

            game.DebugTeleport(DebugLocation.SpineTower);
            Assert.That(Vector3.Distance(game.DebugPlayerPosition, game.SpineTowerPosition), Is.LessThan(0.01f));

            game.DebugSetInfiniteSignal(true);
            game.DebugSetSignal(2f);
            yield return null;
            Assert.That(game.CurrentSignal, Is.EqualTo(RunModel.MaximumSignal));
            game.DebugSetInfiniteSignal(false);

            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            var health = game.WardenHealth;
            game.DebugDamageThreat(SecurityReinforcement.Warden);
            Assert.That(game.WardenHealth, Is.LessThan(health));
            game.DebugRepositionThreat(SecurityReinforcement.Warden);
            game.DebugForceThreatAttack(SecurityReinforcement.Warden);
            game.DebugSetThreatsFrozen(true);
            game.DebugSetThreatsFrozen(false);
            game.DebugSetInvulnerable(true);
            game.DebugSetInvulnerable(false);

            game.DebugToggleWorldVisualization();
            Assert.That(Object.FindFirstObjectByType<DeadSignalDebugVisualization>(), Is.Not.Null);
            game.DebugToggleFreeCamera();
            var debugCamera = Object.FindFirstObjectByType<DeadSignalDebugCamera>();
            Assert.That(debugCamera, Is.Not.Null);
            Assert.That(debugCamera.IsFree, Is.True);
            game.DebugToggleFreeCamera();

            game.DebugTeleport(DebugLocation.Extraction);
            game.DebugStartRouteDriver(DebugLocation.CentralTower);
            Assert.That(game.IsDebugRouteDriving, Is.True);
            game.DebugSetInfiniteSignal(true);
            for (var frame = 0; frame < 600 && game.IsDebugRouteDriving; frame++)
            {
                yield return null;
            }
            Assert.That(game.IsDebugRouteDriving, Is.False, game.DebugTelemetry);
            Assert.That(game.DebugDistanceToLocation(DebugLocation.CentralTower), Is.LessThan(2.1f));
            game.DebugSetInfiniteSignal(false);

            game.DebugSetTimeScale(0.5f);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
            game.DebugSetTimeScale(1f);
            game.DebugExerciseCombatFeedback();
            game.DebugExerciseThreatTelegraphs();
            Assert.That(game.DebugTelemetry, Does.Contain("Focus"));
            Assert.That(game.DebugTelemetry, Does.Contain("Signal Δ"));
            Assert.That(game.DebugTelemetry, Does.Contain("Interaction"));
            Assert.That(game.DebugReplayInfo, Does.Contain("Route seed"));
            Assert.That(game.DebugEventLog, Does.Contain("DEBUG"));
        }

        [UnityTest]
        public IEnumerator GeneratedPages_FitSmallViewportAndOwnPausedPresentation()
        {
            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            Screen.SetResolution(1280, 720, false);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var menu = Object.FindFirstObjectByType<DeadSignalDebugMenu>(FindObjectsInactive.Include);
            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);

            menu.SendMessage("_setOpen", true);
            yield return null;
            yield return null;

            Assert.That(game.IsDebugMenuOpen, Is.True);
            Assert.That(hud.IsDebugMenuVisible, Is.True);
            Assert.That(hud.IsPauseOverlayVisible, Is.False, "The normal pause overlay must not compete with AutoUI.");
            Assert.That(menu.DebugCanvas.sortingOrder, Is.GreaterThanOrEqualTo(200));

            var pages = new System.Collections.Generic.List<RectTransform>();
            foreach (var rect in menu.DebugCanvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name.EndsWith(" Page", System.StringComparison.Ordinal))
                {
                    pages.Add(rect);
                }
            }
            Assert.That(pages, Has.Count.EqualTo(6));

            foreach (var page in pages)
            {
                foreach (var candidate in pages)
                {
                    candidate.gameObject.SetActive(candidate == page);
                }
                Canvas.ForceUpdateCanvases();
                foreach (var panel in page.GetComponentsInChildren<ScrollRect>(false))
                {
                    var corners = new Vector3[4];
                    panel.GetComponent<RectTransform>().GetWorldCorners(corners);
                    Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-1f), $"{page.name}/{panel.name} extends left of the viewport.");
                    Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.width + 1f), $"{page.name}/{panel.name} extends right of the viewport.");
                }
            }

            menu.SendMessage("_setOpen", false);
            Screen.SetResolution(originalWidth, originalHeight, false);
        }

        [UnityTest]
        public IEnumerator FreshRun_ClosesOpenMenuWithoutTeardownExceptions()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var menu = Object.FindFirstObjectByType<DeadSignalDebugMenu>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);

            menu.SendMessage("_setOpen", true);
            game.DebugStartRouteDriver(DebugLocation.Extraction);
            game.DebugApplyScenario(DebugScenario.FreshRun);
            yield return null;
            yield return null;

            var restartedGame = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(restartedGame, Is.Not.Null);
            Assert.That(restartedGame.IsDebugRouteDriving, Is.False);
            Assert.That(restartedGame.IsDebugMenuOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator RouteSequence_ExecutesOpeningNavigationActionsAndReport()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugSetTimeScale(2f);
            game.DebugStartRouteSequence(DebugRoutePreset.OpeningLoop, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation);

            var timeout = Time.realtimeSinceStartup + 35f;
            while ((game.DebugRouteSequenceState == DebugRouteRunState.Navigating ||
                    game.DebugRouteSequenceState == DebugRouteRunState.Verifying) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(game.DebugRouteSequenceState, Is.EqualTo(DebugRouteRunState.Completed),
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.IsTowerOnline, Is.True);
            Assert.That(game.DebugRouteSequenceReport, Does.Contain("ROUTE COMPLETE"));
            game.DebugSetTimeScale(1f);
        }
    }
}
