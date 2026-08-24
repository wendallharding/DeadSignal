using UnityEngine;

namespace DeadSignal.World
{
    public enum PoweredTerritorySource
    {
        CentralTower,
        RelayTower,
        SpineTower
    }

    public sealed class AuthoredPoweredTerritory : MonoBehaviour
    {
        [SerializeField] private PoweredTerritorySource m_source;
        [SerializeField] private Vector2 m_halfExtents = Vector2.one;
        [SerializeField] private GameObject m_signalRouting;

        public PoweredTerritorySource Source => m_source;
        public Vector2 HalfExtents => m_halfExtents;

        public bool Contains(Vector3 position)
        {
            var local = transform.InverseTransformPoint(position);
            return Mathf.Abs(local.x) <= m_halfExtents.x && Mathf.Abs(local.z) <= m_halfExtents.y;
        }

        public void Configure(PoweredTerritorySource source, Vector2 halfExtents, GameObject signalRouting)
        {
            m_source = source;
            m_halfExtents = new Vector2(Mathf.Max(0.1f, halfExtents.x), Mathf.Max(0.1f, halfExtents.y));
            m_signalRouting = signalRouting;
            SetPowered(false);
        }

        public void SetPowered(bool powered)
        {
            if (m_signalRouting != null)
            {
                m_signalRouting.SetActive(powered);
            }
        }

        private void OnValidate()
        {
            m_halfExtents.x = Mathf.Max(0.1f, m_halfExtents.x);
            m_halfExtents.y = Mathf.Max(0.1f, m_halfExtents.y);
        }
    }
}
