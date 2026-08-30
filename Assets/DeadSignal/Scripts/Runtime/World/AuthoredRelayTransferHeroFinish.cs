using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Owns the collider-free shared finish and restrained lifecycle tint for the Relay Fork and Transfer Vault.
    /// The objectives and route gate remain the sole gameplay authorities.
    /// </summary>
    public sealed class AuthoredRelayTransferHeroFinish : MonoBehaviour
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private MeshRenderer m_finishRenderer;
        [SerializeField] private AuthoredRelayForkObjective m_relayObjective;
        [SerializeField] private AuthoredTransferVaultObjective m_transferObjective;

        public MeshRenderer FinishRenderer => m_finishRenderer;
        public bool IsRelayFinish => m_relayObjective != null;
        public RelayForkPresentationState RelayState { get; private set; } = RelayForkPresentationState.Locked;
        public TransferVaultPresentationState TransferState { get; private set; } = TransferVaultPresentationState.Locked;

        private void Awake()
        {
            _refreshState(true);
        }

        public void ConfigureRelay(MeshRenderer finishRenderer, AuthoredRelayForkObjective objective)
        {
            m_finishRenderer = finishRenderer;
            m_relayObjective = objective;
            m_transferObjective = null;
            _refreshState(true);
        }

        public void ApplyRelayState(RelayForkPresentationState state)
        {
            RelayState = state;
            _applyColor(state switch
            {
                RelayForkPresentationState.Locked => new Color(0.42f, 0.31f, 0.24f),
                RelayForkPresentationState.Available => new Color(0.9f, 0.55f, 0.2f),
                RelayForkPresentationState.Routing => new Color(1f, 0.76f, 0.28f),
                _ => new Color(0.48f, 0.86f, 0.9f)
            });
        }

        public void ApplyTransferState(TransferVaultPresentationState state)
        {
            TransferState = state;
            _applyColor(state switch
            {
                TransferVaultPresentationState.Locked => new Color(0.46f, 0.39f, 0.32f),
                TransferVaultPresentationState.Available => new Color(0.88f, 0.62f, 0.27f),
                TransferVaultPresentationState.Processing => new Color(0.98f, 0.8f, 0.36f),
                _ => new Color(0.7f, 0.9f, 0.91f)
            });
        }

        public void ConfigureTransfer(MeshRenderer finishRenderer, AuthoredTransferVaultObjective objective)
        {
            m_finishRenderer = finishRenderer;
            m_relayObjective = null;
            m_transferObjective = objective;
            _refreshState(true);
        }

        private void _refreshState(bool force)
        {
            if (m_relayObjective != null)
            {
                var state = m_relayObjective.PresentationState;
                if (force || state != RelayState)
                {
                    ApplyRelayState(state);
                }

                return;
            }

            if (m_transferObjective == null)
            {
                return;
            }

            var transferState = m_transferObjective.PresentationState;
            if (!force && transferState == TransferState)
            {
                return;
            }

            ApplyTransferState(transferState);
        }

        private void _applyColor(Color color)
        {
            if (m_finishRenderer == null || m_finishRenderer.sharedMaterials.Length < 3)
            {
                return;
            }

            m_properties ??= new MaterialPropertyBlock();
            m_finishRenderer.GetPropertyBlock(m_properties, 2);
            m_properties.SetColor(s_baseColor, color);
            m_finishRenderer.SetPropertyBlock(m_properties, 2);
        }

        private MaterialPropertyBlock m_properties;
    }
}
