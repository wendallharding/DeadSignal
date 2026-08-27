using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using DeadSignal.World;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
            Assert.That(game.HasRuntimeNavMesh, Is.True, game.DebugNavMeshStatus);
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(debugCanvas, Is.Not.Null);
            Assert.That(debugCanvas.gameObject.activeSelf, Is.False);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
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
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);

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
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
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
            Assert.That(restartedGame.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CentralTower));
            Assert.That(restartedGame.CurrentMissionGuidanceTitle, Is.EqualTo("RESTORE CENTRAL"));
            Assert.That(restartedGame.CurrentObjectiveBeaconLabel, Is.EqualTo(restartedGame.CurrentMissionGuidanceAction));
            Assert.That(restartedGame.IsDebugRouteDriving, Is.False);
            Assert.That(restartedGame.IsDebugMenuOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator DestroyedRun_KeyboardRestartReentersAuthoredObjectiveGraphFromCentral()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                Assert.That(game, Is.Not.Null);

                game.DebugActivateTower();
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling));
                game.DebugApplyScenario(DebugScenario.Failure);
                Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Destroyed));

                var destroyedRunInstanceId = game.GetInstanceID();
                InputState.Change(keyboard, new KeyboardState(Key.R), InputUpdateType.Dynamic);
                Assert.That(keyboard.rKey.wasPressedThisFrame, Is.True);
                game.SendMessage("Update");
                InputState.Change(keyboard, new KeyboardState(), InputUpdateType.Dynamic);

                var restartDeadline = Time.realtimeSinceStartup + 5f;
                DeadSignalGame restartedGame = null;
                while (Time.realtimeSinceStartup < restartDeadline)
                {
                    restartedGame = Object.FindFirstObjectByType<DeadSignalGame>();
                    if (restartedGame != null && restartedGame.GetInstanceID() != destroyedRunInstanceId)
                    {
                        break;
                    }

                    yield return null;
                }

                Assert.That(restartedGame, Is.Not.Null);
                Assert.That(restartedGame.GetInstanceID(), Is.Not.EqualTo(destroyedRunInstanceId));
                Assert.That(restartedGame.CurrentRunOutcome, Is.EqualTo(RunOutcome.Running));
                Assert.That(restartedGame.IsTowerOnline, Is.False);
                Assert.That(restartedGame.IsCentralPayloadSecured, Is.False);
                Assert.That(restartedGame.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CentralTower));
                Assert.That(restartedGame.CurrentMissionGuidanceTitle, Is.EqualTo("RESTORE CENTRAL"));
                Assert.That(restartedGame.CurrentObjectiveBeaconLabel,
                    Is.EqualTo(restartedGame.CurrentMissionGuidanceAction));

                restartedGame.DebugActivateTower();
                Assert.That(restartedGame.IsTowerOnline, Is.True);
                Assert.That(restartedGame.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling));
                Assert.That(restartedGame.CurrentMissionGuidanceTitle, Is.EqualTo("RESTART CENTRAL"));
                Assert.That(restartedGame.CurrentObjectiveBeaconLabel,
                    Is.EqualTo(restartedGame.CurrentMissionGuidanceAction));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
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

        [UnityTest]
        public IEnumerator NavMeshRouteSequence_CompletesFullExtractionPlan()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.HasRuntimeNavMesh, Is.True, game.DebugNavMeshStatus);
            game.DebugSetTimeScale(2f);
            game.DebugStartRouteSequence(DebugRoutePreset.FullExtraction, DebugAutomationMode.DeterministicValidation,
                DebugAutomationProfile.SafeNavigation);

            var timeout = Time.realtimeSinceStartup + 45f;
            while ((game.DebugRouteSequenceState == DebugRouteRunState.Navigating ||
                    game.DebugRouteSequenceState == DebugRouteRunState.Verifying) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(game.DebugRouteSequenceState, Is.EqualTo(DebugRouteRunState.Completed),
                $"{game.DebugRouteSequenceReport}\n{game.DebugRouteSequenceStatus}\n{game.DebugTelemetry}");
            Assert.That(game.CurrentSalvage, Is.EqualTo(RunModel.SalvageRequired));
            Assert.That(game.IsExtractionUplinkActive, Is.True);
            game.DebugSetTimeScale(1f);
        }

        [UnityTest]
        public IEnumerator Warden_UsesNavMeshToAdvanceAcrossAuthoredObstacles()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugActivateTower();
            game.DebugTeleport(DebugLocation.FarEast);
            game.DebugSetInvulnerable(true);
            var start = game.DebugThreatPosition(SecurityReinforcement.Warden);
            var timeout = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            var end = game.DebugThreatPosition(SecurityReinforcement.Warden);
            Assert.That(Vector3.Distance(start, end), Is.GreaterThan(1f));
            Assert.That(game.DebugNavMeshStatus, Does.Contain("corner").Or.Contain("Complete"));
        }

        [UnityTest]
        public IEnumerator TacticalWindowScenarios_StagePoweredReturnThreatAndBoltWithoutChangingCollision()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var foregroundController = Object.FindFirstObjectByType<ForegroundOcclusionController>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(foregroundController == null || !foregroundController.enabled, Is.True,
                "The comparison preset must not reactivate runtime foreground culling.");

            foreach (var scenario in new[]
                     {
                         DebugScenario.OpeningTacticalWindow,
                         DebugScenario.SpineReturnTacticalWindow
                     })
            {
                game.DebugApplyScenario(scenario);

                var expectedPosition = scenario == DebugScenario.OpeningTacticalWindow
                    ? game.DebugPlayerPosition
                    : game.SpineTowerPosition + Vector3.forward * 3.35f;
                if (scenario == DebugScenario.OpeningTacticalWindow)
                {
                    Assert.That(game.DebugDistanceToLocation(DebugLocation.Extraction), Is.LessThan(0.01f));
                }
                else
                {
                    Assert.That(Vector3.Distance(game.DebugPlayerPosition, expectedPosition), Is.LessThan(0.01f));
                }
                Assert.That(game.IsTowerOnline, Is.True);
                Assert.That(game.IsRelayTowerOnline, Is.True);
                Assert.That(game.IsSpineTowerOnline, Is.True);
                Assert.That(game.CurrentMissionObjective, Does.Contain("TACTICAL WINDOW"));
                Assert.That(game.CurrentMissionObjective, Does.Contain("BOLT PATH"));
                Assert.That(game.SapperHealth, Is.GreaterThan(0f));
                Assert.That(game.WardenHealth, Is.EqualTo(0f));
                Assert.That(game.InterceptorHealth, Is.EqualTo(0f));
                Assert.That(game.SuppressorHealth, Is.EqualTo(0f));
                Assert.That(game.ActiveSignalBoltCount, Is.EqualTo(1),
                    "Each preset should stage one immediate player bolt for event-timed framing.");
                yield return new WaitForSeconds(1f);
                var playerViewport = camera.WorldToViewportPoint(game.DebugPlayerPosition + Vector3.up * 0.5f);
                var sapperViewport = camera.WorldToViewportPoint(
                    game.DebugThreatPosition(SecurityReinforcement.Sapper) + Vector3.up * 0.5f);
                Assert.That(game.AreTacticalWindowActorsInSafeViewport, Is.True,
                    $"{scenario} viewport framing: player={playerViewport}, sapper={sapperViewport}.");
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(137));
                if (scenario == DebugScenario.OpeningTacticalWindow)
                {
                    Assert.That(game.IsExtractionReady, Is.True,
                        "The opening comparison should expose the released direct return lane.");
                }

                foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900) })
                {
                    camera.aspect = (float)resolution.x / resolution.y;
                    var coverage = TacticalWindowCoverageDiagnostic.Measure(
                        camera,
                        Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                            .Where(renderer => renderer is MeshRenderer &&
                                               renderer.bounds.center.y > 0.2f && renderer.bounds.size.y > 0.45f));
                    Assert.That(coverage, Is.Not.Empty);
                    Assert.That(coverage[0].WindowCoverage, Is.LessThanOrEqualTo(0.2f),
                        $"{coverage[0].RendererName} consumes too much of the {scenario} tactical window at " +
                        $"{resolution.x}x{resolution.y}.");
                }
            }
        }

        [UnityTest]
        public IEnumerator TacticalWindowSweep_RecordsRealMovementWithoutChangingAuthoredCollision()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var camera = Object.FindFirstObjectByType<Camera>();
            Assert.That(game, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            camera.aspect = 16f / 9f;

            foreach (var scenario in new[]
                     {
                         DebugScenario.OpeningTacticalWindow,
                         DebugScenario.SpineReturnTacticalWindow
                     })
            {
                game.DebugApplyScenario(scenario);
                var obstacleCount = game.AuthoredMapObstacleCount;
                game.DebugStartTacticalWindowSweep();
                Assert.That(game.IsTacticalWindowSweepActive, Is.True);

                var timeout = Time.realtimeSinceStartup + 5f;
                while (game.IsTacticalWindowSweepActive && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(game.IsTacticalWindowSweepActive, Is.False, $"{scenario} sweep timed out.");
                Assert.That(game.TacticalWindowSweepSamples, Is.EqualTo(4));
                Assert.That(game.TacticalWindowSweepUnsafeActorSamples, Is.Zero,
                    $"{scenario} lost the player or Sapper during the diagnostic sweep.");
                Assert.That(game.TacticalWindowSweepDistance, Is.GreaterThan(1f),
                    $"{scenario} did not exercise meaningful real movement.");
                Assert.That(game.TacticalWindowSweepMaximumCoverage, Is.InRange(0f, 1f));
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(obstacleCount));
                TestContext.WriteLine(
                    $"{scenario}: pass={game.DidTacticalWindowSweepPass}, " +
                    $"maxCoverage={game.TacticalWindowSweepMaximumCoverage:P1}, " +
                    $"distance={game.TacticalWindowSweepDistance:0.00}m.");
            }
        }

        [UnityTest]
        public IEnumerator EasternRoomCombatWithoutSwarmers_PreservesMatchedSpecialistSchedule()
        {
            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            Screen.SetResolution(1600, 900, false);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            game.DebugApplyScenario(DebugScenario.EasternRoomCombatNoSwarmers);
            yield return null;
            yield return null;

            Assert.That(game.IsEasternCombatScenarioActive, Is.True);
            Assert.That(game.DebugCombatScenarioIncludesSwarmers, Is.False);
            Assert.That(game.SelectedWeaponOverclock, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
            Assert.That(game.IsOverclockChoicePending, Is.False);
            Assert.That(game.IsWeaponOverclockChoicePending, Is.False);
            Assert.That(game.AreDebugCombatActorsInSafeViewport, Is.True, game.DebugCombatScenarioStatus);
            Assert.That(game.ActiveSwarmerCount, Is.Zero);
            Assert.That(game.SwarmersSpawned, Is.Zero);

            game.DebugSetTimeScale(8f);
            var timeout = Time.realtimeSinceStartup + 45f;
            while (game.DebugCombatScenarioSeconds < 30f && Time.realtimeSinceStartup < timeout)
            {
                Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Running), game.DebugCombatScenarioStatus);
                Assert.That(game.AreDebugCombatActorsInSafeViewport, Is.True, game.DebugCombatScenarioStatus);
                yield return null;
            }

            Assert.That(game.DebugCombatScenarioSeconds, Is.GreaterThanOrEqualTo(30f), game.DebugCombatScenarioStatus);
            Assert.That(game.DebugCombatScenarioAttackCount, Is.EqualTo(4), game.DebugCombatScenarioStatus);
            Assert.That(game.PeakThreatConcurrency, Is.EqualTo(4), game.DebugCombatScenarioStatus);
            Assert.That(game.ActiveSwarmerCount, Is.Zero);
            Assert.That(game.SwarmersSpawned, Is.Zero);
            Assert.That(game.SwarmerContacts, Is.Zero);
            Assert.That(game.WardenHealth, Is.GreaterThan(0f));
            Assert.That(game.SapperHealth, Is.GreaterThan(0f));
            Assert.That(game.InterceptorHealth, Is.GreaterThan(0f));
            Assert.That(game.SuppressorHealth, Is.GreaterThan(0f));
            Assert.That(game.DebugNavMeshStatus, Does.Not.Contain("Failed"));
            game.DebugSetTimeScale(1f);
            Screen.SetResolution(originalWidth, originalHeight, false);
        }

        [UnityTest]
        public IEnumerator EasternRoomCombat_UsesAuthoredAnchorsAndSurvivesThirtySeconds()
        {
            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            Screen.SetResolution(1600, 900, false);
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scenario = Object.FindFirstObjectByType<AuthoredCombatScenario>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(scenario, Is.Not.Null);
            Assert.That(scenario.IsComplete, Is.True);
            Assert.That(Resources.Load<Texture2D>("Environment/EasternCombatLabTarget"), Is.Not.Null);
            Assert.That(Resources.Load<Material>("Materials/EasternCombatLab/EasternCombatLabTarget"), Is.Not.Null);
            Assert.That(Resources.Load<GameObject>("Actors/SwarmerAssembly"), Is.Not.Null);
            Assert.That(Resources.Load<SwarmerPressureTuning>("Tuning/SwarmerPressureTuning"), Is.Not.Null);
            var obstacles = Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None);
            foreach (var anchor in new[]
                     {
                         scenario.PlayerAnchor, scenario.WardenAnchor, scenario.SapperAnchor,
                         scenario.InterceptorAnchor, scenario.SuppressorAnchor
                     })
            {
                foreach (var obstacle in obstacles)
                {
                    Assert.That(obstacle.OverlapsCircle(anchor.position, 0.6f), Is.False,
                        $"{anchor.name} must keep a safe authored clearance from {obstacle.name}.");
                }
            }

            for (var load = 0; load < 5; load++)
            {
                game.DebugApplyScenario(DebugScenario.EasternRoomCombat);
                yield return null;
                yield return null;
                Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Running), game.DebugCombatScenarioStatus);
                Assert.That(game.IsEasternCombatScenarioActive, Is.True);
                Assert.That(game.IsTowerOnline, Is.True);
                Assert.That(game.IsRelayTowerOnline, Is.True);
                Assert.That(game.IsSpineTowerOnline, Is.True);
                Assert.That(game.SelectedWeaponOverclock, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
                Assert.That(game.IsOverclockChoicePending, Is.False);
                Assert.That(game.IsWeaponOverclockChoicePending, Is.False);
                Assert.That(game.CurrentMissionObjective, Does.Contain("COMBAT LAB"));
                Assert.That(game.CurrentMissionObjective, Does.Not.Contain("SALVAGE"));
                Assert.That(game.AreDebugCombatActorsInSafeViewport, Is.True, game.DebugCombatScenarioStatus);
                Assert.That(game.HasSwarmerAssets, Is.True);
                Assert.That(game.ActiveSwarmerCount, Is.EqualTo(3), game.DebugCombatScenarioStatus);
            }

            var player = game.transform.Find("Maintenance Drone");
            var swarmer = game.transform.Find("Security Swarmer 1");
            Assert.That(player, Is.Not.Null);
            Assert.That(swarmer, Is.Not.Null);
            swarmer.position = player.position + player.forward * 2f;
            game.DebugSetSignal(50f);
            var shotsBeforeSwarmer = game.ShotsFired;
            game.DebugFireAt(swarmer.position);
            var purgeDeadline = Time.realtimeSinceStartup + 2f;
            while (game.SwarmersPurged == 0 && Time.realtimeSinceStartup < purgeDeadline)
            {
                yield return null;
            }
            Assert.That(game.SwarmersPurged, Is.EqualTo(1), game.DebugCombatScenarioStatus);
            Assert.That(game.ShotsFired - shotsBeforeSwarmer, Is.EqualTo(1),
                "One fragile Swarmer should be purged by one basic Signal bolt.");
            Assert.That(game.CurrentSignal, Is.EqualTo(53f).Within(0.1f),
                "The finite Swarmer reward should restore the tuned three Signal without charging for the bolt.");

            game.DebugApplyScenario(DebugScenario.EasternRoomCombat);
            yield return null;
            yield return null;

            game.DebugSetTimeScale(8f);
            var timeout = Time.realtimeSinceStartup + 45f;
            while (game.DebugCombatScenarioSeconds < 30f && Time.realtimeSinceStartup < timeout)
            {
                Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Running), game.DebugCombatScenarioStatus);
                Assert.That(game.AreDebugCombatActorsInSafeViewport, Is.True, game.DebugCombatScenarioStatus);
                yield return null;
            }

            Assert.That(game.DebugCombatScenarioSeconds, Is.GreaterThanOrEqualTo(30f), game.DebugCombatScenarioStatus);
            Assert.That(game.DebugCombatScenarioAttackCount, Is.EqualTo(5), game.DebugCombatScenarioStatus);
            Assert.That(game.SwarmersSpawned, Is.EqualTo(6), game.DebugCombatScenarioStatus);
            Assert.That(game.PeakSwarmerCount, Is.EqualTo(6), game.DebugCombatScenarioStatus);
            Assert.That(game.PeakThreatConcurrency, Is.EqualTo(10), game.DebugCombatScenarioStatus);
            Assert.That(game.ActiveSwarmerCount, Is.EqualTo(6), game.DebugCombatScenarioStatus);
            Assert.That(game.SwarmerContacts, Is.GreaterThan(0), game.DebugCombatScenarioStatus);
            Assert.That(game.WardenHealth, Is.GreaterThan(0f));
            Assert.That(game.SapperHealth, Is.GreaterThan(0f));
            Assert.That(game.InterceptorHealth, Is.GreaterThan(0f));
            Assert.That(game.SuppressorHealth, Is.GreaterThan(0f));
            Assert.That(game.DebugNavMeshStatus, Does.Not.Contain("Failed"));
            game.DebugSetTimeScale(1f);
            Screen.SetResolution(originalWidth, originalHeight, false);
        }
    }
}
