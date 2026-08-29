using UnityEngine;

namespace DeadSignal.World
{
    public enum CentralTowerPresentationState
    {
        Dormant,
        ActivationAvailable,
        Activating,
        Powered,
        PayloadInstallAvailable,
        PayloadInstalled
    }

    /// <summary>
    /// Owns persistent presentation for the Central Tower while mission rules remain in
    /// <see cref="DeadSignal.Missions.RunModel"/>.
    /// </summary>
    public sealed class AuthoredCentralTowerReadability : MonoBehaviour
    {
        private const float ACTIVATION_PRESENTATION_SECONDS = 0.8f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer m_statusRenderer;
        [SerializeField] private Renderer[] m_payloadSocketRenderers;

        public bool IsConfigured => m_statusRenderer != null && m_payloadSocketRenderers is { Length: > 0 };
        public CentralTowerPresentationState State { get; private set; } = CentralTowerPresentationState.Dormant;

        private void Awake()
        {
            _applyState(CentralTowerPresentationState.Dormant);
        }

        private void Update()
        {
            if (m_activationRemaining <= 0f)
            {
                return;
            }

            m_activationRemaining = Mathf.Max(0f, m_activationRemaining - Time.unscaledDeltaTime);
            _refreshState();
        }

        public void Configure(Renderer statusRenderer, Renderer[] payloadSocketRenderers)
        {
            m_statusRenderer = statusRenderer;
            m_payloadSocketRenderers = payloadSocketRenderers;
            m_lastPowered = false;
            m_activationRemaining = 0f;
            m_hasAppliedState = false;
            _applyState(CentralTowerPresentationState.Dormant);
        }

        public void SetState(bool activationAvailable, bool powered, bool payloadInstallAvailable, bool payloadInstalled)
        {
            if (powered && !m_lastPowered)
            {
                m_activationRemaining = ACTIVATION_PRESENTATION_SECONDS;
            }

            m_lastPowered = powered;
            m_activationAvailable = activationAvailable;
            m_powered = powered;
            m_payloadInstallAvailable = payloadInstallAvailable;
            m_payloadInstalled = payloadInstalled;
            _refreshState();
        }

        private void _refreshState()
        {
            var state = m_payloadInstalled
                ? CentralTowerPresentationState.PayloadInstalled
                : m_payloadInstallAvailable
                    ? CentralTowerPresentationState.PayloadInstallAvailable
                    : m_activationRemaining > 0f
                        ? CentralTowerPresentationState.Activating
                        : m_powered
                            ? CentralTowerPresentationState.Powered
                            : m_activationAvailable
                                ? CentralTowerPresentationState.ActivationAvailable
                                : CentralTowerPresentationState.Dormant;
            _applyState(state);
        }

        private void _applyState(CentralTowerPresentationState state)
        {
            if (m_hasAppliedState && State == state)
            {
                return;
            }

            m_hasAppliedState = true;
            State = state;
            var statusColor = state switch
            {
                CentralTowerPresentationState.Dormant => new Color(0.14f, 0.18f, 0.2f),
                CentralTowerPresentationState.ActivationAvailable => new Color(1f, 0.48f, 0.06f),
                CentralTowerPresentationState.Activating => new Color(0.95f, 0.72f, 0.18f),
                CentralTowerPresentationState.Powered => new Color(0.02f, 0.82f, 0.94f),
                CentralTowerPresentationState.PayloadInstallAvailable => new Color(1f, 0.58f, 0.08f),
                _ => new Color(0.1f, 0.95f, 1f)
            };
            var socketColor = state == CentralTowerPresentationState.PayloadInstallAvailable
                ? new Color(1f, 0.48f, 0.06f)
                : state == CentralTowerPresentationState.PayloadInstalled
                    ? new Color(0.02f, 0.92f, 1f)
                    : new Color(0.12f, 0.16f, 0.18f);
            _setColor(m_statusRenderer, statusColor, state == CentralTowerPresentationState.Dormant ? 0.1f : 1.35f);
            foreach (var renderer in m_payloadSocketRenderers)
            {
                _setColor(renderer, socketColor, state is CentralTowerPresentationState.PayloadInstallAvailable or
                    CentralTowerPresentationState.PayloadInstalled ? 1.15f : 0.06f);
            }
        }

        private static void _setColor(Renderer renderer, Color color, float emissionMultiplier)
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

        private bool m_activationAvailable;
        private bool m_powered;
        private bool m_payloadInstallAvailable;
        private bool m_payloadInstalled;
        private bool m_lastPowered;
        private bool m_hasAppliedState;
        private float m_activationRemaining;
    }
}
