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
        public IEnumerator CoolantFirstThenCargo_ReachesRelayThroughOneInstallationReturn()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var cargo = Object.FindFirstObjectByType<AuthoredCargoAnnexObjective>(FindObjectsInactive.Include);
            var coolant = Object.FindFirstObjectByType<AuthoredCoolantReclamationObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(cargo, Is.Not.Null);
            Assert.That(coolant, Is.Not.Null);

            game.DebugActivateTower();
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling),
                "Cargo remains the deterministic guidance tie-breaker while both jobs are available.");

            scene.Player.position = coolant.FirstBafflePosition;
            yield return null;
            scene.Player.position = coolant.SecondBafflePosition;
            yield return null;
            scene.Player.position = coolant.SealPosition;
            yield return null;
            Assert.That(game.IsCoolantSealSecured, Is.False,
                "Coolant must still require the outward release crossing when selected first.");
            scene.Player.position = coolant.ReleasePosition;
            yield return null;

            Assert.That(game.IsCoolantSealSecured, Is.True);
            Assert.That(game.IsCargoCouplingSecured, Is.False);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CargoCoupling));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1));

            scene.Player.position = cargo.CommitmentPosition;
            yield return null;
            Assert.That(game.IsCargoCouplingSecured, Is.False,
                "Cargo must still require withdrawal when selected second.");
            scene.Player.position = cargo.WithdrawalPosition;
            yield return null;

            Assert.That(game.IsCargoCouplingSecured, Is.True);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.RelayFork));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1),
                "Completing both Central jobs in reverse order must preserve the single regional reward.");

            game.DebugRouteCentralComponents();
            game.DebugAssembleCentralPayload();
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.CentralInstallation));
            Assert.That(game.IsCentralPayloadSecured, Is.False);

            game.DebugInstallCentralPayload();
            game.DebugInstallCentralPayload();
            yield return null;

            Assert.That(game.IsCentralPayloadSecured, Is.True);
            Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.RelayTower));
            Assert.That(game.CurrentSalvage, Is.EqualTo(1),
                "The single Central installation return must remain idempotent.");
        }

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
            var authoredObstacleCount = game.AuthoredMapObstacleCount;
            Assert.That(authoredObstacleCount, Is.GreaterThan(0));

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
            Assert.That(transferVault.RoutePresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
            var northFrameBounds = transferVault.transform.Find("Vault East Exit North Bounds")
                .GetComponent<AuthoredMapObstacle>();
            var southFrameBounds = transferVault.transform.Find("Vault East Exit South Bounds")
                .GetComponent<AuthoredMapObstacle>();
            Assert.That(northFrameBounds.OverlapsCircle(
                transferVault.transform.TransformPoint(new Vector3(-3.15f, 0f, 2.4f)), 0.1f), Is.True,
                "The north frame upright must have authored collision.");
            Assert.That(southFrameBounds.OverlapsCircle(
                transferVault.transform.TransformPoint(new Vector3(-3.15f, 0f, -2.4f)), 0.1f), Is.True,
                "The south frame upright must have authored collision.");
            var routeCenter = transferVault.transform.TransformPoint(new Vector3(-3.15f, 0f, 0f));
            Assert.That(northFrameBounds.OverlapsCircle(routeCenter, 0.45f), Is.False);
            Assert.That(southFrameBounds.OverlapsCircle(routeCenter, 0.45f), Is.False,
                "The corrected frame collision must preserve a player-width central passage.");
            Assert.That(transferVault.GetComponent<AuthoredRouteDoorReadability>().FrameKit.transform.lossyScale.z,
                Is.EqualTo(1.6f).Within(0.001f),
                "The required route frame must retain its widened player and NavMesh clearance.");
            var routeFrame = transferVault.GetComponent<AuthoredRouteDoorReadability>().FrameKit;
            Assert.That(transferVault.GetComponentsInChildren<AuthoredStatefulDoorFrame>(true), Has.Length.EqualTo(1),
                "The Relay route must present a single structural door frame.");
            Assert.That(routeFrame.transform.Find("Tracks Pistons and Pockets").gameObject.activeSelf, Is.False,
                "The Relay route must not overlay a second full-height mechanism silhouette on its frame.");
            var northSweepStart = transferVault.transform.TransformPoint(new Vector3(-2.2f, 0f, 2.4f));
            var northSweepEnd = transferVault.transform.TransformPoint(new Vector3(-4.1f, 0f, 2.4f));
            Assert.That(northFrameBounds.TryResolveSlide(
                northSweepStart, northSweepEnd, 0.45f, out var northResolved), Is.True,
                "Swept player movement must collide with the north frame upright.");
            Assert.That(Vector3.Distance(northResolved, northSweepEnd), Is.GreaterThan(0.1f));
            var southSweepStart = transferVault.transform.TransformPoint(new Vector3(-2.2f, 0f, -2.4f));
            var southSweepEnd = transferVault.transform.TransformPoint(new Vector3(-4.1f, 0f, -2.4f));
            Assert.That(southFrameBounds.TryResolveSlide(
                southSweepStart, southSweepEnd, 0.45f, out var southResolved), Is.True,
                "Swept player movement must collide with the south frame upright.");
            Assert.That(Vector3.Distance(southResolved, southSweepEnd), Is.GreaterThan(0.1f));
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
            Assert.That(transferVault.RoutePresentationState, Is.EqualTo(RouteDoorPresentationState.Open));
            Assert.That(relayRouteGate.gameObject.activeSelf, Is.False);
            Assert.That(transferVault.transform.Find("Central Relay Route Threshold").gameObject.activeSelf, Is.True,
                "The open route must keep a persistent authored threshold read.");
            Assert.That(centralInstallation.transform.Find("Central Payload Installed").gameObject.activeSelf, Is.True);
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(authoredObstacleCount),
                "Opening the route must preserve the authored obstacle registry.");

            transferVault.SetRouteOpen(false);
            Assert.That(transferVault.RoutePresentationState, Is.EqualTo(RouteDoorPresentationState.Locked));
            yield return null;
            Assert.That(transferVault.IsRelayRouteOpen, Is.True,
                "The route must reconcile its physical state from installed-payload authority.");
            Assert.That(transferVault.RoutePresentationState, Is.EqualTo(RouteDoorPresentationState.Open),
                "The route frame must reconcile its presentation from installed-payload authority.");

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
