using System.Collections;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class CentralTransferMissionFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator RoutingThenAssembly_GatesProgressAndPersistsAuthoredState()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var relayFork = Object.FindFirstObjectByType<AuthoredRelayForkObjective>(FindObjectsInactive.Include);
            var transferVault = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            var centralInstallation =
                Object.FindFirstObjectByType<AuthoredCentralInstallationObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(relayFork, Is.Not.Null);
            Assert.That(transferVault, Is.Not.Null);
            Assert.That(centralInstallation, Is.Not.Null);
            Assert.That(relayFork.IsConfigured, Is.True);
            Assert.That(transferVault.IsConfigured, Is.True);
            Assert.That(centralInstallation.IsConfigured, Is.True);

            game.DebugActivateTower();
            game.DebugAssembleCentralPayload();
            Assert.That(game.IsCentralPayloadSecured, Is.False, "Assembly must reject unrouted components.");

            game.DebugCollectNextCache();
            game.DebugCollectNextCache();
            yield return null;
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.RelayFork));
            Assert.That(game.IsCentralPayloadSecured, Is.False);

            game.DebugAssembleCentralPayload();
            Assert.That(game.IsCentralPayloadSecured, Is.False, "The vault must not bypass Relay Fork routing.");
            game.DebugRouteCentralComponents();
            game.DebugRouteCentralComponents();
            yield return null;
            Assert.That(game.AreRelayFeedsRouted, Is.True);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CentralAssembly));
            Assert.That(relayFork.transform.Find("Relay Feeds Routed").gameObject.activeSelf, Is.True);

            game.DebugAssembleCentralPayload();
            game.DebugAssembleCentralPayload();
            yield return null;
            Assert.That(game.IsCentralPayloadAssembled, Is.True);
            Assert.That(game.IsCentralPayloadSecured, Is.False);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CentralInstallation));
            Assert.That(transferVault.transform.Find("Central Payload Assembled").gameObject.activeSelf, Is.True);
            Assert.That(transferVault.IsRouteConfigured, Is.True);
            Assert.That(transferVault.IsRelayRouteOpen, Is.False);
            var relayRouteGate = transferVault.transform.Find("Central Relay Route Gate");
            Assert.That(relayRouteGate, Is.Not.Null);
            Assert.That(relayRouteGate.gameObject.activeSelf, Is.True);
            Assert.That(relayRouteGate.GetComponent<AuthoredMapObstacle>().OverlapsCircle(relayRouteGate.position, 0.2f),
                Is.True);

            game.DebugActivateRelayTower();
            Assert.That(game.IsRelayTowerOnline, Is.True,
                "Compatibility debug setup may complete installation before activating Relay.");

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            game = Object.FindFirstObjectByType<DeadSignalGame>();
            game.DebugActivateTower();
            game.DebugCollectNextCache();
            game.DebugCollectNextCache();
            game.DebugRouteCentralComponents();
            game.DebugAssembleCentralPayload();
            game.DebugInstallCentralPayload();
            game.DebugInstallCentralPayload();
            yield return null;
            centralInstallation =
                Object.FindFirstObjectByType<AuthoredCentralInstallationObjective>(FindObjectsInactive.Include);
            transferVault = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            relayRouteGate = transferVault.transform.Find("Central Relay Route Gate");
            Assert.That(game.IsCentralPayloadSecured, Is.True);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.RelayTower));
            Assert.That(transferVault.IsRelayRouteOpen, Is.True);
            Assert.That(relayRouteGate.gameObject.activeSelf, Is.False);
            Assert.That(centralInstallation.transform.Find("Central Payload Installed").gameObject.activeSelf, Is.True);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            var restarted = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(restarted.AreRelayFeedsRouted, Is.False);
            Assert.That(restarted.IsCentralPayloadSecured, Is.False);
            Assert.That(restarted.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CentralTower));
        }
    }
}
