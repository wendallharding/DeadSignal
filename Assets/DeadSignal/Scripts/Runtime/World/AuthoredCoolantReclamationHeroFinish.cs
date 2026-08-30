using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Owns the collider-free finish and restrained state tint for Coolant Reclamation. Objective, baffle,
    /// movement, projectile, and obstacle authority remain with the existing authored room systems.
    /// </summary>
    public sealed class AuthoredCoolantReclamationHeroFinish : MonoBehaviour
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private MeshRenderer m_finishRenderer;
        [SerializeField] private MeshRenderer m_sealHardwareRenderer;
        [SerializeField] private MeshRenderer[] m_baffleRenderers;

        public MeshRenderer FinishRenderer => m_finishRenderer;
        public MeshRenderer SealHardwareRenderer => m_sealHardwareRenderer;
        public int BaffleRendererCount => m_baffleRenderers?.Length ?? 0;
        public CoolantReclamationPresentationState AppliedState { get; private set; }

        public void Configure(
            MeshRenderer finishRenderer,
            MeshRenderer sealHardwareRenderer,
            MeshRenderer[] baffleRenderers)
        {
            m_finishRenderer = finishRenderer;
            m_sealHardwareRenderer = sealHardwareRenderer;
            m_baffleRenderers = baffleRenderers;
            ApplyState(CoolantReclamationPresentationState.Locked);
        }

        public void ApplyState(CoolantReclamationPresentationState state)
        {
            AppliedState = state;
            if (m_finishRenderer == null)
            {
                return;
            }

            m_properties ??= new MaterialPropertyBlock();
            m_finishRenderer.GetPropertyBlock(m_properties);
            m_properties.SetColor(s_baseColor, state switch
            {
                CoolantReclamationPresentationState.Locked => new Color(0.48f, 0.5f, 0.52f),
                CoolantReclamationPresentationState.Stable => new Color(0.82f, 0.95f, 0.94f),
                _ => Color.white
            });
            m_finishRenderer.SetPropertyBlock(m_properties);
        }

        private MaterialPropertyBlock m_properties;
    }
}
