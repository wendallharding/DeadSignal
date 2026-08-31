using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records collider-free presentation geometry for the Arc Furnace and Quench stages. Existing objective,
    /// shutter, collision, projectile, route, powered-territory, and lifecycle components retain authority.
    /// </summary>
    public sealed class AuthoredFurnaceQuenchHeroFinish : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_finishRenderer;

        public MeshRenderer FinishRenderer => m_finishRenderer;
        public bool IsConfigured => m_finishRenderer != null;

        public void Configure(MeshRenderer finishRenderer)
        {
            m_finishRenderer = finishRenderer;
        }
    }
}
