using DeadSignal.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only focus and device guidance for the authored pause surface.</summary>
    public sealed class PauseMenuPresentation : MonoBehaviour
    {
        [SerializeField] private RectTransform m_pausePanel;
        [SerializeField] private Button[] m_actionButtons;
        [SerializeField] private RectTransform m_selectionRail;
        [SerializeField] private Text m_selectionDetail;
        [SerializeField] private Text m_inputHint;

        private IDeadSignalInput m_input;
        private float m_railVelocity;

        public RectTransform PausePanel => m_pausePanel;
        public RectTransform SelectionRail => m_selectionRail;
        public Text SelectionDetail => m_selectionDetail;
        public Text InputHint => m_inputHint;

        internal void Configure(IDeadSignalInput input)
        {
            m_input = input;
            Apply(true);
        }

        public void Apply(bool immediate)
        {
            if (m_pausePanel == null || !m_pausePanel.gameObject.activeInHierarchy)
            {
                return;
            }

            var selected = EventSystem.current?.currentSelectedGameObject;
            var selectedButton = System.Array.Find(m_actionButtons,
                button => button != null && button.gameObject == selected);
            if (selectedButton != null && m_selectionRail != null)
            {
                var selectedRect = selectedButton.transform as RectTransform;
                var railPosition = m_selectionRail.anchoredPosition;
                railPosition.x = immediate
                    ? selectedRect.anchoredPosition.x
                    : Mathf.SmoothDamp(railPosition.x, selectedRect.anchoredPosition.x, ref m_railVelocity, 0.08f,
                        Mathf.Infinity, Time.unscaledDeltaTime);
                m_selectionRail.anchoredPosition = railPosition;
                m_selectionDetail.text = selectedButton.name == "Main Menu"
                    ? "END THIS RUN AND RETURN TO MISSION CONTROL"
                    : "RETURN TO THE HELD STATION STATE";
            }

            if (m_inputHint != null)
            {
                m_inputHint.text = m_input?.ActivePromptDevice == InputPromptDevice.Gamepad
                    ? "D-PAD / STICK  NAVIGATE     A  SELECT     MENU  RESUME"
                    : "ARROWS  NAVIGATE     ENTER  SELECT     ESC  RESUME";
            }
        }
    }
}
