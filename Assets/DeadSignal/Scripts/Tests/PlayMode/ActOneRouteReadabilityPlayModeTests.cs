using System.Collections;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
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
            var heroFinishes = Object.FindObjectsByType<AuthoredRelayTransferHeroFinish>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var relayFinish = System.Array.Find(heroFinishes, finish => finish.IsRelayFinish);
            var transferFinish = System.Array.Find(heroFinishes, finish => !finish.IsRelayFinish);

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
            Assert.That(Resources.Load<Texture2D>("Environment/RelayTransferHeroAtlas"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/RelayForkHeroFinish"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/TransferVaultHeroFinish"), Is.Not.Null);
            Assert.That(relayFinish, Is.Not.Null);
            Assert.That(transferFinish, Is.Not.Null);
            Assert.That(relayFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(transferFinish.FinishRenderer.sharedMaterials, Has.Length.EqualTo(4));
            Assert.That(relayFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("RelayForkHeroFinish"));
            Assert.That(transferFinish.FinishRenderer.GetComponent<MeshFilter>().sharedMesh.name,
                Is.EqualTo("TransferVaultHeroFinish"));
            Assert.That(relayFinish.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(transferFinish.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(shortcutDoor.IsConfigured, Is.True);
            Assert.That(relayRouteDoor.IsConfigured, Is.True);
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Locked));
            Assert.That(relayFinish.RelayState, Is.EqualTo(RelayForkPresentationState.Locked));
            Assert.That(transferFinish.TransferState, Is.EqualTo(TransferVaultPresentationState.Locked));
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
            Assert.That(relayFinish.RelayState, Is.EqualTo(RelayForkPresentationState.Available));

            game.DebugRouteCentralComponents();
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Routing));
            yield return null;
            Assert.That(relayFinish.RelayState, Is.EqualTo(RelayForkPresentationState.Routing));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(relayFork.PresentationState, Is.EqualTo(RelayForkPresentationState.Routed));
            Assert.That(relayFinish.RelayState, Is.EqualTo(RelayForkPresentationState.Routed));

            var captureDirectory = System.Environment.GetEnvironmentVariable("DEAD_SIGNAL_RELAY_TRANSFER_CAPTURE_DIR");
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                game.DebugTeleport(DebugLocation.RelayFork);
                yield return new WaitForSecondsRealtime(0.3f);
                _captureCamera(scene.PlayerCamera, Path.Combine(captureDirectory, "P05-Relay-Fork-Finish-1600x900.png"));
                game.DebugTeleport(DebugLocation.TransferVault);
                yield return new WaitForSecondsRealtime(0.3f);
                _captureCamera(scene.PlayerCamera, Path.Combine(captureDirectory, "P05-Transfer-Vault-Finish-1600x900.png"));
            }

            game.DebugOpenShortcut();
            Assert.That(shortcutDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
            Assert.That(scene.ShortcutGate.transform.Find("Signal Shortcut Gate").gameObject.activeSelf, Is.False);
            Assert.That(scene.ShortcutGate.transform.Find("Shortcut Gate Signal").gameObject.activeSelf, Is.True);
            Assert.That(scene.ShortcutGate.transform.Find("Central Shortcut Threshold").gameObject.activeSelf, Is.True,
                "Opening the shortcut must retain a readable physical threshold.");

            game.DebugAssembleCentralPayload();
            yield return null;
            Assert.That(transferFinish.TransferState, Is.EqualTo(TransferVaultPresentationState.Processing));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(transferFinish.TransferState, Is.EqualTo(TransferVaultPresentationState.Assembled));
            Assert.That(relayRouteDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
            Assert.That(transferVault.transform.Find("Central Relay Route Gate").gameObject.activeSelf, Is.True);
            game.DebugInstallCentralPayload();
            yield return null;

            Assert.That(relayRouteDoor.PresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
            Assert.That(transferFinish.TransferState, Is.EqualTo(TransferVaultPresentationState.Assembled));
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
