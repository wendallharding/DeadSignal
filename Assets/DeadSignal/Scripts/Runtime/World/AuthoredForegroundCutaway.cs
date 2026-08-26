using System.Collections.Generic;
using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>Explicitly assigns presentation-only wall renderers to the foreground cutaway system.</summary>
    public sealed class AuthoredForegroundCutaway : MonoBehaviour
    {
        [SerializeField] private AuthoredMapObstacle m_collisionOwner;
        [SerializeField] private Renderer[] m_renderers;

        public AuthoredMapObstacle CollisionOwner => m_collisionOwner;
        public IReadOnlyList<Renderer> Renderers => m_renderers;

        public void Configure(AuthoredMapObstacle collisionOwner, params Renderer[] renderers)
        {
            m_collisionOwner = collisionOwner;
            m_renderers = renderers;
        }
    }
}
