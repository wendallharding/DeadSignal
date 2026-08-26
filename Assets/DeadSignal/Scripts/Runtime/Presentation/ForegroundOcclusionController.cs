using System.Collections.Generic;
using DeadSignal.World;
using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Creates a clean camera cutaway when tall foreground blockers cover the player.</summary>
    public sealed class ForegroundOcclusionController : MonoBehaviour
    {
        private readonly List<ObstacleRenderGroup> m_groups = new();

        private Camera m_camera;
        private Transform m_player;

        public int HiddenGroupCount { get; private set; }

        private sealed class ObstacleRenderGroup
        {
            public Renderer[] Renderers;
        }

        public void Configure(Camera targetCamera, Transform player, IReadOnlyList<AuthoredMapObstacle> obstacles)
        {
            _restoreRenderers();
            m_camera = targetCamera;
            m_player = player;
            m_groups.Clear();
            foreach (var obstacle in obstacles)
            {
                var renderers = obstacle.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    m_groups.Add(new ObstacleRenderGroup { Renderers = renderers });
                }
            }
        }

        private void LateUpdate()
        {
            if (m_camera == null || m_player == null)
            {
                return;
            }

            var playerPoint = m_camera.WorldToScreenPoint(m_player.position + Vector3.up * 0.35f);
            HiddenGroupCount = 0;
            foreach (var group in m_groups)
            {
                var hidden = _coversPlayer(group.Renderers, playerPoint);
                foreach (var renderer in group.Renderers)
                {
                    renderer.forceRenderingOff = hidden;
                }

                if (hidden)
                {
                    HiddenGroupCount++;
                }
            }
        }

        private void OnDisable()
        {
            _restoreRenderers();
        }

        private void _restoreRenderers()
        {
            foreach (var group in m_groups)
            {
                foreach (var renderer in group.Renderers)
                {
                    if (renderer != null)
                    {
                        renderer.forceRenderingOff = false;
                    }
                }
            }
        }

        private bool _coversPlayer(IReadOnlyList<Renderer> renderers, Vector3 playerPoint)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.bounds.size.y < 0.65f)
                {
                    continue;
                }

                var bounds = renderer.bounds;
                var centerPoint = m_camera.WorldToScreenPoint(bounds.center);
                if (centerPoint.z <= 0f || centerPoint.z >= playerPoint.z)
                {
                    continue;
                }

                var extents = bounds.extents;
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var point = m_camera.WorldToScreenPoint(bounds.center + Vector3.Scale(extents, new Vector3(x, y, z)));
                            minimum = Vector2.Min(minimum, point);
                            maximum = Vector2.Max(maximum, point);
                        }
                    }
                }

                const float margin = 24f;
                if (playerPoint.x >= minimum.x - margin && playerPoint.x <= maximum.x + margin &&
                    playerPoint.y >= minimum.y - margin && playerPoint.y <= maximum.y + margin)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
