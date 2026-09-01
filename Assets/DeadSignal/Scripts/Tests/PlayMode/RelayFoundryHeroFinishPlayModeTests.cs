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
    public sealed class RelayFoundryHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_PreservesRoomAuthorityAndTracksRelayPower()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var finish = Object.FindFirstObjectByType<AuthoredRelayFoundryHeroFinish>();
            var readability = Object.FindFirstObjectByType<AuthoredRelayNetworkReadability>();
            Assert.That(game, Is.Not.Null);
            var authoredObstacleCount = game.AuthoredMapObstacleCount;
            Assert.That(authoredObstacleCount, Is.GreaterThan(0));
            Assert.That(scene, Is.Not.Null);
            Assert.That(finish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(readability, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(finish.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The Foundry hero finish must remain presentation-only and collider-free.");
            Assert.That(finish.StructureRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("RelayFoundryHeroStructure"));
            Assert.That(finish.StructureRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(96));
            Assert.That(finish.StructureRenderer.sharedMaterials, Has.Length.EqualTo(3));
            Assert.That(finish.PowerRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("RelayFoundryHeroPower"));
            Assert.That(finish.PowerRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(48));
            Assert.That(finish.PowerRenderer.sharedMaterials, Has.Length.EqualTo(1));
            var turbine = finish.transform.Find("Relay Induction Turbine");
            var tower = finish.transform.Find("Relay Tower Assembly");
            Assert.That(turbine.GetComponentsInChildren<Renderer>(true), Has.Length.EqualTo(6));
            Assert.That(turbine.Find("Turbine Ceramic Ring").GetComponent<Renderer>().sharedMaterial.name,
                Is.EqualTo("RelayFoundryHeroCeramic"));
            Assert.That(turbine.Find("Turbine Signal Rotor").GetComponent<Renderer>().sharedMaterial.name,
                Is.EqualTo("RelayNetworkStatus"));
            Assert.That(tower.Find("Tower Base").GetComponent<Renderer>().sharedMaterial.name,
                Is.EqualTo("RelayFoundryHeroArmor"));
            Assert.That(tower.Find("Tower Column").GetComponent<Renderer>().sharedMaterial.name,
                Is.EqualTo("RelayFoundryHeroCeramic"));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(authoredObstacleCount));
            Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9));
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));

            game.DebugActivateTower();
            game.DebugCollectNextCache();
            game.DebugCollectNextCache();
            game.DebugRouteCentralComponents();
            game.DebugAssembleCentralPayload();
            game.DebugInstallCentralPayload();
            yield return null;
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.ActivationAvailable));

            game.DebugActivateRelayTower();
            yield return null;
            Assert.That(game.IsRelayTowerOnline, Is.True);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Activating));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Powered));
            Assert.That(finish.transform.Find("Relay Return Bulkhead").gameObject.activeSelf, Is.False);
            Assert.That(finish.transform.Find("Relay Return Threshold"), Is.Null,
                "The Foundry finish must not duplicate the Central route frame at the shared doorway.");
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(authoredObstacleCount),
                "Removing the duplicate frame must not change the authored collision registry.");

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_FOUNDRY_HERO_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                game.transform.Find("Maintenance Drone").position = game.RelayTowerPosition + new Vector3(-4.4f, 0f, -3.5f);
                yield return new WaitForSecondsRealtime(0.25f);
                _captureCamera(scene.PlayerCamera,
                    Path.Combine(captureDirectory, "P07-Relay-Foundry-Hero-Finish-1600x900.png"));
            }

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            readability = Object.FindFirstObjectByType<AuthoredRelayNetworkReadability>();
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
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
