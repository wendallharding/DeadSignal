using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class WardenTacticsTests
    {
        [Test]
        public void CalculateSapperScreenPoint_InterceptsDirectApproachAtTunedOffset()
        {
            var sapper = new Vector3(1f, 0f, 2f);
            var player = new Vector3(7f, 0f, 2f);

            var target = WardenTactics.CalculateSapperScreenPoint(player, sapper, 2.8f, 2f);

            Assert.That(Vector3.Distance(target, new Vector3(3.8f, 0f, 2f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Dot((target - sapper).normalized, (player - sapper).normalized), Is.GreaterThan(0.999f));
        }

        [Test]
        public void CalculateSapperScreenPoint_PlayerInsideGuardBreakTargetsPlayer()
        {
            var sapper = Vector3.zero;
            var player = new Vector3(1.5f, 0f, 0.25f);

            Assert.That(Vector3.Distance(WardenTactics.CalculateSapperScreenPoint(player, sapper, 2.8f, 2f), player),
                Is.LessThan(0.001f));
        }

        [Test]
        public void CalculateSapperScreenPoint_LeavesPerpendicularApproachesOpen()
        {
            var sapper = Vector3.zero;
            var player = new Vector3(6f, 0f, 0f);

            var target = WardenTactics.CalculateSapperScreenPoint(player, sapper, 2.8f, 2f);

            Assert.That(Vector3.Distance(target, new Vector3(0f, 0f, 2.8f)), Is.GreaterThan(3.5f),
                "The Warden should contest the disclosed direct line without covering every flank around the Sapper.");
        }
    }
}
