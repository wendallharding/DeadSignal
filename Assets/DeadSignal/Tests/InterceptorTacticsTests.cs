using NUnit.Framework;
using UnityEngine;
using DeadSignal.Combat;

namespace DeadSignal.Tests
{
    public sealed class InterceptorTacticsTests
    {
        [Test]
        public void SelectSafestEntrance_UsesFartherAuthoredGate()
        {
            var selected = InterceptorTactics.SelectSafestEntrance(
                new Vector3(-10f, 0f, 6f),
                new Vector3(-16f, 0f, 7f),
                new Vector3(2f, 0f, -7f));

            Assert.That(selected, Is.EqualTo(1));
        }

        [Test]
        public void CalculateCutoffPoint_StaysBetweenPlayerAndExtraction()
        {
            var cutoff = InterceptorTactics.CalculateCutoffPoint(
                new Vector3(10f, 3f, 6f),
                new Vector3(-10f, 0f, -6f),
                0.5f);

            Assert.That(cutoff, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CalculateSuppressionExitPoint_BlocksObviousExitAndLeavesOtherAnglesOpen()
        {
            var center = new Vector3(2f, 3f, -1f);
            var cutoff = InterceptorTactics.CalculateSuppressionExitPoint(
                center,
                center + Vector3.right,
                center + Vector3.left * 6f,
                3.25f,
                0.65f);

            Assert.That(cutoff, Is.EqualTo(new Vector3(5.9f, 0f, -1f)));
            Assert.That(Vector3.Distance(cutoff, new Vector3(2f, 0f, 2.9f)), Is.GreaterThan(5f),
                "Only the predicted exit should be contested; perpendicular ring exits must remain open.");
        }

        [Test]
        public void CalculateSuppressionExitPoint_CenteredPlayerChoosesExitAwayFromInterceptor()
        {
            var center = Vector3.zero;
            var cutoff = InterceptorTactics.CalculateSuppressionExitPoint(
                center,
                center,
                Vector3.left * 5f,
                3.25f,
                0.65f);

            Assert.That(cutoff, Is.EqualTo(Vector3.right * 3.9f));
        }

        [Test]
        public void CalculateSapperFlankPoint_ContestsNearestSideAndLeavesOppositeFlankOpen()
        {
            var sapper = new Vector3(1f, 2f, -1f);
            var player = sapper + Vector3.right * 6f;
            var interceptor = sapper + Vector3.forward * 8f;

            var cutoff = InterceptorTactics.CalculateSapperFlankPoint(player, sapper, interceptor, 3.6f);

            Assert.That(cutoff, Is.EqualTo(new Vector3(1f, 0f, 2.6f)));
            Assert.That(Vector3.Dot(
                (cutoff - new Vector3(sapper.x, 0f, sapper.z)).normalized,
                (player - sapper).normalized), Is.EqualTo(0f).Within(0.001f),
                "The Interceptor should contest a perpendicular approach instead of duplicating the direct screen.");
            Assert.That(Vector3.Distance(cutoff, new Vector3(1f, 0f, -4.6f)), Is.EqualTo(7.2f).Within(0.001f),
                "The opposite flank must remain open as readable counterplay.");
        }

        [Test]
        public void CalculateSapperFlankPoint_ChoosesOtherSideWhenInterceptorIsCloser()
        {
            var cutoff = InterceptorTactics.CalculateSapperFlankPoint(
                Vector3.right * 6f,
                Vector3.zero,
                Vector3.back * 8f,
                3.6f);

            Assert.That(cutoff, Is.EqualTo(Vector3.back * 3.6f));
        }

        [Test]
        public void CalculateDashRecoveryDuration_CoverCrashCreatesLongerCounterattackWindow()
        {
            Assert.That(InterceptorTactics.CalculateDashRecoveryDuration(false, 0.7f, 1.5f), Is.EqualTo(0.7f));
            Assert.That(InterceptorTactics.CalculateDashRecoveryDuration(true, 0.7f, 1.5f), Is.EqualTo(1.5f));
        }
    }
}
