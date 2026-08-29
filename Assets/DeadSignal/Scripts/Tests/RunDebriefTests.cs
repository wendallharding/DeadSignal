using NUnit.Framework;
using DeadSignal.Missions;

namespace DeadSignal.Tests
{
    public sealed class RunDebriefTests
    {
        [Test]
        public void Evaluate_CleanVictoryAwardsTopGradeAndActionableReadings()
        {
            var model = new RunModel();
            var metrics = new RunMetrics();
            _completeRequiredJourney(model);
            model.TryBeginExtractionUplink();
            model.TryCompleteExtractionUplink(true);
            metrics.Advance(10f, true);
            var debrief = RunDebrief.Evaluate(model, metrics);
            Assert.That(debrief.Grade, Is.EqualTo("S"));
            Assert.That(debrief.Signal, Is.EqualTo("RESERVE SECURE"));
            Assert.That(debrief.Combat, Is.EqualTo("NO SECURITY DRAINS"));
            Assert.That(debrief.Exposure, Is.EqualTo("EXPOSURE CONTROLLED"));
            Assert.That(debrief.Route, Is.EqualTo("REQUIRED ROUTE — WITHDREW"));
        }

        [Test]
        public void Evaluate_PressuredShortcutRunReportsEveryTradeoff()
        {
            var model = new RunModel();
            var metrics = new RunMetrics();
            model.TryActivateTower(); model.TryOpenShortcut(); model.TakeSecurityHit();
            metrics.RecordSecurityHit(); metrics.RecordSapperPulse();
            metrics.Advance(4f, true); metrics.Advance(6f, false);
            var debrief = RunDebrief.Evaluate(model, metrics);
            Assert.That(debrief.Combat, Is.EqualTo("2 SECURITY DRAINS"));
            Assert.That(debrief.Exposure, Is.EqualTo("EXPOSURE SEVERE"));
            Assert.That(debrief.Route, Is.EqualTo("SHORTCUT ROUTE"));
            Assert.That(debrief.Grade, Is.EqualTo("D"));
        }

        [Test]
        public void Evaluate_OptionalCacheNamesGreedRoute()
        {
            var model = new RunModel();
            var metrics = new RunMetrics();
            _completeRequiredJourney(model);
            model.CollectOptionalSalvage(12f);

            var debrief = RunDebrief.Evaluate(model, metrics);

            Assert.That(debrief.Route, Is.EqualTo("GREED ROUTE — OPTIONAL SECURED"));
        }

        [Test]
        public void FailureDebrief_ReportsExactCauseProgressAndDeadZoneLesson()
        {
            var model = new RunModel();
            var metrics = new RunMetrics();
            model.TryActivateTower();
            metrics.Advance(12f, false);
            metrics.Advance(4f, true);
            metrics.RecordTraversalDrain(72f, 18f);
            model.TrySpend(model.Signal);
            model.Advance(RunModel.CriticalRecoveryDuration, false, false);

            var debrief = RunFailureDebrief.Evaluate(model, metrics);

            Assert.That(debrief.Cause, Is.EqualTo("SIGNAL DEPLETED — EMERGENCY RECOVERY EXPIRED"));
            Assert.That(debrief.Progress, Does.Contain("FAILED AT  RESTART CENTRAL"));
            Assert.That(debrief.Progress, Does.Contain("CARGO ANNEX"));
            Assert.That(debrief.Summary, Does.Contain("RUN 00:16"));
            Assert.That(debrief.Summary, Does.Contain("TRAVEL 90"));
            Assert.That(debrief.Coaching, Does.Contain("CROSS CYAN POWER"));
        }

        [Test]
        public void FailureDebrief_PrioritizesSecurityLessonWhenExposureIsControlled()
        {
            var model = new RunModel();
            var metrics = new RunMetrics();
            metrics.Advance(20f, true);
            metrics.RecordSecurityHit();
            metrics.RecordSapperPulse();
            model.TrySpend(model.Signal);
            model.Advance(RunModel.CriticalRecoveryDuration, true, false);

            var debrief = RunFailureDebrief.Evaluate(model, metrics);

            Assert.That(debrief.Summary, Does.Contain("CONTACTS 2"));
            Assert.That(debrief.Coaching, Does.Contain("PURGE THE SAPPER LINK FIRST"));
        }

        private static void _completeRequiredJourney(RunModel model)
        {
            model.TryActivateTower();
            model.CollectPayload(SignalRegion.Central);
            model.TryRouteCentralComponents();
            model.TryAssembleCentralPayload();
            model.TryInstallCentralPayload();
            model.TryActivateRelayTower();
            model.CollectPayload(SignalRegion.Relay);
            model.TryInstallRelayPayload();
            model.TryVentSpineBerth();
            model.TryActivateSpineTower();
            model.TryChargeInductionLattice();
            model.TryRouteFluxShunt();
            model.TryBeginConvergenceCalibration();
            model.AdvanceConvergenceCalibration(model.ConvergenceCalibrationDuration, true);
            model.TryResetBreakerDistribution();
            model.TryForgeLattice();
            model.TryStabilizeCore();
            model.TryCommitSecurityTrial();
            model.TryCompleteSecurityTrial();
            model.TryRecoverStationCapacitor();
            model.TryInstallSpineCore();
            model.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.RelayShortcut);
            model.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.TransferVault);
            model.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.CentralFoothold);
            model.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.WardenBay);
            model.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.SapperCradle);
            model.TryAdvancePoweredWithdrawal(PoweredWithdrawalPhase.DepartureSurge);
        }
    }
}
