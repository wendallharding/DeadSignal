using UnityEngine;
using UnityEngine.UI;

namespace DeadSignal.Presentation
{
    public enum InteractionPromptState
    {
        Available,
        Blocked,
        Progress,
        Choice
    }

    public readonly struct InteractionPromptPresentation
    {
        public InteractionPromptPresentation(
            bool isVisible,
            InteractionPromptState state,
            string primaryGlyph,
            string primaryAction,
            string detail,
            string secondaryGlyph = "",
            string secondaryAction = "")
        {
            IsVisible = isVisible;
            State = state;
            PrimaryGlyph = primaryGlyph ?? string.Empty;
            PrimaryAction = primaryAction ?? string.Empty;
            Detail = detail ?? string.Empty;
            SecondaryGlyph = secondaryGlyph ?? string.Empty;
            SecondaryAction = secondaryAction ?? string.Empty;
        }

        public static InteractionPromptPresentation Hidden =>
            new InteractionPromptPresentation(false, InteractionPromptState.Available, "", "", "");

        public bool IsVisible { get; }
        public InteractionPromptState State { get; }
        public string PrimaryGlyph { get; }
        public string PrimaryAction { get; }
        public string Detail { get; }
        public string SecondaryGlyph { get; }
        public string SecondaryAction { get; }
    }

    /// <summary>Owns the presentation-only hierarchy and transition for nearby interaction guidance.</summary>
    public sealed class InteractionPromptHud : MonoBehaviour
    {
        private const float TRANSITION_SECONDS = 0.12f;

        [SerializeField] private Image m_background;
        [SerializeField] private Image m_accent;
        [SerializeField] private Text m_stateText;
        [SerializeField] private GameObject m_primaryGlyph;
        [SerializeField] private Text m_primaryGlyphText;
        [SerializeField] private Text m_primaryActionText;
        [SerializeField] private Text m_detailText;
        [SerializeField] private GameObject m_secondaryAction;
        [SerializeField] private Text m_secondaryGlyphText;
        [SerializeField] private Text m_secondaryActionText;
        [SerializeField] private CanvasGroup m_canvasGroup;

        private string m_signature = string.Empty;
        private float m_transitionElapsed;

        public bool IsConfigured =>
            m_background != null && m_accent != null && m_stateText != null && m_primaryGlyph != null && m_primaryGlyphText != null &&
            m_primaryActionText != null && m_detailText != null && m_secondaryAction != null &&
            m_secondaryGlyphText != null && m_secondaryActionText != null && m_canvasGroup != null;
        public InteractionPromptState CurrentState { get; private set; }
        public string StateLabel => m_stateText == null ? string.Empty : m_stateText.text;
        public string PrimaryGlyph => m_primaryGlyphText == null ? string.Empty : m_primaryGlyphText.text;
        public string PrimaryAction => m_primaryActionText == null ? string.Empty : m_primaryActionText.text;
        public string Detail => m_detailText == null ? string.Empty : m_detailText.text;
        public string SecondaryGlyph => m_secondaryGlyphText == null ? string.Empty : m_secondaryGlyphText.text;
        public string SecondaryActionLabel => m_secondaryActionText == null ? string.Empty : m_secondaryActionText.text;
        public bool HasSecondaryAction => m_secondaryAction != null && m_secondaryAction.activeSelf;
        public float Opacity => m_canvasGroup == null ? 0f : m_canvasGroup.alpha;

        public void Configure(
            Image background,
            Image accent,
            Text stateText,
            GameObject primaryGlyph,
            Text primaryGlyphText,
            Text primaryActionText,
            Text detailText,
            GameObject secondaryAction,
            Text secondaryGlyphText,
            Text secondaryActionText,
            CanvasGroup canvasGroup)
        {
            m_background = background;
            m_accent = accent;
            m_stateText = stateText;
            m_primaryGlyph = primaryGlyph;
            m_primaryGlyphText = primaryGlyphText;
            m_primaryActionText = primaryActionText;
            m_detailText = detailText;
            m_secondaryAction = secondaryAction;
            m_secondaryGlyphText = secondaryGlyphText;
            m_secondaryActionText = secondaryActionText;
            m_canvasGroup = canvasGroup;
        }

        public void Apply(InteractionPromptPresentation presentation, float unscaledDeltaTime)
        {
            if (!presentation.IsVisible)
            {
                _hide(unscaledDeltaTime);
                return;
            }

            var signature = _signature(presentation);
            if (!gameObject.activeSelf || signature != m_signature)
            {
                gameObject.SetActive(true);
                m_signature = signature;
                m_transitionElapsed = 0f;
                m_canvasGroup.alpha = 0f;
                transform.localScale = Vector3.one * 0.96f;
            }

            CurrentState = presentation.State;
            m_stateText.text = _stateLabel(presentation.State);
            m_primaryGlyph.SetActive(!string.IsNullOrEmpty(presentation.PrimaryGlyph));
            m_primaryGlyphText.text = presentation.PrimaryGlyph;
            m_primaryActionText.text = presentation.PrimaryAction;
            m_detailText.text = presentation.Detail;
            var hasSecondary = !string.IsNullOrEmpty(presentation.SecondaryAction);
            m_secondaryAction.SetActive(hasSecondary);
            m_secondaryGlyphText.text = presentation.SecondaryGlyph;
            m_secondaryActionText.text = presentation.SecondaryAction;
            _applyStateColors(presentation.State);

            m_transitionElapsed += Mathf.Max(0f, unscaledDeltaTime);
            var progress = Mathf.Clamp01(m_transitionElapsed / TRANSITION_SECONDS);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            m_canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
            transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
        }

        private void _hide(float unscaledDeltaTime)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            m_canvasGroup.alpha = Mathf.MoveTowards(
                m_canvasGroup.alpha,
                0f,
                Mathf.Max(0f, unscaledDeltaTime) / TRANSITION_SECONDS);
            transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, m_canvasGroup.alpha);
            if (m_canvasGroup.alpha <= 0.001f)
            {
                gameObject.SetActive(false);
                m_signature = string.Empty;
            }
        }

        private void _applyStateColors(InteractionPromptState state)
        {
            var accent = state switch
            {
                InteractionPromptState.Available => new Color(0.08f, 0.94f, 1f, 1f),
                InteractionPromptState.Progress => new Color(0.22f, 0.86f, 1f, 1f),
                InteractionPromptState.Choice => new Color(1f, 0.64f, 0.14f, 1f),
                _ => new Color(1f, 0.42f, 0.14f, 1f)
            };
            m_background.color = new Color(0.012f, 0.022f, 0.032f, 0.96f);
            m_accent.color = accent;
            m_stateText.color = accent;
            m_primaryGlyphText.color = accent;
            m_secondaryGlyphText.color = accent;
        }

        private static string _stateLabel(InteractionPromptState state) => state switch
        {
            InteractionPromptState.Available => "ACTION READY",
            InteractionPromptState.Progress => "PROCESS ACTIVE",
            InteractionPromptState.Choice => "SELECT ROUTE",
            _ => "SYSTEM LOCK"
        };

        // Detail can contain a live countdown, so it must not restart the prompt entrance transition.
        private static string _signature(InteractionPromptPresentation presentation) =>
            $"{presentation.State}|{presentation.PrimaryGlyph}|{presentation.PrimaryAction}|" +
            $"{presentation.SecondaryGlyph}|{presentation.SecondaryAction}";
    }
}
