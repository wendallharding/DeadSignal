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
    public sealed class SecurityTrialHeroFinishPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroFinish_DistinguishesRoomsAndPreservesTrialAuthority()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var wing = game.transform.Find(
                "Spine Induction Gallery Region/Convergence Chamber Region/Arc Furnace Region/Security Trial Wing Region");
            var chamber = wing.GetComponent<AuthoredCombatChamber>();
            var finish = wing.GetComponent<AuthoredSecurityTrialHeroFinish>();
            var drone = game.transform.Find("Maintenance Drone");

            Assert.That(finish, Is.Not.Null.And.Property("IsConfigured").True);
            Assert.That(finish.CommitmentRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(finish.LockdownRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(finish.VaultRenderer.GetComponents<Collider>(), Is.Empty);
            Assert.That(finish.CommitmentRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(64));
            Assert.That(finish.LockdownRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(128));
            Assert.That(finish.VaultRenderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(72));
            Assert.That(finish.CommitmentRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(finish.LockdownRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(finish.VaultRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(chamber.GetComponentsInChildren<AuthoredMapObstacle>(true).Length, Is.EqualTo(15));
            Assert.That(chamber.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(chamber.RewardSignal, Is.EqualTo(20f));
            Assert.That(Resources.Load<Texture2D>("Environment/SecurityTrialHeroAtlas"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/SecurityTrialCommitmentHeroFinish"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/SecurityTrialLockdownHeroFinish"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/SecurityTrialVaultHeroFinish"), Is.Not.Null);
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Locked));
            Assert.That(chamber.LockdownPresentationState, Is.EqualTo(LockdownChamberPresentationState.Dormant));

            game.DebugStabilizeCore();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.TrialCommitment));
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Available));
            drone.position = wing.TransformPoint(new Vector3(0f, 0f, 0f));
            yield return new WaitForSecondsRealtime(0.25f);
            _captureIfRequested(scene.PlayerCamera, "P13-Security-Trial-Commitment-1600x900.png");

            game.DebugCommitSecurityTrial();
            yield return null;
            Assert.That(chamber.CommitmentPresentationState,
                Is.EqualTo(TrialCommitmentPresentationState.CommittedActive));
            Assert.That(chamber.LockdownPresentationState,
                Is.EqualTo(LockdownChamberPresentationState.Armed));
            drone.position = wing.TransformPoint(new Vector3(0f, 0f, 21f));
            yield return new WaitForSecondsRealtime(0.25f);
            _captureIfRequested(scene.PlayerCamera, "P13-Security-Trial-Lockdown-1600x900.png");

            chamber.Complete();
            yield return null;
            Assert.That(chamber.LockdownPresentationState, Is.EqualTo(LockdownChamberPresentationState.Cleared));
            drone.position = wing.TransformPoint(new Vector3(0f, 0f, 42f));
            yield return new WaitForSecondsRealtime(0.25f);
            _captureIfRequested(scene.PlayerCamera, "P13-Security-Trial-Vault-1600x900.png");

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>(FindObjectsInactive.Include);
            Assert.That(chamber.CommitmentPresentationState, Is.EqualTo(TrialCommitmentPresentationState.Locked));
            Assert.That(chamber.LockdownPresentationState, Is.EqualTo(LockdownChamberPresentationState.Dormant));
        }

        private static void _captureIfRequested(Camera camera, string fileName)
        {
            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_SECURITY_TRIAL_CAPTURE_DIR");
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
