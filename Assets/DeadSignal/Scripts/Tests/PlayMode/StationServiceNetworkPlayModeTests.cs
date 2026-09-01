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
    public sealed class StationServiceNetworkPlayModeTests
    {
        [UnityTest]
        public IEnumerator MajorMachinery_HasColliderFreeServiceConnections()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var network = Object.FindFirstObjectByType<AuthoredStationServiceNetwork>();
            Assert.That(network, Is.Not.Null);
            Assert.That(network.IsConfigured, Is.True);
            Assert.That(network.LayerCount, Is.EqualTo(4));
            Assert.That(network.ConnectionClusterCount, Is.EqualTo(8));
            Assert.That(network.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(network.GetComponentsInChildren<AuthoredMapObstacle>(true), Is.Empty);

            var expectedLayers = new[] { "Cable Trays", "Power Buses", "Coolant Pipes", "Junctions and Terminations" };
            foreach (var layerName in expectedLayers)
            {
                var layer = network.transform.Find(layerName);
                Assert.That(layer, Is.Not.Null, $"Missing station service layer {layerName}.");
                Assert.That(layer.GetComponent<MeshRenderer>(), Is.Not.Null);
                Assert.That(layer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
            }

            Assert.That(Resources.Load<GameObject>("Environment/StationServiceNetwork"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationCableTrays"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationPowerBuses"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationCoolantPipes"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StationServiceTerminations"), Is.Not.Null);

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            Assert.That(game, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));

            game.DebugTeleport(DeadSignal.Diagnostics.DebugLocation.CentralTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P28-Central-Services-1600x900.png", 1600, 900);
            game.DebugTeleport(DeadSignal.Diagnostics.DebugLocation.RelayTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P28-Relay-Services-1280x720.png", 1280, 720);
            references.Player.position = new Vector3(42.5f, 0f, 25.5f);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P28-Deep-Core-Services-1600x900.png", 1600, 900);
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P28_CAPTURE_DIR");
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
