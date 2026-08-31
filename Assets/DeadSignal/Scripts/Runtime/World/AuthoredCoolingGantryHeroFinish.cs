using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records the collider-free visual finish for the Cooling Gantry. Existing authored components retain
    /// processing, traversal, collision, reinforcement, powered-territory, and lifecycle authority.
    /// </summary>
    public sealed class AuthoredCoolingGantryHeroFinish : MonoBehaviour
    {
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private MeshRenderer m_finishRenderer;

        public MeshRenderer FinishRenderer => m_finishRenderer;
        public bool IsConfigured => m_finishRenderer != null;

        public void Configure(MeshRenderer finishRenderer)
        {
            m_finishRenderer = finishRenderer;
        }

        public void SetPracticalLighting(Color color, float emission)
        {
            if (m_finishRenderer == null)
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
            m_finishRenderer.GetPropertyBlock(properties);
            properties.SetColor(s_emissionColor, color * emission);
            m_finishRenderer.SetPropertyBlock(properties);
        }

        private bool m_hasAppliedPracticalLighting;
        private Color m_practicalColor;
        private float m_practicalEmission;
    }
}
