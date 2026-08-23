using NUnit.Framework;
using UnityEngine;
using DeadSignal.Presentation;

namespace DeadSignal.Tests
{
    public sealed class SignalHudPresentationTests
    {
        private SignalHudTuning m_tuning;

        [SetUp]
        public void SetUp()
        {
            m_tuning = ScriptableObject.CreateInstance<SignalHudTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_tuning);
        }

        [TestCase(100f, SignalReserveState.Stable)]
        [TestCase(60f, SignalReserveState.Strained)]
        [TestCase(25f, SignalReserveState.Critical)]
        [TestCase(0f, SignalReserveState.Critical)]
        public void Evaluate_SelectsReadableReserveState(float signal, SignalReserveState expected)
        {
            var presentation = SignalHudPresentation.Evaluate(signal, 100f, false, 0f, m_tuning);

            Assert.That(presentation.State, Is.EqualTo(expected));
        }

        [Test]
        public void Evaluate_ReducedFlashesSuppressesCriticalPulse()
        {
            var first = SignalHudPresentation.Evaluate(20f, 100f, true, 0f, m_tuning);
            var later = SignalHudPresentation.Evaluate(20f, 100f, true, 1f, m_tuning);

            Assert.That(first.Alpha, Is.EqualTo(1f));
            Assert.That(later.Alpha, Is.EqualTo(first.Alpha));
        }

        [Test]
        public void Evaluate_CriticalPulseRemainsRestrained()
        {
            var presentation = SignalHudPresentation.Evaluate(20f, 100f, false, 0f, m_tuning);

            Assert.That(presentation.Alpha, Is.InRange(m_tuning.CriticalMinimumAlpha, 1f));
        }
    }
}
