using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.Player;

namespace DeadSignal.Presentation
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
        [SerializeField] private RawImage m_vignette;

        public const float WarningThreshold = 30f;

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
            const float criticalThreshold = 25f;
            float danger = Mathf.Clamp01(1f - signal / WarningThreshold);
            float critical = Mathf.Clamp01(1f - signal / criticalThreshold);
            danger = danger * danger * (3f - 2f * danger);
            if (reducedFlashes)
            {
                return danger * 0.1f;
            }

            var maximumAlpha = Mathf.Lerp(0.14f, 0.2f, critical);
            return danger * Mathf.Lerp(0.07f, maximumAlpha, Mathf.Clamp01(pulsePhase));
        }

        public void Configure(RunModel model)
        {
            m_model = model;
            m_texture = m_vignette != null ? m_vignette.texture as Texture2D : null;
            m_tuning = Resources.Load<ScreenFeedbackTuning>(TUNING_PATH);
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
            float pulseSpeed = m_tuning == null ? 2.4f : m_tuning.SignalPulseSpeed;
            float pulsePhase = Mathf.Sin(m_pulseTime * pulseSpeed) * 0.5f + 0.5f;
            CurrentIntensity = m_tuning == null
                ? CalculateIntensity(m_model.Signal, m_comfortSettings.ReducedFlashesEnabled, pulsePhase)
                : _calculateIntensity(m_model.Signal, m_comfortSettings.ReducedFlashesEnabled, pulsePhase, m_tuning);
            if (m_model.IsCriticalRecovery)
            {
                CurrentIntensity = Mathf.Min(CurrentIntensity, 0.1f);
            }
            _refreshPresentation();
        }

        private static float _calculateIntensity(float signal, bool reducedFlashes, float pulsePhase,
            ScreenFeedbackTuning tuning)
        {
            var signalRatio = Mathf.Clamp01(signal / RunModel.MaximumSignal);
            var danger = Mathf.Clamp01(1f - signalRatio / tuning.WarningThreshold);
            var critical = Mathf.Clamp01(1f - signalRatio / tuning.CriticalThreshold);
            danger = danger * danger * (3f - 2f * danger);
            if (reducedFlashes)
            {
                return danger * tuning.ReducedFlashesSignalAlpha;
            }

            var maximumAlpha = Mathf.Lerp(tuning.WarningMaximumAlpha, tuning.CriticalMaximumAlpha, critical);
            return danger * Mathf.Lerp(tuning.WarningMinimumAlpha, maximumAlpha, Mathf.Clamp01(pulsePhase));
        }

        private void _refreshPresentation()
        {
            if (m_vignette == null)
            {
                return;
            }

            bool visible = m_model != null && m_texture != null && CurrentIntensity > 0f &&
                           !m_combatFeedback.IsPaused && m_model.Outcome == RunOutcome.Running;
            m_vignette.gameObject.SetActive(visible);
            m_vignette.color = new Color(1f, 1f, 1f, CurrentIntensity);
        }

        private const string TEXTURE_PATH = "UI/LowSignalWarningVignette";
        private const string TUNING_PATH = "Tuning/ScreenFeedbackTuning";

        private RunModel m_model;
        private ICombatFeedback m_combatFeedback;
        private IComfortSettings m_comfortSettings;
        private Texture2D m_texture;
        private ScreenFeedbackTuning m_tuning;
        private float m_pulseTime;
    }
}
