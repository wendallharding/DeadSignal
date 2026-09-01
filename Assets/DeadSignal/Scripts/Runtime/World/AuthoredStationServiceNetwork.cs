using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the collider-free station service layers that visually connect major machinery.</summary>
    public sealed class AuthoredStationServiceNetwork : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_layers;
        [SerializeField] private int m_connectionClusterCount;

        public bool IsConfigured => m_layers != null && m_layers.Length == 4 && m_connectionClusterCount == 8;
        public int ConnectionClusterCount => m_connectionClusterCount;
        public int LayerCount => m_layers?.Length ?? 0;
        public Renderer[] Layers => m_layers;

        public void Configure(Renderer[] layers, int connectionClusterCount)
        {
            m_layers = layers;
            m_connectionClusterCount = connectionClusterCount;
        }
    }
}
