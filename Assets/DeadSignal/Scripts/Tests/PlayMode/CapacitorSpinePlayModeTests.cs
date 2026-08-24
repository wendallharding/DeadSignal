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
    public sealed class CapacitorSpinePlayModeTests
    {
        [UnityTest]
        public IEnumerator Region_ProvidesTwoApproachesLandmarkAndRelocatedGreedCache()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var foundry = game.transform.Find("Relay Foundry Region");
                var spine = GameObject.Find("Capacitor Spine Region").transform;
                var socket = spine.GetComponentInChildren<AuthoredSalvageSocket>();

                Assert.That(spine.position, Is.EqualTo(new Vector3(42.5f, 0f, 0f)));
                Assert.That(spine.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(8));
                Assert.That(spine.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(spine.Find("Capacitor Transfer Bank"), Is.Not.Null);
                Assert.That(spine.Find("North Capacitor Shield"), Is.Not.Null);
                Assert.That(spine.Find("Third Tower Berth"), Is.Not.Null);
                Assert.That(spine.Find("Capacitor Spine Route Decal"), Is.Not.Null);
                Assert.That(socket, Is.Not.Null);
                Assert.That(socket.Position, Is.EqualTo(new Vector3(47.15f, 0f, -3.35f)));
                Assert.That(Resources.Load<Texture2D>("Environment/CapacitorSpineRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/CapacitorSpine/CapacitorSpineRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(42));
                Assert.That(game.AuthoredSalvageSocketCount, Is.EqualTo(1));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(4),
                    "The extension must preserve the Foundry's established safe reinforcement pair.");
                Assert.That(foundry.Find("Foundry East Bulkhead"), Is.Null);
                Assert.That(foundry.Find("Foundry East North"), Is.Not.Null);
                Assert.That(foundry.Find("Foundry East Center"), Is.Not.Null);
                Assert.That(foundry.Find("Foundry East South"), Is.Not.Null);

                player.position = new Vector3(34.6f, 0f, 3f);
                yield return _moveRight(gamepad, player, 37f);
                Assert.That(player.position.x, Is.GreaterThan(37f),
                    "The protected north opening should cross from the Foundry into the new region.");

                player.position = new Vector3(34.6f, 0f, -3f);
                yield return _moveRight(gamepad, player, 37f);
                Assert.That(player.position.x, Is.GreaterThan(37f),
                    "The exposed south opening should remain an independent traversable approach.");

                player.position = new Vector3(39f, 0f, 0f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The central transfer landmark should shape both movement and projectile positioning.");
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _moveRight(Gamepad gamepad, Transform player, float targetX)
        {
            var deadline = Time.time + 2f;
            while (player.position.x <= targetX && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
                yield return null;
            }

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
