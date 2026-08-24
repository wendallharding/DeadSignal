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
    public sealed class SpineInductionGalleryPlayModeTests
    {
        [UnityTest]
        public IEnumerator Gallery_AddsCoverRouteThatPowersWithSpine()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var spine = game.transform.Find("Capacitor Spine Region");
                var gallery = game.transform.Find("Spine Induction Gallery Region");
                var territory = gallery.GetComponent<AuthoredPoweredTerritory>();
                var routing = gallery.Find("Induction Gallery Signal Lines").gameObject;

                Assert.That(gallery.position, Is.EqualTo(new Vector3(42.5f, 0f, 8.5f)));
                Assert.That(gallery.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(6));
                Assert.That(gallery.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(gallery.Find("Induction Coil"), Is.Not.Null);
                Assert.That(gallery.Find("West Deflection Baffle"), Is.Not.Null);
                Assert.That(gallery.Find("East Deflection Baffle"), Is.Not.Null);
                Assert.That(gallery.Find("Induction Gallery Route Decal"), Is.Not.Null);
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(territory.HalfExtents, Is.EqualTo(new Vector2(6.65f, 3.15f)));
                Assert.That(Resources.Load<GameObject>("Environment/SpineInductionGalleryRegion"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/SpineInductionGalleryRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>(
                    "Materials/SpineInductionGallery/SpineInductionGalleryRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(50));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(4));
                Assert.That(spine.Find("Capacitor Spine North Bulkhead"), Is.Null);
                Assert.That(spine.Find("Capacitor Spine North West"), Is.Not.Null);
                Assert.That(spine.Find("Capacitor Spine North Center"), Is.Not.Null);
                Assert.That(spine.Find("Capacitor Spine North East"), Is.Not.Null);

                player.position = new Vector3(37.75f, 0f, 4.25f);
                yield return _moveUp(gamepad, player, 6f);
                Assert.That(player.position.z, Is.GreaterThan(6f),
                    "The west opening should connect the protected Spine lane to the outer gallery.");

                player.position = new Vector3(47.25f, 0f, 4.25f);
                yield return _moveUp(gamepad, player, 6f);
                Assert.That(player.position.z, Is.GreaterThan(6f),
                    "The east opening should independently reconnect beside the third tower.");

                player.position = new Vector3(38.2f, 0f, 9.15f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The angled gallery baffle should provide projectile-authoritative cover.");

                var galleryCenter = new Vector3(42.5f, 0f, 9.6f);
                game.DebugActivateRelayTower();
                Assert.That(game.DebugIsPoweredAt(galleryCenter), Is.False,
                    "The protected outward route should remain a Signal-spending dead-zone commitment.");
                Assert.That(routing.activeSelf, Is.False);

                game.DebugActivateSpineTower();
                Assert.That(game.DebugIsPoweredAt(galleryCenter), Is.True,
                    "The deepest tower should turn the gallery into a powered return foothold.");
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
