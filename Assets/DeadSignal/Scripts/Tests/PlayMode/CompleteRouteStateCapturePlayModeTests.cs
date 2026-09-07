using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.Player;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class CompleteRouteStateCapturePlayModeTests
    {
        private const string CAPTURE_DIRECTORY_ENVIRONMENT_VARIABLE = "DEAD_SIGNAL_P59_CAPTURE_DIR";
        private const int CAPTURE_WIDTH = 1600;
        private const int CAPTURE_HEIGHT = 900;

        [UnityTest]
        public IEnumerator MajorMissionRooms_RecordCompleteStateJourney()
        {
            yield return _loadCaptureContext(result => m_context = result);
            var rooms = _majorMissionRooms(m_context.Game, m_context.Chamber);

            Assert.That(rooms.Count, Is.EqualTo(19),
                "The gallery must retain the authoritative 19-room required mission contract.");
            yield return _captureSweep("01-Dormant-Or-Locked", rooms);

            yield return _capture("02-Available", "01-Central-Chamber-Activation", m_context.Game.TowerPosition);
            m_context.Game.DebugActivateTower();
            yield return _settle();
            Assert.That(m_context.Game.IsTowerOnline, Is.True);
            yield return _capture("03-Complete", "01-Central-Chamber-Powered", m_context.Game.TowerPosition);

            yield return _capture("02-Available", "02-Cargo-Annex-Coupling", m_context.Game.CargoCouplingPosition);
            yield return _capture("02-Available", "03-Coolant-Reclamation-Seal", m_context.Game.CoolantSealPosition);
            m_context.Game.DebugCollectNextCache();
            m_context.Game.DebugCollectNextCache();
            yield return _settle();
            Assert.That(m_context.Game.IsCargoCouplingSecured, Is.True);
            Assert.That(m_context.Game.IsCoolantSealSecured, Is.True);
            yield return _capture("03-Complete", "02-Cargo-Annex-Secured", m_context.Game.CargoCouplingPosition);
            yield return _capture("03-Complete", "03-Coolant-Reclamation-Stable", m_context.Game.CoolantSealPosition);

            yield return _capture("02-Available", "04-Relay-Fork-Route", m_context.Game.RelayForkPosition);
            m_context.Game.DebugRouteCentralComponents();
            yield return _settle();
            Assert.That(m_context.Game.AreRelayFeedsRouted, Is.True);
            yield return _capture("03-Complete", "04-Relay-Fork-Routed", m_context.Game.RelayForkPosition);

            yield return _capture("02-Available", "05-East-Transfer-Vault-Assemble", m_context.Game.TransferVaultPosition);
            m_context.Game.DebugAssembleCentralPayload();
            yield return _settle();
            Assert.That(m_context.Game.IsCentralPayloadAssembled, Is.True);
            yield return _capture("03-Complete", "05-East-Transfer-Vault-Assembled", m_context.Game.TransferVaultPosition);

            yield return _capture("02-Available", "01-Central-Chamber-Install", m_context.Game.CentralInstallationPosition);
            m_context.Game.DebugInstallCentralPayload();
            yield return _settle();
            Assert.That(m_context.Game.IsCentralPayloadSecured, Is.True);
            yield return _capture("03-Complete", "01-Central-Chamber-Route-Open", m_context.Game.CentralInstallationPosition);

            yield return _capture("02-Available", "06-Relay-Foundry-Activate", m_context.Game.RelayTowerPosition);
            m_context.Game.DebugActivateRelayTower();
            yield return _settle();
            Assert.That(m_context.Game.IsRelayTowerOnline, Is.True);
            yield return _capture("03-Complete", "06-Relay-Foundry-Powered", m_context.Game.RelayTowerPosition);

            var relayPayload = Object.FindFirstObjectByType<AuthoredRelayPayloadObjective>();
            Assert.That(relayPayload, Is.Not.Null);
            yield return _capture("02-Available", "07-Cooling-Gantry-Stabilize", relayPayload.Position);
            m_context.Game.DebugCollectNextCache();
            yield return _settle();
            Assert.That(m_context.Game.IsRelayPayloadStabilized, Is.True);
            yield return _capture("03-Complete", "07-Cooling-Gantry-Stable", relayPayload.Position);

            yield return _capture("02-Available", "06-Relay-Foundry-Install", m_context.Game.RelayTowerPosition);
            m_context.Game.DebugInstallRelayPayload();
            m_context.Game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
            yield return _settle();
            Assert.That(m_context.Game.IsRelayPayloadSecured, Is.True);
            yield return _capture("03-Complete", "06-Relay-Foundry-Calibrated", m_context.Game.RelayTowerPosition);

            yield return _capture("02-Available", "09-Spine-Discharge-Trench-Vent", m_context.Game.SpineVentingPosition);
            m_context.Game.DebugVentSpineBerth();
            yield return _settle();
            Assert.That(m_context.Game.IsSpineBerthVented, Is.True);
            yield return _capture("03-Complete", "09-Spine-Discharge-Trench-Vented", m_context.Game.SpineVentingPosition);

            yield return _capture("02-Available", "08-Capacitor-Spine-Install-Relay", m_context.Game.SpineTowerInteractionPosition);
            m_context.Game.DebugActivateSpineTower();
            yield return _settle();
            Assert.That(m_context.Game.IsSpineTowerOnline, Is.True);
            yield return _capture("03-Complete", "08-Capacitor-Spine-Powered", m_context.Game.SpineTowerPosition);

            yield return _capture("02-Available", "10-Induction-Gallery-Charge", m_context.Game.InductionLatticePosition);
            m_context.Game.DebugChargeInductionLattice();
            yield return _settle();
            Assert.That(m_context.Game.IsInductionLatticeCharged, Is.True);
            yield return _capture("03-Complete", "10-Induction-Gallery-Charged", m_context.Game.InductionLatticePosition);

            yield return _capture("02-Available", "11-Flux-Bypass-Route", m_context.Game.FluxShuntPosition);
            m_context.Game.DebugRouteFluxShunt();
            yield return _settle();
            Assert.That(m_context.Game.IsFluxShuntRouted, Is.True);
            yield return _capture("03-Complete", "11-Flux-Bypass-Routed", m_context.Game.FluxShuntPosition);

            yield return _capture("02-Available", "12-Convergence-Chamber-Calibrate", m_context.Game.ConvergenceCalibrationPosition);
            m_context.Game.DebugSetThreatsFrozen(false);
            m_context.Game.DebugBeginConvergenceCalibration();
            yield return new WaitForSecondsRealtime(0.35f);
            m_context.Game.DebugSetThreatsFrozen(true);
            Assert.That(m_context.Game.IsConvergenceCalibrationActive, Is.True);
            yield return _capture("04-Active", "12-Convergence-Chamber-Holdout", m_context.Game.ConvergenceCalibrationPosition);

            yield return _loadCaptureContext(result => m_context = result);
            m_context.Game.DebugSetInfiniteSignal(true);
            m_context.Game.DebugSetInvulnerable(true);
            m_context.Game.DebugSetThreatsFrozen(true);
            m_context.Game.DebugCompleteConvergenceCalibration();
            yield return _settle();
            Assert.That(m_context.Game.IsConvergenceCalibrated, Is.True);
            yield return _capture("03-Complete", "12-Convergence-Chamber-Calibrated", m_context.Game.ConvergenceCalibrationPosition);

            yield return _capture("02-Available", "13-Breaker-Gallery-Reset", m_context.Game.BreakerResetPosition);
            m_context.Game.DebugResetBreakerDistribution();
            yield return _settle();
            Assert.That(m_context.Game.IsBreakerDistributionReset, Is.True);
            yield return _capture("03-Complete", "13-Breaker-Gallery-Reset", m_context.Game.BreakerResetPosition);

            yield return _capture("02-Available", "14-Arc-Furnace-Forge", m_context.Game.FurnaceForgePosition);
            m_context.Game.DebugForgeLattice();
            yield return _settle();
            Assert.That(m_context.Game.IsLatticeForged, Is.True);
            yield return _capture("03-Complete", "14-Arc-Furnace-Forged", m_context.Game.FurnaceForgePosition);

            yield return _capture("02-Available", "15-Quench-Loop-Stabilize", m_context.Game.QuenchStabilizationPosition);
            m_context.Game.DebugStabilizeCore();
            yield return _settle();
            Assert.That(m_context.Game.IsCoreStabilized, Is.True);
            yield return _capture("03-Complete", "15-Quench-Loop-Stable", m_context.Game.QuenchStabilizationPosition);

            yield return _capture("02-Available", "16-Room-A-Commit", m_context.Chamber.CommitmentSwitch.position);
            m_context.Game.DebugCommitSecurityTrial();
            yield return _settle();
            Assert.That(m_context.Game.IsSecurityTrialCommitted, Is.True);
            yield return _capture("04-Active", "16-Room-A-Committed", m_context.Chamber.CommitmentSwitch.position);
            yield return _capture("02-Available", "17-Room-B-Armed", m_context.Chamber.ArenaPosition);

            m_context.Game.DebugSetThreatsFrozen(false);
            m_context.Player.position = m_context.Chamber.LockdownThreshold.TransformPoint(new Vector3(0f, 0f, 1f));
            yield return new WaitForSecondsRealtime(1.25f);
            m_context.Game.DebugSetThreatsFrozen(true);
            Assert.That(m_context.Chamber.State, Is.EqualTo(CombatChamberState.Lockdown));
            yield return _capture("04-Active", "17-Room-B-Lockdown", m_context.Chamber.ArenaPosition);

            yield return _loadCaptureContext(result => m_context = result);
            m_context.Game.DebugSetInfiniteSignal(true);
            m_context.Game.DebugSetInvulnerable(true);
            m_context.Game.DebugSetThreatsFrozen(true);
            m_context.Game.DebugCompleteSecurityTrial();
            yield return _settle();
            Assert.That(m_context.Game.IsSecurityTrialCleared, Is.True);
            yield return _capture("03-Complete", "16-Room-A-Released", m_context.Chamber.CommitmentSwitch.position);
            yield return _capture("03-Complete", "17-Room-B-Cleared", m_context.Chamber.ArenaPosition);
            yield return _capture("02-Available", "18-Room-C-Capacitor", m_context.Chamber.RewardPosition);

            m_context.Game.DebugRecoverStationCapacitor();
            yield return _settle();
            Assert.That(m_context.Game.IsStationCapacitorRecovered, Is.True);
            yield return _capture("03-Complete", "18-Room-C-Recovered", m_context.Chamber.RewardPosition);

            yield return _capture("02-Available", "08-Capacitor-Spine-Install-Core", m_context.Game.SpineCoreInstallationPosition);
            m_context.Game.DebugInstallSpineCore();
            yield return _settle();
            Assert.That(m_context.Game.IsSpineCoreInstalled, Is.True);
            yield return _capture("03-Complete", "08-Capacitor-Spine-Core-Online", m_context.Game.SpineCoreInstallationPosition);

            m_context.Game.DebugCompletePoweredWithdrawal();
            yield return _settle();
            Assert.That(m_context.Game.IsExtractionReady, Is.True);
            Object.FindFirstObjectByType<StationStateFeedbackController>()?.SetPaused(true);
            rooms = _majorMissionRooms(m_context.Game, m_context.Chamber);
            yield return _captureSweep("05-Powered-Return", rooms);

            yield return _capture("02-Available", "19-Extraction-Dock-Uplink", m_context.Game.DebugExtractionPosition);
            m_context.Game.DebugBeginExtraction(ExtractionUplinkMode.Stable);
            yield return _settle();
            Assert.That(m_context.Game.IsExtractionUplinkActive, Is.True);
            yield return _capture("04-Active", "19-Extraction-Dock-Uplink", m_context.Game.DebugExtractionPosition);

            m_context.Game.DebugCompleteExtraction();
            yield return _settle();
            Assert.That(m_context.Game.CurrentRunOutcome, Is.EqualTo(RunOutcome.Victory));
            yield return _capture("03-Complete", "19-Extraction-Dock-Victory", m_context.Game.DebugExtractionPosition);
        }

        private CaptureContext m_context;

        private static IEnumerator _loadCaptureContext(Action<CaptureContext> assign)
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var followCamera = Object.FindFirstObjectByType<PlayerFollowCamera>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var chamber = Object.FindFirstObjectByType<AuthoredCombatChamber>();
            Assert.That(game, Is.Not.Null);
            Assert.That(followCamera, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(chamber, Is.Not.Null);

            game.DebugSetInfiniteSignal(true);
            game.DebugSetInvulnerable(true);
            game.DebugSetThreatsFrozen(true);
            assign(new CaptureContext(game, followCamera, camera, game.transform.Find("Maintenance Drone"), chamber));
        }

        private static IReadOnlyList<RoomView> _majorMissionRooms(DeadSignalGame game, AuthoredCombatChamber chamber)
        {
            var relayPayload = Object.FindFirstObjectByType<AuthoredRelayPayloadObjective>();
            Assert.That(relayPayload, Is.Not.Null);
            return new[]
            {
                new RoomView("01-Central-Chamber", game.TowerPosition),
                new RoomView("02-Cargo-Annex", game.CargoCouplingPosition),
                new RoomView("03-Coolant-Reclamation", game.CoolantSealPosition),
                new RoomView("04-Relay-Fork", game.RelayForkPosition),
                new RoomView("05-East-Transfer-Vault", game.TransferVaultPosition),
                new RoomView("06-Relay-Foundry", game.RelayTowerPosition),
                new RoomView("07-Cooling-Gantry", relayPayload.Position),
                new RoomView("08-Capacitor-Spine", game.SpineTowerPosition),
                new RoomView("09-Spine-Discharge-Trench", game.SpineVentingPosition),
                new RoomView("10-Induction-Gallery", game.InductionLatticePosition),
                new RoomView("11-Flux-Bypass", game.FluxShuntPosition),
                new RoomView("12-Convergence-Chamber", game.ConvergenceCalibrationPosition),
                new RoomView("13-Breaker-Gallery", game.BreakerResetPosition),
                new RoomView("14-Arc-Furnace", game.FurnaceForgePosition),
                new RoomView("15-Quench-Loop", game.QuenchStabilizationPosition),
                new RoomView("16-Room-A", chamber.CommitmentSwitch.position),
                new RoomView("17-Room-B", chamber.ArenaPosition),
                new RoomView("18-Room-C", chamber.RewardPosition),
                new RoomView("19-Extraction-Dock", game.DebugExtractionPosition)
            };
        }

        private IEnumerator _captureSweep(string state, IReadOnlyList<RoomView> rooms)
        {
            foreach (var room in rooms)
            {
                yield return _capture(state, room.Name, room.Focus);
            }
        }

        private IEnumerator _capture(string state, string label, Vector3 focus)
        {
            m_context.Player.position = focus - _screenForward(m_context.Camera) * 1.8f;
            m_context.FollowCamera.SnapToFocus(focus);
            yield return new WaitForSecondsRealtime(0.08f);

            var playerViewport = m_context.Camera.WorldToViewportPoint(m_context.Player.position + Vector3.up * 0.35f);
            var focusViewport = m_context.Camera.WorldToViewportPoint(focus + Vector3.up * 0.35f);
            Assert.That(playerViewport.z, Is.GreaterThan(0f), label + " player must remain in front of the camera.");
            Assert.That(focusViewport.z, Is.GreaterThan(0f), label + " focus must remain in front of the camera.");

            var captureDirectory = Environment.GetEnvironmentVariable(CAPTURE_DIRECTORY_ENVIRONMENT_VARIABLE);
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                yield break;
            }

            Directory.CreateDirectory(captureDirectory);
            var renderTexture = new RenderTexture(CAPTURE_WIDTH, CAPTURE_HEIGHT, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(CAPTURE_WIDTH, CAPTURE_HEIGHT, TextureFormat.RGB24, false);
            var previousTarget = m_context.Camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                m_context.Camera.targetTexture = renderTexture;
                m_context.Camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, CAPTURE_WIDTH, CAPTURE_HEIGHT), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, $"{state}--{label}.png"), texture.EncodeToPNG());
            }
            finally
            {
                m_context.Camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }

        private static IEnumerator _settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);
        }

        private static Vector3 _screenForward(Camera camera)
        {
            var forward = camera.transform.forward;
            forward.y = 0f;
            return forward.normalized;
        }

        private readonly struct RoomView
        {
            public RoomView(string name, Vector3 focus)
            {
                Name = name;
                Focus = focus;
            }

            public string Name { get; }
            public Vector3 Focus { get; }
        }

        private sealed class CaptureContext
        {
            public CaptureContext(
                DeadSignalGame game,
                PlayerFollowCamera followCamera,
                Camera camera,
                Transform player,
                AuthoredCombatChamber chamber)
            {
                Game = game;
                FollowCamera = followCamera;
                Camera = camera;
                Player = player;
                Chamber = chamber;
            }

            public DeadSignalGame Game { get; }
            public PlayerFollowCamera FollowCamera { get; }
            public Camera Camera { get; }
            public Transform Player { get; }
            public AuthoredCombatChamber Chamber { get; }
        }
    }
}
