using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class CoolingGantryHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_PreservesProcessingAuthorityAndStateReadability()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var foundry = game.transform.Find("Relay Foundry Region");
            var gantry = foundry.Find("Relay Cooling Gantry Region");
            var finish = gantry.GetComponent<AuthoredCoolingGantryHeroFinish>();
            var readability = foundry.GetComponent<AuthoredRelayNetworkReadability>();
            Assert.That(finish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(readability, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(finish.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The Gantry finish must remain presentation-only and collider-free.");
            Assert.That(finish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CoolingGantryHeroFinish"));
            Assert.That(finish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(144));
            Assert.That(finish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(finish.FinishRenderer.sharedMaterials[0].name, Is.EqualTo("CoolingGantryHeroDeck"));
            Assert.That(finish.FinishRenderer.sharedMaterials[1].name, Is.EqualTo("CoolingGantryHeroCeramic"));
            Assert.That(finish.FinishRenderer.sharedMaterials[2].name, Is.EqualTo("CoolingGantryHeroCopper"));
            Assert.That(finish.FinishRenderer.sharedMaterials[3].name, Is.EqualTo("CoolingGantryHeroVent"));
            var exchanger = gantry.Find("Relay Heat Exchanger");
            Assert.That(exchanger.Find("Exchanger armored plinth").GetComponent<Renderer>().sharedMaterial.name,
                Is.EqualTo("CoolingGantryHeroDeck"));
            Assert.That(exchanger.Find("South copper manifold").GetComponent<Renderer>().sharedMaterial.name,
                Is.EqualTo("CoolingGantryHeroCopper"));
            Assert.That(exchanger.Find("West coolant coil").GetComponent<Renderer>().sharedMaterial.name,
                Is.EqualTo("RelayNetworkStatus"));
            Assert.That(gantry.GetComponentsInChildren<AuthoredMapObstacle>(), Has.Length.EqualTo(6));
            Assert.That(gantry.GetComponentsInChildren<AuthoredInterceptorEntrance>(), Has.Length.EqualTo(1));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
            Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));

            game.DebugActivateTower();
            game.DebugCollectNextCache();
            game.DebugCollectNextCache();
            game.DebugRouteCentralComponents();
            game.DebugAssembleCentralPayload();
            game.DebugInstallCentralPayload();
            game.DebugActivateRelayTower();
            yield return null;
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.ProcessingAvailable));

            game.DebugCollectNextCache();
            yield return null;
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.Active));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.Stabilized));

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_GANTRY_HERO_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                game.transform.Find("Maintenance Drone").position = gantry.position + new Vector3(-1.8f, 0f, 0.5f);
                yield return new WaitForSecondsRealtime(0.25f);
                _captureCamera(scene.PlayerCamera,
                    Path.Combine(captureDirectory, "P08-Cooling-Gantry-Hero-Finish-1600x900.png"));
            }

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            readability = Object.FindFirstObjectByType<AuthoredRelayNetworkReadability>();
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));
        }

        private static void _captureCamera(Camera camera, string path)
        {
            Assert.That(camera, Is.Not.Null);
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
                File.WriteAllBytes(path, texture.EncodeToPNG());
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
