using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class CargoAnnexMissionFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator CouplingRequiresCommitPickupAndWithdrawalBeforeObjectiveCompletion()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var objective = Object.FindFirstObjectByType<AuthoredCargoAnnexObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(objective, Is.Not.Null);
            Assert.That(objective.IsConfigured, Is.True);
            Assert.That(objective.HasReadabilityAssets, Is.True);
            Assert.That(objective.PresentationState, Is.EqualTo(CargoAnnexPresentationState.Locked));

            var heroFinish = Object.FindFirstObjectByType<AuthoredCargoAnnexHeroFinish>(FindObjectsInactive.Include);
            Assert.That(heroFinish, Is.Not.Null);
            Assert.That(heroFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CargoAnnexHeroFinish"));
            Assert.That(heroFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(heroFinish.FinishRenderer.sharedMaterials.All(material =>
                material.mainTexture != null && material.mainTexture.name == "CargoAnnexHeroAtlas"), Is.True);
            Assert.That(heroFinish.BarrierRendererCount, Is.EqualTo(9));
            Assert.That(heroFinish.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Cargo finish geometry must remain presentation-only and preserve the withdrawal lane.");
            Assert.That(heroFinish.CouplingBaseRenderer.sharedMaterial.name, Is.EqualTo("CargoAnnexGraphite"));

            var couplingBase = objective.transform.Find("Cargo Coupling Base");
            var couplingRotor = objective.transform.Find("Cargo Coupling Rotor");
            Assert.That(couplingBase, Is.Not.Null);
            Assert.That(couplingRotor, Is.Not.Null);
            Assert.That(couplingBase.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CargoCouplingBaseReadability"));
            Assert.That(couplingRotor.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CargoCouplingRotorReadability"));
            Assert.That(couplingBase.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThanOrEqualTo(24));
            Assert.That(couplingRotor.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThanOrEqualTo(24));
            Assert.That(couplingRotor.GetComponent<MeshFilter>().sharedMesh.HasVertexAttribute(
                UnityEngine.Rendering.VertexAttribute.TexCoord0), Is.True);
            Assert.That(couplingRotor.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("CargoCouplingStatusPanel"));
            Assert.That(couplingBase.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(couplingRotor.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Cargo readability meshes must remain presentation-only.");
            Assert.That(objective.transform.Find("Cargo Commitment Marker").gameObject.activeSelf, Is.False,
                "Locked Cargo machinery must not imply that its commitment threshold is active.");

            game.DebugActivateTower();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling));
            Assert.That(game.CurrentSalvage, Is.Zero);
            Assert.That(objective.PresentationState, Is.EqualTo(CargoAnnexPresentationState.Available));

            var justOutsideCommitment = Vector3.MoveTowards(
                objective.CommitmentPosition,
                objective.WithdrawalPosition,
                0.05f);
            scene.Player.position = justOutsideCommitment;
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.False);
            Assert.That(game.CargoCouplingPhase, Is.EqualTo(CargoCouplingRetrievalPhase.AwaitingCommit));

            scene.Player.position = objective.CommitmentPosition;
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.False,
                "Taking the coupling inside the pocket must not complete the objective.");
            Assert.That(game.CargoCouplingPhase, Is.EqualTo(CargoCouplingRetrievalPhase.Withdrawing));
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling));
            Assert.That(objective.PresentationState, Is.EqualTo(CargoAnnexPresentationState.Committed));

            scene.Player.position = Vector3.Lerp(objective.CommitmentPosition, objective.WithdrawalPosition, 0.5f);
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.False);
            Assert.That(game.CurrentSalvage, Is.Zero);

            scene.Player.position = objective.WithdrawalPosition;
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.True);
            Assert.That(game.CargoCouplingPhase, Is.EqualTo(CargoCouplingRetrievalPhase.Complete));
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CoolantSeal));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));
            Assert.That(objective.PresentationState, Is.EqualTo(CargoAnnexPresentationState.Secured));
            Assert.That(objective.transform.Find("Cargo Commitment Marker").gameObject.activeSelf, Is.False);
            Assert.That(objective.transform.Find("Cargo Withdrawal Marker").gameObject.activeSelf, Is.False);
            Assert.That(objective.transform.Find("Power Coupling Secured Marker").gameObject.activeSelf, Is.True);

            scene.Player.position = objective.CouplingPosition;
            yield return null;
            scene.Player.position = objective.WithdrawalPosition;
            yield return null;

            Assert.That(game.CurrentSalvage, Is.EqualTo(1), "The completed coupling must remain idempotent.");

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            objective = Object.FindFirstObjectByType<AuthoredCargoAnnexObjective>(FindObjectsInactive.Include);
            Assert.That(objective.PresentationState, Is.EqualTo(CargoAnnexPresentationState.Locked),
                "A fresh run must restore the persistent dormant Cargo read.");

            var capturePath = Environment.GetEnvironmentVariable("DEAD_SIGNAL_CARGO_HERO_CAPTURE");
            if (!string.IsNullOrWhiteSpace(capturePath))
            {
                scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
                scene.Player.position = objective.WithdrawalPosition;
                yield return null;
                yield return new WaitForSecondsRealtime(0.6f);
                _captureCamera(scene.PlayerCamera, capturePath);
            }
        }

        private static void _captureCamera(Camera camera, string path)
        {
            Assert.That(camera, Is.Not.Null);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

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
