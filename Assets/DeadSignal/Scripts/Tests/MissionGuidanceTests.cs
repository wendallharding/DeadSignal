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
        public void Evaluate_CentralPayloadPhase_OffersOneOfTwoRoutes()
        {
            var model = _createOnlineModel();

            var guidance = MissionGuidance.Evaluate(model, true, false, 0f);

            Assert.That(guidance.Phase, Is.EqualTo(2));
            Assert.That(guidance.Title, Is.EqualTo("CENTRAL PAYLOAD"));
            Assert.That(guidance.Advisory, Does.Contain("ONE REQUIRED"));
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

        private static RunModel _createOnlineModel()
        {
            var model = new RunModel();
            Assert.That(model.TryActivateTower(), Is.True);
            return model;
        }

        private static void _completeNetworkJourney(RunModel model)
        {
            Assert.That(model.CollectPayload(SignalRegion.Central), Is.True);
            Assert.That(model.TryActivateRelayTower(), Is.True);
            Assert.That(model.CollectPayload(SignalRegion.Relay), Is.True);
            Assert.That(model.TryActivateSpineTower(), Is.True);
            Assert.That(model.CollectPayload(SignalRegion.Spine), Is.True);
        }
    }
}
