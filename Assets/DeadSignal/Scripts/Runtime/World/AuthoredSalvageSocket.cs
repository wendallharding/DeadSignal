using UnityEngine;
using DeadSignal.Missions;

namespace DeadSignal.World
{
    /// <summary>
    /// Marks a scene-authored cache location while runtime systems retain collection ownership.
    /// </summary>
    public sealed class AuthoredSalvageSocket : MonoBehaviour
    {
        [SerializeField] private SignalRegion m_region = SignalRegion.Spine;
        [SerializeField] private bool m_isOptional = true;

        public Vector3 Position => transform.position;
        public SignalRegion Region => m_region;
        public bool IsOptional => m_isOptional;

        public void Configure(SignalRegion region, bool isOptional)
        {
            m_region = region;
            m_isOptional = isOptional;
        }
    }
}
