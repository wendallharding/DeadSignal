using System;
using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Player;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class StationWallKitPlayModeTests
    {
        [UnityTest]
        public IEnumerator StationWallKit_FinishesRoomEdgesWithoutChangingTraversal()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var kit = Object.FindFirstObjectByType<AuthoredStationWallKit>();
            Assert.That(kit, Is.Not.Null);
            Assert.That(kit.IsConfigured, Is.True);
            Assert.That(kit.SectionCount, Is.EqualTo(6));
            Assert.That(kit.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The wall finish is presentation-only and must not change movement, projectile, or NavMesh collision.");
            Assert.That(kit.GetComponentsInChildren<AuthoredMapObstacle>(true), Is.Empty);

            var expectedNames = new[]
            {
                "Wall Faces", "Corner Caps", "Parapets", "Supports", "Shadow Backs", "End Pieces"
            };
            var expectedMaterials = new[]
            {
                "MaintenanceBulkhead", "DroneWhite", "StationSteel", "StationSteel", "StationBlack",
                "MaintenanceBulkhead"
            };
            for (var index = 0; index < expectedNames.Length; index++)
            {
                Assert.That(kit.Sections[index], Is.Not.Null);
                Assert.That(kit.Sections[index].name, Is.EqualTo(expectedNames[index]));
                Assert.That(kit.Sections[index].sharedMaterial.name, Is.EqualTo(expectedMaterials[index]));
                Assert.That(kit.Sections[index].GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
            }

            Assert.That(kit.Sections[0].bounds.Contains(new Vector3(27.5f, -0.2f, 7.3f)), Is.True,
                "The Relay-region wall face must be part of the shared kit.");
            Assert.That(kit.Sections[0].bounds.Contains(new Vector3(42.5f, -0.2f, 78.4f)), Is.True,
                "The Security Trial north wall must be part of the shared kit.");
            Assert.That(kit.Sections[0].bounds.Contains(new Vector3(12.5f, -0.2f, 54f)), Is.True,
                "The Security Trial west wall finish must follow Room B's expanded edge.");
            Assert.That(kit.Sections[0].bounds.Contains(new Vector3(72.5f, -0.2f, 54f)), Is.True,
                "The Security Trial east wall finish must follow Room B's expanded edge.");
            Assert.That(kit.Sections[2].bounds.Contains(new Vector3(12.5f, 0.24f, 54f)), Is.True,
                "The west parapet must follow Room B's expanded edge.");
            Assert.That(kit.Sections[2].bounds.Contains(new Vector3(72.5f, 0.24f, 54f)), Is.True,
                "The east parapet must follow Room B's expanded edge.");
            Assert.That(kit.Sections[2].bounds.max.y, Is.LessThanOrEqualTo(0.46f),
                "Parapets must stay low enough to preserve the combat field and foreground cutaway.");

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(141),
                "Presentation walls must preserve the established collision footprint.");
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            game.DebugTeleport(DebugLocation.CentralTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P25-Central-Wall-Kit-1600x900.png", 1600, 900);
            game.DebugTeleport(DebugLocation.RelayTower);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P25-Relay-Wall-Kit-1280x720.png", 1280, 720);
            _moveForCapture(references, new Vector3(42.5f, 0f, 25.5f));
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P25-Deep-Core-Wall-Kit-1600x900.png", 1600, 900);
            _moveForCapture(references, new Vector3(42.5f, 0f, 54f));
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P25-Security-Wall-Kit-1600x900.png", 1600, 900);
        }

        private static void _moveForCapture(DeadSignalSceneReferences references, Vector3 position)
        {
            references.Player.position = position;
            var followCamera = references.CameraRig.GetComponent<PlayerFollowCamera>();
            Assert.That(followCamera, Is.Not.Null);
            followCamera.SnapToFocus(position);
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P25_CAPTURE_DIR");
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
