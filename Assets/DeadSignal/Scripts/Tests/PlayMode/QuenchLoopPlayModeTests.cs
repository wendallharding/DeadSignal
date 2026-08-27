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
    public sealed class QuenchLoopPlayModeTests
    {
        [UnityTest]
        public IEnumerator Loop_LinksFurnaceThresholdsAsPoweredGreedReturnFlank()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var sceneReferences = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
                var player = game.transform.Find("Maintenance Drone");
                var gallery = game.transform.Find("Spine Induction Gallery Region");
                var chamber = gallery.Find("Convergence Chamber Region");
                var furnace = chamber.Find("Arc Furnace Region");
                var loop = furnace.Find("Quench Loop Region");
                var territory = loop.GetComponent<AuthoredPoweredTerritory>();
                var routing = loop.Find("Quench Loop Signal Lines").gameObject;

                Assert.That(loop.position, Is.EqualTo(new Vector3(53f, 0f, 25.5f)));
                Assert.That(loop.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(10));
                Assert.That(loop.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(loop.Find("Quench Condenser Assembly"), Is.Not.Null);
                Assert.That(loop.Find("South Quench Deflector"), Is.Not.Null);
                Assert.That(loop.Find("North Quench Deflector"), Is.Not.Null);
                Assert.That(loop.Find("Quench Loop Route Decal"), Is.Not.Null);
                var shutter = loop.Find("Quench Pressure Shutter").gameObject;
                var cacheReturnSignal = loop.Find("Quench Cache Return Signal").gameObject;
                Assert.That(shutter.activeSelf, Is.True);
                Assert.That(cacheReturnSignal.activeSelf, Is.False);
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(territory.HalfExtents, Is.EqualTo(new Vector2(3.15f, 4.15f)));
                Assert.That(Resources.Load<GameObject>("Environment/QuenchLoopRegion"), Is.Not.Null);
                Assert.That(Resources.Load<GameObject>("Environment/QuenchCondenser"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/QuenchLoopRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/QuenchLoop/QuenchLoopRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/QuenchCacheReturnDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/QuenchLoop/QuenchCacheReturnDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
                Assert.That(sceneReferences.ArenaHalfExtents, Is.EqualTo(new Vector2(57.5f, 81f)));
                Assert.That(furnace.Find("Arc Furnace East Bulkhead"), Is.Null);

                player.position = new Vector3(48.4f, 0f, 23f);
                yield return _move(gamepad, player, Vector2.right, () => player.position.x > 50.2f);
                var nearbySouthObstacles = string.Join(", ", Object.FindObjectsByType<AuthoredMapObstacle>(
                        FindObjectsSortMode.None)
                    .Where(obstacle => Vector2.Distance(
                        obstacle.Center, new Vector2(player.position.x, player.position.z)) < 4f)
                    .Select(obstacle => $"{obstacle.name}@{obstacle.Center}"));
                Assert.That(player.position.x, Is.GreaterThan(50.2f),
                    $"The south threshold should connect the Furnace to the Quench Loop. Nearby: {nearbySouthObstacles}");

                player.position = new Vector3(55.2f, 0f, 23f);
                yield return _move(gamepad, player, Vector2.up, () => player.position.z > 28f);
                Assert.That(player.position.z, Is.GreaterThan(28f),
                    "The loop should provide a complete northbound route around the Furnace firing line.");

                player.position = new Vector3(50.6f, 0f, 28f);
                yield return _move(gamepad, player, Vector2.left, () => player.position.x < 48.8f);
                Assert.That(player.position.x, Is.LessThan(48.8f),
                    "The north threshold should connect the loop back into the Furnace cache lane.");

                player.position = new Vector3(53.8f, 0f, 21.7f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.up });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The angled Quench deflector should block projectiles as well as movement.");

                var loopCenter = new Vector3(53f, 0f, 25.5f);
                game.DebugActivateRelayTower();
                Assert.That(game.DebugIsPoweredAt(loopCenter), Is.False);
                Assert.That(routing.activeSelf, Is.False);
                game.DebugActivateSpineTower();
                Assert.That(game.DebugIsPoweredAt(loopCenter), Is.True,
                    "The loop should become a powered return flank with the Spine tower.");
                Assert.That(routing.activeSelf, Is.True);

                player.position = new Vector3(51.75f, 0f, 24.2f);
                yield return _move(gamepad, player, Vector2.up, () => player.position.z > 26.1f);
                Assert.That(player.position.z, Is.LessThan(25.3f),
                    "The authored pressure shutter should block the direct cut-through before the optional cache.");

                game.DebugMakeExtractionReady();
                while (!game.IsOptionalSalvageSecured)
                {
                    game.DebugCollectNextCache();
                    yield return null;
                }
                yield return null;

                Assert.That(shutter.activeSelf, Is.False,
                    "Securing the deep optional cache should retract the Quench pressure shutter.");
                Assert.That(cacheReturnSignal.activeSelf, Is.True,
                    "The opened cut-through should reveal its authored cyan return cue.");
                player.position = new Vector3(51.75f, 0f, 24.2f);
                yield return _move(gamepad, player, Vector2.up, () => player.position.z > 26.1f);
                Assert.That(player.position.z, Is.GreaterThan(26.1f),
                    "The released shutter should open the direct Quench return for movement.");
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
