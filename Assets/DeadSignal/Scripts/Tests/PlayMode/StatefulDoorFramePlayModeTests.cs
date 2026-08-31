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
    public sealed class StatefulDoorFramePlayModeTests
    {
        [UnityTest]
        public IEnumerator ProgressionDoors_KeepStatefulFramesWithoutChangingAuthority()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var doors = Object.FindObjectsByType<AuthoredRouteDoorReadability>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(doors, Has.Length.EqualTo(7));
            Assert.That(Resources.Load<GameObject>("Environment/StatefulDoorFrameKit"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StatefulDoorFrameHousing"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StatefulDoorFrameMechanisms"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StatefulDoorFrameStatus"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/StatefulDoorOpenGlyph"), Is.Not.Null);

            foreach (var door in doors)
            {
                var frame = door.FrameKit;
                Assert.That(door.IsConfigured, Is.True, $"{door.name} lost its existing door authority.");
                Assert.That(frame, Is.Not.Null, $"{door.name} has no persistent frame kit.");
                Assert.That(frame.IsConfigured, Is.True);
                Assert.That(frame.RendererCount, Is.EqualTo(4));
                Assert.That(frame.GetComponentsInChildren<Collider>(true), Is.Empty,
                    "Door-frame finish must not own movement, projectile, or NavMesh authority.");
                Assert.That(frame.GetComponentsInChildren<AuthoredMapObstacle>(true), Is.Empty);
                Assert.That(frame.transform.Find("Frame Housing"), Is.Not.Null);
                Assert.That(frame.transform.Find("Tracks Pistons and Pockets"), Is.Not.Null);
                Assert.That(frame.transform.Find("Threshold Seals and Warning Lamps"), Is.Not.Null);

                var openGlyph = frame.transform.Find("Open Route Glyph").GetComponent<Renderer>();
                Assert.That(door.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
                Assert.That(frame.IsOpen, Is.False);
                Assert.That(openGlyph.enabled, Is.False);

                door.SetOpen(true);
                Assert.That(door.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
                Assert.That(frame.IsOpen, Is.True);
                Assert.That(openGlyph.enabled, Is.True);
                door.SetOpen(false);
                Assert.That(frame.IsOpen, Is.False);
                Assert.That(openGlyph.enabled, Is.False);
            }

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var references = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            Assert.That(game, Is.Not.Null);
            Assert.That(references, Is.Not.Null);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138),
                "The frame kit must preserve the established collision-authority footprint.");

            game.DebugTeleport(DebugLocation.Shortcut);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P26-Central-Door-Locked-1600x900.png", 1600, 900);
            game.DebugOpenShortcut();
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P26-Central-Door-Open-1600x900.png", 1600, 900);
            game.DebugTeleport(DebugLocation.TransferVault);
            yield return new WaitForSecondsRealtime(0.5f);
            _captureIfRequested(references.PlayerCamera, "P26-Relay-Door-Locked-1280x720.png", 1280, 720);
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P26_CAPTURE_DIR");
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
