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
    public sealed class ActOneRouteReadabilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator RelayRoutingAndCentralDoors_PresentPersistentLifecycleAndReset()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var relayFork = Object.FindFirstObjectByType<AuthoredRelayForkObjective>(FindObjectsInactive.Include);
            var transferVault = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            var shortcutDoor = scene.ShortcutGate.GetComponent<AuthoredRouteDoorReadability>();
            var relayRouteDoor = transferVault.GetComponent<AuthoredRouteDoorReadability>();

            Assert.That(game, Is.Not.Null);
            Assert.That(relayFork, Is.Not.Null);
            Assert.That(transferVault, Is.Not.Null);
            Assert.That(relayFork.HasReadabilityAssets, Is.True);
            Assert.That(Resources.Load<Texture2D>("Environment/RelayForkStatusPanel"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/RelayForkConsoleReadability"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/RelayForkPanelReadability"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/RelayForkSelectorReadability"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/RouteDoorThresholdReadability"), Is.Not.Null);
            Assert.That(Resources.Load<Material>("Materials/RelayForkStatus"), Is.Not.Null);
            Assert.That(Resources.Load<Material>("Materials/RouteDoorThresholdStatus"), Is.Not.Null);
            Assert.That(shortcutDoor.IsConfigured, Is.True);
            Assert.That(relayRouteDoor.IsConfigured, Is.True);
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Locked));
            Assert.That(shortcutDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
            Assert.That(relayRouteDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
            Assert.That(scene.ShortcutGate.transform.Find("Central Shortcut Threshold").gameObject.activeSelf, Is.True);
            Assert.That(transferVault.transform.Find("Central Relay Route Threshold").gameObject.activeSelf, Is.True);

            game.DebugActivateTower();
            game.DebugCollectNextCache();
            game.DebugCollectNextCache();
            yield return null;

            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.RelayFork));
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Available));

            game.DebugRouteCentralComponents();
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Routing));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Routed));

            game.DebugOpenShortcut();
            Assert.That(shortcutDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
            Assert.That(scene.ShortcutGate.transform.Find("Signal Shortcut Gate").gameObject.activeSelf, Is.False);
            Assert.That(scene.ShortcutGate.transform.Find("Shortcut Gate Signal").gameObject.activeSelf, Is.True);
            Assert.That(scene.ShortcutGate.transform.Find("Central Shortcut Threshold").gameObject.activeSelf, Is.True,
                "Opening the shortcut must retain a readable physical threshold.");

            game.DebugAssembleCentralPayload();
            Assert.That(relayRouteDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
            Assert.That(transferVault.transform.Find("Central Relay Route Gate").gameObject.activeSelf, Is.True);
            game.DebugInstallCentralPayload();
            yield return null;

            Assert.That(relayRouteDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
            Assert.That(transferVault.transform.Find("Central Relay Route Gate").gameObject.activeSelf, Is.False);
            Assert.That(transferVault.transform.Find("Central Relay Route Open").gameObject.activeSelf, Is.True);
            Assert.That(transferVault.transform.Find("Central Relay Route Threshold").gameObject.activeSelf, Is.True,
                "Opening the Relay route must retain a readable physical threshold.");

            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            relayFork = Object.FindFirstObjectByType<AuthoredRelayForkObjective>(FindObjectsInactive.Include);
            transferVault = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);
            scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            shortcutDoor = scene.ShortcutGate.GetComponent<AuthoredRouteDoorReadability>();
            relayRouteDoor = transferVault.GetComponent<AuthoredRouteDoorReadability>();
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Locked));
            Assert.That(shortcutDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
            Assert.That(relayRouteDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
        }
    }
}
