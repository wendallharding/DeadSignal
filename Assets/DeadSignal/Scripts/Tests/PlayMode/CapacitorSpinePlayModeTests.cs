using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.World;

namespace DeadSignal.Tests
{
    public sealed class CapacitorSpinePlayModeTests
    {
        [UnityTest]
        public IEnumerator CompletedCore_GamepadInstallsThenPoweredReturnEnablesExtraction()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
                var player = game.transform.Find("Maintenance Drone");
                var spine = GameObject.Find("Capacitor Spine Region").transform;
                var transferVault = GameObject.Find("Optional East Salvage Vault")
                    .GetComponent<AuthoredTransferVaultObjective>();
                var objective = spine.GetComponent<AuthoredSpineCoreInstallationObjective>();
                var availableMarker = spine.Find("Core Installation Available").gameObject;
                var installedMarker = spine.Find("Core Installation Complete").gameObject;

                Assert.That(objective, Is.Not.Null);
                Assert.That(objective.IsConfigured, Is.True);
                Assert.That(game.SpineCoreInstallationPosition, Is.EqualTo(game.SpineTowerInteractionPosition));

                player.position = game.SpineTowerInteractionPosition;
                yield return _interact(gamepad);
                Assert.That(game.IsSpineCoreInstalled, Is.False,
                    "The final installation must reject before Room C completes the core.");

