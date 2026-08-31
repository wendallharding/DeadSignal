using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records collider-free presentation geometry for the three Security Trial rooms. The combat chamber,
    /// doors, objective graph, reward, collision, and encounter systems retain all gameplay authority.
    /// </summary>
    public sealed class AuthoredSecurityTrialHeroFinish : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_commitmentRenderer;
        [SerializeField] private MeshRenderer m_lockdownRenderer;
        [SerializeField] private MeshRenderer m_vaultRenderer;

        public MeshRenderer CommitmentRenderer => m_commitmentRenderer;
        public MeshRenderer LockdownRenderer => m_lockdownRenderer;
        public MeshRenderer VaultRenderer => m_vaultRenderer;
        public bool IsConfigured => m_commitmentRenderer != null && m_lockdownRenderer != null && m_vaultRenderer != null;

        public void Configure(
            MeshRenderer commitmentRenderer,
            MeshRenderer lockdownRenderer,
            MeshRenderer vaultRenderer)
        {
            m_commitmentRenderer = commitmentRenderer;
            m_lockdownRenderer = lockdownRenderer;
            m_vaultRenderer = vaultRenderer;
        }
    }
}
