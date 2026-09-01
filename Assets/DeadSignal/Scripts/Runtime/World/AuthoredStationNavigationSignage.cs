using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the collider-free, text-free signage layers used to teach station routes in world space.</summary>
    public sealed class AuthoredStationNavigationSignage : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_layers;

        public bool IsConfigured => m_layers != null && m_layers.Length == 5;
        public int LayerCount => m_layers?.Length ?? 0;
        public Renderer[] Layers => m_layers;

        public void Configure(Renderer[] layers)
        {
            m_layers = layers;
        }
    }
}
