using NUnit.Framework;
using DeadSignal.Combat;
using DeadSignal.Missions;

namespace DeadSignal.Tests
{
    public sealed class SecurityEscalationDirectorTests
    {
        [Test]
        public void Tick_EachSalvageQueuesInterceptorThenFirstPurgedCoreRole()
        {
            var director = new SecurityEscalationDirector(2f, 6f);

            Assert.That(director.Tick(0f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.Tick(2f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Interceptor));
            Assert.That(director.Tick(0f, true, 2, false, true, true, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Sapper));
            Assert.That(director.Tick(2f, true, 2, false, true, true, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Sapper));
            Assert.That(director.Tick(0f, true, 3, false, true, false, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Warden));
            Assert.That(director.Tick(2f, true, 3, false, true, false, true, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Warden));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);
        }

        [TestCase(false, true, SecurityReinforcement.Warden, SecurityReinforcement.Sapper)]
        [TestCase(true, false, SecurityReinforcement.Sapper, SecurityReinforcement.Warden)]
        public void Tick_FirstCacheAfterSinglePurge_ReplacesMissingRoleBeforeInterceptor(
            bool wardenAlive,
            bool sapperAlive,
            SecurityReinforcement first,
            SecurityReinforcement third)
        {
            var director = new SecurityEscalationDirector(1f, 6f);

            director.Tick(0f, true, 1, false, false, wardenAlive, sapperAlive, false, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(first));
            Assert.That(director.Tick(1f, true, 1, false, false, wardenAlive, sapperAlive, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(first));

            director.Tick(0f, true, 2, false, false, true, true, false, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Interceptor));
            Assert.That(director.Tick(1f, true, 2, false, false, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.Interceptor));

            director.Tick(0f, true, 3, false, true, first == SecurityReinforcement.Warden,
                first == SecurityReinforcement.Sapper, false, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(third));
            Assert.That(director.Tick(1f, true, 3, false, true, first == SecurityReinforcement.Warden,
                first == SecurityReinforcement.Sapper, false, 8f, 8f, 8f, 8f), Is.EqualTo(third));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);
        }

        [TestCase(false, SecurityReinforcement.Warden, SecurityReinforcement.Sapper)]
        [TestCase(true, SecurityReinforcement.Sapper, SecurityReinforcement.Warden)]
        public void Tick_FirstCacheAfterDoublePurge_UsesRunPreferenceThenInterceptor(
            bool preferSapper,
            SecurityReinforcement first,
            SecurityReinforcement third)
        {
            var director = new SecurityEscalationDirector(1f, 6f, preferSapper);

            director.Tick(0f, true, 1, false, false, false, false, false, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(first));
            Assert.That(director.Tick(1f, true, 1, false, false, false, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(first));
            director.Tick(0f, true, 2, false, false, true, true, false, 8f, 8f, 8f, 8f);
            Assert.That(director.Tick(1f, true, 2, false, false, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.Interceptor));
            director.Tick(0f, true, 3, false, true, first == SecurityReinforcement.Warden,
                first == SecurityReinforcement.Sapper, false, 8f, 8f, 8f, 8f);
            Assert.That(director.Tick(1f, true, 3, false, true, first == SecurityReinforcement.Warden,
                first == SecurityReinforcement.Sapper, false, 8f, 8f, 8f, 8f), Is.EqualTo(third));
        }

        [Test]
        public void Tick_ContinuousDeadZoneExposureBanksExistingInterceptorResponse()
        {
            var director = new SecurityEscalationDirector(2f, 6f, deadZoneTraceDuration: 4f);

            Assert.That(director.Tick(2f, true, false, 0, false, false, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.IsDeadZoneTraceActive, Is.True);
            Assert.That(director.DeadZoneTraceSecondsRemaining, Is.EqualTo(2f));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);

            Assert.That(director.Tick(2f, true, false, 0, false, false, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.IsDeadZoneTraceCompleted, Is.True);
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Interceptor));
            Assert.That(director.ReinforcementsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void Tick_ShortPoweredCrossingCoolsButDoesNotEraseDeadZoneTrace()
        {
            var director = new SecurityEscalationDirector(
                2f,
                6f,
                deadZoneTraceDuration: 4f,
                deadZoneTraceRecoveryRate: 0.5f);

            director.Tick(2f, true, false, 0, false, false, true, true, false, 8f, 8f, 8f, 8f);
            director.Tick(1f, true, true, 0, false, false, true, true, false, 8f, 8f, 8f, 8f);

            Assert.That(director.IsDeadZoneTraceActive, Is.True);
            Assert.That(director.IsDeadZoneTraceCooling, Is.True);
            Assert.That(director.DeadZoneTraceSecondsRemaining, Is.EqualTo(2.5f));
            director.Tick(2.5f, true, false, 0, false, false, true, true, false, 8f, 8f, 8f, 8f);
            Assert.That(director.IsDeadZoneTraceCompleted, Is.True);
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Interceptor));
        }

