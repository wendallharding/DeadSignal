using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class PlayerFollowCameraPlayModeTests
    {
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
