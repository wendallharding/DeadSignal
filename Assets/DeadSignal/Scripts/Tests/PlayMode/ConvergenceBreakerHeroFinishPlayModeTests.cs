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
    public sealed class ConvergenceBreakerHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_PreservesHoldoutDistributionAndPoweredReturn()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var convergence = game.transform.Find(
                "Spine Induction Gallery Region/Convergence Chamber Region");
            var breaker = convergence.Find("Convergence Breaker Gallery Region");
            var convergenceFinish = convergence.GetComponent<AuthoredConvergenceBreakerHeroFinish>();
            var breakerFinish = breaker.GetComponent<AuthoredConvergenceBreakerHeroFinish>();
            var convergenceObjective = convergence.GetComponent<AuthoredConvergenceCalibrationObjective>();
            var breakerObjective = breaker.GetComponent<AuthoredBreakerResetObjective>();

            Assert.That(convergenceFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(breakerFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(convergenceFinish.FinishRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(breakerFinish.FinishRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(convergenceFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("ConvergenceChamberHeroFinish"));
            Assert.That(breakerFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("BreakerGalleryHeroFinish"));
            Assert.That(convergenceFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.EqualTo(144));
            Assert.That(breakerFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                Is.EqualTo(80));
            Assert.That(convergenceFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(breakerFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(convergence.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(59));
            Assert.That(breaker.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(8));
            Assert.That(convergenceObjective.PresentationState,
                Is.EqualTo(ConvergenceCalibrationPresentationState.Dormant));
            Assert.That(breakerObjective.PresentationState,
                Is.EqualTo(BreakerResetPresentationState.DistributionLocked));

            game.DebugActivateSpineTower();
            game.DebugChargeInductionLattice();
            game.DebugRouteFluxShunt();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.ConvergenceCalibration));
            Assert.That(convergenceObjective.PresentationState,
                Is.EqualTo(ConvergenceCalibrationPresentationState.Available));
            Assert.That(game.DebugIsPoweredAt(convergence.position), Is.True);
            Assert.That(game.DebugIsPoweredAt(breaker.position), Is.True);

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_CONVERGENCE_BREAKER_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                game.transform.Find("Maintenance Drone").position = convergence.position + new Vector3(0f, 0f, -0.7f);
                yield return new WaitForSecondsRealtime(0.25f);
                _captureCamera(scene.PlayerCamera,
                    Path.Combine(captureDirectory, "P11-Convergence-Chamber-Hero-Finish-1600x900.png"));
                game.transform.Find("Maintenance Drone").position = breaker.position + new Vector3(0f, 0f, -0.7f);
                yield return new WaitForSecondsRealtime(0.25f);
                _captureCamera(scene.PlayerCamera,
                    Path.Combine(captureDirectory, "P11-Breaker-Gallery-Hero-Finish-1600x900.png"));
            }

            game.DebugCompleteConvergenceCalibration();
            yield return null;
            Assert.That(convergenceObjective.PresentationState,
                Is.EqualTo(ConvergenceCalibrationPresentationState.Complete));
            Assert.That(breakerObjective.PresentationState, Is.EqualTo(BreakerResetPresentationState.ResetAvailable));

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            convergenceObjective = Object.FindFirstObjectByType<AuthoredConvergenceCalibrationObjective>(
                FindObjectsInactive.Include);
            breakerObjective = Object.FindFirstObjectByType<AuthoredBreakerResetObjective>(FindObjectsInactive.Include);
            Assert.That(convergenceObjective.PresentationState,
                Is.EqualTo(ConvergenceCalibrationPresentationState.Dormant));
            Assert.That(breakerObjective.PresentationState,
                Is.EqualTo(BreakerResetPresentationState.DistributionLocked));
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