        [Test]
        public void Tick_SustainedPoweredRecoveryClearsPartialDeadZoneTrace()
        {
            var director = new SecurityEscalationDirector(
                2f,
                6f,
                deadZoneTraceDuration: 4f,
                deadZoneTraceRecoveryRate: 0.5f);

            director.Tick(2f, true, false, 0, false, false, true, true, false, 8f, 8f, 8f, 8f);
            director.Tick(4f, true, true, 0, false, false, true, true, false, 8f, 8f, 8f, 8f);

            Assert.That(director.IsDeadZoneTraceActive, Is.False);
            Assert.That(director.IsDeadZoneTraceCooling, Is.False);
            Assert.That(director.DeadZoneTraceSecondsRemaining, Is.Zero);
        }

        [Test]
        public void Tick_DeadZoneTraceAndFirstCacheShareOneResponseSlot()
        {
            var director = new SecurityEscalationDirector(1f, 0f, deadZoneTraceDuration: 2f);

            director.Tick(2f, true, false, 0, false, false, true, true, false, 8f, 8f, 8f, 8f);
            Assert.That(director.Tick(1f, true, false, 0, false, false, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.Interceptor));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);

            Assert.That(director.Tick(5f, true, false, 1, false, true, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);
        }

        [TestCase(false, SecurityReinforcement.Warden, SecurityReinforcement.Sapper)]
        [TestCase(true, SecurityReinforcement.Sapper, SecurityReinforcement.Warden)]
        public void Tick_BothCoreRolesPurged_UsesRunPreferenceWithoutRepeating(
            bool preferSapper,
            SecurityReinforcement first,
            SecurityReinforcement second)
        {
            var director = new SecurityEscalationDirector(1f, 6f, preferSapper);

            director.Tick(0f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f);
            Assert.That(director.Tick(1f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.Interceptor));
            director.Tick(0f, true, 2, false, true, false, false, false, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(first));
            Assert.That(director.Tick(1f, true, 2, false, true, false, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(first));
            director.Tick(0f, true, 3, false, true, first == SecurityReinforcement.Warden, first == SecurityReinforcement.Sapper,
                false, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(second));
            Assert.That(director.Tick(1f, true, 3, false, true, first == SecurityReinforcement.Warden,
                first == SecurityReinforcement.Sapper, false, 8f, 8f, 8f, 8f), Is.EqualTo(second));
        }

        [Test]
        public void Tick_HoldsCoreResponseUntilEitherRoleHasBeenPurged()
        {
            var director = new SecurityEscalationDirector(2f, 6f);

            director.Tick(0f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f);
            director.Tick(2f, true, 1, false, false, true, true, false, 8f, 8f, 8f, 8f);

            Assert.That(director.Tick(5f, true, 2, false, true, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.EntryCountdown, Is.Zero);
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

            director.Tick(0f, true, 3, false, false, true, true, false, 8f, 8f, 8f, 8f);
            director.Tick(2f, true, 3, false, false, true, true, false, 8f, 8f, 8f, 8f);
            director.Tick(0f, true, 3, false, true, false, true, false, 8f, 8f, 8f, 8f);
            director.Tick(2f, true, 3, false, true, false, true, false, 8f, 8f, 8f, 8f);
            director.Tick(0f, true, 3, false, true, true, false, false, 8f, 8f, 8f, 8f);
            director.Tick(2f, true, 3, false, true, true, false, false, 8f, 8f, 8f, 8f);
            Assert.That(director.ReinforcementsRemaining, Is.Zero);

            Assert.That(director.Tick(0f, true, 3, true, false, false, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Suppressor));
            Assert.That(director.Tick(2f, true, 3, true, false, false, false, false, 8f, 8f, 8f, 8f), Is.EqualTo(SecurityReinforcement.Suppressor));
            Assert.That(director.ReinforcementsRemaining, Is.Zero);
        }

        [Test]
        public void Tick_ExtractionPromotesSuppressorAheadOfUnresolvedSalvageResponses()
        {
            var director = new SecurityEscalationDirector(2f, 6f);

            director.Tick(0f, true, 3, false, false, true, true, false, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Interceptor));

            director.Tick(0f, true, 3, true, false, true, true, false, 8f, 8f, 8f, 8f);

            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Suppressor));
            Assert.That(director.EntryCountdown, Is.EqualTo(2f),
                "Changing roles at uplink start must restart the full readable entry warning.");
            Assert.That(director.ReinforcementsRemaining, Is.EqualTo(4));
            Assert.That(director.Tick(2f, true, 3, true, false, true, true, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.Suppressor));
            Assert.That(director.ReinforcementsRemaining, Is.EqualTo(3));

            director.Tick(0f, true, 3, true, false, true, true, true, 8f, 8f, 8f, 8f);
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Interceptor),
                "The earlier bounded salvage response should remain available after the promoted Suppressor.");
        }

        [Test]
        public void Tick_PromotedSuppressorStillRequiresSafeUniqueEntry()
        {
            var director = new SecurityEscalationDirector(2f, 6f);

            Assert.That(director.Tick(5f, true, 3, true, false, false, false, true, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.Tick(5f, true, 3, true, false, false, false, false, 8f, 8f, 8f, 3f),
                Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.Tick(0f, true, 3, true, false, false, false, false, 8f, 8f, 8f, 8f),
                Is.EqualTo(SecurityReinforcement.None));
            Assert.That(director.PendingReinforcement, Is.EqualTo(SecurityReinforcement.Suppressor));
            Assert.That(director.EntryCountdown, Is.EqualTo(2f));
        }
    }
}
