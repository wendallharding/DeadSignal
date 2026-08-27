using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.World;

namespace DeadSignal.Tests
{
    public sealed class DepartureChannelPlayModeTests
    {
        [UnityTest]
        public IEnumerator OpeningReturn_PreservesTacticalWindowWithoutChangingCollision()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = UnityEngine.Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            var player = game.transform.Find("Maintenance Drone");
            var channel = GameObject.Find("Extraction Departure Channel").transform;
            game.DebugActivateTower();
            game.DebugTeleport(DebugLocation.Extraction);
            yield return new WaitForSeconds(1f);

            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900) })
            {
                camera.aspect = (float)resolution.x / resolution.y;
                var coverage = TacticalWindowCoverageDiagnostic.Measure(
                    camera,
                    UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                        .Where(renderer => renderer is MeshRenderer &&
                                           renderer.bounds.center.y > 0.2f && renderer.bounds.size.y > 0.45f));
                Assert.That(coverage, Is.Not.Empty);
                Debug.Log($"Opening tactical-window coverage at {resolution.x}x{resolution.y}:\n" + string.Join("\n",
                    coverage.Take(10).Select(item => $"{item.WindowCoverage:P1} {item.RendererName}")));
                Assert.That(coverage[0].WindowCoverage, Is.LessThanOrEqualTo(0.2f),
                    $"{coverage[0].RendererName} consumes too much of the opening tactical window at {resolution.x}x{resolution.y}.");
            }

            foreach (var capacitorName in new[] { "North Departure Capacitor", "South Departure Capacitor" })
            {
                var capacitor = channel.Find(capacitorName);
                Assert.That(capacitor.localScale, Is.EqualTo(new Vector3(1f, 0.5f, 1f)));
                Assert.That(capacitor.Find("Departure Capacitor Armor").localScale,
                    Is.EqualTo(new Vector3(0.5f, 1f, 1f)));
                Assert.That(capacitor.Find("Departure Capacitor Cells").localScale,
                    Is.EqualTo(new Vector3(0.5f, 1f, 1f)));
                Assert.That(capacitor.Find("Departure Threshold Beacons").localScale, Is.EqualTo(Vector3.one),
                    "The full-length low beacon rail should continue to describe the obstacle footprint.");
                var halfSize = capacitor.GetComponent<AuthoredMapObstacle>().ScaledHalfSize;
                Assert.That(halfSize.x, Is.EqualTo(2.3f).Within(0.001f),
                    $"{capacitorName} must retain its object-aligned movement and projectile footprint.");
                Assert.That(halfSize.y, Is.EqualTo(0.42f).Within(0.001f),
                    $"{capacitorName} must retain its object-aligned movement and projectile footprint.");
                Assert.That(capacitor.GetComponent<AuthoredMapObstacle>().OverlapsCircle(capacitor.position, 0.35f), Is.True);
            }

            Assert.That(channel.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(3));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(123));
            Assert.That(player, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator CargoShutter_ForcesOutboundFlanksAndOpensDirectExtractionReturn()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = UnityEngine.Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var channel = GameObject.Find("Extraction Departure Channel").transform;
                var shutter = channel.Find("Departure Cargo Shutter").gameObject;
                var returnSignal = channel.Find("Departure Cargo Return Signal").gameObject;
                var surgeSignal = channel.Find("Departure Capacitor Surge Signal").gameObject;

                Assert.That(channel.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(3));
                Assert.That(channel.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(shutter.activeSelf, Is.True);
                Assert.That(returnSignal.activeSelf, Is.False);
                Assert.That(surgeSignal.activeSelf, Is.False);
                Assert.That(Resources.Load<Texture2D>("Environment/DepartureCargoReturnDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/DepartureCargoReturnDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/DepartureCapacitorSurgeDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/DepartureCapacitorSurgeDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(123));

                var directStart = channel.TransformPoint(new Vector3(-1.35f, 0f, 0f));
                var directDirection = channel.TransformDirection(Vector3.right);
                player.position = directStart;
                yield return _move(gamepad, player, directDirection, () =>
                    channel.InverseTransformPoint(player.position).x > 0.75f);
                Assert.That(channel.InverseTransformPoint(player.position).x, Is.LessThan(-0.35f),
                    "The closed cargo shutter should block the direct extraction-channel line.");

                yield return _crossFlank(gamepad, player, channel, 2.15f);
                yield return _crossFlank(gamepad, player, channel, -2.15f);

                player.position = channel.TransformPoint(new Vector3(-1.35f, 0f, 0f));
                var aim = new Vector2(directDirection.x, directDirection.z).normalized;
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = aim }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = aim });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The closed cargo shutter should block projectiles as well as movement.");

                game.DebugMakeExtractionReady();
                yield return null;

                Assert.That(shutter.activeSelf, Is.False,
                    "Completing all three required regional payloads should retract the cargo shutter.");
                Assert.That(returnSignal.activeSelf, Is.True,
                    "The open channel should reveal its cyan direct-return cue.");
                Assert.That(surgeSignal.activeSelf, Is.True,
                    "The open channel should reveal the one-shot capacitor surge cue.");
                yield return _crossFlank(gamepad, player, channel, 2.15f);
                Assert.That(game.IsDepartureSurgeConsumed, Is.False,
                    "The protected flank should remain a valid return without consuming the direct-lane reserve.");
                game.DebugSetSignal(40f);
                yield return null;
                var signalBeforeSurge = game.CurrentSignal;
                var surgeStart = channel.TransformPoint(new Vector3(-2.85f, 0f, 0f));
                player.position = surgeStart;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                yield return _move(gamepad, player, directDirection, () =>
                    channel.InverseTransformPoint(player.position).x > 0.75f);
                Assert.That(channel.InverseTransformPoint(player.position).x, Is.GreaterThan(0.75f),
                    "The released shutter should open the direct route into extraction.");
                Assert.That(game.IsDepartureSurgeConsumed, Is.True);
                Assert.That(game.CurrentSignal, Is.GreaterThan(signalBeforeSurge),
                    "Crossing the direct return should discharge the one-shot Signal reserve.");
                Assert.That(surgeSignal.activeSelf, Is.False,
                    "The surge cue should switch off once its reserve is consumed.");

                var signalAfterSurge = game.CurrentSignal;
                player.position = surgeStart;
                yield return _move(gamepad, player, directDirection, () =>
                    channel.InverseTransformPoint(player.position).x > 0.75f);
                Assert.That(game.CurrentSignal, Is.LessThanOrEqualTo(signalAfterSurge),
                    "The departure capacitor must not restore Signal more than once.");
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _crossFlank(
            Gamepad gamepad,
            Transform player,
            Transform channel,
            float localZ)
        {
            player.position = channel.TransformPoint(new Vector3(-2.85f, 0f, localZ));
            var direction = channel.TransformDirection(Vector3.right);
            yield return _move(gamepad, player, direction, () =>
                channel.InverseTransformPoint(player.position).x > 2.85f);
            Assert.That(channel.InverseTransformPoint(player.position).x, Is.GreaterThan(2.85f),
                $"The {(localZ > 0f ? "north" : "south")} outbound flank should remain traversable.");
        }

        private static IEnumerator _move(
            Gamepad gamepad,
            Transform player,
            Vector3 worldDirection,
            Func<bool> complete)
        {
            var input = new Vector2(worldDirection.x, worldDirection.z).normalized;
            var deadline = Time.time + 2.5f;
            while (!complete() && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = input });
                yield return null;
            }

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
