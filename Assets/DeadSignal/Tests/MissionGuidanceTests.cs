using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class MissionGuidanceTests
    {
        [Test]
        public void Evaluate_BeforeTower_PreviewsActivationTransaction()
        {
            var guidance = MissionGuidance.Evaluate(new RunModel(), false, false, 0f);

            Assert.That(guidance.Phase, Is.EqualTo(1));
            Assert.That(guidance.Title, Is.EqualTo("RESTORE NETWORK"));
            Assert.That(guidance.Action, Does.Contain("ACTIVATE"));
            Assert.That(guidance.Advisory, Does.Contain("-10"));
            Assert.That(guidance.Advisory, Does.Contain("+62"));
        }

        [Test]
        public void Evaluate_SalvagePhase_ReportsRemainingCaches()
        {
            var model = _createOnlineModel();
            model.CollectSalvage();

            var guidance = MissionGuidance.Evaluate(model, true, false, 0f);

            Assert.That(guidance.Phase, Is.EqualTo(2));
            Assert.That(guidance.Title, Is.EqualTo("RECOVER SALVAGE"));
            Assert.That(guidance.Advisory, Does.StartWith("2 SALVAGE REMAINING"));
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
            for (var i = 0; i < RunModel.SalvageRequired; i++)
            {
                model.CollectSalvage();
            }

            var guidance = MissionGuidance.Evaluate(model, true, true, 0.44f);

            Assert.That(guidance.Phase, Is.EqualTo(3));
            Assert.That(guidance.Action, Does.Contain("CYAN DOCK"));
            Assert.That(guidance.Advisory, Is.EqualTo("SAPPER DRAIN IN 0.4s  //  EXTRACTION READY"));
        }

        private static RunModel _createOnlineModel()
        {
            var model = new RunModel();
            Assert.That(model.TryActivateTower(), Is.True);
            return model;
        }
    }
}
