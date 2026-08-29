using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class ProductShellMenuPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredMenu_PausesUntouchedRunAndOpensSettingsControlsAndStart()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(shell, Is.Not.Null);
            Assert.That(game, Is.Not.Null);

            shell.DebugShowMenu();
            yield return null;
            _assertMenuOwnsPresentation(shell, game);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Start Run"));

            _button(shell, "Settings").onClick.Invoke();
            yield return null;
            Assert.That(shell.CurrentPage, Is.EqualTo(ProductShellPage.Settings));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Steady Camera"));
            var reducedFlashes = _button(shell, "Reduced Flashes");
            var initialReducedFlashesLabel = reducedFlashes.GetComponentInChildren<Text>().text;
            reducedFlashes.onClick.Invoke();
            yield return null;
            Assert.That(reducedFlashes.GetComponentInChildren<Text>().text, Is.Not.EqualTo(initialReducedFlashesLabel));
            reducedFlashes.onClick.Invoke();

            _button(shell, "Back").onClick.Invoke();
            _button(shell, "Controls").onClick.Invoke();
            yield return null;
            Assert.That(shell.CurrentPage, Is.EqualTo(ProductShellPage.Controls));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Move Up"));
            Assert.That(_button(shell, "Fire").GetComponentInChildren<Text>().text, Does.StartWith("FIRE  //"));

            _button(shell, "Back").onClick.Invoke();
            _button(shell, "Start Run").onClick.Invoke();
            yield return null;
            Assert.That(shell.IsMenuVisible, Is.False);
            Assert.That(game.IsMainMenuOpen, Is.False);
            Assert.That(game.enabled, Is.True);
            Assert.That(game.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            _assertOverlay("Run HUD", true);
            _assertOverlay("Pause Overlay", false);
            _assertOverlay("Outcome Overlay", false);
        }

        [UnityTest]
        public IEnumerator AuthoredMenu_GamepadNavigatesSelectsAndReturns()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            shell.DebugShowMenu();
            yield return null;
            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return _pressAndRelease(gamepad, new GamepadState { leftStick = Vector2.down });
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Settings"));

                yield return _pressAndRelease(gamepad, new GamepadState().WithButton(GamepadButton.South));
                Assert.That(shell.CurrentPage, Is.EqualTo(ProductShellPage.Settings));
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Steady Camera"));

                yield return _pressAndRelease(gamepad, new GamepadState().WithButton(GamepadButton.East));
                Assert.That(shell.CurrentPage, Is.EqualTo(ProductShellPage.Main));
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Start Run"));
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator ReturnToMenu_FromPauseDefeatAndVictory_ReplacesRuntimeWithoutDuplicates()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            shell.DebugShowMenu();
            yield return null;
            _button(shell, "Start Run").onClick.Invoke();
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var runningGame = Object.FindFirstObjectByType<DeadSignalGame>();
                var runningId = runningGame.GetInstanceID();
                yield return _pressAndRelease(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Resume Run"));
                _activeHudButton("Main Menu").onClick.Invoke();
                yield return null;
                var pausedReturnGame = _assertFreshMenuRuntime(runningId);

                _button(Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include), "Start Run").onClick.Invoke();
                pausedReturnGame.DebugApplyScenario(DebugScenario.Failure);
                yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Restart Run"));
                _assertDefeatPresentation();
                var defeatId = pausedReturnGame.GetInstanceID();
                _activeHudButton("Main Menu").onClick.Invoke();
                yield return null;
                var defeatReturnGame = _assertFreshMenuRuntime(defeatId);

                _button(Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include), "Start Run").onClick.Invoke();
                defeatReturnGame.DebugApplyScenario(DebugScenario.Victory);
                yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Restart Run"));
                _assertVictoryPresentation();
                var victoryId = defeatReturnGame.GetInstanceID();
                _activeHudButton("Main Menu").onClick.Invoke();
                yield return null;
                var victoryReturnGame = _assertFreshMenuRuntime(victoryId);

                _button(Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include), "Start Run").onClick.Invoke();
                victoryReturnGame.DebugApplyScenario(DebugScenario.Failure);
                yield return null;
                var restartId = victoryReturnGame.GetInstanceID();
                _activeHudButton("Restart Run").onClick.Invoke();
                yield return null;
                _assertFreshRunningRuntime(restartId);
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
                Time.timeScale = 1f;
            }
        }

        private static IEnumerator _pressAndRelease(Gamepad gamepad, GamepadState state)
        {
            InputSystem.QueueStateEvent(gamepad, state);
            yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }

        private static Button _button(DeadSignalShellController shell, string name)
        {
            foreach (var button in shell.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name && button.gameObject.activeInHierarchy)
                {
                    return button;
                }
            }

            Assert.Fail($"Active authored menu button '{name}' was not found.");
            return null;
        }

        private static Button _activeHudButton(string name)
        {
            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            foreach (var button in hud.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name && button.gameObject.activeInHierarchy)
                {
                    return button;
                }
            }

            Assert.Fail($"Active authored HUD button '{name}' was not found.");
            return null;
        }

        private static DeadSignalGame _assertFreshMenuRuntime(int previousInstanceId)
        {
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            Assert.That(game.GetInstanceID(), Is.Not.EqualTo(previousInstanceId));
            _assertMenuOwnsPresentation(shell, game);
            _assertSingletonServices();
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Start Run"));
            return game;
        }

        private static void _assertFreshRunningRuntime(int previousInstanceId)
        {
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game.GetInstanceID(), Is.Not.EqualTo(previousInstanceId));
            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Running));
            Assert.That(game.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            _assertSingletonServices();
            _assertOverlay("Run HUD", true);
            _assertOverlay("Pause Overlay", false);
            _assertOverlay("Outcome Overlay", false);
        }

        private static void _assertSingletonServices()
        {
            Assert.That(Object.FindObjectsByType<DeadSignalGame>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DeadSignalHud>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DeadSignalAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<CombatFeedbackController>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            var enabledActions = InputSystem.ListEnabledActions();
            foreach (var actionName in new[]
                     {
                         "DEAD SIGNAL Fire", "DEAD SIGNAL Interact", "DEAD SIGNAL Move Up", "DEAD SIGNAL Move Down",
                         "DEAD SIGNAL Move Left", "DEAD SIGNAL Move Right"
                     })
            {
                Assert.That(enabledActions.Count(action => action.name == actionName), Is.EqualTo(1),
                    $"Expected one enabled DEAD SIGNAL '{actionName}' action.");
            }
        }

        private static void _assertMenuOwnsPresentation(DeadSignalShellController shell, DeadSignalGame game)
        {
            Assert.That(shell.IsMenuVisible, Is.True);
            Assert.That(shell.CurrentPage, Is.EqualTo(ProductShellPage.Main));
            Assert.That(game.IsMainMenuOpen, Is.True);
            Assert.That(game.enabled, Is.False);
            Assert.That(game.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            _assertOverlay("Run HUD", false);
            _assertOverlay("Pause Overlay", false);
            _assertOverlay("Outcome Overlay", false);
        }

        private static void _assertOverlay(string name, bool expected)
        {
            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            Assert.That(hud.transform.Find(name).gameObject.activeSelf, Is.EqualTo(expected));
        }

        private static void _assertDefeatPresentation()
        {
            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            var outcome = hud.transform.Find("Outcome Overlay");
            Assert.That(outcome.Find("Result").GetComponent<Text>().text, Is.EqualTo("MISSION LOST"));
            Assert.That(outcome.Find("Detail").GetComponent<Text>().text,
                Is.EqualTo("SIGNAL DEPLETED — EMERGENCY RECOVERY EXPIRED"));
            var report = outcome.Find("Run Report").GetComponent<Text>();
            Assert.That(report.text, Does.Contain("FAILED AT"));
            Assert.That(report.text, Does.Contain("NEXT:"));
            Assert.That(report.preferredHeight, Is.LessThanOrEqualTo(report.rectTransform.rect.height),
                "The concise defeat report must fit its authored text region without covering outcome actions.");
            Assert.That(outcome.Find("Restart").GetComponent<Text>().text, Does.Contain("MAIN MENU AVAILABLE"));
        }

        private static void _assertVictoryPresentation()
        {
            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            var outcome = hud.transform.Find("Outcome Overlay");
            Assert.That(outcome.Find("Result").GetComponent<Text>().text, Is.EqualTo("MISSION COMPLETE"));
            Assert.That(outcome.Find("Detail").GetComponent<Text>().text,
                Is.EqualTo("STATION RESTARTED  //  NETWORK EXTENDED  //  SIGNAL CORE REBUILT  //  EXTRACTION SECURED"));
            var report = outcome.Find("Run Report").GetComponent<Text>();
            Assert.That(report.text, Does.Contain("MISSION "));
            Assert.That(report.text, Does.Contain("CENTRAL > RELAY > SPINE > DOCK"));
            Assert.That(report.text, Does.Contain("COMBAT"));
            Assert.That(report.text, Does.Contain("SIGNAL"));
            Assert.That(report.preferredHeight, Is.LessThanOrEqualTo(report.rectTransform.rect.height),
                "The completion report must fit its authored text region without covering outcome actions.");
            Assert.That(outcome.Find("Restart").GetComponent<Text>().text, Does.Contain("MAIN MENU AVAILABLE"));
        }
    }
}
