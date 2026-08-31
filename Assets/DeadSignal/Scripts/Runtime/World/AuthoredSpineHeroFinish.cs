using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records a collider-free visual finish for a Spine-region room. Existing authored objectives retain
    /// progression, collision, projectile, route, powered-territory, and lifecycle presentation authority.
    /// </summary>
    public sealed class AuthoredSpineHeroFinish : MonoBehaviour
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
