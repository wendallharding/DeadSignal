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
            Assert.That(game, Is.Not.Null);
            Assert.That(relayFork, Is.Not.Null);
            Assert.That(transferVault, Is.Not.Null);
            Assert.That(relayFork.IsConfigured, Is.True);
            Assert.That(transferVault.IsConfigured, Is.True);

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
            Assert.That(game.IsCentralPayloadSecured, Is.True);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.RelayTower));
            Assert.That(transferVault.transform.Find("Central Payload Assembled").gameObject.activeSelf, Is.True);
            Assert.That(Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None).Length, Is.EqualTo(137));

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
