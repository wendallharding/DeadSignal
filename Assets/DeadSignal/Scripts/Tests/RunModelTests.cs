using NUnit.Framework;
using DeadSignal.Missions;
using DeadSignal.Salvage;

namespace DeadSignal.Tests
{
    public sealed class RunModelTests
    {
        [Test]
        public void RestoreSignal_ClampsAtMaximumAndReportsActualRecovery()
        {
            var model = new RunModel();
            Assert.That(model.TrySpend(5f), Is.True);

            Assert.That(model.RestoreSignal(50f), Is.EqualTo(33f).Within(0.001f));
            Assert.That(model.Signal, Is.EqualTo(RunModel.MaximumSignal));
            Assert.That(model.RestoreSignal(10f), Is.Zero);
        }

        [Test]
        public void NewRun_HasPlayableStartingState()
        {
            var run = new RunModel();

            Assert.That(run.Signal, Is.EqualTo(RunModel.StartingSignal));
            Assert.That(run.Salvage, Is.Zero);
            Assert.That(run.TowerOnline, Is.False);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Running));
        }

        [Test]
        public void DeadZoneMovement_DrainsFasterThanPoweredMovement()
        {
            var powered = new RunModel();
            var deadZone = new RunModel();

            powered.Advance(2f, true, true);
            deadZone.Advance(2f, true, false);

            Assert.That(deadZone.Signal, Is.LessThan(powered.Signal));
            Assert.That(powered.Signal, Is.EqualTo(RunModel.StartingSignal - 0.76f).Within(0.001f));
            Assert.That(deadZone.Signal, Is.EqualTo(RunModel.StartingSignal - 12f).Within(0.001f));
        }

        [Test]
        public void TowerActivation_SpendsThenRefillsAndCannotRepeat()
        {
            var run = new RunModel();

            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.TowerOnline, Is.True);
            Assert.That(run.Signal, Is.EqualTo(RunModel.MaximumSignal));
            float afterFirstActivation = run.Signal;

            Assert.That(run.TryActivateTower(), Is.False);
            Assert.That(run.Signal, Is.EqualTo(afterFirstActivation));
        }

        [Test]
        public void TowerActivation_IsAtomicAtExactlyTheRequiredSignal()
        {
            var run = new RunModel();
            run.TakeSecurityHit();
            run.TakeSecurityHit();
            run.TakeSecurityHit();
            Assert.That(run.TrySpend(8f), Is.True);
            Assert.That(run.Signal, Is.EqualTo(RunModel.TowerCost));

            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.Signal, Is.EqualTo(RunModel.TowerRefill));
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Running));
        }

        [Test]
        public void RelayTower_RequiresCentralPayloadButActivatesAtZeroSignal()
        {
            var run = new RunModel();

            Assert.That(run.TryActivateRelayTower(), Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.TryActivateRelayTower(), Is.False,
                "Commissioning should remain mission-gated until the Central payload is installed.");
            _assembleCentralPayload(run);
            Assert.That(run.TrySpend(run.Signal), Is.True);
            Assert.That(run.Signal, Is.Zero);
            Assert.That(run.TryActivateRelayTower(), Is.True,
                "Mandatory Foundry commissioning must not be gated by the player's current Signal reserve.");
            Assert.That(run.RelayTowerOnline, Is.True);
            Assert.That(run.Signal, Is.EqualTo(RunModel.RelayTowerRefill));
        }

        [Test]
        public void RelayTower_ExpandsNetworkOnceAndRestoresSignal()
        {
            var run = new RunModel();
            run.TryActivateTower();
            _assembleCentralPayload(run);
            var before = run.Signal;

            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.RelayTowerOnline, Is.True);
            Assert.That(run.Signal,
                Is.EqualTo(System.Math.Min(RunModel.MaximumSignal, before + RunModel.RelayTowerRefill)).Within(0.001f));
            Assert.That(run.TryActivateRelayTower(), Is.False);
        }

        [Test]
        public void SpineTower_RequiresRelayAndVentingButInstallsAtZeroSignalOnce()
        {
            var run = new RunModel();

            Assert.That(run.TryActivateSpineTower(), Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.False);
            _assembleCentralPayload(run);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.False);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.False);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.False);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.False);
            Assert.That(run.TrySpend(run.Signal), Is.True);
            Assert.That(run.Signal, Is.Zero);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.SpineTowerOnline, Is.True);
            Assert.That(run.SpineRelayResultInstalled, Is.True);
            Assert.That(run.DeepReturnNetworkPowered, Is.True);
            Assert.That(run.CoreRebuildUnlocked, Is.True);
            Assert.That(run.Signal, Is.EqualTo(RunModel.SpineTowerRefill));
            Assert.That(run.TryActivateSpineTower(), Is.False);
        }

        [Test]
        public void InductionLattice_RequiresCoreRebuildAndChargesOnce()
        {
            var run = new RunModel();

            Assert.That(run.TryChargeInductionLattice(), Is.False);
            Assert.That(run.InductionLatticeCharged, Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            _assembleCentralPayload(run);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryChargeInductionLattice(), Is.False);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.InductionLattice));

            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.InductionLatticeCharged, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.FluxShunt));
            Assert.That(run.TryChargeInductionLattice(), Is.False);
        }

        [Test]
        public void FluxShunt_RequiresChargedLatticeAndRoutesOnce()
        {
            var run = new RunModel();

            Assert.That(run.TryRouteFluxShunt(), Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            _assembleCentralPayload(run);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.TryRouteFluxShunt(), Is.False);
            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.FluxShunt));

            Assert.That(run.TryRouteFluxShunt(), Is.True);
            Assert.That(run.FluxShuntRouted, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.ConvergenceCalibration));
            Assert.That(run.TryRouteFluxShunt(), Is.False);
        }

        [Test]
        public void ConvergenceCalibration_RequiresFluxAndOnlyAdvancesInsideHoldVolume()
        {
            var run = new RunModel(convergenceCalibrationDuration: 12f);

            Assert.That(run.TryBeginConvergenceCalibration(), Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            _assembleCentralPayload(run);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.TryRouteFluxShunt(), Is.True);

            Assert.That(run.TryBeginConvergenceCalibration(), Is.True);
            Assert.That(run.AdvanceConvergenceCalibration(6f, true), Is.False);
            Assert.That(run.ConvergenceCalibrationProgress, Is.EqualTo(6f));
            Assert.That(run.AdvanceConvergenceCalibration(4f, false), Is.False);
            Assert.That(run.ConvergenceCalibrationProgress, Is.EqualTo(6f));
            Assert.That(run.AdvanceConvergenceCalibration(6f, true), Is.True);
            Assert.That(run.ConvergenceCalibrated, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.BreakerReset));
            Assert.That(run.TryBeginConvergenceCalibration(), Is.False);
        }

        [Test]
        public void BreakerReset_RequiresCalibrationAndResetsOnce()
        {
            var run = new RunModel();

            Assert.That(run.TryResetBreakerDistribution(), Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            _assembleCentralPayload(run);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.TryRouteFluxShunt(), Is.True);
            Assert.That(run.TryBeginConvergenceCalibration(), Is.True);
            Assert.That(run.AdvanceConvergenceCalibration(run.ConvergenceCalibrationDuration, true), Is.True);

            Assert.That(run.TryResetBreakerDistribution(), Is.True);
            Assert.That(run.BreakerDistributionReset, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.FurnaceForge));
            Assert.That(run.TryResetBreakerDistribution(), Is.False);
        }

        [Test]
        public void CoreProcessing_RequiresForgeBeforeQuenchAndCompletesOnce()
        {
            var run = new RunModel();

            Assert.That(run.TryForgeLattice(), Is.False);
            Assert.That(run.TryStabilizeCore(), Is.False);
            _completeThroughBreaker(run);

            Assert.That(run.TryStabilizeCore(), Is.False);
            Assert.That(run.TryForgeLattice(), Is.True);
            Assert.That(run.LatticeForged, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.QuenchStabilization));
            Assert.That(run.TryForgeLattice(), Is.False);
            Assert.That(run.TryStabilizeCore(), Is.True);
            Assert.That(run.CoreStabilized, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.TrialCommitment));
            Assert.That(run.TryStabilizeCore(), Is.False);
        }

        [Test]
        public void SecurityTrial_RequiresCommitmentClearAndCapacitorInOrder()
        {
            var run = new RunModel();

            Assert.That(run.TryCommitSecurityTrial(), Is.False);
            Assert.That(run.TryCompleteSecurityTrial(), Is.False);
            Assert.That(run.TryRecoverStationCapacitor(), Is.False);
            _completeThroughBreaker(run);
            Assert.That(run.TryForgeLattice(), Is.True);
            Assert.That(run.TryStabilizeCore(), Is.True);

            Assert.That(run.TryCompleteSecurityTrial(), Is.False);
            Assert.That(run.TryCommitSecurityTrial(), Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.TrialLockdown));
            Assert.That(run.TryCommitSecurityTrial(), Is.False);
            Assert.That(run.TryCompleteSecurityTrial(), Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.StationCapacitor));
            Assert.That(run.TryCompleteSecurityTrial(), Is.False);
            Assert.That(run.TryRecoverStationCapacitor(), Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.SpineCoreInstallation));
            Assert.That(run.TryRecoverStationCapacitor(), Is.False);
            Assert.That(run.TryInstallSpineCore(), Is.True);
            Assert.That(run.SpineCoreInstalled, Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.PoweredWithdrawal));
            Assert.That(run.CanExtract, Is.False);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.TransferVault), Is.False,
                "The powered return checkpoints must not be skipped or completed out of order.");
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.RelayShortcut), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.TransferVault), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.CentralFoothold), Is.True);
            Assert.That(run.CurrentPoweredWithdrawalPhase, Is.EqualTo(PoweredWithdrawalPhase.WardenBay));
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.SapperCradle), Is.False,
                "The Sapper Cradle must not bypass the Warden Bay pursuit beat.");
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.WardenBay), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.SapperCradle), Is.True);
            Assert.That(run.CurrentPoweredWithdrawalPhase, Is.EqualTo(PoweredWithdrawalPhase.DepartureSurge));
            Assert.That(run.CanExtract, Is.False,
                "The Dock uplink must remain locked until the Departure capacitor surge is crossed.");
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.DepartureSurge), Is.True);
            Assert.That(run.CurrentObjective.Id, Is.EqualTo(MissionObjectiveId.Extraction));
            Assert.That(run.TryInstallSpineCore(), Is.False);
        }

        [Test]
        public void Shortcut_RequiresOnlineTowerAndPreservesLastSignal()
        {
            var run = new RunModel();

            Assert.That(run.TryOpenShortcut(), Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.TrySpend(run.Signal - RunModel.ShortcutCost), Is.True);
            Assert.That(run.Signal, Is.EqualTo(RunModel.ShortcutCost));

            Assert.That(run.TryOpenShortcut(), Is.False);
            Assert.That(run.ShortcutOpen, Is.False);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Running));
        }

        [Test]
        public void Shortcut_SpendsOnceAndCannotBePurchasedAgain()
        {
            var run = new RunModel();
            run.TryActivateTower();
            float before = run.Signal;

            Assert.That(run.TryOpenShortcut(), Is.True);
            Assert.That(run.ShortcutOpen, Is.True);
            Assert.That(run.Signal, Is.EqualTo(before - RunModel.ShortcutCost));

            Assert.That(run.TryOpenShortcut(), Is.False);
            Assert.That(run.Signal, Is.EqualTo(before - RunModel.ShortcutCost));
        }

        [Test]
        public void Extraction_RequiresAllTowersAndOnePayloadFromEachRegion()
        {
            var run = new RunModel();

            Assert.That(run.TryBeginExtractionUplink(), Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            _assembleCentralPayload(run);
            Assert.That(run.CanExtract, Is.False);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.CanExtract, Is.False);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.TryRouteFluxShunt(), Is.True);
            Assert.That(run.TryBeginConvergenceCalibration(), Is.True);
            Assert.That(run.AdvanceConvergenceCalibration(run.ConvergenceCalibrationDuration, true), Is.True);
            Assert.That(run.TryResetBreakerDistribution(), Is.True);
            Assert.That(run.TryForgeLattice(), Is.True);
            Assert.That(run.TryStabilizeCore(), Is.True);
            Assert.That(run.TryCommitSecurityTrial(), Is.True);
            Assert.That(run.TryCompleteSecurityTrial(), Is.True);
            Assert.That(run.TryRecoverStationCapacitor(), Is.True);
            Assert.That(run.TryInstallSpineCore(), Is.True);
            Assert.That(run.TryBeginExtractionUplink(), Is.False,
                "Installing the core must not permit bypassing the required powered withdrawal.");
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.RelayShortcut), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.TransferVault), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.CentralFoothold), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.WardenBay), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.SapperCradle), Is.True);
            Assert.That(run.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.DepartureSurge), Is.True);

            Assert.That(run.CanExtract, Is.True);
            Assert.That(run.TryCompleteExtractionUplink(true), Is.False,
                "A completed countdown cannot grant victory before the Dock uplink starts.");
            Assert.That(run.TryBeginExtractionUplink(), Is.True);
            Assert.That(run.ExtractionUplinkActive, Is.True);
            Assert.That(run.TryCompleteExtractionUplink(false), Is.False,
                "Starting the Dock uplink must not grant instant victory.");
            Assert.That(run.TryCompleteExtractionUplink(true), Is.True);
            Assert.That(run.ExtractionUplinkActive, Is.False);
            Assert.That(run.ExtractionUplinkComplete, Is.True);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Victory));
        }

        [Test]
        public void OptionalSalvage_RequiresExtractionReadinessAndPaysOnce()
        {
            var run = new RunModel();

            Assert.That(run.CollectOptionalSalvage(18f), Is.Zero);
            Assert.That(run.OptionalSalvageSecured, Is.False);
            Assert.That(run.TryActivateTower(), Is.True);
            _assembleCentralPayload(run);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.TryRouteFluxShunt(), Is.True);
            Assert.That(run.TryBeginConvergenceCalibration(), Is.True);
            Assert.That(run.AdvanceConvergenceCalibration(run.ConvergenceCalibrationDuration, true), Is.True);
            Assert.That(run.TryResetBreakerDistribution(), Is.True);
            Assert.That(run.TryForgeLattice(), Is.True);
            Assert.That(run.TryStabilizeCore(), Is.True);
            Assert.That(run.TryCommitSecurityTrial(), Is.True);
            Assert.That(run.TryCompleteSecurityTrial(), Is.True);
            Assert.That(run.TryRecoverStationCapacitor(), Is.True);
            Assert.That(run.TryInstallSpineCore(), Is.True);

            run.TrySpend(30f);
            Assert.That(run.CollectOptionalSalvage(18f), Is.EqualTo(18f));
            Assert.That(run.OptionalSalvageSecured, Is.True);
            Assert.That(run.Salvage, Is.EqualTo(RunModel.SalvageRequired));
            Assert.That(run.CollectOptionalSalvage(18f), Is.Zero);
        }

        [Test]
        public void SignalDepletion_EndsRunAndBlocksSpending()
        {
            var run = new RunModel();

            run.Advance(30f, true, false);

            Assert.That(run.Signal, Is.Zero);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Destroyed));
            Assert.That(run.TrySpend(1f), Is.False);
        }

        private static void _assembleCentralPayload(RunModel run)
        {
            Assert.That(run.CollectPayload(SignalRegion.Central), Is.True);
            Assert.That(run.TryRouteCentralComponents(), Is.True);
            Assert.That(run.TryAssembleCentralPayload(), Is.True);
            Assert.That(run.TryInstallCentralPayload(), Is.True);
        }

        private static void _completeThroughBreaker(RunModel run)
        {
            Assert.That(run.TryActivateTower(), Is.True);
            _assembleCentralPayload(run);
            Assert.That(run.TryActivateRelayTower(), Is.True);
            Assert.That(run.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(run.TryInstallRelayPayload(), Is.True);
            Assert.That(run.TryVentSpineBerth(), Is.True);
            Assert.That(run.TryActivateSpineTower(), Is.True);
            Assert.That(run.TryChargeInductionLattice(), Is.True);
            Assert.That(run.TryRouteFluxShunt(), Is.True);
            Assert.That(run.TryBeginConvergenceCalibration(), Is.True);
            Assert.That(run.AdvanceConvergenceCalibration(run.ConvergenceCalibrationDuration, true), Is.True);
            Assert.That(run.TryResetBreakerDistribution(), Is.True);
        }

        [Test]
        public void SecurityDamage_IsDeterministicAndCanDestroyDrone()
        {
            var run = new RunModel();

            run.TakeSecurityHit();
            Assert.That(run.Signal, Is.EqualTo(RunModel.StartingSignal - RunModel.SecurityHitCost));

            run.TakeSecurityHit();
            run.TakeSecurityHit();
            run.TakeSecurityHit();
            Assert.That(run.Signal, Is.Zero);
            Assert.That(run.IsCriticalRecovery, Is.True);
            run.Advance(RunModel.CriticalRecoveryDuration, false, true);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Destroyed));
        }

        [Test]
        public void SapperPulse_DrainsSignalAndCanDestroyDrone()
        {
            var run = new RunModel();

            run.TakeSapperPulse();
            Assert.That(run.Signal, Is.EqualTo(RunModel.StartingSignal - RunModel.SapperPulseCost));

            for (int i = 1; i < 9; i++)
            {
                run.TakeSapperPulse();
            }

            Assert.That(run.Signal, Is.Zero);
            run.Advance(RunModel.CriticalRecoveryDuration, false, true);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Destroyed));
        }

        [Test]
        public void SuppressionPulse_ClampsPartialReserveAndCanDestroyDrone()
        {
            var run = new RunModel();
            Assert.That(run.TrySpend(run.Signal - 2f), Is.True);

            run.TakeSuppressionPulse(4f);

            Assert.That(run.Signal, Is.Zero);
            run.Advance(RunModel.CriticalRecoveryDuration, false, true);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Destroyed));
        }

        [Test]
        public void CriticalRecovery_RestoringSignalCancelsDestruction()
        {
            var run = new RunModel();
            run.TrySpend(run.Signal);

            Assert.That(run.IsCriticalRecovery, Is.True);
            run.Advance(2f, false, true);
            Assert.That(run.RestoreSignal(12f), Is.EqualTo(12f));
            Assert.That(run.IsCriticalRecovery, Is.False);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Running));
        }

        [Test]
        public void CriticalRecovery_TowerActivationFinancesRescueAtZeroSignal()
        {
            var run = new RunModel();
            run.TrySpend(run.Signal);

            Assert.That(run.IsCriticalRecovery, Is.True);
            Assert.That(run.TryActivateTower(), Is.True);
            Assert.That(run.Signal, Is.EqualTo(RunModel.TowerRefill));
            Assert.That(run.IsCriticalRecovery, Is.False);
        }

        [Test]
        public void RunMetrics_TrackOnlyPositiveTimeAndDeadZoneExposure()
        {
            var metrics = new RunMetrics();

            metrics.Advance(3f, true);
            metrics.Advance(2.5f, false);
            metrics.Advance(-4f, false);

            Assert.That(metrics.ElapsedSeconds, Is.EqualTo(5.5f));
            Assert.That(metrics.DeadZoneSeconds, Is.EqualTo(2.5f));
        }

        [Test]
        public void RunMetrics_CountShotsAndSecurityHits()
        {
            var metrics = new RunMetrics();

            metrics.RecordShot();
            metrics.RecordShot();
            metrics.RecordSecurityHit();
            metrics.RecordSapperPulse();

            Assert.That(metrics.ShotsFired, Is.EqualTo(2));
            Assert.That(RunModel.ShotCost, Is.Zero, "Basic shots are permanently free under the combat-first direction.");
            Assert.That(metrics.WeaponSignalSpent, Is.Zero);
            Assert.That(metrics.SecurityHits, Is.EqualTo(1));
            Assert.That(metrics.SapperPulses, Is.EqualTo(1));
        }

        [Test]
        public void RunMetrics_AttributeSwarmerContactsAndPurges()
        {
            var metrics = new RunMetrics();

            metrics.RecordSwarmerContact();
            metrics.RecordSwarmerPurge(3f);

            Assert.That(metrics.SwarmerContacts, Is.EqualTo(1));
            Assert.That(metrics.SwarmersPurged, Is.EqualTo(1));
            Assert.That(metrics.ThreatsPurged, Is.EqualTo(1));
            Assert.That(metrics.SignalRecovered, Is.EqualTo(3f));
        }

        [Test]
        public void RunMetrics_TrackMinimumSignalAndPeakThreatConcurrency()
        {
            var metrics = new RunMetrics();

            metrics.RecordSignal(64f);
            metrics.RecordSignal(81f);
            metrics.RecordSignal(19f);
            metrics.RecordThreatConcurrency(4);
            metrics.RecordThreatConcurrency(10);
            metrics.RecordThreatConcurrency(6);

            Assert.That(metrics.MinimumSignal, Is.EqualTo(19f));
            Assert.That(metrics.PeakThreatConcurrency, Is.EqualTo(10));
        }

        [Test]
        public void SalvageChain_EscalatesRewardsInsideWindow()
        {
            var chain = new SalvageChain();

            Assert.That(chain.RecordCollection(12f, 4f, 8f), Is.Zero);
            chain.Advance(5f);
            Assert.That(chain.RecordCollection(12f, 4f, 8f), Is.EqualTo(4f));
            chain.Advance(11.9f);
            Assert.That(chain.RecordCollection(12f, 4f, 8f), Is.EqualTo(8f));
            Assert.That(chain.BestCount, Is.EqualTo(3));
        }

        [Test]
        public void SalvageChain_ExpiresAndRestartsWithoutReward()
        {
            var chain = new SalvageChain();
            chain.RecordCollection(12f, 4f, 8f);

            chain.Advance(12f);

            Assert.That(chain.Count, Is.Zero);
            Assert.That(chain.RecordCollection(12f, 4f, 8f), Is.Zero);
            Assert.That(chain.Count, Is.EqualTo(1));
        }

        [Test]
        public void RunMetrics_TrackBestSalvageChainAndActualRecovery()
        {
            var metrics = new RunMetrics();
            metrics.RecordSalvageChain(2, 4f);
            metrics.RecordSalvageChain(3, 2f);

            Assert.That(metrics.BestSalvageChain, Is.EqualTo(3));
            Assert.That(metrics.SalvageSignalRecovered, Is.EqualTo(6f));
        }

        [Test]
        public void RunMetrics_DepartureSurgeRecordsOnlyActualRecovery()
        {
            var metrics = new RunMetrics();

            metrics.RecordDepartureSurge(12f);
            metrics.RecordDepartureSurge(0f);
            metrics.RecordDepartureSurge(-4f);

            Assert.That(metrics.SignalRecovered, Is.EqualTo(12f));
        }
    }
}
