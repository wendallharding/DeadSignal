using UnityEngine;

namespace DeadSignal.World
{
    public enum DepartureDockHeroOwner
    {
        DepartureChannel,
        ExtractionDock
    }

    /// <summary>
    /// Records collider-free presentation geometry for the opening/finale landmark pair. Departure routing,
    /// surge, uplink, collision, and outcome systems retain all gameplay authority.
    /// </summary>
    public sealed class AuthoredDepartureDockHeroFinish : MonoBehaviour
    {
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private DepartureDockHeroOwner m_owner;
        [SerializeField] private MeshRenderer m_renderer;

        public DepartureDockHeroOwner Owner => m_owner;
        public MeshRenderer Renderer => m_renderer;
        public bool IsConfigured => m_renderer != null;
        public Color PracticalEmission { get; private set; }

        public void Configure(DepartureDockHeroOwner owner, MeshRenderer renderer)
        {
            m_owner = owner;
            m_renderer = renderer;
        }

        public void SetPracticalLighting(Color color, float intensity)
        {
            var emission = color * Mathf.Max(0f, intensity);
            if (m_hasAppliedPracticalLighting && PracticalEmission == emission)
            {
                return;
            }

            PracticalEmission = emission;
            m_hasAppliedPracticalLighting = true;
            _setMaterialEmission(2, PracticalEmission);
            if (m_owner == DepartureDockHeroOwner.DepartureChannel)
            {
                _setMaterialEmission(3, new Color(1f, 0.38f, 0.04f) * intensity * 0.62f);
            }
        }

        private void _setMaterialEmission(int materialIndex, Color emission)
        {
            if (m_renderer == null || materialIndex < 0 || materialIndex >= m_renderer.sharedMaterials.Length)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            m_renderer.GetPropertyBlock(block, materialIndex);
            block.SetColor(s_emissionColor, emission);
            m_renderer.SetPropertyBlock(block, materialIndex);
        }

        private bool m_hasAppliedPracticalLighting;
    }
}