                game.DebugRecoverStationCapacitor();
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.SpineCoreInstallation));
                game.DebugSelectAuxiliary(SignalAuxiliaryOverclock.FeedbackShield);
                yield return null;
                Assert.That(availableMarker.activeSelf, Is.True);
                Assert.That(installedMarker.activeSelf, Is.False);

                player.position = game.SpineCoreInstallationPosition;
                yield return _interact(gamepad);

                Assert.That(game.IsSpineCoreInstalled, Is.True);
                Assert.That(game.IsExtractionReady, Is.False);
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.PoweredWithdrawal));
                Assert.That(game.CurrentPoweredWithdrawalPhase, Is.EqualTo(PoweredWithdrawalPhase.RelayShortcut));
                Assert.That(availableMarker.activeSelf, Is.False);
                Assert.That(installedMarker.activeSelf, Is.True);
                Assert.That(game.CurrentSalvage, Is.EqualTo(RunModel.SalvageRequired));
                Assert.That(scene.RelayShortcutGate.activeSelf, Is.False,
                    "Relay activation should leave its authored shortcut open for withdrawal.");
                Assert.That(scene.RelaySignalRouting.activeSelf, Is.True,
                    "The first required return checkpoint should retain Relay's visible cyan routing.");

                yield return _interact(gamepad);
                Assert.That(game.CurrentSalvage, Is.EqualTo(RunModel.SalvageRequired),
                    "Repeated installation interaction must not duplicate final-payload progress.");

                player.position = scene.RelayShortcutPosition;
                yield return null;
                Assert.That(game.CurrentPoweredWithdrawalPhase, Is.EqualTo(PoweredWithdrawalPhase.TransferVault));
                player.position = transferVault.Position;
                yield return null;
                Assert.That(game.CurrentPoweredWithdrawalPhase, Is.EqualTo(PoweredWithdrawalPhase.CentralFoothold));
                player.position = scene.TowerPosition;
                yield return null;

                Assert.That(game.IsExtractionReady, Is.True);
                Assert.That(game.CurrentMissionObjectiveId, Is.EqualTo(MissionObjectiveId.Extraction));
                Assert.That(game.DebugIsPoweredAt(scene.TowerPosition), Is.True,
                    "The required return should finish inside the persistent Central foothold.");
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        [UnityTest]
        public IEnumerator Region_ProvidesTwoApproachesLandmarkAndRelocatedGreedCache()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var player = game.transform.Find("Maintenance Drone");
                var foundry = game.transform.Find("Relay Foundry Region");
                var spine = GameObject.Find("Capacitor Spine Region").transform;

                Assert.That(spine.position, Is.EqualTo(new Vector3(42.5f, 0f, 0f)));
                Assert.That(spine.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(18));
                Assert.That(spine.GetComponentsInChildren<Collider>().Length, Is.Zero);
                Assert.That(spine.Find("Capacitor Transfer Bank"), Is.Not.Null);
                Assert.That(spine.Find("North Capacitor Shield"), Is.Not.Null);
                Assert.That(spine.Find("Third Tower Berth"), Is.Not.Null);
                Assert.That(spine.Find("Spine Signal Lines"), Is.Not.Null);
                Assert.That(spine.Find("Capacitor Spine Activation Decal"), Is.Not.Null);
                Assert.That(game.SpineTowerInteractionPosition,
                    Is.EqualTo(spine.Find("Capacitor Spine Activation Decal").position),
                    "The authored activation marking should define the usable interaction side of the Spine Tower.");
                var interactionOffset = game.SpineTowerInteractionPosition - game.SpineTowerPosition;
                Assert.That(new Vector2(interactionOffset.x, interactionOffset.z).magnitude,
                    Is.GreaterThan(2f), "The interaction anchor should not fall inside the tower blocker.");
                Assert.That(spine.Find("Capacitor Spine Return Decal"), Is.Not.Null);
                Assert.That(spine.Find("Capacitor Spine Route Decal"), Is.Not.Null);
                Assert.That(spine.GetComponentInChildren<AuthoredSalvageSocket>(), Is.Null,
                    "The optional greed cache should move to the deeper Arc Furnace route.");
                Assert.That(Resources.Load<Texture2D>("Environment/CapacitorSpineRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/CapacitorSpine/CapacitorSpineRouteDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/CapacitorSpineActivationDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/CapacitorSpine/CapacitorSpineActivationDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Texture2D>("Environment/CapacitorSpineReturnDecal"), Is.Not.Null);
                Assert.That(Resources.Load<Material>("Materials/CapacitorSpine/CapacitorSpineReturnDecal"), Is.Not.Null);
                Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
                Assert.That(game.AuthoredSalvageSocketCount, Is.EqualTo(2));
                Assert.That(game.AuthoredInterceptorEntranceCount, Is.EqualTo(9),
                    "The Spine extensions should preserve the established gates and add two safe deep-region entrances.");
                Assert.That(foundry.Find("Foundry East Bulkhead"), Is.Null);
                Assert.That(foundry.Find("Foundry East North"), Is.Not.Null);
                Assert.That(foundry.Find("Foundry East Center"), Is.Not.Null);
                Assert.That(foundry.Find("Foundry East South"), Is.Not.Null);

                player.position = new Vector3(34.6f, 0f, 3f);
                yield return _moveRight(gamepad, player, 37f);
                Assert.That(player.position.x, Is.GreaterThan(37f),
                    "The protected north opening should cross from the Foundry into the new region.");

                player.position = new Vector3(34.6f, 0f, -3f);
                yield return _moveRight(gamepad, player, 37f);
                Assert.That(player.position.x, Is.GreaterThan(37f),
                    "The exposed south opening should remain an independent traversable approach.");

                player.position = new Vector3(39f, 0f, 0f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.25f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.True,
                    "The central transfer landmark should shape both movement and projectile positioning.");

                player.position = new Vector3(39f, 0f, 0f);
                yield return _moveRight(gamepad, player, 43f);
                Assert.That(player.position.x, Is.LessThan(41f),
                    "The discharge bank should keep the direct center return closed on the outward journey.");

                game.DebugActivateTower();
                game.DebugActivateRelayTower();
                game.DebugCollectNextCache();
                game.DebugSelectOverclock(DeadSignal.Missions.SignalOverclock.OverdriveThrusters);
                game.DebugSelectAuxiliary(DeadSignal.Missions.SignalAuxiliaryOverclock.FeedbackShield);
                game.DebugInstallRelayPayload();
                game.DebugSelectWeapon(DeadSignal.Missions.SignalWeaponOverclock.PiercingPulse);
                player.position = game.SpineTowerInteractionPosition;
                yield return _interact(gamepad);
                Assert.That(game.IsSpineTowerOnline, Is.False,
                    "The third tower must remain locked until its adjacent berth is vented.");

                player.position = game.SpineVentingPosition;
                yield return _interact(gamepad);
                Assert.That(game.IsSpineBerthVented, Is.True);

                player.position = game.SpineTowerInteractionPosition;
                yield return _interact(gamepad);

                Assert.That(game.IsSpineTowerOnline, Is.True);
                Assert.That(game.IsSpineRelayResultInstalled, Is.True);
                Assert.That(game.IsDeepReturnNetworkPowered, Is.True);
                Assert.That(game.IsCoreRebuildUnlocked, Is.True);
                Assert.That(game.IsWeaponEvolved, Is.True);
                Assert.That(spine.Find("Spine Signal Lines").gameObject.activeSelf, Is.True);
                Assert.That(game.transform.Find("Spine Induction Gallery Region/Induction Gallery Signal Lines")
                    .gameObject.activeSelf, Is.True,
                    "Installing the Relay result should visibly power the first deep-return corridor.");
                Assert.That(spine.Find("Capacitor Transfer Bank").gameObject.activeSelf, Is.False,
                    "Powering the Spine should retract the transfer bank and reveal the direct return.");
                Assert.That(game.CurrentSignal, Is.GreaterThan(RunModel.SpineTowerRefill));

                player.position = new Vector3(44f, 0f, 0f);
                yield return _moveLeft(gamepad, player, 40f);
                Assert.That(player.position.x, Is.LessThan(40f),
                    "The activated tower should open a direct central return instead of forcing either outward lane.");

                game.DebugSpawnThreat(SecurityReinforcement.Interceptor);
                var warden = game.transform.Find("Security Warden");
                var sapper = game.transform.Find("Signal Sapper");
                var interceptor = game.transform.Find("Security Interceptor");
                player.position = new Vector3(22f, 0f, 5f);
                warden.position = new Vector3(24f, 0f, 5f);
                sapper.position = new Vector3(26f, 0f, 5f);
                interceptor.position = new Vector3(28f, 0f, 5f);
                var wardenHealth = game.WardenHealth;
                var sapperHealth = game.SapperHealth;
                var interceptorHealth = game.InterceptorHealth;
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.45f);

                Assert.That(game.WardenHealth, Is.EqualTo(wardenHealth - 1f));
                Assert.That(game.SapperHealth, Is.EqualTo(sapperHealth - 1f));
                Assert.That(game.InterceptorHealth, Is.EqualTo(interceptorHealth - 1f),
                    "The evolved Piercing Pulse should reward the third-region commitment with a third aligned hit.");
                Assert.That(game.PiercingPulseFollowThroughs, Is.EqualTo(2));
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        private static IEnumerator _interact(Gamepad gamepad)
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
            yield return null;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }

        private static IEnumerator _moveRight(Gamepad gamepad, Transform player, float targetX)
        {
            var deadline = Time.time + 2f;
            while (player.position.x <= targetX && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
                yield return null;
            }

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }

        private static IEnumerator _moveLeft(Gamepad gamepad, Transform player, float targetX)
        {
            var deadline = Time.time + 2f;
            while (player.position.x >= targetX && Time.time < deadline)
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.left });
                yield return null;
            }

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
        }
    }
}
