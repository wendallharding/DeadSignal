using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records the collider-free authored finish around the Cargo Annex coupling job. Objective, movement,
    /// projectile, and obstacle authority remain with the existing Cargo objective and authored barriers.
    /// </summary>
    public sealed class AuthoredCargoAnnexHeroFinish : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_finishRenderer;
        [SerializeField] private MeshRenderer m_couplingBaseRenderer;
        [SerializeField] private MeshRenderer m_couplingRotorRenderer;
        [SerializeField] private MeshRenderer[] m_barrierRenderers;

        public MeshRenderer FinishRenderer => m_finishRenderer;
        public MeshRenderer CouplingBaseRenderer => m_couplingBaseRenderer;
        public MeshRenderer CouplingRotorRenderer => m_couplingRotorRenderer;
        public int BarrierRendererCount => m_barrierRenderers?.Length ?? 0;

        public void Configure(
            MeshRenderer finishRenderer,
            MeshRenderer couplingBaseRenderer,
            MeshRenderer couplingRotorRenderer,
            MeshRenderer[] barrierRenderers)
        {
            m_finishRenderer = finishRenderer;
            m_couplingBaseRenderer = couplingBaseRenderer;
            m_couplingRotorRenderer = couplingRotorRenderer;
            m_barrierRenderers = barrierRenderers;
        }
    }
}
