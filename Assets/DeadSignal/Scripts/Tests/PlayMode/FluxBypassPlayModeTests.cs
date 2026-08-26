using System.Collections;
using System.Linq;
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
    public sealed class FluxBypassPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bypass_LinksGalleryAndChamberAsPoweredReturnFlank()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var gallery = game.transform.Find("Spine Induction Gallery Region");
                var chamber = gallery.Find("Convergence Chamber Region");
                var bypass = gallery.Find("Flux Bypass Region");
                var territory = bypass.GetComponent<AuthoredPoweredTerritory>();
                var routing = bypass.Find("Flux Bypass Signal Lines").gameObject;

                Assert.That(bypass.position, Is.EqualTo(new Vector3(32f, 0f, 12.75f)));
                Assert.That(bypass.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(9));
                Assert.That(bypass.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(bypass.Find("Flux Shunt Regulator"), Is.Not.Null);
                Assert.That(bypass.Find("South Flux Deflector"), Is.Not.Null);
                Assert.That(bypass.Find("North Flux Deflector"), Is.Not.Null);
                Assert.That(bypass.Find("Flux Bypass Route Decal"), Is.Not.Null);
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(territory.HalfExtents, Is.EqualTo(new Vector2(3.15f, 5.4f)));
                Assert.That(Resources.Load<GameObject>("Environment/FluxBypassRegion"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/FluxBypassRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/FluxBypass/FluxBypassRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(123));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
                Assert.That(gallery.Find("Induction Gallery West Bulkhead"), Is.Null);
                Assert.That(chamber.Find("Convergence West Bulkhead"), Is.Null);

                player.position = new Vector3(36.25f, 0f, 9.5f);
                yield return _move(gamepad, player, Vector2.left, () => player.position.x < 34.8f);
                var nearbySouthObstacles = string.Join(", ", Object.FindObjectsByType<AuthoredMapObstacle>(
                        FindObjectsSortMode.None)
                    .Where(obstacle => Vector2.Distance(
                        obstacle.Center, new Vector2(player.position.x, player.position.z)) < 4f)
                    .Select(obstacle => $"{obstacle.name}@{obstacle.Center}"));
                Assert.That(player.position.x, Is.LessThan(34.8f),
                    $"The south threshold should connect the Gallery to the exterior bypass. Nearby: {nearbySouthObstacles}");

                player.position = new Vector3(30f, 0f, 9.5f);
                yield return _move(gamepad, player, Vector2.up, () => player.position.z > 15.5f);
                Assert.That(player.position.z, Is.GreaterThan(15.5f),
                    "The bypass should provide a complete northbound route around the chamber kill line.");

                player.position = new Vector3(34.25f, 0f, 15.5f);
                yield return _move(gamepad, player, Vector2.right, () => player.position.x > 36f);
                Assert.That(player.position.x, Is.GreaterThan(36f),
                    "The north threshold should connect the bypass back into the Convergence Chamber.");

                player.position = new Vector3(29.25f, 0f, 11.25f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The angled bypass deflector should block projectiles as well as movement.");

                var bypassCenter = new Vector3(32f, 0f, 12.75f);
                game.DebugActivateRelayTower();
                Assert.That(game.DebugIsPoweredAt(bypassCenter), Is.False);
                Assert.That(routing.activeSelf, Is.False);
                game.DebugActivateSpineTower();
                Assert.That(game.DebugIsPoweredAt(bypassCenter), Is.True,
                    "The exterior loop should become a powered return flank with the Spine tower.");
                Assert.That(routing.activeSelf, Is.True);
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _move(Gamepad gamepad, Transform player, Vector2 direction, System.Func<bool> complete)
        {
            var deadline = Time.time + 2f;
            while (!complete() && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = direction });
                yield return null;
            }

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
