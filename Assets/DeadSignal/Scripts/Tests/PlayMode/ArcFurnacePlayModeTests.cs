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
    public sealed class ArcFurnacePlayModeTests
    {
        [UnityTest]
        public IEnumerator Furnace_ExtendsGreedRouteWithTwoDistinctApproaches()
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
                var furnace = chamber.Find("Arc Furnace Region");
                var sceneReferences = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
                var territory = furnace.GetComponent<AuthoredPoweredTerritory>();
                var socket = furnace.GetComponentInChildren<AuthoredSalvageSocket>();
                var routing = furnace.Find("Arc Furnace Signal Lines").gameObject;

                Assert.That(furnace.position, Is.EqualTo(new Vector3(42.5f, 0f, 25.5f)));
                Assert.That(furnace.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(37));
                Assert.That(furnace.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(furnace.Find("Arc Furnace Assembly"), Is.Not.Null);
                Assert.That(furnace.Find("West Furnace Shield South"), Is.Not.Null);
                Assert.That(furnace.Find("West Furnace Shield North"), Is.Not.Null);
                Assert.That(furnace.Find("Arc Furnace Route Decal"), Is.Not.Null);
                Assert.That(socket.Position, Is.EqualTo(new Vector3(42.5f, 0f, 29.05f)));
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(Resources.Load<GameObject>("Environment/ArcFurnaceRegion"), Is.Not.Null);
                Assert.That(Resources.Load<GameObject>("Environment/ArcFurnace"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/ArcFurnaceRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/ArcFurnace/ArcFurnaceRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
                Assert.That(game.AuthoredSalvageSocketCount, Is.EqualTo(3));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
                Assert.That(sceneReferences.ArenaHalfExtents, Is.EqualTo(new Vector2(57.5f, 81f)));
                Assert.That(chamber.Find("Convergence North Bulkhead"), Is.Null);

                player.position = new Vector3(37.9f, 0f, 19.2f);
                yield return _move(gamepad, player, Vector2.up, () => player.position.z > 22.2f);
                Assert.That(player.position.z, Is.GreaterThan(22.2f),
                    "The west threshold should open into the protected switchback.");

                player.position = new Vector3(47.1f, 0f, 19.2f);
                yield return _move(gamepad, player, Vector2.up, () => player.position.z > 22.2f);
                Assert.That(player.position.z, Is.GreaterThan(22.2f),
                    "The east threshold should open into the exposed firing lane.");

                player.position = new Vector3(42.5f, 0f, 22.5f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.up });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The furnace landmark should block projectiles and create a real positioning split.");

                var furnaceCenter = new Vector3(42.5f, 0f, 25.5f);
                game.DebugActivateRelayTower();
                Assert.That(game.DebugIsPoweredAt(furnaceCenter), Is.False);
                Assert.That(routing.activeSelf, Is.False);
                game.DebugActivateSpineTower();
                Assert.That(game.DebugIsPoweredAt(furnaceCenter), Is.True);
                Assert.That(routing.activeSelf, Is.True);
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _move(
            Gamepad gamepad,
            Transform player,
            Vector2 direction,
            System.Func<bool> complete)
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
