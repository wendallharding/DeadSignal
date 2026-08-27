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
    public sealed class ConvergenceChamberPlayModeTests
    {
        [UnityTest]
        public IEnumerator Chamber_AddsDeepCoverLoopAndSafeSecurityDirection()
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
                var territory = chamber.GetComponent<AuthoredPoweredTerritory>();
                var entrances = chamber.GetComponentsInChildren<AuthoredInterceptorEntrance>();
                var entrance = entrances.Single(item => item.Priority == 5);
                var breakerGalleryEntrance = entrances.Single(item => item.Priority == 16);
                var routing = chamber.Find("Convergence Signal Lines").gameObject;

                Assert.That(chamber.position, Is.EqualTo(new Vector3(42.5f, 0f, 17f)));
                Assert.That(chamber.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(59));
                Assert.That(chamber.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(chamber.Find("Convergence Busbar Assembly"), Is.Not.Null);
                Assert.That(chamber.Find("West Convergence Baffle"), Is.Not.Null);
                Assert.That(chamber.Find("East Convergence Baffle"), Is.Not.Null);
                Assert.That(chamber.Find("Convergence Chamber Route Decal"), Is.Not.Null);
                Assert.That(entrance.Priority, Is.EqualTo(5));
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(territory.HalfExtents, Is.EqualTo(new Vector2(6.65f, 3.65f)));
                Assert.That(Resources.Load<GameObject>("Environment/ConvergenceChamberRegion"), Is.Not.Null);
                Assert.That(Resources.Load<GameObject>("Environment/ConvergenceBusbar"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/ConvergenceChamberRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>(
                    "Materials/ConvergenceChamber/ConvergenceChamberRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(137));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
                Assert.That(gallery.Find("Induction Gallery North Bulkhead"), Is.Null);
                Assert.That(gallery.Find("Induction Gallery North West"), Is.Not.Null);
                Assert.That(gallery.Find("Induction Gallery North Center"), Is.Not.Null);
                Assert.That(gallery.Find("Induction Gallery North East"), Is.Not.Null);

                player.position = new Vector3(38f, 0f, 11.75f);
                yield return _moveUp(gamepad, player, 14.25f);
                var nearbyWestObstacles = string.Join(", ", Object.FindObjectsByType<AuthoredMapObstacle>(
                        FindObjectsSortMode.None)
                    .Where(obstacle => Vector2.Distance(
                        obstacle.Center, new Vector2(player.position.x, player.position.z)) < 4f)
                    .Select(obstacle => $"{obstacle.name}@{obstacle.Center}"));
                Assert.That(player.position.z, Is.GreaterThan(13.5f),
                    $"The west doorway should connect the gallery to the deep chamber. Nearby: {nearbyWestObstacles}");

                player.position = new Vector3(47f, 0f, 11.75f);
                yield return _moveUp(gamepad, player, 14.25f);
                Assert.That(player.position.z, Is.GreaterThan(13.5f),
                    "The east doorway should preserve an independent flanking route.");

                player.position = new Vector3(38.7f, 0f, 18.25f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The rotated chamber baffle should block projectiles as well as movement.");

                var chamberCenter = new Vector3(42.5f, 0f, 17f);
                player.position = chamberCenter;
                Assert.That(game.SafestReinforcementEntryPosition, Is.EqualTo(breakerGalleryEntrance.Position),
                    "Deep-route pressure should select the new lateral gate instead of a distant old lane.");
                Assert.That(Vector3.Distance(player.position, breakerGalleryEntrance.Position), Is.GreaterThan(6f),
                    "The lateral gate should remain immediately safe while the player occupies the chamber.");
                game.DebugActivateRelayTower();
                Assert.That(game.DebugIsPoweredAt(chamberCenter), Is.False);
                Assert.That(routing.activeSelf, Is.False);

                game.DebugActivateSpineTower();
                Assert.That(game.DebugIsPoweredAt(chamberCenter), Is.True,
                    "The room should become a powered return foothold with the deepest tower.");
                Assert.That(routing.activeSelf, Is.True);
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _moveUp(Gamepad gamepad, Transform player, float targetZ)
        {
            var deadline = Time.time + 2f;
            while (player.position.z <= targetZ && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up });
                yield return null;
            }

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
