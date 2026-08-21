using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class RunModelTests
    {
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
        public void Extraction_RequiresAllSalvage()
        {
            var run = new RunModel();

            Assert.That(run.TryExtract(), Is.False);
            for (int i = 0; i < RunModel.SalvageRequired; i++)
            {
                run.CollectSalvage();
            }

            Assert.That(run.CanExtract, Is.True);
            Assert.That(run.TryExtract(), Is.True);
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Victory));
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
            Assert.That(run.Outcome, Is.EqualTo(RunOutcome.Destroyed));
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
            Assert.That(metrics.SecurityHits, Is.EqualTo(1));
            Assert.That(metrics.SapperPulses, Is.EqualTo(1));
        }
    }
}
