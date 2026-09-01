using System;
using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class StationFunctionalPropsPlayModeTests
    {
        [UnityTest]
        public IEnumerator RequiredRoute_HasColliderFreeFunctionalPropKit()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var props = Object.FindFirstObjectByType<AuthoredStationFunctionalProps>();
            Assert.That(props, Is.Not.Null);
            Assert.That(props.IsConfigured, Is.True);
            Assert.That(props.PropTypeCount, Is.EqualTo(6));
            Assert.That(props.PlacementCount, Is.EqualTo(18));
            Assert.That(props.PropRenderers, Has.Length.EqualTo(18));
            Assert.That(props.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(props.GetComponentsInChildren<AuthoredMapObstacle>(true), Is.Empty);

            var expectedProps = new[]
            {
                "CargoCrate", "ToolCart", "ServiceCanister", "CableReel", "GuardRail", "MaintenanceFixture"
            };
            foreach (var propName in expectedProps)
            {
                var prefab = Resources.Load<GameObject>($"Environment/FunctionalProps/{propName}");
                Assert.That(prefab, Is.Not.Null, $"Missing functional prop prefab {propName}.");
                Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
                Assert.That(prefab.GetComponent<MeshRenderer>().sharedMaterial.enableInstancing, Is.True);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);
            }

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            Assert.That(game, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));

            references.Player.position = new Vector3(-11.2f, 0f, -2.9f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P30-Dock-Crates-1600x900.png", 1600, 900);
            references.Player.position = new Vector3(25.2f, 0f, 8.9f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P30-Relay-Cart-1280x720.png", 1280, 720);
            references.Player.position = new Vector3(46f, 0f, 73.6f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P30-Trial-Fixture-1600x900.png", 1600, 900);
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P30_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                return;
            }

            Directory.CreateDirectory(captureDirectory);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(renderTexture);
                Object.Destroy(texture);
            }
        }
    }
}
