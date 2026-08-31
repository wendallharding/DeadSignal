using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class WithdrawalLandmarkHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinishes_PreserveCoverAuthorityAndRenderOwningThreats()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var wardenBay = GameObject.Find("Security Warden Staging Bay");
            var sapperCradle = GameObject.Find("Signal Sapper Service Cradle");
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            _assertFinish(wardenBay, "Warden Bay Hero Finish", "WardenBayHeroFinish", 88, 3);
            _assertFinish(sapperCradle, "Sapper Cradle Hero Finish", "SapperCradleHeroFinish", 80, 2);
            Assert.That(scene.Warden.gameObject.activeSelf, Is.False,
                "The dormant Warden mount must not present an active threat before gameplay releases it.");
            Assert.That(scene.Sapper.gameObject.activeSelf, Is.False,
                "The dormant Sapper cradle must not present an active threat before gameplay releases it.");

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_WITHDRAWAL_HERO_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                yield break;
            }

            Directory.CreateDirectory(captureDirectory);
            var player = game.transform.Find("Maintenance Drone");
            player.position = wardenBay.transform.position + new Vector3(-2.4f, 0f, -2.6f);
            game.DebugSpawnThreat(SecurityReinforcement.Warden);
            yield return new WaitForSecondsRealtime(0.35f);
            _captureCamera(scene.PlayerCamera,
                Path.Combine(captureDirectory, "P06-Warden-Bay-Hero-Finish-1600x900.png"));

            player.position = sapperCradle.transform.position + new Vector3(2.4f, 0f, -2.6f);
            game.DebugSpawnThreat(SecurityReinforcement.Sapper);
            yield return new WaitForSecondsRealtime(0.35f);
            _captureCamera(scene.PlayerCamera,
                Path.Combine(captureDirectory, "P06-Sapper-Cradle-Hero-Finish-1600x900.png"));
        }

        private static void _assertFinish(
            GameObject landmark,
            string childName,
            string meshName,
            int vertexCount,
            int obstacleCount)
        {
            Assert.That(landmark, Is.Not.Null);
            var finish = landmark.transform.Find(childName);
            Assert.That(finish, Is.Not.Null);
            Assert.That(finish.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Hero finishes must remain presentation-only and collider-free.");
            Assert.That(landmark.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(obstacleCount));
            var filter = finish.GetComponent<MeshFilter>();
            var renderer = finish.GetComponent<MeshRenderer>();
            Assert.That(filter, Is.Not.Null);
            Assert.That(filter.sharedMesh.name, Is.EqualTo(meshName));
            Assert.That(filter.sharedMesh.vertexCount, Is.EqualTo(vertexCount));
            Assert.That(filter.sharedMesh.subMeshCount, Is.EqualTo(4));
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(4));
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
