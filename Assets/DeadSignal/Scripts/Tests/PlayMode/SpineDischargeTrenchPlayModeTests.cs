using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.World;

namespace DeadSignal.Tests
{
    public sealed class SpineDischargeTrenchPlayModeTests
    {
        [UnityTest]
        public IEnumerator Trench_AddsTwoApproachesCoverSafeGateAndPoweredReturn()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var spine = game.transform.Find("Capacitor Spine Region");
                var trench = spine.Find("Spine Discharge Trench Region");
                var territory = trench.GetComponent<AuthoredPoweredTerritory>();
                var signalLines = trench.Find("Discharge Trench Signal Lines").gameObject;

                Assert.That(trench, Is.Not.Null);
                Assert.That(trench.localPosition, Is.EqualTo(new Vector3(0f, 0f, -8f)));
                Assert.That(trench.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(6));
                Assert.That(trench.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(trench.GetComponentsInChildren<AuthoredInterceptorEntrance>().Length, Is.EqualTo(1));
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(signalLines.activeSelf, Is.False);
                Assert.That(Resources.Load<GameObject>("Environment/SpineDischargeTrenchRegion"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/SpineDischargeTrenchRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>(
                    "Materials/SpineDischargeTrench/SpineDischargeTrenchRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(123));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));

                player.position = new Vector3(40.3f, 0f, -4.2f);
                yield return _moveDown(gamepad, player, -6.2f);
                Assert.That(player.position.z, Is.LessThan(-6.2f),
                    "The west threshold should independently enter the discharge trench.");

                player.position = new Vector3(44.7f, 0f, -4.2f);
                yield return _moveDown(gamepad, player, -6.2f);
                Assert.That(player.position.z, Is.LessThan(-6.2f),
                    "The east threshold should remain an independent tactical approach.");

                player.position = new Vector3(42.5f, 0f, -6.5f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.down }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.down });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The central discharge coil should block projectiles as well as movement.");

                game.DebugActivateSpineTower();
                yield return null;
                Assert.That(signalLines.activeSelf, Is.True,
                    "The dead-zone trench should become a powered return foothold with the Spine tower.");
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _moveDown(Gamepad gamepad, Transform player, float targetZ)
        {
            var deadline = Time.time + 2f;
            while (player.position.z >= targetZ && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.down });
                yield return null;
            }

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
