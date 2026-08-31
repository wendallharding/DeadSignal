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
    public sealed class InductionFluxHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_PreservesDistinctLifecycleAndPoweredReturn()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var induction = game.transform.Find("Spine Induction Gallery Region");
            var flux = induction.Find("Flux Bypass Region");
            var inductionFinish = induction.GetComponent<AuthoredInductionFluxHeroFinish>();
            var fluxFinish = flux.GetComponent<AuthoredInductionFluxHeroFinish>();
            var inductionObjective = induction.GetComponent<AuthoredInductionLatticeObjective>();
            var fluxObjective = flux.GetComponent<AuthoredFluxShuntObjective>();

            Assert.That(inductionFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(fluxFinish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(inductionFinish.FinishRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(fluxFinish.FinishRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(inductionFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("InductionGalleryHeroFinish"));
            Assert.That(fluxFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("FluxBypassHeroFinish"));
            Assert.That(inductionFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(160));
            Assert.That(fluxFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(104));
            Assert.That(inductionFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(fluxFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(inductionObjective.PresentationState,
                Is.EqualTo(InductionLatticePresentationState.PrerequisiteLocked));
            Assert.That(fluxObjective.PresentationState, Is.EqualTo(FluxShuntPresentationState.PrerequisiteLocked));
            Assert.That(induction.Find("Induction Gallery Signal Lines").gameObject.activeSelf, Is.False);
            Assert.That(flux.Find("Flux Bypass Signal Lines").gameObject.activeSelf, Is.False);

            game.DebugActivateSpineTower();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.InductionLattice));
            Assert.That(inductionObjective.PresentationState, Is.EqualTo(InductionLatticePresentationState.ChargeAvailable));

            game.DebugChargeInductionLattice();
            yield return null;
            Assert.That(inductionObjective.PresentationState, Is.EqualTo(InductionLatticePresentationState.Charging));
            Assert.That(fluxObjective.PresentationState, Is.EqualTo(FluxShuntPresentationState.RoutingAvailable));

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_INDUCTION_FLUX_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                game.transform.Find("Maintenance Drone").position = induction.position + new Vector3(0f, 0f, -0.9f);
                yield return new WaitForSecondsRealtime(0.25f);
                _captureCamera(scene.PlayerCamera,
                    Path.Combine(captureDirectory, "P10-Induction-Gallery-Hero-Finish-1600x900.png"));
                game.transform.Find("Maintenance Drone").position = flux.position + new Vector3(0.35f, 0f, -0.9f);
                yield return new WaitForSecondsRealtime(0.25f);
                _captureCamera(scene.PlayerCamera,
                    Path.Combine(captureDirectory, "P10-Flux-Bypass-Hero-Finish-1600x900.png"));
            }

            game.DebugRouteFluxShunt();
            yield return null;
            Assert.That(fluxObjective.PresentationState, Is.EqualTo(FluxShuntPresentationState.Routing));
            Assert.That(induction.Find("Induction Gallery Signal Lines").gameObject.activeSelf, Is.True);
            Assert.That(flux.Find("Flux Bypass Signal Lines").gameObject.activeSelf, Is.True);

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            inductionObjective = Object.FindFirstObjectByType<AuthoredInductionLatticeObjective>(FindObjectsInactive.Include);
            fluxObjective = Object.FindFirstObjectByType<AuthoredFluxShuntObjective>(FindObjectsInactive.Include);
            Assert.That(inductionObjective.PresentationState,
                Is.EqualTo(InductionLatticePresentationState.PrerequisiteLocked));
            Assert.That(fluxObjective.PresentationState, Is.EqualTo(FluxShuntPresentationState.PrerequisiteLocked));
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
