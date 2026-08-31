using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records the collider-free visual finish around the Relay Foundry. Existing room components retain
    /// progression, collision, gate, reinforcement, and state authority.
    /// </summary>
    public sealed class AuthoredRelayFoundryHeroFinish : MonoBehaviour
    {
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private MeshRenderer m_structureRenderer;
        [SerializeField] private MeshRenderer m_powerRenderer;

        public MeshRenderer StructureRenderer => m_structureRenderer;
        public MeshRenderer PowerRenderer => m_powerRenderer;
        public bool IsConfigured => m_structureRenderer != null && m_powerRenderer != null;

        public void Configure(MeshRenderer structureRenderer, MeshRenderer powerRenderer)
        {
            m_structureRenderer = structureRenderer;
            m_powerRenderer = powerRenderer;
        }

        public void SetPracticalLighting(Color color, float emission)
        {
            if (m_powerRenderer == null)
            {
                return;
            }
            if (m_hasAppliedPracticalLighting && m_practicalColor == color &&
                Mathf.Approximately(m_practicalEmission, emission))
            {
                return;
            }

            m_hasAppliedPracticalLighting = true;
            m_practicalColor = color;
            m_practicalEmission = emission;

            var properties = new MaterialPropertyBlock();
            m_powerRenderer.GetPropertyBlock(properties);
            properties.SetColor(s_emissionColor, color * emission);
            m_powerRenderer.SetPropertyBlock(properties);
        }

        private bool m_hasAppliedPracticalLighting;
        private Color m_practicalColor;
        private float m_practicalEmission;
    }
}
