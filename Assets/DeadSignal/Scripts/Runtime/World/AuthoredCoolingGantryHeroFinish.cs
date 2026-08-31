using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records the collider-free visual finish for the Cooling Gantry. Existing authored components retain
    /// processing, traversal, collision, reinforcement, powered-territory, and lifecycle authority.
    /// </summary>
    public sealed class AuthoredCoolingGantryHeroFinish : MonoBehaviour
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
