using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the collider-free functional prop dressing placed around the required route.</summary>
    public sealed class AuthoredStationFunctionalProps : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_propRenderers;
        [SerializeField] private int m_propTypeCount;
        [SerializeField] private int m_placementCount;

        public bool IsConfigured => m_propRenderers != null && m_propRenderers.Length == m_placementCount &&
                                    m_propTypeCount == 6 && m_placementCount == 18;
        public int PlacementCount => m_placementCount;
        public int PropTypeCount => m_propTypeCount;
        public Renderer[] PropRenderers => m_propRenderers;

        public void Configure(Renderer[] propRenderers, int propTypeCount, int placementCount)
        {
            m_propRenderers = propRenderers;
            m_propTypeCount = propTypeCount;
            m_placementCount = placementCount;
        }
    }
}
