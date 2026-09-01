using UnityEngine;
using UnityEngine.UI;

namespace DeadSignal.Presentation
{
    /// <summary>Owns the authored Signal reserve instrument without changing Signal authority.</summary>
    public sealed class SignalReserveInstrument : MonoBehaviour
    {
        [SerializeField] private Image m_fill;
        [SerializeField] private Image m_changeBand;
        [SerializeField] private Image m_transactionMarker;
        [SerializeField] private Text m_reserveText;
        [SerializeField] private Text m_flowText;
        [SerializeField] private Text m_transactionText;

        private float m_displayedRatio;
        private float m_previousSignal;
        private float m_recoveryLabelRemaining;
        private bool m_initialized;

        public bool IsConfigured => m_fill != null && m_changeBand != null && m_transactionMarker != null &&
                                    m_reserveText != null && m_flowText != null && m_transactionText != null;
        public float DisplayedRatio => m_fill == null ? 0f : m_fill.fillAmount;
        public float TransactionMarkerRatio => m_transactionMarker == null
            ? 0f
            : m_transactionMarker.rectTransform.anchorMin.x;
        public string ReserveLabel => m_reserveText == null ? string.Empty : m_reserveText.text;
        public string FlowLabel => m_flowText == null ? string.Empty : m_flowText.text;
        public string TransactionLabel => m_transactionText == null ? string.Empty : m_transactionText.text;

        public void Configure(
            Image fill,
            Image changeBand,
            Image transactionMarker,
            Text reserveText,
            Text flowText,
            Text transactionText)
        {
            m_fill = fill;
            m_changeBand = changeBand;
            m_transactionMarker = transactionMarker;
            m_reserveText = reserveText;
            m_flowText = flowText;
            m_transactionText = transactionText;
        }

        public void Apply(
            float signal,
            float maximumSignal,
            SignalHudPresentation presentation,
            float drainPerSecond,
            bool powered,
            float transactionCost,
            string transactionName,
            float deltaTime,
            SignalHudTuning tuning)
        {
            if (!IsConfigured || tuning == null)
            {
                return;
            }

            var exactRatio = presentation.Ratio;
            if (!m_initialized)
            {
                m_displayedRatio = exactRatio;
                m_previousSignal = signal;
                m_initialized = true;
            }

            var signalDelta = signal - m_previousSignal;
            if (signalDelta > 0.01f)
            {
                m_recoveryLabelRemaining = tuning.RecoveryLabelSeconds;
                m_flowText.text = $"↑ RECOVERY  +{signalDelta:0.#}";
            }
            else
            {
                m_recoveryLabelRemaining = Mathf.Max(0f, m_recoveryLabelRemaining - Mathf.Max(0f, deltaTime));
                if (m_recoveryLabelRemaining <= 0f)
                {
                    m_flowText.text = powered || drainPerSecond <= 0f
                        ? "— RESERVE STABLE  //  POWERED"
                        : $"↓ DRAIN  −{drainPerSecond:0.0}/s  //  DEAD ZONE";
                }
            }

            m_previousSignal = signal;
            m_displayedRatio = Mathf.MoveTowards(
                m_displayedRatio,
                exactRatio,
                tuning.FillCatchUpPerSecond * Mathf.Max(0f, deltaTime));
            m_fill.fillAmount = exactRatio;
            var fillColor = presentation.Color;
            fillColor.a = presentation.Alpha;
            m_fill.color = fillColor;
            _updateChangeBand(exactRatio, presentation.Color);

            m_reserveText.text = $"{_stateGlyph(presentation.State)}  SIGNAL  {Mathf.CeilToInt(signal):000}  //  " +
                                 presentation.State.ToString().ToUpperInvariant();
            m_reserveText.color = presentation.Color;
            _updateTransactionPreview(signal, maximumSignal, transactionCost, transactionName);
        }

        private void _updateChangeBand(float exactRatio, Color stateColor)
        {
            var minimum = Mathf.Min(exactRatio, m_displayedRatio);
            var maximum = Mathf.Max(exactRatio, m_displayedRatio);
            var rect = m_changeBand.rectTransform;
            rect.anchorMin = new Vector2(minimum, 0f);
            rect.anchorMax = new Vector2(maximum, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            m_changeBand.color = new Color(stateColor.r, stateColor.g, stateColor.b, 0.42f);
            m_changeBand.gameObject.SetActive(maximum - minimum > 0.002f);
        }

        private void _updateTransactionPreview(
            float signal,
            float maximumSignal,
            float transactionCost,
            string transactionName)
        {
            var hasPreview = transactionCost > 0f && !string.IsNullOrWhiteSpace(transactionName);
            m_transactionMarker.gameObject.SetActive(hasPreview);
            m_transactionText.gameObject.SetActive(hasPreview);
            if (!hasPreview)
            {
                return;
            }

            var resultingRatio = maximumSignal > 0f
                ? Mathf.Clamp01((signal - transactionCost) / maximumSignal)
                : 0f;
            var marker = m_transactionMarker.rectTransform;
            marker.anchorMin = new Vector2(resultingRatio, 0f);
            marker.anchorMax = new Vector2(resultingRatio, 1f);
            marker.anchoredPosition = Vector2.zero;
            marker.sizeDelta = new Vector2(3f, 0f);
            m_transactionText.text = $"PREVIEW  {transactionName}  −{transactionCost:0}  →  " +
                                     $"{Mathf.CeilToInt(Mathf.Max(0f, signal - transactionCost)):000}";
        }

        private static string _stateGlyph(SignalReserveState state) => state switch
        {
            SignalReserveState.Stable => "◆",
            SignalReserveState.Strained => "▲",
            SignalReserveState.Critical => "!!",
            _ => "—"
        };
    }
}
