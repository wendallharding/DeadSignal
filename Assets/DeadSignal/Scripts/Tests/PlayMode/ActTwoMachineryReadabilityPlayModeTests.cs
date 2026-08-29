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
            var returnDoor = calibration.GetComponent<AuthoredRouteDoorReadability>();
            Assert.That(game, Is.Not.Null);
            Assert.That(readability, Is.Not.Null);
            Assert.That(calibration, Is.Not.Null);
            Assert.That(returnDoor, Is.Not.Null);
            Assert.That(readability.IsConfigured, Is.True);
            Assert.That(calibration.HasReadabilityAssets, Is.True);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));
            Assert.That(calibration.PresentationState,
                Is.EqualTo(RelayCalibrationPresentationState.PrerequisiteLocked));
            Assert.That(returnDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));

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
            Assert.That(returnThreshold.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("RouteDoorThresholdReadability"));
            Assert.That(calibrationPanel.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(calibrationSelector.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(returnThreshold.GetComponentsInChildren<Collider>(true), Is.Empty);

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
            Assert.That(returnDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
            Assert.That(calibration.transform.Find("Relay Return Bulkhead").gameObject.activeSelf, Is.False);
            Assert.That(returnThreshold.gameObject.activeSelf, Is.True,
                "The completed doorway threshold must remain visible after its blocking slab retracts.");
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
            returnDoor = calibration.GetComponent<AuthoredRouteDoorReadability>();
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));
            Assert.That(calibration.PresentationState,
                Is.EqualTo(RelayCalibrationPresentationState.PrerequisiteLocked));
            Assert.That(returnDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
        }
    }
}
