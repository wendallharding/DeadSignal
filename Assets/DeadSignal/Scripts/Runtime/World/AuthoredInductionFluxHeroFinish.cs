using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records a collider-free visual finish for the Induction Gallery or Flux Bypass. Existing objectives retain
    /// progression, collision, projectile, route, powered-territory, and lifecycle presentation authority.
    /// </summary>
    public sealed class AuthoredInductionFluxHeroFinish : MonoBehaviour
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
