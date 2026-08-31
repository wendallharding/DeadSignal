using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records a collider-free visual finish for a Spine-region room. Existing authored objectives retain
    /// progression, collision, projectile, route, powered-territory, and lifecycle presentation authority.
    /// </summary>
    public sealed class AuthoredSpineHeroFinish : MonoBehaviour
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
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
            m_finishRenderer.GetPropertyBlock(properties, 2);
            properties.SetColor(s_baseColor, Color.Lerp(new Color(0.2f, 0.16f, 0.12f), color, 0.42f));
            properties.SetColor(s_emissionColor, color * emission);
            m_finishRenderer.SetPropertyBlock(properties, 2);
        }

        private bool m_hasAppliedPracticalLighting;
        private Color m_practicalColor;
        private float m_practicalEmission;
    }
}
