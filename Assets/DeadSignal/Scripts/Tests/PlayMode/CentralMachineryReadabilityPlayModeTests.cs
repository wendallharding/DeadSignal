using System.Collections;
using DeadSignal.Application;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class CentralMachineryReadabilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator CentralTowerAndTransferVault_ShowPersistentAuthoritativeStatesAndReset()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var tower = Object.FindFirstObjectByType<AuthoredCentralTowerReadability>(FindObjectsInactive.Include);
            var transfer = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(tower, Is.Not.Null);
            Assert.That(transfer, Is.Not.Null);
            Assert.That(tower.IsConfigured, Is.True);
            Assert.That(transfer.HasReadabilityAssets, Is.True);
            Assert.That(tower.State, Is.EqualTo(CentralTowerPresentationState.ActivationAvailable));
            Assert.That(transfer.PresentationState, Is.EqualTo(TransferVaultPresentationState.Locked));

            var towerCore = tower.transform.Find("Tower Core");
            var assembler = transfer.transform.Find("Transfer Assembler Rotor");
            Assert.That(towerCore.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("CentralTowerCoreReadability"));
            Assert.That(towerCore.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("CentralMachineryStatusPanel"));
            Assert.That(assembler.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("TransferVaultAssemblerReadability"));
            Assert.That(assembler.GetComponent<Renderer>().sharedMaterials[1].mainTexture.name,
                Is.EqualTo("CentralMachineryStatusPanel"));
            Assert.That(tower.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The purpose-built landmark meshes must remain presentation-only.");
            Assert.That(assembler.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The assembler mesh must not become interaction or movement authority.");

            game.DebugActivateTower();
            yield return null;
            Assert.That(tower.State, Is.EqualTo(CentralTowerPresentationState.Activating));
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(tower.State, Is.EqualTo(CentralTowerPresentationState.Powered));

            game.DebugCollectNextCache();
            game.DebugCollectNextCache();
            game.DebugRouteCentralComponents();
            game.DebugRouteCentralComponents();
            yield return null;
            Assert.That(transfer.PresentationState, Is.EqualTo(TransferVaultPresentationState.Available));

            game.DebugAssembleCentralPayload();
            yield return null;
            Assert.That(transfer.PresentationState, Is.EqualTo(TransferVaultPresentationState.Processing));
            Assert.That(tower.State, Is.EqualTo(CentralTowerPresentationState.PayloadInstallAvailable));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(transfer.PresentationState, Is.EqualTo(TransferVaultPresentationState.Assembled));

            game.DebugInstallCentralPayload();
            yield return null;
            Assert.That(tower.State, Is.EqualTo(CentralTowerPresentationState.PayloadInstalled));
            Assert.That(game.IsCentralPayloadSecured, Is.True);

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            tower = Object.FindFirstObjectByType<AuthoredCentralTowerReadability>(FindObjectsInactive.Include);
            transfer = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            Assert.That(tower.State, Is.EqualTo(CentralTowerPresentationState.ActivationAvailable));
            Assert.That(transfer.PresentationState, Is.EqualTo(TransferVaultPresentationState.Locked));
        }
    }
}
