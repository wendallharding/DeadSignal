using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Keeps progression-door structure and route state visible after the authoritative slab retracts.</summary>
    public sealed class AuthoredStatefulDoorFrame : MonoBehaviour
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer m_housingRenderer;
        [SerializeField] private Renderer m_mechanismRenderer;
        [SerializeField] private Renderer m_statusRenderer;
        [SerializeField] private Renderer m_openGlyphRenderer;

        public bool IsConfigured => m_housingRenderer != null && m_mechanismRenderer != null &&
                                    m_statusRenderer != null && m_openGlyphRenderer != null;
        public bool IsOpen { get; private set; }
        public int RendererCount => IsConfigured ? 4 : 0;

        public void Configure(
            Renderer housingRenderer,
            Renderer mechanismRenderer,
            Renderer statusRenderer,
            Renderer openGlyphRenderer)
        {
            m_housingRenderer = housingRenderer;
            m_mechanismRenderer = mechanismRenderer;
            m_statusRenderer = statusRenderer;
            m_openGlyphRenderer = openGlyphRenderer;
            SetOpen(false);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (m_openGlyphRenderer != null)
            {
                m_openGlyphRenderer.enabled = open;
            }

            if (m_statusRenderer == null)
            {
                return;
            }

            var color = open ? new Color(0.015f, 0.82f, 1f) : new Color(0.72f, 0.08f, 0.045f);
            m_statusProperties ??= new MaterialPropertyBlock();
            m_statusRenderer.GetPropertyBlock(m_statusProperties);
            m_statusProperties.SetColor(s_baseColor, color);
            m_statusProperties.SetColor(s_emissionColor, color * (open ? 1.3f : 0.58f));
            m_statusRenderer.SetPropertyBlock(m_statusProperties);

            if (m_openGlyphRenderer == null)
            {
                return;
            }

            m_glyphProperties ??= new MaterialPropertyBlock();
            m_openGlyphRenderer.GetPropertyBlock(m_glyphProperties);
            m_glyphProperties.SetColor(s_baseColor, color);
            m_glyphProperties.SetColor(s_emissionColor, color * 1.15f);
            m_openGlyphRenderer.SetPropertyBlock(m_glyphProperties);
        }

        private MaterialPropertyBlock m_statusProperties;
        private MaterialPropertyBlock m_glyphProperties;
    }
}
