using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DeadSignal.Application;
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
    }
}
