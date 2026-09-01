using DeadSignal.Missions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only staging and focus feedback for terminal run outcomes.</summary>
    public sealed class OutcomePresentation : MonoBehaviour
    {
        [SerializeField] private Image m_backdrop;
        [SerializeField] private Image m_frame;
        [SerializeField] private Image m_accentRail;
        [SerializeField] private RectTransform m_selectionRail;
        [SerializeField] private RawImage m_insignia;
        [SerializeField] private Text m_protocol;
        [SerializeField] private Text m_causeLabel;
        [SerializeField] private Text m_evidenceLabel;
        [SerializeField] private Text m_optionsLabel;
        [SerializeField] private Text m_selectionDetail;
        [SerializeField] private Button m_restartButton;
        [SerializeField] private Button m_mainMenuButton;
        [SerializeField] private CanvasGroup m_titleGroup;
        [SerializeField] private CanvasGroup m_causeGroup;
        [SerializeField] private CanvasGroup m_evidenceGroup;
        [SerializeField] private CanvasGroup[] m_actionGroups;

        public bool IsConfigured => m_backdrop != null && m_frame != null && m_accentRail != null &&
                                    m_selectionRail != null && m_selectionDetail != null &&
                                    m_insignia != null && m_protocol != null && m_causeLabel != null &&
                                    m_evidenceLabel != null && m_optionsLabel != null &&
                                    m_restartButton != null && m_mainMenuButton != null &&
                                    m_titleGroup != null && m_causeGroup != null && m_evidenceGroup != null &&
                                    m_actionGroups != null && m_actionGroups.Length == 3;

        public bool IsDefeatPresentation => m_outcome == RunOutcome.Destroyed;
        public bool IsVictoryPresentation => m_outcome == RunOutcome.Victory;
        public float EvidenceOpacity => m_evidenceGroup == null ? 0f : m_evidenceGroup.alpha;
        public string Protocol => m_protocol == null ? string.Empty : m_protocol.text;
        public string EvidenceLabel => m_evidenceLabel == null ? string.Empty : m_evidenceLabel.text;
        public string OptionsLabel => m_optionsLabel == null ? string.Empty : m_optionsLabel.text;
        public string SelectionDetail => m_selectionDetail == null ? string.Empty : m_selectionDetail.text;

        private void OnDisable()
        {
            m_outcome = RunOutcome.Running;
            m_elapsed = 0f;
        }

        private void Update()
        {
            if (m_outcome == RunOutcome.Running || !IsConfigured)
            {
                return;
            }

            m_elapsed += Time.unscaledDeltaTime;
            _applyReveal();
            _applySelection();
        }

        public void Present(RunOutcome outcome, bool reducedFlashes)
        {
            if (!IsConfigured || outcome == RunOutcome.Running)
            {
                return;
            }

            if (m_outcome == outcome && m_reducedFlashes == reducedFlashes)
            {
                return;
            }

            m_outcome = outcome;
            m_reducedFlashes = reducedFlashes;
            m_elapsed = 0f;

            var defeat = outcome == RunOutcome.Destroyed;
            m_backdrop.color = defeat
                ? new Color(0.018f, 0.006f, 0.008f, 0.92f)
                : new Color(0.002f, 0.012f, 0.016f, 0.94f);
            m_frame.color = defeat
                ? new Color(0.055f, 0.012f, 0.016f, 0.97f)
                : new Color(0.008f, 0.035f, 0.045f, 0.995f);
            m_accentRail.color = defeat
                ? new Color(0.92f, 0.12f, 0.08f, 0.95f)
                : new Color(0.08f, 0.9f, 1f, 0.95f);
            m_insignia.color = defeat
                ? new Color(0.8f, 0.14f, 0.12f, 0.92f)
                : new Color(0.08f, 0.9f, 1f, 0.92f);
            m_protocol.text = defeat
                ? "STATION RECOVERY  /  TERMINAL STATE"
                : "STATION RECOVERY  /  EXTRACTION VERIFIED";
            m_causeLabel.text = defeat ? "FAILURE CAUSE" : "RECOVERY ROUTE COMPLETE";
            m_evidenceLabel.text = defeat
                ? "RUN EVIDENCE  /  LAST STABLE TELEMETRY"
                : "MISSION DEBRIEF  /  EXTRACTED TELEMETRY";
            m_optionsLabel.text = defeat ? "RECOVERY OPTIONS" : "NEXT DEPLOYMENT";

            _applyReveal();
            _applySelection();
        }

        private void _applyReveal()
        {
            if (m_reducedFlashes)
            {
                _setAlpha(m_titleGroup, 1f);
                _setAlpha(m_causeGroup, 1f);
                _setAlpha(m_evidenceGroup, 1f);
                foreach (var actionGroup in m_actionGroups)
                {
                    _setAlpha(actionGroup, 1f);
                }
                return;
            }

            _setAlpha(m_titleGroup, _reveal(0f, 0.2f));
            _setAlpha(m_causeGroup, _reveal(0.1f, 0.28f));
            _setAlpha(m_evidenceGroup, _reveal(0.24f, 0.34f));
            var actionOpacity = _reveal(0.42f, 0.3f);
            foreach (var actionGroup in m_actionGroups)
            {
                _setAlpha(actionGroup, actionOpacity);
            }
        }

        private void _applySelection()
        {
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            var mainMenuSelected = selected == m_mainMenuButton.gameObject;
            var target = mainMenuSelected ? (RectTransform)m_mainMenuButton.transform : (RectTransform)m_restartButton.transform;
            m_selectionRail.anchoredPosition = new Vector2(
                target.anchoredPosition.x - target.sizeDelta.x * 0.5f - 10f,
                target.anchoredPosition.y);
            m_selectionDetail.text = mainMenuSelected
                ? "RETURN TO MISSION CONTROL"
                : m_outcome == RunOutcome.Destroyed
                    ? "RESTART FROM STATION ENTRY"
                    : "BEGIN A NEW RECOVERY";
        }

        private float _reveal(float delay, float duration)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((m_elapsed - delay) / duration));
        }

        private static void _setAlpha(CanvasGroup group, float alpha)
        {
            group.alpha = alpha;
        }

        private RunOutcome m_outcome = RunOutcome.Running;
        private float m_elapsed;
        private bool m_reducedFlashes;
    }
}
