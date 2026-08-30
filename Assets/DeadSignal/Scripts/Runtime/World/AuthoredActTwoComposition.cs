using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the non-colliding authored structure that visually unifies the Act II rooms.</summary>
    public sealed class AuthoredActTwoComposition : MonoBehaviour
    {
        [SerializeField] private Renderer[] m_sections;

        public bool IsConfigured => m_sections != null && m_sections.Length == 3;
        public int SectionCount => m_sections?.Length ?? 0;
        public Renderer[] Sections => m_sections;

        public void Configure(Renderer[] sections)
        {
            m_sections = sections;
        }
    }
}
