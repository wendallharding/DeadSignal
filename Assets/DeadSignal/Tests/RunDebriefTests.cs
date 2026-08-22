using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class RunDebriefTests
    {
        [Test]
        public void Evaluate_CleanVictoryAwardsTopGradeAndActionableReadings()
        {
            var model = new RunModel();
            var metrics = new RunMetrics();
            model.TryActivateTower();
            for (var i = 0; i < RunModel.SalvageRequired; i++) model.CollectSalvage();
            model.TryExtract();
            metrics.Advance(10f, true);
            var debrief = RunDebrief.Evaluate(model, metrics);
            Assert.That(debrief.Grade, Is.EqualTo("S"));
            Assert.That(debrief.Signal, Is.EqualTo("RESERVE SECURE"));
            Assert.That(debrief.Combat, Is.EqualTo("NO SECURITY DRAINS"));
            Assert.That(debrief.Exposure, Is.EqualTo("EXPOSURE CONTROLLED"));
            Assert.That(debrief.Route, Is.EqualTo("CONSERVATION ROUTE"));
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
    }
}
