using UnityEngine;

namespace DeadSignal.World
{
    public enum TransferVaultPresentationState
    {
        Locked,
        Available,
        Processing,
        Assembled
    }

    /// <summary>
    /// Scene-authored interaction and persistent presentation for assembling the Central payload.
    /// </summary>
    public sealed class AuthoredTransferVaultObjective : MonoBehaviour
    {
        private const float PROCESSING_PRESENTATION_SECONDS = 0.75f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_assemblyAnchor;
        [SerializeField] private GameObject m_availableMarker;
        [SerializeField] private GameObject m_assembledMarker;
        [SerializeField] private Renderer m_assemblerStatusRenderer;
        [SerializeField] private Transform m_assemblerRotor;
        [SerializeField] private GameObject m_relayRouteGate;
        [SerializeField] private GameObject m_relayRouteOpenMarker;
        [SerializeField] private AuthoredRouteDoorReadability m_relayRouteReadability;
        [SerializeField] private AuthoredRelayTransferHeroFinish m_heroFinish;

        public Vector3 Position => m_assemblyAnchor != null ? m_assemblyAnchor.position : transform.position;
        public bool IsConfigured => m_assemblyAnchor != null && m_availableMarker != null && m_assembledMarker != null;
        public bool HasReadabilityAssets => m_assemblerStatusRenderer != null && m_assemblerRotor != null;
        public bool IsRouteConfigured => m_relayRouteGate != null && m_relayRouteOpenMarker != null &&
                                         m_relayRouteReadability != null && m_relayRouteReadability.IsConfigured;
        public bool IsRelayRouteOpen => m_relayRouteGate != null && !m_relayRouteGate.activeSelf;
        public RouteDoorPresentationState RoutePresentationState => m_relayRouteReadability != null
            ? m_relayRouteReadability.PresentationState
            : RouteDoorPresentationState.Locked;
        public TransferVaultPresentationState PresentationState { get; private set; } = TransferVaultPresentationState.Locked;

        private void Awake()
        {
            SetState(false, false);
            SetRouteOpen(false);
        }

        private void Update()
        {
            if (m_processingRemaining <= 0f)
            {
                return;
            }

            m_processingRemaining = Mathf.Max(0f, m_processingRemaining - Time.unscaledDeltaTime);
            if (m_assemblerRotor != null)
            {
                m_assemblerRotor.Rotate(Vector3.up, 105f * Time.unscaledDeltaTime, Space.Self);
            }

            _refreshPresentation();
        }

        public void Configure(Transform assemblyAnchor, GameObject availableMarker, GameObject assembledMarker)
        {
            m_assemblyAnchor = assemblyAnchor;
            m_availableMarker = availableMarker;
            m_assembledMarker = assembledMarker;
            SetState(false, false);
        }

        public void ConfigureReadability(Renderer assemblerStatusRenderer, Transform assemblerRotor)
        {
            m_assemblerStatusRenderer = assemblerStatusRenderer;
            m_assemblerRotor = assemblerRotor;
            m_processingRemaining = 0f;
            m_wasComplete = false;
            m_hasAppliedPresentation = false;
            _applyPresentation(TransferVaultPresentationState.Locked);
        }

        public void ConfigureHeroFinish(AuthoredRelayTransferHeroFinish heroFinish)
        {
            m_heroFinish = heroFinish;
            m_heroFinish?.ApplyTransferState(PresentationState);
        }

        public void SetState(bool available, bool complete)
        {
            if (complete && !m_wasComplete)
            {
                m_processingRemaining = PROCESSING_PRESENTATION_SECONDS;
            }

            m_available = available;
            m_complete = complete;
            m_wasComplete = complete;
            if (m_availableMarker != null)
            {
                m_availableMarker.SetActive(available && !complete);
            }

            if (m_assembledMarker != null)
            {
                m_assembledMarker.SetActive(complete);
            }

            _refreshPresentation();
        }

        public void ConfigureRouteGate(
            GameObject relayRouteGate,
            GameObject relayRouteOpenMarker,
            AuthoredRouteDoorReadability relayRouteReadability)
        {
            m_relayRouteGate = relayRouteGate;
            m_relayRouteOpenMarker = relayRouteOpenMarker;
            m_relayRouteReadability = relayRouteReadability;
            SetRouteOpen(false);
        }

        public void SetRouteOpen(bool open)
        {
            if (m_relayRouteReadability != null)
            {
                m_relayRouteReadability.SetOpen(open);
                return;
            }

            if (m_relayRouteGate != null)
            {
                m_relayRouteGate.SetActive(!open);
            }

            if (m_relayRouteOpenMarker != null)
            {
                m_relayRouteOpenMarker.SetActive(open);
            }
        }

        private void _refreshPresentation()
        {
            _applyPresentation(m_processingRemaining > 0f
                ? TransferVaultPresentationState.Processing
                : m_complete
                    ? TransferVaultPresentationState.Assembled
                    : m_available
                        ? TransferVaultPresentationState.Available
                        : TransferVaultPresentationState.Locked);
        }

        private void _applyPresentation(TransferVaultPresentationState state)
        {
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            m_heroFinish?.ApplyTransferState(state);
            if (m_assemblerStatusRenderer == null)
            {
                return;
            }

            var color = state switch
            {
                TransferVaultPresentationState.Locked => new Color(0.2f, 0.08f, 0.07f),
                TransferVaultPresentationState.Available => new Color(1f, 0.48f, 0.06f),
                TransferVaultPresentationState.Processing => new Color(0.95f, 0.72f, 0.18f),
                _ => new Color(0.02f, 0.92f, 1f)
            };
            var properties = new MaterialPropertyBlock();
            m_assemblerStatusRenderer.GetPropertyBlock(properties, 1);
            properties.SetColor(s_baseColor, color);
            properties.SetColor(s_emissionColor, color * (state == TransferVaultPresentationState.Locked ? 0.08f : 1.25f));
            m_assemblerStatusRenderer.SetPropertyBlock(properties, 1);
        }

        private bool m_available;
        private bool m_complete;
        private bool m_wasComplete;
        private bool m_hasAppliedPresentation;
        private float m_processingRemaining;
    }
}
