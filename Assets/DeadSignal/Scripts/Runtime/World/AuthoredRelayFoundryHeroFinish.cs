using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Records the collider-free visual finish around the Relay Foundry. Existing room components retain
    /// progression, collision, gate, reinforcement, and state authority.
    /// </summary>
    public sealed class AuthoredRelayFoundryHeroFinish : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_structureRenderer;
        [SerializeField] private MeshRenderer m_powerRenderer;

        public MeshRenderer StructureRenderer => m_structureRenderer;
        public MeshRenderer PowerRenderer => m_powerRenderer;
        public bool IsConfigured => m_structureRenderer != null && m_powerRenderer != null;

        public void Configure(MeshRenderer structureRenderer, MeshRenderer powerRenderer)
        {
            m_structureRenderer = structureRenderer;
            m_powerRenderer = powerRenderer;
        }
    }
}
