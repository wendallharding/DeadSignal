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
            model.TryExtract();
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

        private static void _completeRequiredJourney(RunModel model)
        {
            model.TryActivateTower();
            model.CollectPayload(SignalRegion.Central);
            model.TryRouteCentralComponents();
            model.TryAssembleCentralPayload();
            model.TryActivateRelayTower();
            model.CollectPayload(SignalRegion.Relay);
            model.TryActivateSpineTower();
            model.CollectPayload(SignalRegion.Spine);
        }
    }
}
