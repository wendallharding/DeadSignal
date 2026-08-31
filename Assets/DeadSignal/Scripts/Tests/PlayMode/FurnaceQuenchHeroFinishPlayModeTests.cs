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
    public sealed class FurnaceQuenchHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_PreservesForgeQuenchAndShortcutAuthority()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var furnace = game.transform.Find(
                "Spine Induction Gallery Region/Convergence Chamber Region/Arc Furnace Region");
            var quench = furnace.Find("Quench Loop Region");
            var furnaceFinish = furnace.GetComponent<AuthoredFurnaceQuenchHeroFinish>();
            var quenchFinish = quench.GetComponent<AuthoredFurnaceQuenchHeroFinish>();
            var forgeObjective = furnace.GetComponent<AuthoredFurnaceForgeObjective>();
            var stabilizationObjective = quench.GetComponent<AuthoredQuenchStabilizationObjective>();
            var shutter = quench.Find("Quench Pressure Shutter").gameObject;
            var drone = game.transform.Find("Maintenance Drone");

            Assert.That(furnaceFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(quenchFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(furnaceFinish.FinishRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(quenchFinish.FinishRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(furnaceFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("ArcFurnaceHeroFinish"));
            Assert.That(quenchFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("QuenchLoopHeroFinish"));
            Assert.That(furnaceFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.EqualTo(112));
            Assert.That(quenchFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.EqualTo(72));
            Assert.That(furnaceFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(quenchFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(furnace.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(37));
            Assert.That(quench.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(10));
            Assert.That(forgeObjective.PresentationState, Is.EqualTo(CoreProcessingPresentationState.Locked));
            Assert.That(stabilizationObjective.PresentationState, Is.EqualTo(CoreProcessingPresentationState.Locked));
            Assert.That(shutter.activeSelf, Is.True);
            Assert.That(Resources.Load<Texture2D>("Environment/FurnaceQuenchHeroAtlas"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/ArcFurnaceHeroFinish"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/QuenchLoopHeroFinish"), Is.Not.Null);

            game.DebugResetBreakerDistribution();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.FurnaceForge));
            Assert.That(forgeObjective.PresentationState, Is.EqualTo(CoreProcessingPresentationState.Available));
            drone.position = furnace.position + new Vector3(0f, 0f, -0.7f);
            yield return new WaitForSecondsRealtime(0.25f);
            _captureIfRequested(scene.PlayerCamera, "P12-Arc-Furnace-Hero-Finish-1600x900.png");

            game.DebugForgeLattice();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.QuenchStabilization));
            Assert.That(stabilizationObjective.PresentationState, Is.EqualTo(CoreProcessingPresentationState.Available));
            drone.position = quench.position + new Vector3(-2f, 0f, -2f);
            yield return new WaitForSecondsRealtime(0.25f);
            _captureIfRequested(scene.PlayerCamera, "P12-Quench-Loop-Hero-Finish-1600x900.png");

            game.DebugStabilizeCore();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.TrialCommitment));
            Assert.That(shutter.activeSelf, Is.False);

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            forgeObjective = Object.FindFirstObjectByType<AuthoredFurnaceForgeObjective>(FindObjectsInactive.Include);
            stabilizationObjective = Object.FindFirstObjectByType<AuthoredQuenchStabilizationObjective>(
                FindObjectsInactive.Include);
            Assert.That(forgeObjective.PresentationState, Is.EqualTo(CoreProcessingPresentationState.Locked));
            Assert.That(stabilizationObjective.PresentationState, Is.EqualTo(CoreProcessingPresentationState.Locked));
        }

        private static void _captureIfRequested(Camera camera, string fileName)
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_FURNACE_QUENCH_CAPTURE_DIR");
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
