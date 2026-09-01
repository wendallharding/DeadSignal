using System;
using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class StationNavigationSignagePlayModeTests
    {
        [UnityTest]
        public IEnumerator StationRoute_HasTextFreeColliderFreeSignageLayers()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var signage = Object.FindFirstObjectByType<AuthoredStationNavigationSignage>();
            Assert.That(signage, Is.Not.Null);
            Assert.That(signage.IsConfigured, Is.True);
            Assert.That(signage.LayerCount, Is.EqualTo(5));
            Assert.That(signage.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(signage.GetComponentsInChildren<AuthoredMapObstacle>(true), Is.Empty);
            Assert.That(signage.GetComponentsInChildren<TextMesh>(true), Is.Empty,
                "World signage must use learned symbols instead of duplicating objective copy.");

            var expectedLayers = new[]
            {
                "Sector Symbols", "Hazard Bands", "Directional Chevrons", "Room Identifiers", "Powered Return Decals"
            };
            foreach (var layerName in expectedLayers)
            {
                var layer = signage.transform.Find(layerName);
                Assert.That(layer, Is.Not.Null, $"Missing signage layer {layerName}.");
                Assert.That(layer.GetComponent<MeshRenderer>(), Is.Not.Null);
                Assert.That(layer.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            }

            Assert.That(Resources.Load<GameObject>("Environment/StationNavigationSignageKit"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationSectorSymbols"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationHazardBands"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationDirectionalChevrons"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationRoomIdentifiers"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationPoweredReturnDecals"), Is.Not.Null);

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            Assert.That(game, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));

            game.DebugTeleport(DebugLocation.CentralTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P27-Central-Signage-1600x900.png", 1600, 900);
            references.Player.position = new Vector3(42.5f, 0f, 18.2f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P27-Spine-Signage-1280x720.png", 1280, 720);
            references.Player.position = new Vector3(42.5f, 0f, 50.2f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P27-Trial-Signage-1600x900.png", 1600, 900);
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P27_CAPTURE_DIR");
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
