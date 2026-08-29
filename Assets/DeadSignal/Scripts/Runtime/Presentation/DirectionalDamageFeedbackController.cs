using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.Player;

namespace DeadSignal.Presentation
{
    public enum PlayerDamageFeedbackKind
    {
        Security,
        Sapper
    }

    internal interface IDirectionalDamageFeedback
    {
        void Configure(RunModel model, Camera targetCamera);
        void Play(Vector3 sourcePosition, Vector3 playerPosition, PlayerDamageFeedbackKind kind);
        void Tick(float dt);
    }

    /// <summary>Owns a short-lived screen-edge chevron that points toward resolved player damage.</summary>
    public sealed class DirectionalDamageFeedbackController : MonoBehaviour, IDirectionalDamageFeedback
    {
        [SerializeField] private RectTransform m_indicator;
        [SerializeField] private Image[] m_segments;

        public bool HasAuthoredIndicator => m_indicator != null && m_segments != null && m_segments.Length == 2;
        public bool IsVisible => m_indicator != null && m_indicator.gameObject.activeSelf;
        public float CurrentAlpha { get; private set; }
        public Vector2 CurrentDirection { get; private set; }

        [Inject]
        private void _construct(ICombatFeedback combatFeedback, IComfortSettings comfortSettings)
        {
            m_combatFeedback = combatFeedback;
            m_comfortSettings = comfortSettings;
        }

        public static Vector2 CalculateScreenDirection(Vector3 sourceViewport, Vector3 playerViewport)
        {
            var direction = new Vector2(sourceViewport.x - playerViewport.x, sourceViewport.y - playerViewport.y);
            if (sourceViewport.z < 0f)
            {
                direction = -direction;
            }

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
        }

        public void Configure(RunModel model, Camera targetCamera)
        {
            m_model = model;
            m_targetCamera = targetCamera;
            m_tuning = Resources.Load<ScreenFeedbackTuning>(TUNING_PATH);
            _clear();
        }

        public void Play(Vector3 sourcePosition, Vector3 playerPosition, PlayerDamageFeedbackKind kind)
        {
            if (!HasAuthoredIndicator || m_targetCamera == null || m_tuning == null ||
                m_model == null || m_model.Outcome != RunOutcome.Running)
            {
                return;
            }

            CurrentDirection = CalculateScreenDirection(
                m_targetCamera.WorldToViewportPoint(sourcePosition),
                m_targetCamera.WorldToViewportPoint(playerPosition));
            m_indicator.anchorMin = new Vector2(
                0.5f + CurrentDirection.x * m_tuning.HorizontalAnchorRadius,
                0.5f + CurrentDirection.y * m_tuning.VerticalAnchorRadius);
            m_indicator.anchorMax = m_indicator.anchorMin;
            m_indicator.anchoredPosition = Vector2.zero;
            var angle = Mathf.Atan2(CurrentDirection.y, CurrentDirection.x) * Mathf.Rad2Deg - 90f;
            m_indicator.localRotation = Quaternion.Euler(0f, 0f, angle);
            m_color = kind == PlayerDamageFeedbackKind.Sapper
                ? m_tuning.SapperDamageColor
                : m_tuning.SecurityDamageColor;
            m_remaining = m_tuning.DirectionalDuration;
            m_indicator.gameObject.SetActive(true);
            _refreshAlpha(1f);
        }

        public void Tick(float dt)
        {
            if (m_remaining <= 0f || m_model == null || m_model.Outcome != RunOutcome.Running ||
                m_combatFeedback == null || m_combatFeedback.IsPaused)
            {
                _clear();
                return;
            }

            m_remaining = Mathf.Max(0f, m_remaining - Mathf.Max(0f, dt));
            var normalizedRemaining = m_tuning == null ? 0f : m_remaining / m_tuning.DirectionalDuration;
            _refreshAlpha(normalizedRemaining * normalizedRemaining);
            if (m_remaining <= 0f)
            {
                _clear();
            }
        }

        private void OnDisable()
        {
            _clear();
        }

        private void _refreshAlpha(float normalizedAlpha)
        {
            var maximumAlpha = m_comfortSettings?.ReducedFlashesEnabled ?? false
                ? m_tuning.ReducedFlashesDirectionalAlpha
                : m_tuning.DirectionalMaximumAlpha;
            CurrentAlpha = Mathf.Clamp01(normalizedAlpha) * maximumAlpha;
            var tint = new Color(m_color.r, m_color.g, m_color.b, CurrentAlpha);
            foreach (var segment in m_segments)
            {
                if (segment != null)
                {
                    segment.color = tint;
                }
            }
        }

        private void _clear()
        {
            m_remaining = 0f;
            CurrentAlpha = 0f;
            if (m_indicator != null)
            {
                m_indicator.gameObject.SetActive(false);
            }
        }

        private const string TUNING_PATH = "Tuning/ScreenFeedbackTuning";

        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private RunModel m_model;
        private Camera m_targetCamera;
        private ScreenFeedbackTuning m_tuning;
        private float m_remaining;
        private Color m_color;
    }
}
