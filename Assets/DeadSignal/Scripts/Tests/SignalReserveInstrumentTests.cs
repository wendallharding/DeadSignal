using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DeadSignal.Tests
{
    public sealed class SignalReserveInstrumentTests
    {
        private GameObject m_root;
        private SignalReserveInstrument m_instrument;
        private SignalHudTuning m_tuning;
        private Image m_fill;
        private Image m_changeBand;
        private Image m_marker;
        private Text m_reserve;
        private Text m_flow;
        private Text m_transaction;

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("Signal Instrument", typeof(RectTransform), typeof(SignalReserveInstrument));
            m_instrument = m_root.GetComponent<SignalReserveInstrument>();
            m_fill = _createImage("Fill");
            m_changeBand = _createImage("Change Band");
            m_marker = _createImage("Marker");
            m_reserve = _createText("Reserve");
            m_flow = _createText("Flow");
            m_transaction = _createText("Transaction");
            m_instrument.Configure(m_fill, m_changeBand, m_marker, m_reserve, m_flow, m_transaction);
            m_tuning = ScriptableObject.CreateInstance<SignalHudTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_root);
            Object.DestroyImmediate(m_tuning);
        }

        [Test]
        public void Apply_CombinesExactReserveDrainStateAndTransactionPreview()
        {
            m_instrument.Apply(80f, 100f, _evaluate(80f), 0f, true, 0f, string.Empty, 0f, m_tuning);
            m_instrument.Apply(52f, 100f, _evaluate(52f), 1.4f, false, 16f, "SHORTCUT", 0.1f, m_tuning);

            Assert.That(m_instrument.DisplayedRatio, Is.EqualTo(0.52f).Within(0.001f));
            Assert.That(m_instrument.ReserveLabel, Is.EqualTo("▲  SIGNAL  052  //  STRAINED"));
            Assert.That(m_instrument.FlowLabel, Is.EqualTo("↓ DRAIN  −1.4/s  //  DEAD ZONE"));
            Assert.That(m_instrument.TransactionMarkerRatio, Is.EqualTo(0.36f).Within(0.001f));
            Assert.That(m_instrument.TransactionLabel, Is.EqualTo("PREVIEW  SHORTCUT  −16  →  036"));
            Assert.That(m_changeBand.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Apply_RecoveryUsesDirectionAndAmountInsteadOfColorAlone()
        {
            m_instrument.Apply(40f, 100f, _evaluate(40f), 1f, false, 0f, string.Empty, 0f, m_tuning);
            m_instrument.Apply(55.5f, 100f, _evaluate(55.5f), 0f, true, 0f, string.Empty, 0.1f, m_tuning);

            Assert.That(m_instrument.FlowLabel, Is.EqualTo("↑ RECOVERY  +15.5"));
            Assert.That(m_marker.gameObject.activeSelf, Is.False);
            Assert.That(m_transaction.gameObject.activeSelf, Is.False);
        }

        private SignalHudPresentation _evaluate(float signal) =>
            SignalHudPresentation.Evaluate(signal, 100f, false, 0f, m_tuning);

        private Image _createImage(string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(m_root.transform, false);
            return child.GetComponent<Image>();
        }

        private Text _createText(string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(m_root.transform, false);
            return child.GetComponent<Text>();
        }
    }
}
