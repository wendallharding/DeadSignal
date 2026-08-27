using System;
using DeadSignal.Missions;
using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class CoolantSealThreadingTests
    {
        [Test]
        public void BafflesMustBeThreadedInOrderBeforeSealRelease()
        {
            var threading = new CoolantSealThreading(-1f, -1f, 1f, 1f, 0.5f);

            threading.Observe(1f, 1f, true);
            Assert.That(threading.Phase, Is.EqualTo(CoolantSealThreadingPhase.AwaitingFirstBaffle));
            Assert.That(threading.TryReleaseSeal(true), Is.False);

            threading.Observe(-1f, -1f, true);
            Assert.That(threading.Phase, Is.EqualTo(CoolantSealThreadingPhase.AwaitingSecondBaffle));
            threading.Observe(1f, 1f, true);
            Assert.That(threading.Phase, Is.EqualTo(CoolantSealThreadingPhase.SealAvailable));
        }

        [Test]
        public void ReleasedSealCompletesOnlyAtExitThreshold()
        {
            var threading = new CoolantSealThreading(-1f, -1f, 1f, 1f, 0.5f);
            threading.Observe(-1f, -1f, true);
            threading.Observe(1f, 1f, true);

            Assert.That(threading.TryReleaseSeal(true), Is.True);
            Assert.That(threading.CanCompleteRelease(false, true), Is.False);
            Assert.That(threading.CanCompleteRelease(true, false), Is.False);
            Assert.That(threading.CanCompleteRelease(true, true), Is.True);
            threading.CompleteRelease();

            Assert.That(threading.Phase, Is.EqualTo(CoolantSealThreadingPhase.Complete));
            Assert.That(threading.TryReleaseSeal(true), Is.False);
        }

        [Test]
        public void InvalidWaypointRadiusIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CoolantSealThreading(0f, 0f, 1f, 1f, 0f));
        }
    }
}
