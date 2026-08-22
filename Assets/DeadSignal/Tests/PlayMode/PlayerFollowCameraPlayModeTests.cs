using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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

                InputSystem.QueueStateEvent(gamepad, new GamepadState
                {
                    leftStick = Vector2.up,
                    rightStick = Vector2.right
                });
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                }

                Assert.That(player.position.z, Is.GreaterThan(startingPosition.z));
                Assert.That(Quaternion.Angle(Quaternion.identity, body.localRotation), Is.GreaterThan(0.1f),
                    "Acceleration should bank the visual pivot without tilting the movement root.");
                Assert.That(Vector3.Dot(body.forward, Vector3.forward), Is.GreaterThan(0.95f),
                    "The chassis should face resolved movement rather than aim.");
                Assert.That(Vector3.Dot(turret.forward, Vector3.right), Is.GreaterThan(0.9f),
                    "The stabilized turret should independently follow right-stick aim.");
                Assert.That(turret.localPosition.y, Is.EqualTo(tuning.TurretMountHeight).Within(0.001f),
                    "The turret should remain visibly mounted above the chassis in perspective.");
                Assert.That(signalWake.IsEmitting, Is.True,
                    "The twin Signal wake should communicate retained flight speed.");
                Assert.That(Vector3.Dot(player.Find("Signal Wake Emitters").forward, Vector3.forward),
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
            Assert.That(camera.transform.localPosition,
                Is.EqualTo(new Vector3(0f, tuning.Height, -tuning.FollowDistance)));
            Assert.That(camera.transform.localRotation.eulerAngles.x, Is.EqualTo(tuning.Pitch).Within(0.001f));

            float startingRigX = followCamera.transform.position.x;
            player.position = new Vector3(8f, 0f, 0f);
            for (int frame = 0; frame < 20; frame++)
            {
                yield return null;
            }

            Assert.That(followCamera.transform.position.x, Is.GreaterThan(startingRigX + 2f),
                "The tactical camera rig should visibly travel with the player.");
            Assert.That(camera.transform.localPosition,
                Is.EqualTo(new Vector3(0f, tuning.Height, -tuning.FollowDistance)),
                "Follow motion must not consume the child camera offset reserved for combat impulse.");
            var viewportPosition = camera.WorldToViewportPoint(player.position);
            Assert.That(viewportPosition.x, Is.InRange(0.1f, 0.9f));
            Assert.That(viewportPosition.y, Is.InRange(0.1f, 0.9f));
        }
    }
}
