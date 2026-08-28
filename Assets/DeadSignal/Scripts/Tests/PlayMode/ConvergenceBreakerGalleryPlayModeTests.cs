using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.World;

namespace DeadSignal.Tests
{
    public sealed class ConvergenceBreakerGalleryPlayModeTests
    {
        [UnityTest]
        public IEnumerator Gallery_AddsTwoThresholdsSafeGateCoverAndPoweredReturn()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var chamber = game.transform.Find("Spine Induction Gallery Region/Convergence Chamber Region");
                var gallery = chamber.Find("Convergence Breaker Gallery Region");
                var territory = gallery.GetComponent<AuthoredPoweredTerritory>();
                var objective = gallery.GetComponent<AuthoredBreakerResetObjective>();
                var routing = gallery.Find("Breaker Gallery Signal Lines").gameObject;
                var available = gallery.Find("Breaker Reset Available").gameObject;
                var complete = gallery.Find("Breaker Reset Complete").gameObject;
                var entrance = gallery.GetComponentInChildren<AuthoredInterceptorEntrance>();

                Assert.That(gallery.position, Is.EqualTo(new Vector3(53f, 0f, 17f)));
                Assert.That(gallery.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(8));
                Assert.That(gallery.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(gallery.Find("Breaker Bank Assembly"), Is.Not.Null);
                Assert.That(objective, Is.Not.Null);
                Assert.That(objective.IsConfigured, Is.True);
                Assert.That(gallery.Find("South Ceramic Breaker Shield"), Is.Not.Null);
                Assert.That(gallery.Find("North Ceramic Breaker Shield"), Is.Not.Null);
                Assert.That(territory.Source, Is.EqualTo(PoweredTerritorySource.SpineTower));
                Assert.That(territory.HalfExtents, Is.EqualTo(new Vector2(3.15f, 3.65f)));
                Assert.That(Resources.Load<GameObject>("Environment/ConvergenceBreakerGalleryRegion"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/ConvergenceBreakerGalleryRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>(
                    "Materials/ConvergenceBreakerGallery/ConvergenceBreakerGalleryRouteDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
                Assert.That(chamber.Find("Convergence East Bulkhead"), Is.Null);

                player.position = new Vector3(48.6f, 0f, 14.8f);
                yield return _moveRight(gamepad, player, 50.2f);
                Assert.That(player.position.x, Is.GreaterThan(50.2f),
                    "The south threshold should independently enter the breaker gallery.");

                player.position = new Vector3(48.6f, 0f, 19.2f);
                yield return _moveRight(gamepad, player, 50.2f);
                Assert.That(player.position.x, Is.GreaterThan(50.2f),
                    "The north threshold should preserve a second tactical approach.");

                player.position = new Vector3(51f, 0f, 17f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The breaker bank should block projectiles and split firing positions.");

                player.position = new Vector3(49.9f, 0f, 14.8f);
                Assert.That(Vector3.Distance(player.position, entrance.Position), Is.GreaterThan(6f),
                    "The far-side gate should preserve the established entry safety exclusion.");
                Assert.That(game.SafestReinforcementEntryPosition, Is.EqualTo(entrance.Position));

                var galleryCenter = new Vector3(53f, 0f, 17f);
                game.DebugActivateRelayTower();
                Assert.That(game.DebugIsPoweredAt(galleryCenter), Is.False);
                Assert.That(routing.activeSelf, Is.False);
                game.DebugActivateSpineTower();
                game.DebugSelectOverclock(SignalOverclock.ChainArc);
                game.DebugSelectAuxiliary(SignalAuxiliaryOverclock.FeedbackShield);
                Assert.That(game.DebugIsPoweredAt(galleryCenter), Is.True);
                Assert.That(routing.activeSelf, Is.True,
                    "The dead-zone gallery should become a powered withdrawal foothold with the Spine tower.");

                Assert.That(game.IsBreakerDistributionReset, Is.False);
                Assert.That(available.activeSelf, Is.False);
                Assert.That(complete.activeSelf, Is.False);
                game.DebugChargeInductionLattice();
                game.DebugRouteFluxShunt();
                game.DebugCompleteConvergenceCalibration();
                yield return null;
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.BreakerReset));
                Assert.That(available.activeSelf, Is.True);
                Assert.That(complete.activeSelf, Is.False);

                player.position = game.BreakerResetPosition;
                yield return _interact(gamepad);
                Assert.That(game.IsBreakerDistributionReset, Is.True);
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.SpinePayload));
                Assert.That(available.activeSelf, Is.False);
                Assert.That(complete.activeSelf, Is.True);

                yield return _interact(gamepad);
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.SpinePayload),
                    "Repeated reset interaction must remain idempotent.");
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

        private static IEnumerator _interact(Gamepad gamepad)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
            yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
