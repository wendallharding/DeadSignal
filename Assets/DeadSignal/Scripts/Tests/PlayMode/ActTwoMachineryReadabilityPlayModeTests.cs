using System.Collections;
using DeadSignal.Application;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class ActTwoMachineryReadabilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator RelayFoundryAndCoolingGantry_ShowAuthoritativeLifecycleAndReset()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var readability = Object.FindFirstObjectByType<AuthoredRelayNetworkReadability>(FindObjectsInactive.Include);
            var calibration = Object.FindFirstObjectByType<AuthoredRelayPayloadObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(readability, Is.Not.Null);
            Assert.That(calibration, Is.Not.Null);
            Assert.That(readability.IsConfigured, Is.True);
            Assert.That(calibration.HasReadabilityAssets, Is.True);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));
            Assert.That(calibration.PresentationState,
                Is.EqualTo(RelayCalibrationPresentationState.PrerequisiteLocked));
            Assert.That(calibration.GetComponent<AuthoredRouteDoorReadability>(), Is.Null,
                "The Central route threshold owns this shared doorway's presentation.");

            var relayPanel = readability.transform.Find("Relay Foundry Network Status");
            var gantryPanel = readability.transform.Find("Cooling Gantry Network Status");
            var calibrationPanel = calibration.transform.Find("Relay Calibration Status Panel");
            var calibrationSelector = calibration.transform.Find("Relay Calibration Selector");
            var returnThreshold = calibration.transform.Find("Relay Return Threshold");
            Assert.That(relayPanel.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("RelayForkPanelReadability"));
            Assert.That(relayPanel.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("RelayNetworkStatusPanel"));
            Assert.That(gantryPanel.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("RelayNetworkStatusPanel"));
            Assert.That(relayPanel.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(gantryPanel.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(calibrationPanel.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("RelayFoundryWeaponCalibrationDecal"));
            Assert.That(calibrationSelector.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("RelayForkSelectorReadability"));
            Assert.That(returnThreshold, Is.Null,
                "The overlapping Relay return threshold must not duplicate the Central route frame.");
            Assert.That(calibrationPanel.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(calibrationSelector.GetComponentsInChildren<Collider>(true), Is.Empty);

            game.DebugActivateTower();
            game.DebugCollectNextCache();
            game.DebugCollectNextCache();
            game.DebugRouteCentralComponents();
            game.DebugAssembleCentralPayload();
            game.DebugInstallCentralPayload();
            yield return null;
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.ActivationAvailable));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));

            game.DebugActivateRelayTower();
            yield return null;
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Activating));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.ProcessingAvailable));
            Assert.That(calibration.transform.Find("Relay Return Bulkhead").gameObject.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Powered));

            game.DebugCollectNextCache();
            yield return null;
            Assert.That(game.IsRelayPayloadStabilized, Is.True);
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.Active));
            Assert.That(calibration.PresentationState,
                Is.EqualTo(RelayCalibrationPresentationState.PayloadStabilized));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.Stabilized));
            Assert.That(calibration.PresentationState,
                Is.EqualTo(RelayCalibrationPresentationState.InstallationAvailable));

            game.DebugInstallRelayPayload();
            yield return null;
            Assert.That(calibration.PresentationState,
                Is.EqualTo(RelayCalibrationPresentationState.InstallationActive));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(calibration.PresentationState, Is.EqualTo(RelayCalibrationPresentationState.Installed));

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            readability = Object.FindFirstObjectByType<AuthoredRelayNetworkReadability>(FindObjectsInactive.Include);
            calibration = Object.FindFirstObjectByType<AuthoredRelayPayloadObjective>(FindObjectsInactive.Include);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));
            Assert.That(calibration.PresentationState,
                Is.EqualTo(RelayCalibrationPresentationState.PrerequisiteLocked));
            Assert.That(calibration.GetComponent<AuthoredRouteDoorReadability>(), Is.Null);
        }
    }
}
