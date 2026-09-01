using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Player;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class PlayerFollowCameraPlayModeTests
    {
        [UnityTest]
        public IEnumerator DroneMovement_AcceleratesCoastsAndBanksPresentation()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var presentation = player.Find("Drone Presentation");
                var body = presentation.Find("Drone Body Facing");
                var turret = presentation.Find("Drone Turret Facing");
                var signalWake = player.GetComponent<PlayerDroneSignalWake>();
                var tuning = Resources.Load<PlayerDroneMovementTuning>("Tuning/PlayerDroneMovementTuning");
                var startingPosition = player.position;
                var cameraForward = Object.FindFirstObjectByType<Camera>().transform.forward;
                cameraForward.y = 0f;
                cameraForward.Normalize();

                InputSystem.QueueStateEvent(gamepad, new GamepadState
                {
                    leftStick = Vector2.up,
                    rightStick = Vector2.right
                });
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                }

                var movementDirection = player.position - startingPosition;
                movementDirection.y = 0f;
                Assert.That(Vector3.Dot(movementDirection.normalized, cameraForward), Is.GreaterThan(0.95f),
                    "Up input should move toward the top of the gameplay camera rather than world north.");
                Assert.That(Quaternion.Angle(Quaternion.identity, body.localRotation), Is.GreaterThan(0.1f),
                    "Acceleration should bank the visual pivot without tilting the movement root.");
                Assert.That(Vector3.Dot(body.forward, cameraForward), Is.GreaterThan(0.95f),
                    "The chassis should face resolved movement rather than aim.");
                Assert.That(Vector3.Dot(turret.forward, Vector3.right), Is.GreaterThan(0.9f),
                    "The stabilized turret should independently follow right-stick aim.");
                Assert.That(turret.localPosition.y, Is.EqualTo(tuning.TurretMountHeight).Within(0.001f),
                    "The turret should remain visibly mounted above the chassis in perspective.");
                Assert.That(signalWake.IsEmitting, Is.True,
                    "The twin Signal wake should communicate retained flight speed.");
                Assert.That(Vector3.Dot(player.Find("Signal Wake Emitters").forward, cameraForward),
                    Is.GreaterThan(0.95f), "Wake spacing should align to resolved travel direction.");
                Assert.That(player.rotation.eulerAngles.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(player.rotation.eulerAngles.y, Is.EqualTo(0f).Within(0.01f));
                Assert.That(player.rotation.eulerAngles.z, Is.EqualTo(0f).Within(0.01f));

                var releasePosition = player.position;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;

                Assert.That(player.position.z, Is.GreaterThan(releasePosition.z),
                    "Releasing movement should brake retained velocity instead of stopping instantly.");
                Assert.That(game.HasPlayerMovementTuning, Is.True);

                signalWake.SetPaused(true);
                Assert.That(signalWake.IsEmitting, Is.False,
                    "Pausing should immediately suppress the presentation-only wake.");
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        [UnityTest]
        public IEnumerator PlayerMovement_MovesRigWithoutChangingCameraImpulseRestOffset()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var followCamera = Object.FindFirstObjectByType<PlayerFollowCamera>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var player = game.transform.Find("Maintenance Drone");
            var tuning = Resources.Load<PlayerCameraTuning>("Tuning/PlayerCameraTuning");

            Assert.That(game.HasPlayerCameraTuning, Is.True);
            Assert.That(game.IsPlayerCameraFollowing, Is.True);
            Assert.That(followCamera, Is.Not.Null);
            Assert.That(camera.transform.parent, Is.EqualTo(followCamera.transform));
            Assert.That(camera.orthographic, Is.False);
            Assert.That(camera.fieldOfView, Is.EqualTo(tuning.FieldOfView).Within(0.001f));
            var expectedCameraOffset = Quaternion.Euler(0f, tuning.Yaw, 0f) *
                                       new Vector3(0f, tuning.Height, -tuning.FollowDistance);
            Assert.That(camera.transform.localPosition,
                Is.EqualTo(expectedCameraOffset));
            Assert.That(Quaternion.Angle(
                    camera.transform.localRotation,
                    Quaternion.Euler(tuning.Pitch, tuning.Yaw, 0f)),
                Is.LessThan(0.001f));

            float startingRigX = followCamera.transform.position.x;
            player.position = new Vector3(8f, 0f, 0f);
            for (int frame = 0; frame < 20; frame++)
            {
                yield return null;
            }

            Assert.That(followCamera.transform.position.x, Is.GreaterThan(startingRigX + 2f),
                "The tactical camera rig should visibly travel with the player.");
            Assert.That(camera.transform.localPosition,
                Is.EqualTo(expectedCameraOffset),
                "Follow motion must not consume the child camera offset reserved for combat impulse.");
            var viewportPosition = camera.WorldToViewportPoint(player.position);
            Assert.That(viewportPosition.x, Is.InRange(0.1f, 0.9f));
            Assert.That(viewportPosition.y, Is.InRange(0.1f, 0.9f));

            player.position = new Vector3(18.7f, 0f, 0f);
            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            viewportPosition = camera.WorldToViewportPoint(player.position);
            Assert.That(viewportPosition.x, Is.InRange(0.08f, 0.92f),
                "The drone must remain visible on the far side of the authored eastern room.");
            Assert.That(viewportPosition.y, Is.InRange(0.08f, 0.92f));
        }

        [UnityTest]
        public IEnumerator DronePresentation_CommunicatesFireDamageCriticalRecoveryAndDefeat()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var presentationRoot = player.Find("Drone Presentation");
            var controller = player.GetComponent<PlayerDronePresentation>();
            var tool = presentationRoot.Find("Drone Turret Facing/Drone Tool");
            var restToolScale = tool.localScale;

            Assert.That(game.HasPlayerDronePresentation, Is.True);
            Assert.That(controller.IsConfigured, Is.True);

            controller.PlayFire(true);
            yield return null;
            Assert.That(tool.localScale.magnitude, Is.GreaterThan(restToolScale.magnitude),
                "Evolved fire should visibly emphasize the authored tool without moving the muzzle authority.");

            controller.PlayDamage(player.position + Vector3.right);
            yield return null;
            Assert.That(controller.IsDamageReacting, Is.True,
                "A resolved hostile hit should produce a bounded directional body reaction.");

            game.DebugApplyScenario(DebugScenario.CriticalRecovery);
            yield return null;
            Assert.That(controller.IsCritical, Is.True,
                "The drone should enter its urgent critical-Signal motion while deterministic recovery is active.");

            game.DebugActivateTower();
            yield return null;
            Assert.That(controller.IsCritical, Is.False,
                "Restoring Signal should settle the drone out of its critical presentation.");

            game.DebugApplyScenario(DebugScenario.Failure);
            yield return null;
            Assert.That(controller.IsDefeated, Is.True);
            Assert.That(presentationRoot.localPosition.y, Is.LessThan(-0.1f),
                "Defeat should leave a readable static collapsed pose.");

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            controller = Object.FindFirstObjectByType<DeadSignalGame>()
                .transform.Find("Maintenance Drone").GetComponent<PlayerDronePresentation>();
            Assert.That(controller.IsDefeated, Is.False,
                "A fresh run must reset every presentation-only state.");
        }
    }
}
