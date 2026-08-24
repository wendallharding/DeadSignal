using DeadSignal.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Diagnostics
{
    /// <summary>Renders authored blockers, entries, and the player collision envelope in development builds.</summary>
    public sealed class DeadSignalDebugVisualization : MonoBehaviour
    {
        private GameObject m_root;
        private Transform m_player;
        private LineRenderer m_playerEnvelope;
        private Material m_material;

        public bool IsVisible => m_root != null && m_root.activeSelf;

        public void Configure(Camera targetCamera, Transform player)
        {
            m_player = player;
            m_material = Resources.Load<Material>("Materials/SignalBoltTrail");
            m_root = new GameObject("DEBUG — World Visualization");
            m_root.transform.SetParent(transform, false);

            foreach (var obstacle in Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None))
            {
                var half = obstacle.ScaledHalfSize;
                var center = new Vector3(obstacle.Center.x, 0.38f, obstacle.Center.y);
                var right = new Vector3(obstacle.RightAxis.x, 0f, obstacle.RightAxis.y) * half.x;
                var forward = new Vector3(obstacle.ForwardAxis.x, 0f, obstacle.ForwardAxis.y) * half.y;
                _createLoop($"Blocker — {obstacle.name}", new[]
                {
                    center - right - forward, center + right - forward, center + right + forward, center - right + forward
                }, new Color(1f, 0.2f, 0.12f, 0.85f));
            }

            foreach (var entrance in Object.FindObjectsByType<AuthoredInterceptorEntrance>(FindObjectsSortMode.None))
            {
                _createCircle($"Entry — {entrance.name}", entrance.Position + Vector3.up * 0.4f, 0.75f,
                    new Color(1f, 0.68f, 0.12f, 0.9f));
            }

            m_playerEnvelope = _createCircle("Player Collision Envelope", player.position + Vector3.up * 0.42f, 0.48f,
                new Color(0.15f, 1f, 1f, 0.95f));
            m_root.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (m_root != null)
            {
                m_root.SetActive(visible);
            }
        }

        private void LateUpdate()
        {
            if (!IsVisible || m_player == null || m_playerEnvelope == null)
            {
                return;
            }

            var center = m_player.position + Vector3.up * 0.42f;
            for (var index = 0; index < m_playerEnvelope.positionCount; index++)
            {
                var angle = index / (float)(m_playerEnvelope.positionCount - 1) * Mathf.PI * 2f;
                m_playerEnvelope.SetPosition(index, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.48f);
            }
        }

        private LineRenderer _createCircle(string objectName, Vector3 center, float radius, Color color)
        {
            const int SEGMENTS = 25;
            var points = new Vector3[SEGMENTS];
            for (var index = 0; index < SEGMENTS; index++)
            {
                var angle = index / (float)(SEGMENTS - 1) * Mathf.PI * 2f;
                points[index] = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
            return _createLoop(objectName, points, color, false);
        }

        private LineRenderer _createLoop(string objectName, Vector3[] points, Color color, bool close = true)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(m_root.transform, false);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = m_material;
            line.useWorldSpace = true;
            line.positionCount = close ? points.Length + 1 : points.Length;
            line.startWidth = 0.055f;
            line.endWidth = 0.055f;
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (var index = 0; index < points.Length; index++)
            {
                line.SetPosition(index, points[index]);
            }
            if (close)
            {
                line.SetPosition(points.Length, points[0]);
            }
            return line;
        }
    }
}
