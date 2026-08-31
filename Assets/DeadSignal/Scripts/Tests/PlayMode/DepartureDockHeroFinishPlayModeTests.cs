using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class DepartureDockHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_PreservesOpeningFlanksAndExtractionAuthority()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var channel = GameObject.Find("Extraction Departure Channel");
            var dockReadability = Object.FindFirstObjectByType<AuthoredExtractionDockReadability>();
            var dock = dockReadability.gameObject;
            var channelFinish = channel.GetComponent<AuthoredDepartureDockHeroFinish>();
            var dockFinish = dock.GetComponent<AuthoredDepartureDockHeroFinish>();

            _assertFinish(channelFinish, DepartureDockHeroOwner.DepartureChannel,
                "DepartureChannelHeroFinish", 80);
            _assertFinish(dockFinish, DepartureDockHeroOwner.ExtractionDock,
                "ExtractionDockHeroFinish", 112);
            Assert.That(channel.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(3));
            Assert.That(channel.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(dock.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
            Assert.That(dockReadability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Dormant));
            Assert.That(Resources.Load<Texture2D>("Environment/DepartureDockHeroAtlas"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/DepartureChannelHeroFinish"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/ExtractionDockHeroFinish"), Is.Not.Null);

            var player = game.transform.Find("Maintenance Drone");
            player.position = channel.transform.TransformPoint(new Vector3(-2.5f, 0f, 2.15f));
            yield return new WaitForSecondsRealtime(0.25f);
            _captureIfRequested(scene.PlayerCamera, "P14-Departure-Channel-1600x900.png");

            game.DebugMakeExtractionReady();
            yield return null;
            Assert.That(dockReadability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.Available));
            player.position = dock.transform.position + new Vector3(0f, 0f, -2.8f);
            yield return new WaitForSecondsRealtime(0.25f);
            _captureIfRequested(scene.PlayerCamera, "P14-Extraction-Dock-1600x900.png");

            game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(dockReadability.PresentationState, Is.EqualTo(ExtractionDockPresentationState.ActiveProgress));
        }

        private static void _assertFinish(
            AuthoredDepartureDockHeroFinish finish,
            DepartureDockHeroOwner owner,
            string meshName,
            int vertexCount)
        {
            Assert.That(finish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(finish.Owner, Is.EqualTo(owner));
            Assert.That(finish.Renderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(finish.Renderer.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo(meshName));
            Assert.That(finish.Renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(vertexCount));
            Assert.That(finish.Renderer.sharedMaterials, Has.Length.EqualTo(4));
        }

        private static void _captureIfRequested(Camera camera, string fileName)
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_DEPARTURE_DOCK_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                return;
            }

            Directory.CreateDirectory(captureDirectory);
            var renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
