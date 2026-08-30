using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks non-colliding authored structure that frames the Security Trial sequence.</summary>
    public sealed class AuthoredSecurityTrialComposition : MonoBehaviour
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
