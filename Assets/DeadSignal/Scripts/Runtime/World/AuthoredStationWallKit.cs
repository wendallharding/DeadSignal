using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the collider-free authored wall and parapet finish shared across the station.</summary>
    public sealed class AuthoredStationWallKit : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_sections;

        public bool IsConfigured => m_sections != null && m_sections.Length == 6;
        public int SectionCount => m_sections?.Length ?? 0;
        public Renderer[] Sections => m_sections;

        public void Configure(Renderer[] sections)
        {
            m_sections = sections;
        }
    }
}
