using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class SecurityEscalationDirectorTests
    {
        [Test]
        public void Tick_EachSalvageQueuesInterceptorThenExistingRoles()
        {
            var director = new SecurityEscalationDirector(2f, 6f);

            Assert.That(director.Tick(0f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.Tick(2f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Interceptor));
            Assert.That(director.Tick(0f, true, 2, false, true, false, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.Tick(2f, true, 2, false, true, false, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Warden));
            Assert.That(director.Tick(0f, true, 3, false, true, true, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.Tick(2f, true, 3, false, true, true, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Sapper));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);
        }

        [Test]
        public void Tick_HoldsEntryUntilRoleIsDeadAndPlayerLeavesBay()
        {
            var director = new SecurityEscalationDirector(2f, 6f);

            Assert.That(director.Tick(5f, true, 1, false, true, true, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.EntryCountdown, Is.Zero);
            Assert.That(director.Tick(5f, true, 1, false, false, true, true, false, 3f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.EntryCountdown, Is.Zero);
            Assert.That(director.Tick(0f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.EntryCountdown, Is.EqualTo(2f));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Interceptor));
        }

        [Test]
        public void Tick_IgnoresProgressUntilTowerIsOnlineAndCapsAtObjective()
        {
            var director = new SecurityEscalationDirector(1f, 0f);

            director.Tick(10f, false, 9, false, false, false, false, false, 10f, 10f, 10f, 10f);
            Assert.That(director.EscalationTier, Is.Zero);
            director.Tick(0f, true, 9, false, false, false, false, false, 10f, 10f, 10f, 10f);
            Assert.That(director.EscalationTier, Is.EqualTo(RunModel.SalvageRequired));
            Assert.That(director.ReinforcementsRemaining, Is.EqualTo(RunModel.SalvageRequired));
        }

        [Test]
        public void Tick_ExtractionBanksOneFinalBoundedResponse()
        {
            var director = new SecurityEscalationDirector(2f, 6f);

            director.Tick(0f, true, 3, false, false, false, false, false, 8f, 8f, 8f, 8f);
            director.Tick(2f, true, 3, false, false, false, false, false, 8f, 8f, 8f, 8f);
            director.Tick(0f, true, 3, false, true, false, false, false, 8f, 8f, 8f, 8f);
            director.Tick(2f, true, 3, false, true, false, false, false, 8f, 8f, 8f, 8f);
            director.Tick(0f, true, 3, false, true, true, false, false, 8f, 8f, 8f, 8f);
            director.Tick(2f, true, 3, false, true, true, false, false, 8f, 8f, 8f, 8f);
            Assert.That(director.ReinforcementsRemaining, Is.Zero);

            Assert.That(director.Tick(0f, true, 3, true, false, false, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Suppressor));
            Assert.That(director.Tick(2f, true, 3, true, false, false, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Suppressor));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);
        }
    }
}
