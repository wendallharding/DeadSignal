using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DeadSignal.Application;
using DeadSignal.Presentation;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class TacticalMapNavigationPlayModeTests
    {
        [TestCase(1280, 720)]
        [TestCase(1600, 900)]
        [TestCase(3440, 1440)]
        public void TacticalMapCard_FitsTargetViewportWithoutOwningTheTacticalCenter(int width, int height)
        {
            var card = MissionClarityHud.CalculateTacticalMapCard(width, height);
            Assert.That(card.xMin, Is.GreaterThanOrEqualTo(18f));
            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(88f));
            Assert.That(card.xMax, Is.LessThanOrEqualTo(width * 0.45f));
            Assert.That(card.yMax, Is.LessThanOrEqualTo(height - 18f));
            Assert.That(card.width, Is.GreaterThanOrEqualTo(Mathf.Min(524f, width - 36f)));
        }

        [UnityTest]
        public IEnumerator PausedMap_GamepadZoomsPansAndReturnsToFit()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var map = Object.FindFirstObjectByType<MissionClarityHud>();
            var shell = Object.FindFirstObjectByType<DeadSignalShellController>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(map, Is.Not.Null);
            Assert.That(shell, Is.Not.Null);

            if (shell.IsMenuVisible)
            {
                var start = System.Array.Find(shell.GetComponentsInChildren<Button>(true),
                    button => button.name == "Start Run");
                start.onClick.Invoke();
                while (shell.IsTransitioning)
                {
                    yield return null;
                }
            }

            game.DebugSetTimeScale(0f);
            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                Assert.That(map.TacticalMapZoom, Is.EqualTo(1.5f).Within(0.01f));

                for (var index = 0; index < 8; index++)
                {
                    InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                    yield return null;
                }
                Assert.That(map.TacticalMapPan.x, Is.GreaterThan(0f));

                InputSystem.QueueStateEvent(gamepad, new GamepadState()
                    .WithButton(GamepadButton.LeftShoulder)
                    .WithButton(GamepadButton.RightShoulder));
                yield return null;
                Assert.That(map.TacticalMapZoom, Is.EqualTo(1f).Within(0.01f));
                Assert.That(map.TacticalMapPan, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
                game.DebugSetTimeScale(1f);
            }
        }
    }
}
