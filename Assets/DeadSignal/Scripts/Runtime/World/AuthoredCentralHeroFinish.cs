using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records the scene-authored visual finish around the Central Tower. The hierarchy is presentation-only;
    /// interaction, collision, power radius, and mission authority remain with their existing owners.
    /// </summary>
    public sealed class AuthoredCentralHeroFinish : MonoBehaviour
    {
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private MeshRenderer m_platformRenderer;
        [SerializeField] private MeshRenderer[] m_consoleRenderers;

        public MeshRenderer PlatformRenderer => m_platformRenderer;
        public int ConsoleRendererCount => m_consoleRenderers?.Length ?? 0;
        public Color PracticalEmission { get; private set; }

        public void Configure(MeshRenderer platformRenderer, MeshRenderer[] consoleRenderers)
        {
            m_platformRenderer = platformRenderer;
            m_consoleRenderers = consoleRenderers;
        }

        public void SetPracticalLighting(Color color, float intensity)
        {
            var emission = color * Mathf.Max(0f, intensity);
            if (m_hasAppliedPracticalLighting && PracticalEmission == emission)
            {
                return;
            }

            m_hasAppliedPracticalLighting = true;
            PracticalEmission = emission;
            _setMaterialEmission(m_platformRenderer, 2, PracticalEmission);
            if (m_consoleRenderers == null)
            {
                return;
            }

            foreach (var renderer in m_consoleRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    if (materials[index] != null && materials[index].name.Contains("Amber"))
                    {
                        _setMaterialEmission(renderer, index, PracticalEmission);
                    }
                }
            }
        }

        private static void _setMaterialEmission(Renderer renderer, int materialIndex, Color emission)
        {
            if (renderer == null || materialIndex < 0 || materialIndex >= renderer.sharedMaterials.Length)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, materialIndex);
            block.SetColor(s_emissionColor, emission);
            renderer.SetPropertyBlock(block, materialIndex);
        }

        private bool m_hasAppliedPracticalLighting;
    }
}
