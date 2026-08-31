using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Marks the scene-authored, non-colliding surface below the playable station deck.</summary>
    public sealed class AuthoredStationBackdrop : MonoBehaviour
    {
        [SerializeField] private Vector2 m_coverage = new(150f, 100f);
        [SerializeField] private Renderer[] m_structureRenderers;

        public Vector2 Coverage => m_coverage;
        public Renderer[] StructureRenderers => m_structureRenderers;

        public void Configure(Vector2 coverage, Renderer[] structureRenderers)
        {
            m_coverage = new Vector2(Mathf.Max(1f, coverage.x), Mathf.Max(1f, coverage.y));
            m_structureRenderers = structureRenderers ?? new Renderer[0];
        }

        private void OnValidate()
        {
            Configure(m_coverage, m_structureRenderers);
        }
    }
}
