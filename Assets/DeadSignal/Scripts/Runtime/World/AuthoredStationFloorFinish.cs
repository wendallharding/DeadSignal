using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the collider-free deck-finish layers shared across the required station route.</summary>
    public sealed class AuthoredStationFloorFinish : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_layers;
        [SerializeField] private int m_finishedZoneCount;

        public bool IsConfigured => m_layers != null && m_layers.Length == 4 && m_finishedZoneCount == 12;
        public int FinishedZoneCount => m_finishedZoneCount;
        public int LayerCount => m_layers?.Length ?? 0;
        public Renderer[] Layers => m_layers;

        public void Configure(Renderer[] layers, int finishedZoneCount)
        {
            m_layers = layers;
            m_finishedZoneCount = finishedZoneCount;
        }
    }
}
