using NUnit.Framework;
using UnityEngine;

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
    }
}
