using System;
using DeadSignal.Missions;
using NUnit.Framework;

namespace DeadSignal.Tests
{
    public sealed class CargoCouplingRetrievalTests
    {
        [Test]
        public void CouplingCannotBeTakenBeforeCommitment()
        {
            var retrieval = new CargoCouplingRetrieval(0.5f, -0.5f);

            Assert.That(retrieval.TryTakeCoupling(0.49f, true), Is.False);
            Assert.That(retrieval.Phase, Is.EqualTo(CargoCouplingRetrievalPhase.AwaitingCommit));
            Assert.That(retrieval.TryTakeCoupling(0.5f, false), Is.False);
        }

        [Test]
        public void CouplingCompletesOnlyAfterOutwardCrossing()
        {
            var retrieval = new CargoCouplingRetrieval(0.5f, -0.5f);

            Assert.That(retrieval.TryTakeCoupling(0.75f, true), Is.True);
            Assert.That(retrieval.CanCompleteWithdrawal(0f, true), Is.False);
            Assert.That(retrieval.CanCompleteWithdrawal(-0.5f, true), Is.True);
            retrieval.CompleteWithdrawal();

            Assert.That(retrieval.Phase, Is.EqualTo(CargoCouplingRetrievalPhase.Complete));
            Assert.That(retrieval.TryTakeCoupling(0.75f, true), Is.False);
        }

        [Test]
        public void InvalidThresholdOrderIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new CargoCouplingRetrieval(0.5f, 0.5f));
            Assert.Throws<ArgumentException>(() => new CargoCouplingRetrieval(-0.5f, 0.5f));
        }
    }
}
