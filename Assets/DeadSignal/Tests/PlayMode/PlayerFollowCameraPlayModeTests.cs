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
                var signalWake = player.GetComponent<PlayerDroneSignalWake>();
                var startingPosition = player.position;

                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.up });
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                }

                Assert.That(player.position.z, Is.GreaterThan(startingPosition.z));
                Assert.That(Quaternion.Angle(Quaternion.identity, presentation.localRotation), Is.GreaterThan(0.1f),
                    "Acceleration should bank the visual pivot without tilting the movement root.");
                Assert.That(signalWake.IsEmitting, Is.True,
                    "The twin Signal wake should communicate retained flight speed.");
                Assert.That(player.rotation.eulerAngles.x, Is.EqualTo(0f).Within(0.01f));
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
            Assert.That(camera.orthographicSize, Is.EqualTo(tuning.OrthographicSize).Within(0.001f));
            Assert.That(camera.transform.localPosition, Is.EqualTo(new Vector3(0f, 20f, 0f)));

            float startingRigX = followCamera.transform.position.x;
            player.position = new Vector3(8f, 0f, 0f);
            for (int frame = 0; frame < 20; frame++)
            {
                yield return null;
            }

            Assert.That(followCamera.transform.position.x, Is.GreaterThan(startingRigX + 2f),
                "The tactical camera rig should visibly travel with the player.");
            Assert.That(camera.transform.localPosition, Is.EqualTo(new Vector3(0f, 20f, 0f)),
                "Follow motion must not consume the child camera offset reserved for combat impulse.");
            var viewportPosition = camera.WorldToViewportPoint(player.position);
            Assert.That(viewportPosition.x, Is.InRange(0.1f, 0.9f));
            Assert.That(viewportPosition.y, Is.InRange(0.1f, 0.9f));
        }
    }
}
