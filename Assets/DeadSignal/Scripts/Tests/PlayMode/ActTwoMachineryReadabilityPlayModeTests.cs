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
            Assert.That(game, Is.Not.Null);
            Assert.That(readability, Is.Not.Null);
            Assert.That(readability.IsConfigured, Is.True);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));

            var relayPanel = readability.transform.Find("Relay Foundry Network Status");
            var gantryPanel = readability.transform.Find("Cooling Gantry Network Status");
            Assert.That(relayPanel.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("RelayForkPanelReadability"));
            Assert.That(relayPanel.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("RelayNetworkStatusPanel"));
            Assert.That(gantryPanel.GetComponent<Renderer>().sharedMaterial.mainTexture.name,
                Is.EqualTo("RelayNetworkStatusPanel"));
            Assert.That(relayPanel.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(gantryPanel.GetComponentsInChildren<Collider>(true), Is.Empty);

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
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Powered));

            game.DebugCollectNextCache();
            yield return null;
            Assert.That(game.IsRelayPayloadStabilized, Is.True);
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.Active));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.Stabilized));

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            readability = Object.FindFirstObjectByType<AuthoredRelayNetworkReadability>(FindObjectsInactive.Include);
            Assert.That(readability.RelayState, Is.EqualTo(RelayTowerPresentationState.Dormant));
            Assert.That(readability.GantryState, Is.EqualTo(CoolingGantryPresentationState.PrerequisiteLocked));
        }
    }
}
