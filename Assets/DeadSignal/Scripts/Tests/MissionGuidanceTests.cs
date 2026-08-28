using System;
using NUnit.Framework;
using DeadSignal.Missions;

namespace DeadSignal.Tests
{
    public sealed class MissionGuidanceTests
    {
        [Test]
        public void Evaluate_BeforeTower_PreviewsActivationTransaction()
        {
            var guidance = MissionGuidance.Evaluate(new RunModel(), false, false, 0f);

            Assert.That(guidance.Phase, Is.EqualTo(1));
            Assert.That(guidance.Title, Is.EqualTo("RESTORE CENTRAL"));
            Assert.That(guidance.Action, Does.Contain("ACTIVATE"));
            Assert.That(guidance.Advisory, Does.Contain("-10"));
            Assert.That(guidance.Advisory, Does.Contain("+62"));
        }

        [Test]
        public void Evaluate_CentralRestartPhase_RequiresBothDistinctJobs()
        {
            var model = _createOnlineModel();

            var guidance = MissionGuidance.Evaluate(model, true, false, 0f);

            Assert.That(guidance.Phase, Is.EqualTo(2));
            Assert.That(guidance.Title, Is.EqualTo("RESTART CENTRAL"));
            Assert.That(guidance.Action, Does.Contain("POWER COUPLING"));
            Assert.That(guidance.Advisory, Does.Contain("EITHER ORDER"));
        }

        [Test]
        public void Evaluate_LatchedSapper_InterruptsSalvageAdvisory()
        {
            var guidance = MissionGuidance.Evaluate(_createOnlineModel(), true, true, 1.25f);

            Assert.That(guidance.Advisory, Is.EqualTo("INTERRUPT: SAPPER DRAIN IN 1.3s"));
        }

        [Test]
        public void Evaluate_ExtractionPhase_PreservesUrgentDrainWarning()
        {
            var model = _createOnlineModel();
            _completeNetworkJourney(model);

            var guidance = MissionGuidance.Evaluate(model, true, true, 0.44f);

            Assert.That(guidance.Phase, Is.EqualTo(7));
            Assert.That(guidance.Action, Does.Contain("CYAN DOCK"));
            Assert.That(guidance.Advisory, Is.EqualTo("SAPPER DRAIN IN 0.4s  //  EXTRACTION READY"));
        }

        [Test]
        public void Evaluate_UsesCurrentObjectiveAuthoredGuidanceInsteadOfLegacyStageCopy()
        {
            var authoredGuidance = new MissionGuidanceState(9, "AUTHORED TITLE", "AUTHORED ACTION", "AUTHORED ADVISORY");
            var objective = new MissionObjectiveDefinition(MissionObjectiveId.CentralTower, MissionStage.Extraction,
                "Test Room", "Test Anchor", MissionCompletionRule.CentralTowerOnline, MissionWorldMutation.None,
                Array.Empty<MissionReward>(), authoredGuidance, Array.Empty<MissionObjectiveId>(),
                Array.Empty<MissionObjectiveId>());
            var model = new RunModel(new MissionObjectiveGraph(objective));

            var guidance = MissionGuidance.Evaluate(model, false, false, 0f);

            Assert.That(guidance.Phase, Is.EqualTo(9));
            Assert.That(guidance.Title, Is.EqualTo("AUTHORED TITLE"));
            Assert.That(guidance.Action, Is.EqualTo("AUTHORED ACTION"));
            Assert.That(guidance.Advisory, Is.EqualTo("AUTHORED ADVISORY"));
        }

        private static RunModel _createOnlineModel()
        {
            var model = new RunModel();
            Assert.That(model.TryActivateTower(), Is.True);
            return model;
        }

        private static void _completeNetworkJourney(RunModel model)
        {
            Assert.That(model.CollectPayload(SignalRegion.Central), Is.True);
            Assert.That(model.TryRouteCentralComponents(), Is.True);
            Assert.That(model.TryAssembleCentralPayload(), Is.True);
            Assert.That(model.TryInstallCentralPayload(), Is.True);
            Assert.That(model.TryActivateRelayTower(), Is.True);
            Assert.That(model.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(model.TryInstallRelayPayload(), Is.True);
            Assert.That(model.TryVentSpineBerth(), Is.True);
            Assert.That(model.TryActivateSpineTower(), Is.True);
            Assert.That(model.TryChargeInductionLattice(), Is.True);
            Assert.That(model.TryRouteFluxShunt(), Is.True);
            Assert.That(model.TryBeginConvergenceCalibration(), Is.True);
            Assert.That(model.AdvanceConvergenceCalibration(model.ConvergenceCalibrationDuration, true), Is.True);
            Assert.That(model.TryResetBreakerDistribution(), Is.True);
            Assert.That(model.TryForgeLattice(), Is.True);
            Assert.That(model.TryStabilizeCore(), Is.True);
            Assert.That(model.TryCommitSecurityTrial(), Is.True);
            Assert.That(model.TryCompleteSecurityTrial(), Is.True);
            Assert.That(model.TryRecoverStationCapacitor(), Is.True);
            Assert.That(model.CollectPayload(SignalRegion.Spine), Is.True);
        }
    }
}
