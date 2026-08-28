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
using DeadSignal.Missions;
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
                var venting = trench.GetComponent<AuthoredSpineVentingObjective>();
                var signalLines = trench.Find("Discharge Trench Signal Lines").gameObject;

                Assert.That(trench, Is.Not.Null);
                Assert.That(trench.localPosition, Is.EqualTo(new Vector3(0f, 0f, -8f)));
                Assert.That(trench.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(6));
                Assert.That(trench.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(trench.GetComponentsInChildren<AuthoredInterceptorEntrance>().Length, Is.EqualTo(1));
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(venting, Is.Not.Null);
                Assert.That(venting.IsConfigured, Is.True);
                Assert.That(venting.Position, Is.EqualTo(trench.Find("Spine Berth Discharge Control").position));
                Assert.That(trench.Find("Spine Berth Discharge Control/Vent Available"), Is.Not.Null);
                Assert.That(trench.Find("Spine Berth Discharge Control/Berth Vented"), Is.Not.Null);
                Assert.That(signalLines.activeSelf, Is.False);
                Assert.That(Resources.Load<GameObject>("Environment/SpineDischargeTrenchRegion"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/SpineDischargeTrenchRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>(
                    "Materials/SpineDischargeTrench/SpineDischargeTrenchRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
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

                game.DebugActivateRelayTower();
                game.DebugCollectNextCache();
                game.DebugInstallRelayPayload();
                game.DebugSelectOverclock(SignalOverclock.ChainArc);
                game.DebugSelectAuxiliary(SignalAuxiliaryOverclock.FeedbackShield);
                game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
                yield return null;
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.SpineVenting));
                Assert.That(game.IsSpineBerthVented, Is.False);
                Assert.That(trench.Find("Spine Berth Discharge Control/Vent Available").gameObject.activeSelf, Is.True);

                player.position = game.SpineTowerInteractionPosition;
                yield return _interact(gamepad);
                Assert.That(game.IsSpineTowerOnline, Is.False,
                    "The pressurized berth must reject early Spine activation.");

                player.position = game.SpineVentingPosition;
                yield return _interact(gamepad);
                Assert.That(game.IsSpineBerthVented, Is.True);
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.SpineTower));
                Assert.That(trench.Find("Spine Berth Discharge Control/Vent Available").gameObject.activeSelf, Is.False);
                Assert.That(trench.Find("Spine Berth Discharge Control/Berth Vented").gameObject.activeSelf, Is.True);

                yield return _interact(gamepad);
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.SpineTower),
                    "The one-shot vent control must not duplicate or skip progression.");

                player.position = game.SpineTowerInteractionPosition;
                yield return _interact(gamepad);
                yield return null;
                Assert.That(game.IsSpineTowerOnline, Is.True);
                Assert.That(signalLines.activeSelf, Is.True,
                    "The dead-zone trench should become a powered return foothold with the Spine tower.");
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        [UnityTest]
        public IEnumerator Trench_ForegroundRenderersPreserveTacticalWindowWithoutChangingCollision()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var spine = game.transform.Find("Capacitor Spine Region");
            var trench = spine.Find("Spine Discharge Trench Region");
            var camera = Object.FindFirstObjectByType<Camera>();
            camera.aspect = 1600f / 900f;
            game.DebugActivateSpineTower();
            game.DebugTeleport(DebugLocation.SpineTower);
            yield return new WaitForSeconds(1f);
            Debug.Log($"[TACTICAL WINDOW] player={player.position} camera={camera.transform.position}");

            var coverage = TacticalWindowCoverageDiagnostic.Measure(
                camera,
                Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                    .Where(renderer => renderer.bounds.center.y > 0.2f && renderer.bounds.size.y > 0.45f));
            foreach (var item in coverage)
            {
                Debug.Log($"[TACTICAL WINDOW] {item.RendererName}: {item.WindowCoverage:P1} {item.ScreenRect}");
            }

            Assert.That(coverage, Is.Not.Empty);
            Assert.That(coverage[0].WindowCoverage, Is.LessThanOrEqualTo(0.2f),
                $"{coverage[0].RendererName} consumes too much of the central tactical window.");
            var northShield = spine.Find("North Capacitor Shield");
            var northShieldObstacles = northShield.GetComponents<AuthoredMapObstacle>();
            var northShieldCoverage = coverage.Where(item => item.RendererName.Contains("North Capacitor Shield"))
                .Max(item => item.WindowCoverage);
            Assert.That(northShieldCoverage, Is.LessThan(0.15f),
                "The authored north shield presentation should leave more of the Spine return lane visible.");
            Assert.That(northShieldObstacles.Select(obstacle => obstacle.ScaledHalfSize),
                Is.EquivalentTo(new[] { new Vector2(2.3f, 0.42f), new Vector2(1.2f, 0.45f) }),
                "Reducing presentation height must not alter the shield's object-aligned collision footprint.");
            Assert.That(northShieldObstacles.All(obstacle => obstacle.OverlapsCircle(northShield.position, 0.35f)), Is.True,
                "The visually shortened shield must remain movement- and projectile-authoritative.");
            Assert.That(spine.Find("Capacitor Transfer Bank").gameObject.activeSelf, Is.False,
                "The tactical-window sample should represent the powered Spine return state.");
            Assert.That(trench.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(6));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
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

        private static IEnumerator _interact(Gamepad gamepad)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
            yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
