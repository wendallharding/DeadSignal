using UnityEngine;

namespace DeadSignal.World
{
    public enum RouteDoorPresentationState
    {
        Locked,
        Open
    }

    /// <summary>
    /// Keeps a doorway's authored threshold readable after its gameplay blocker retracts.
    /// </summary>
    public sealed class AuthoredRouteDoorReadability : MonoBehaviour
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private GameObject m_blockingSlab;
        [SerializeField] private GameObject m_openMarker;
        [SerializeField] private Renderer m_thresholdRenderer;
        [SerializeField] private AuthoredStatefulDoorFrame m_frameKit;

        public bool IsConfigured => m_blockingSlab != null && m_openMarker != null && m_thresholdRenderer != null;
        public AuthoredStatefulDoorFrame FrameKit => m_frameKit;
        public RouteDoorPresentationState PresentationState { get; private set; } = RouteDoorPresentationState.Locked;

        private void Awake()
        {
            SetOpen(false);
        }

        public void Configure(GameObject blockingSlab, GameObject openMarker, Renderer thresholdRenderer)
        {
            m_blockingSlab = blockingSlab;
            m_openMarker = openMarker;
            m_thresholdRenderer = thresholdRenderer;
            m_hasAppliedPresentation = false;
            SetOpen(false);
        }

        public void ConfigureFrameKit(AuthoredStatefulDoorFrame frameKit)
        {
            m_frameKit = frameKit;
            m_frameKit?.SetOpen(PresentationState == RouteDoorPresentationState.Open);
        }

        public void SetOpen(bool open)
        {
            if (m_blockingSlab != null)
            {
                m_blockingSlab.SetActive(!open);
            }

            if (m_openMarker != null)
            {
                m_openMarker.SetActive(open);
            }

            var state = open ? RouteDoorPresentationState.Open : RouteDoorPresentationState.Locked;
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            m_frameKit?.SetOpen(open);
            if (m_thresholdRenderer == null)
            {
                return;
            }

            var color = open ? new Color(0.015f, 0.82f, 1f) : new Color(0.72f, 0.08f, 0.045f);
            var properties = new MaterialPropertyBlock();
            m_thresholdRenderer.GetPropertyBlock(properties);
            properties.SetColor(s_baseColor, color);
            properties.SetColor(s_emissionColor, color * (open ? 1.15f : 0.52f));
            m_thresholdRenderer.SetPropertyBlock(properties);
        }

        private bool m_hasAppliedPresentation;
    }
}
