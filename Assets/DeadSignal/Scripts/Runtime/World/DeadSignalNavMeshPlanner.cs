using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DeadSignal.World
{
    internal readonly struct NavMeshObstacleBounds
    {
        public NavMeshObstacleBounds(Vector2 center, Vector2 halfSize, Vector2 forwardAxis)
        {
            Center = center;
            HalfSize = halfSize;
            ForwardAxis = forwardAxis;
        }

        public Vector2 Center { get; }
        public Vector2 HalfSize { get; }
        public Vector2 ForwardAxis { get; }
    }

    /// <summary>Builds and queries the runtime NavMesh used for route planning; movement collision remains authoritative.</summary>
    internal sealed class DeadSignalNavMeshPlanner : System.IDisposable
    {
        private const float CORNER_REACHED_DISTANCE = 0.65f;
        private const float DESTINATION_CHANGE_DISTANCE = 0.5f;
        private const float SAMPLE_DISTANCE = 4f;

        private readonly Dictionary<int, RouteState> m_routes = new();
        private NavMeshData m_data;
        private NavMeshDataInstance m_instance;

        public bool IsReady => m_instance.valid;
        public string LastStatus { get; private set; } = "Not built";

        public void Build(Vector2 arenaHalfExtents, IReadOnlyList<NavMeshObstacleBounds> obstacles, float agentRadius)
        {
            Dispose();
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = agentRadius;
            settings.agentHeight = 1f;
            settings.agentClimb = 0.08f;
            settings.agentSlope = 1f;
            settings.minRegionArea = 0.25f;

            var sources = new List<NavMeshBuildSource>(obstacles.Count + 1)
            {
                new()
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(arenaHalfExtents.x * 2f, 0.1f, arenaHalfExtents.y * 2f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.05f, 0f), Quaternion.identity, Vector3.one),
                    area = 0
                }
            };
            var notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            foreach (var obstacle in obstacles)
            {
                var forward = new Vector3(obstacle.ForwardAxis.x, 0f, obstacle.ForwardAxis.y);
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(obstacle.HalfSize.x * 2f, 1f, obstacle.HalfSize.y * 2f),
                    transform = Matrix4x4.TRS(
                        new Vector3(obstacle.Center.x, 0.45f, obstacle.Center.y),
                        Quaternion.LookRotation(forward, Vector3.up),
                        Vector3.one),
                    area = notWalkable
                });
            }

            var bounds = new Bounds(Vector3.zero, new Vector3(arenaHalfExtents.x * 2f + 2f, 4f, arenaHalfExtents.y * 2f + 2f));
            m_data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (m_data == null)
            {
                LastStatus = "Build failed";
                return;
            }
            m_instance = NavMesh.AddNavMeshData(m_data);
            LastStatus = m_instance.valid ? $"Ready — {obstacles.Count} blockers" : "Registration failed";
            m_routes.Clear();
        }

        public Vector3 GetWaypoint(Transform actor, Vector3 destination)
        {
            if (!IsReady || actor == null)
            {
                LastStatus = "Fallback — NavMesh unavailable";
                return destination;
            }

            var key = actor.GetInstanceID();
            if (!m_routes.TryGetValue(key, out var route))
            {
                route = new RouteState();
                m_routes.Add(key, route);
            }
            if (!route.IsValid || DeadSignalWorld.FlatDistance(route.Destination, destination) > DESTINATION_CHANGE_DISTANCE)
            {
                _plan(route, actor.position, destination);
            }
            while (route.IsValid && route.CornerIndex < route.Corners.Length - 1 &&
                   DeadSignalWorld.FlatDistance(actor.position, route.Corners[route.CornerIndex]) <= CORNER_REACHED_DISTANCE)
            {
                route.CornerIndex++;
            }
            if (!route.IsValid || route.CornerIndex >= route.Corners.Length)
            {
                return destination;
            }
            LastStatus = $"Complete — corner {route.CornerIndex + 1}/{route.Corners.Length}";
            return route.Corners[route.CornerIndex];
        }

        public int GetRemainingCornerCount(Transform actor)
        {
            return actor != null && m_routes.TryGetValue(actor.GetInstanceID(), out var route) && route.IsValid
                ? Mathf.Max(0, route.Corners.Length - route.CornerIndex)
                : 0;
        }

        public void Invalidate(Transform actor)
        {
            if (actor != null)
            {
                m_routes.Remove(actor.GetInstanceID());
            }
        }

        public void Dispose()
        {
            if (m_instance.valid)
            {
                m_instance.Remove();
            }
            m_data = null;
            m_routes.Clear();
        }

        private void _plan(RouteState route, Vector3 start, Vector3 destination)
        {
            route.IsValid = false;
            route.Destination = destination;
            if (!NavMesh.SamplePosition(start, out var startHit, SAMPLE_DISTANCE, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(destination, out var destinationHit, SAMPLE_DISTANCE, NavMesh.AllAreas))
            {
                LastStatus = "Fallback — endpoint off mesh";
                return;
            }
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(startHit.position, destinationHit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
            {
                LastStatus = $"Fallback — {path.status}";
                return;
            }
            route.Corners = path.corners;
            route.CornerIndex = 1;
            route.IsValid = true;
            LastStatus = $"Complete — {path.corners.Length} corners";
        }

        private sealed class RouteState
        {
            public Vector3 Destination;
            public Vector3[] Corners = System.Array.Empty<Vector3>();
            public int CornerIndex;
            public bool IsValid;
        }
    }
}
