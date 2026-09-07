using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the collider-free, text-free signage layers used to teach station routes in world space.</summary>
    public sealed class AuthoredStationNavigationSignage : MonoBehaviour
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] m_layers;

        public bool IsConfigured => m_layers != null && m_layers.Length == 5;
        public int LayerCount => m_layers?.Length ?? 0;
        public Renderer[] Layers => m_layers;
        public bool IsHighContrastEnabled { get; private set; }

        public void Configure(Renderer[] layers)
        {
            m_layers = layers;
        }

        public void ApplyHighContrast(bool enabled)
        {
            IsHighContrastEnabled = enabled;
            if (!IsConfigured)
            {
                return;
            }

            _setLayerColor(0, enabled ? new Color(0.52f, 0.52f, 0.52f) : new Color(0.3f, 0.38f, 0.42f));
            _setLayerColor(1, enabled ? new Color(0.96f, 0.96f, 0.96f) : new Color(0.56f, 0.19f, 0.025f));
            _setLayerColor(2, enabled ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.38f, 0.42f));
            _setLayerColor(3, enabled ? new Color(0.52f, 0.52f, 0.52f) : new Color(0.3f, 0.38f, 0.42f));
            _setLayerColor(4, enabled ? new Color(0.68f, 0.68f, 0.68f) : new Color(0.015f, 0.31f, 0.38f));
        }

        private void _setLayerColor(int index, Color color)
        {
            var properties = new MaterialPropertyBlock();
            m_layers[index].GetPropertyBlock(properties);
            properties.SetColor(s_baseColor, color);
            m_layers[index].SetPropertyBlock(properties);
        }
    }
}
