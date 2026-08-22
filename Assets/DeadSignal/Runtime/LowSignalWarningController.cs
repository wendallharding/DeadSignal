using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace DeadSignal
{
    internal interface ILowSignalWarning
    {
        bool HasTexture { get; }
        float CurrentIntensity { get; }

        void Configure(RunModel model);
        void Tick(float dt);
    }

    /// <summary>
    /// Presents a restrained screen-edge warning when the shared Signal reserve approaches depletion.
    /// </summary>
    public sealed class LowSignalWarningController : MonoBehaviour, ILowSignalWarning
    {
        public const float WarningThreshold = 30f;

        private const string TEXTURE_PATH = "UI/LowSignalWarningVignette";
        private const float PULSE_SPEED = 2.4f;
        private const float MINIMUM_ALPHA = 0.16f;
        private const float MAXIMUM_ALPHA = 0.42f;
        private const float REDUCED_FLASHES_ALPHA = 0.16f;

        private RunModel m_model;
        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private Texture2D m_texture;
        [SerializeField] private RawImage m_vignette;
        private float m_pulseTime;

        public bool HasTexture => m_vignette != null && m_vignette.texture != null;
        public float CurrentIntensity { get; private set; }

        [Inject]
        private void _construct(ICombatFeedback combatFeedback, IComfortSettings comfortSettings)
        {
            m_combatFeedback = combatFeedback;
            m_comfortSettings = comfortSettings;
        }

        public static float CalculateIntensity(float signal, bool reducedFlashes, float pulsePhase)
        {
            float danger = Mathf.Clamp01(1f - signal / WarningThreshold);
            danger = danger * danger * (3f - 2f * danger);
            if (reducedFlashes)
            {
                return danger * REDUCED_FLASHES_ALPHA;
            }

            return danger * Mathf.Lerp(MINIMUM_ALPHA, MAXIMUM_ALPHA, Mathf.Clamp01(pulsePhase));
        }

        public void Configure(RunModel model)
        {
            m_model = model;
            m_texture = m_vignette != null ? m_vignette.texture as Texture2D : null;
        }

        public void Tick(float dt)
        {
            if (m_model == null || m_model.Outcome != RunOutcome.Running)
            {
                CurrentIntensity = 0f;
                _refreshPresentation();
                return;
            }

            m_pulseTime += Mathf.Max(0f, dt);
            float pulsePhase = Mathf.Sin(m_pulseTime * PULSE_SPEED) * 0.5f + 0.5f;
            CurrentIntensity = CalculateIntensity(m_model.Signal, m_comfortSettings.ReducedFlashesEnabled, pulsePhase);
            _refreshPresentation();
        }

        private void _refreshPresentation()
        {
            if (m_vignette == null)
            {
                return;
            }

            bool visible = m_texture != null && CurrentIntensity > 0f && !m_combatFeedback.IsPaused &&
                           m_model.Outcome == RunOutcome.Running;
            m_vignette.gameObject.SetActive(visible);
            m_vignette.color = new Color(1f, 1f, 1f, CurrentIntensity);
        }
    }
}
