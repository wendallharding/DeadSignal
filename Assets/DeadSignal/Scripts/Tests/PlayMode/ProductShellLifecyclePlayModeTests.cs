using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Diagnostics;
using DeadSignal.Missions;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class ProductShellLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator PausedSceneReload_CreatesOneFreshPlayableRuntime()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var initialGame = Object.FindFirstObjectByType<DeadSignalGame>();
                Assert.That(initialGame, Is.Not.Null);
                var initialInstanceId = initialGame.GetInstanceID();

                yield return _pressAndRelease(gamepad, GamepadButton.Start);
                Assert.That(initialGame.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.Zero);

                yield return SceneManager.LoadSceneAsync("SampleScene");
                yield return null;

                var reloadedGame = Object.FindFirstObjectByType<DeadSignalGame>();
                Assert.That(reloadedGame, Is.Not.Null);
                Assert.That(reloadedGame.GetInstanceID(), Is.Not.EqualTo(initialInstanceId));
                _assertFreshSingletonRuntime(reloadedGame);
            }
            finally
            {
                Time.timeScale = 1f;
                InputSystem.RemoveDevice(gamepad);
            }
        }

        [UnityTest]
        public IEnumerator VictoryAndDefeatRestart_CreateFreshSingletonRuntimes()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var victoryGame = Object.FindFirstObjectByType<DeadSignalGame>();
                victoryGame.DebugApplyScenario(DebugScenario.Victory);
                yield return null;

                Assert.That(victoryGame.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
                _assertOutcomeOverlay(victoryGame);
                var victoryInstanceId = victoryGame.GetInstanceID();

                yield return _pressAndRelease(gamepad, GamepadButton.South);
                var defeatGame = Object.FindFirstObjectByType<DeadSignalGame>();
                Assert.That(defeatGame.GetInstanceID(), Is.Not.EqualTo(victoryInstanceId));
                _assertFreshSingletonRuntime(defeatGame);

                defeatGame.DebugApplyScenario(DebugScenario.Failure);
                yield return null;

                Assert.That(defeatGame.CurrentRunOutcome, Is.EqualTo(RunOutcome.Destroyed));
                _assertOutcomeOverlay(defeatGame);
                var defeatInstanceId = defeatGame.GetInstanceID();

                yield return _pressAndRelease(gamepad, GamepadButton.South);
                var restartedGame = Object.FindFirstObjectByType<DeadSignalGame>();
                Assert.That(restartedGame.GetInstanceID(), Is.Not.EqualTo(defeatInstanceId));
                _assertFreshSingletonRuntime(restartedGame);
            }
            finally
            {
                Time.timeScale = 1f;
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _pressAndRelease(Gamepad gamepad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(button));
            yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }

        private static void _assertFreshSingletonRuntime(DeadSignalGame game)
        {
            Assert.That(game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Running));
            Assert.That(game.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Object.FindObjectsByType<DeadSignalGame>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DeadSignalHud>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DeadSignalAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<CombatFeedbackController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));

            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.transform.Find("Run HUD").gameObject.activeSelf, Is.True);
            Assert.That(hud.transform.Find("Pause Overlay").gameObject.activeSelf, Is.False);
            Assert.That(hud.transform.Find("Outcome Overlay").gameObject.activeSelf, Is.False);
        }

        private static void _assertOutcomeOverlay(DeadSignalGame game)
        {
            Assert.That(game.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            var hud = Object.FindFirstObjectByType<DeadSignalHud>(FindObjectsInactive.Include);
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.transform.Find("Run HUD").gameObject.activeSelf, Is.False);
            Assert.That(hud.transform.Find("Pause Overlay").gameObject.activeSelf, Is.False);
            Assert.That(hud.transform.Find("Outcome Overlay").gameObject.activeSelf, Is.True);
        }
    }
}
