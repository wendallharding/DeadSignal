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
    public sealed class RelayCoolingGantryPlayModeTests
    {
        [UnityTest]
        public IEnumerator Gantry_AddsDeadZoneFlankAndPoweredRelayReturn()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var foundry = game.transform.Find("Relay Foundry Region");
                var gantry = foundry.Find("Relay Cooling Gantry Region");
                var territory = gantry.GetComponent<AuthoredPoweredTerritory>();
                var routing = gantry.Find("Cooling Gantry Signal Lines").gameObject;

                Assert.That(gantry.position, Is.EqualTo(new Vector3(27.5f, 0f, -11.25f)));
                Assert.That(gantry.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(6));
                Assert.That(gantry.GetComponentsInChildren<AuthoredInterceptorEntrance>().Length, Is.EqualTo(1));
                var gantryPayloadSocket = gantry.GetComponentInChildren<AuthoredSalvageSocket>();
                var protectedPayloadSocket = foundry.Find("Protected Relay Payload Socket")
                    .GetComponent<AuthoredSalvageSocket>();
                Assert.That(gantryPayloadSocket.Region, Is.EqualTo(DeadSignal.Missions.SignalRegion.Relay));
                Assert.That(gantryPayloadSocket.IsOptional, Is.False);
                Assert.That(protectedPayloadSocket.Region, Is.EqualTo(DeadSignal.Missions.SignalRegion.Relay));
                Assert.That(protectedPayloadSocket.IsOptional, Is.False);
                Assert.That(gantry.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(gantry.Find("Relay Heat Exchanger"), Is.Not.Null);
                Assert.That(gantry.Find("West Ceramic Deflector"), Is.Not.Null);
                Assert.That(gantry.Find("East Copper Deflector"), Is.Not.Null);
                Assert.That(gantry.Find("Relay Cooling Gantry Route Decal"), Is.Not.Null);
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.RelayTower));
                Assert.That(territory.HalfExtents, Is.EqualTo(new Vector2(5.7f, 4f)));
                Assert.That(Resources.Load<GameObject>("Environment/RelayCoolingGantryRegion"), Is.Not.Null);
                Assert.That(Resources.Load<GameObject>("Environment/RelayHeatExchanger"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/RelayCoolingGantryRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/RelayCoolingGantry/RelayCoolingGantryRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
                Assert.That(foundry.Find("Foundry South Bulkhead"), Is.Null);

                player.position = new Vector3(23f, 0f, -5.5f);
                yield return _move(gamepad, player, Vector2.down, () => player.position.z < -8.5f);
                Assert.That(player.position.z, Is.LessThan(-8.5f), "The west threshold should enter the gantry.");

                player.position = new Vector3(32f, 0f, -8.6f);
                yield return _move(gamepad, player, Vector2.up, () => player.position.z > -6.3f);
                var eastObstacles = string.Join(", ", Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None)
                    .Where(obstacle => Vector2.Distance(
                        obstacle.Center, new Vector2(player.position.x, player.position.z)) < 5f)
                    .Select(obstacle => $"{obstacle.name}@{obstacle.Center}"));
                Assert.That(player.position.z, Is.GreaterThan(-6.3f),
                    $"The east threshold should return to the Foundry. Nearby: {eastObstacles}");

                player.position = new Vector3(22.5f, 0f, -12.4f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The angled ceramic deflector should block projectiles as well as movement.");

                var gantryCenter = new Vector3(27.5f, 0f, -11.25f);
                game.DebugActivateTower();
                Assert.That(game.DebugIsPoweredAt(gantryCenter), Is.False);
                Assert.That(routing.activeSelf, Is.False);
                game.DebugActivateRelayTower();
                Assert.That(game.DebugIsPoweredAt(gantryCenter), Is.True);
                Assert.That(routing.activeSelf, Is.True);
                Assert.That(game.AuthoredSalvageSocketCount, Is.EqualTo(3));

                var relayCaches = game.transform.Cast<Transform>()
                    .Where(child => child.name == "Salvage Cache" &&
                                    (Vector3.Distance(child.position, gantryPayloadSocket.Position) < 0.1f ||
                                     Vector3.Distance(child.position, protectedPayloadSocket.Position) < 0.1f))
                    .ToArray();
                Assert.That(relayCaches.Length, Is.EqualTo(2));
                Assert.That(relayCaches.All(cache => cache.gameObject.activeSelf), Is.True);

                player.position = gantryPayloadSocket.Position;
                yield return null;
                Assert.That(game.IsRelayPayloadSecured, Is.True,
                    "The Cooling Gantry cache should satisfy the Relay payload branch.");
                Assert.That(relayCaches.All(cache => !cache.gameObject.activeSelf), Is.True,
                    "Securing either Relay payload must retire its authored sibling.");
                yield return null;
                Assert.That(game.CurrentMissionPhase, Is.EqualTo(5),
                    "The gantry branch should advance the required journey to the Spine tower.");
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
