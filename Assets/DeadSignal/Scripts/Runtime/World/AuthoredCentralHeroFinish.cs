using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records the scene-authored visual finish around the Central Tower. The hierarchy is presentation-only;
    /// interaction, collision, power radius, and mission authority remain with their existing owners.
    /// </summary>
    public sealed class AuthoredCentralHeroFinish : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_platformRenderer;
        [SerializeField] private MeshRenderer[] m_consoleRenderers;

        public MeshRenderer PlatformRenderer => m_platformRenderer;
        public int ConsoleRendererCount => m_consoleRenderers?.Length ?? 0;

        public void Configure(MeshRenderer platformRenderer, MeshRenderer[] consoleRenderers)
        {
            m_platformRenderer = platformRenderer;
            m_consoleRenderers = consoleRenderers;
        }
    }
}
