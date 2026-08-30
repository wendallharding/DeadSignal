using DeadSignal.Missions;
using UnityEngine;

namespace DeadSignal.World
{
    public enum ExtractionDockPresentationState
    {
        Dormant,
        Locked,
        Available,
        ActiveProgress,
        Complete,
        Defeat,
        Victory
    }

    /// <summary>
    /// Presents the physical extraction uplink without owning mission, countdown, outcome, or accessibility authority.
    /// </summary>
    public sealed class AuthoredExtractionDockReadability : MonoBehaviour
    {
        private const float COMPLETE_HOLD_SECONDS = 0.75f;
        private const float ACTIVE_ROTATION_DEGREES = 300f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_statusGlyph;
        [SerializeField] private Renderer m_statusRenderer;
        [SerializeField] private Renderer m_beaconRenderer;

        public bool IsConfigured => m_statusGlyph != null && m_statusRenderer != null && m_beaconRenderer != null;
        public bool HasStatusTexture => m_statusRenderer?.sharedMaterial?.mainTexture != null;
        public float ProgressNormalized { get; private set; }
        public ExtractionDockPresentationState PresentationState { get; private set; } =
            ExtractionDockPresentationState.Dormant;

        private void Awake()
        {
            _captureAuthoredTransform();
            ResetPresentation();
        }

        private void Update()
        {
            if (m_completeHoldRemaining <= 0f)
            {
                return;
            }

            m_completeHoldRemaining = Mathf.Max(0f, m_completeHoldRemaining - Time.unscaledDeltaTime);
            if (m_completeHoldRemaining <= 0f && m_pendingOutcome == RunOutcome.Victory)
            {
                _applyState(ExtractionDockPresentationState.Victory);
            }
        }

        public void Configure(Transform statusGlyph, Renderer statusRenderer, Renderer beaconRenderer)
        {
            m_statusGlyph = statusGlyph;
            m_statusRenderer = statusRenderer;
            m_beaconRenderer = beaconRenderer;
            _captureAuthoredTransform();
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            ProgressNormalized = 0f;
            m_mode = ExtractionUplinkMode.None;
            m_pendingOutcome = RunOutcome.Running;
            m_completeHoldRemaining = 0f;
            m_hasAppliedState = false;
            _applyState(ExtractionDockPresentationState.Dormant);
        }

        public void SetState(
            bool stationOnline,
            bool available,
            bool active,
            float progressNormalized,
            bool complete,
            ExtractionUplinkMode mode,
            RunOutcome outcome)
        {
            ProgressNormalized = Mathf.Clamp01(progressNormalized);
            m_mode = mode;

            if (outcome != RunOutcome.Running)
            {
                SetOutcome(outcome, complete);
                return;
            }

            m_pendingOutcome = RunOutcome.Running;
            m_completeHoldRemaining = 0f;
            var state = complete
                ? ExtractionDockPresentationState.Complete
                : active
                    ? ExtractionDockPresentationState.ActiveProgress
                    : available
                        ? ExtractionDockPresentationState.Available
                        : stationOnline
                            ? ExtractionDockPresentationState.Locked
                            : ExtractionDockPresentationState.Dormant;
            _applyState(state);
        }

        public void SetOutcome(RunOutcome outcome, bool extractionComplete = false)
        {
            if (outcome == RunOutcome.Running)
            {
                return;
            }

            if (outcome == RunOutcome.Victory && extractionComplete)
            {
                m_pendingOutcome = RunOutcome.Victory;
                if (PresentationState != ExtractionDockPresentationState.Complete &&
                    PresentationState != ExtractionDockPresentationState.Victory)
                {
                    m_completeHoldRemaining = COMPLETE_HOLD_SECONDS;
                    _applyState(ExtractionDockPresentationState.Complete);
                }
                return;
            }

            m_pendingOutcome = outcome;
            m_completeHoldRemaining = 0f;
            _applyState(outcome == RunOutcome.Victory
                ? ExtractionDockPresentationState.Victory
                : ExtractionDockPresentationState.Defeat);
        }

        private void _captureAuthoredTransform()
        {
            if (m_statusGlyph == null)
            {
                return;
            }

            m_statusBaseRotation = m_statusGlyph.localRotation;
            m_statusBaseScale = m_statusGlyph.localScale;
        }

        private void _applyState(ExtractionDockPresentationState state)
        {
            if (m_statusGlyph != null)
            {
                var rotation = state == ExtractionDockPresentationState.ActiveProgress
                    ? ACTIVE_ROTATION_DEGREES * ProgressNormalized
                    : state == ExtractionDockPresentationState.Complete || state == ExtractionDockPresentationState.Victory
                        ? ACTIVE_ROTATION_DEGREES
                        : 0f;
                var scale = state == ExtractionDockPresentationState.ActiveProgress
                    ? Mathf.Lerp(0.92f, 1.08f, ProgressNormalized)
                    : state == ExtractionDockPresentationState.Victory
                        ? 1.1f
                        : 1f;
                m_statusGlyph.localRotation = m_statusBaseRotation * Quaternion.Euler(0f, rotation, 0f);
                m_statusGlyph.localScale = m_statusBaseScale * scale;
            }

            if (m_hasAppliedState && PresentationState == state && state != ExtractionDockPresentationState.ActiveProgress)
            {
                return;
            }

            m_hasAppliedState = true;
            PresentationState = state;
            var color = state switch
            {
                ExtractionDockPresentationState.Dormant => new Color(0.055f, 0.14f, 0.16f),
                ExtractionDockPresentationState.Locked => new Color(0.42f, 0.025f, 0.035f),
                ExtractionDockPresentationState.Available => new Color(1f, 0.46f, 0.045f),
                ExtractionDockPresentationState.ActiveProgress => m_mode == ExtractionUplinkMode.Overdrive
                    ? new Color(0.98f, 0.12f, 0.7f)
                    : new Color(0.06f, 0.86f, 1f),
                ExtractionDockPresentationState.Complete => new Color(0.04f, 0.95f, 1f),
                ExtractionDockPresentationState.Defeat => new Color(0.82f, 0.025f, 0.22f),
                _ => new Color(0.72f, 0.96f, 1f)
            };
            var emission = state switch
            {
                ExtractionDockPresentationState.Dormant => 0.16f,
                ExtractionDockPresentationState.Locked => 0.28f,
                ExtractionDockPresentationState.Defeat => 0.7f,
                _ => 1.25f
            };
            _setRendererColor(m_statusRenderer, color, emission);
            _setRendererColor(m_beaconRenderer, color, emission * 0.72f);
        }

        private static void _setRendererColor(Renderer renderer, Color color, float emissionMultiplier)
        {
            if (renderer == null)
            {
                return;
            }

            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(s_baseColor, color);
            properties.SetColor(s_emissionColor, color * emissionMultiplier);
            renderer.SetPropertyBlock(properties);
        }

        private Quaternion m_statusBaseRotation = Quaternion.identity;
        private Vector3 m_statusBaseScale = Vector3.one;
        private ExtractionUplinkMode m_mode;
        private RunOutcome m_pendingOutcome = RunOutcome.Running;
        private float m_completeHoldRemaining;
        private bool m_hasAppliedState;
    }
}
