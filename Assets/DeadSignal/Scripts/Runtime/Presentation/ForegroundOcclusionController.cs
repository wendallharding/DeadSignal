using System.Collections.Generic;
using DeadSignal.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Presentation
{
    /// <summary>Creates a clean camera cutaway when tall foreground blockers cover the player.</summary>
    public sealed class ForegroundOcclusionController : MonoBehaviour
    {
        private const float MINIMUM_CUTAWAY_HEIGHT = 0.25f;
        private const string FOOTPRINT_MATERIAL_PATH = "Materials/ForegroundCutawayFootprint";
        private const string AUTHORED_FOOTPRINT_MATERIAL_PATH = "Materials/ForegroundCutawayFootprintAuthored";
        private const string WIDE_FOOTPRINT_MATERIAL_PATH = "Materials/ForegroundCutawayFootprintWide";
        private const string FOOTPRINT_NAME = "Foreground Cutaway Footprint";

        private readonly List<ObstacleRenderGroup> m_groups = new();

        private Camera m_camera;
        private Material m_footprintMaterial;
        private Material m_authoredFootprintMaterial;
        private Material m_wideFootprintMaterial;
        private Mesh m_footprintMesh;
        private Transform m_player;

        public int HiddenGroupCount { get; private set; }
        public int VisibleFootprintCount { get; private set; }
        public int WideCutawayCount { get; private set; }

        private enum CutawayReason
        {
            None,
            DirectOcclusion,
            TacticalWindow,
            WideForeground
        }

        private sealed class ObstacleRenderGroup
        {
            public AuthoredMapObstacle Obstacle;
            public Renderer[] Renderers;
            public MeshRenderer Footprint;
            public Material FootprintMaterial;
            public Material WideFootprintMaterial;
        }

        public void Configure(
            Camera targetCamera,
            Transform player,
            IReadOnlyList<AuthoredMapObstacle> obstacles,
            IReadOnlyList<AuthoredForegroundCutaway> authoredCutaways,
            Material footprintMaterial = null)
        {
            _restoreRenderers();
            _destroyFootprints();
            m_camera = targetCamera;
            m_footprintMaterial = footprintMaterial != null
                ? footprintMaterial
                : Resources.Load<Material>(FOOTPRINT_MATERIAL_PATH);
            m_authoredFootprintMaterial = Resources.Load<Material>(AUTHORED_FOOTPRINT_MATERIAL_PATH);
            m_wideFootprintMaterial = Resources.Load<Material>(WIDE_FOOTPRINT_MATERIAL_PATH);
            m_player = player;
            m_groups.Clear();
            var ownedRenderers = new HashSet<Renderer>();
            foreach (var obstacle in obstacles)
            {
                var renderers = obstacle.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    m_groups.Add(new ObstacleRenderGroup
                    {
                        Obstacle = obstacle,
                        Renderers = renderers,
                        FootprintMaterial = m_footprintMaterial,
                        WideFootprintMaterial = m_wideFootprintMaterial
                    });
                    foreach (var renderer in renderers)
                    {
                        ownedRenderers.Add(renderer);
                    }
                }
            }

            foreach (var authoredCutaway in authoredCutaways)
            {
                var renderers = new List<Renderer>();
                foreach (var renderer in authoredCutaway.Renderers)
                {
                    if (renderer != null && ownedRenderers.Add(renderer))
                    {
                        renderers.Add(renderer);
                    }
                }

                if (renderers.Count > 0)
                {
                    m_groups.Add(new ObstacleRenderGroup
                    {
                        Obstacle = authoredCutaway.CollisionOwner,
                        Renderers = renderers.ToArray(),
                        FootprintMaterial = m_authoredFootprintMaterial != null
                            ? m_authoredFootprintMaterial
                            : m_footprintMaterial,
                        WideFootprintMaterial = m_wideFootprintMaterial
                    });
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
            VisibleFootprintCount = 0;
            WideCutawayCount = 0;
            foreach (var group in m_groups)
            {
                var reason = _cutawayReason(group.Renderers, playerPoint);
                var hidden = reason != CutawayReason.None;
                foreach (var renderer in group.Renderers)
                {
                    renderer.forceRenderingOff = hidden;
                }

                if (hidden)
                {
                    HiddenGroupCount++;
                    var footprint = _ensureFootprint(group);
                    if (footprint != null)
                    {
                        footprint.sharedMaterial = reason == CutawayReason.WideForeground &&
                                                   group.WideFootprintMaterial != null
                            ? group.WideFootprintMaterial
                            : group.FootprintMaterial;
                        footprint.enabled = true;
                        VisibleFootprintCount++;
                        if (reason == CutawayReason.WideForeground)
                        {
                            WideCutawayCount++;
                        }
                    }
                }
                else if (group.Footprint != null)
                {
                    group.Footprint.enabled = false;
                }
            }
        }

        private void OnDisable()
        {
            _restoreRenderers();
        }

        private void OnDestroy()
        {
            _destroyFootprints();
            if (m_footprintMesh != null)
            {
                Destroy(m_footprintMesh);
            }
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

                if (group.Footprint != null)
                {
                    group.Footprint.enabled = false;
                }
            }

            HiddenGroupCount = 0;
            VisibleFootprintCount = 0;
            WideCutawayCount = 0;
        }

        private CutawayReason _cutawayReason(IReadOnlyList<Renderer> renderers, Vector3 playerPoint)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.bounds.size.y < MINIMUM_CUTAWAY_HEIGHT)
                {
                    continue;
                }

                var bounds = renderer.bounds;
                var centerPoint = m_camera.WorldToScreenPoint(bounds.center);
                if (centerPoint.z <= 0f)
                {
                    continue;
                }

                var extents = bounds.extents;
                var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                var nearestDepth = float.PositiveInfinity;
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var point = m_camera.WorldToScreenPoint(bounds.center + Vector3.Scale(extents, new Vector3(x, y, z)));
                            minimum = Vector2.Min(minimum, point);
                            maximum = Vector2.Max(maximum, point);
                            if (point.z > 0f)
                            {
                                nearestDepth = Mathf.Min(nearestDepth, point.z);
                            }
                        }
                    }
                }

                const float directOcclusionMargin = 24f;
                var directlyOccludesPlayer = centerPoint.z < playerPoint.z &&
                                              playerPoint.x >= minimum.x - directOcclusionMargin &&
                                              playerPoint.x <= maximum.x + directOcclusionMargin &&
                                              playerPoint.y >= minimum.y - directOcclusionMargin &&
                                              playerPoint.y <= maximum.y + directOcclusionMargin;
                if (directlyOccludesPlayer)
                {
                    return CutawayReason.DirectOcclusion;
                }

                var horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
                var screenSize = maximum - minimum;
                var projectedArea = Mathf.Max(0f, screenSize.x) * Mathf.Max(0f, screenSize.y);
                var largeScreenFace = projectedArea >= m_camera.pixelWidth * m_camera.pixelHeight * 0.045f;
                var clippedMinimum = new Vector2(
                    Mathf.Clamp(minimum.x, 0f, m_camera.pixelWidth),
                    Mathf.Clamp(minimum.y, 0f, m_camera.pixelHeight));
                var clippedMaximum = new Vector2(
                    Mathf.Clamp(maximum.x, 0f, m_camera.pixelWidth),
                    Mathf.Clamp(maximum.y, 0f, m_camera.pixelHeight));
                var clippedSize = clippedMaximum - clippedMinimum;
                var clippedArea = Mathf.Max(0f, clippedSize.x) * Mathf.Max(0f, clippedSize.y);
                var occupiesWideForeground = nearestDepth < playerPoint.z &&
                                             clippedArea >= m_camera.pixelWidth * m_camera.pixelHeight * 0.1f &&
                                             clippedMaximum.x >= m_camera.pixelWidth * 0.1f &&
                                             clippedMinimum.x <= m_camera.pixelWidth * 0.9f &&
                                             clippedMaximum.y >= m_camera.pixelHeight * 0.1f &&
                                             clippedMinimum.y <= m_camera.pixelHeight * 0.9f;
                if (occupiesWideForeground)
                {
                    return CutawayReason.WideForeground;
                }

                if (horizontalSize < bounds.size.y * 1.4f && !largeScreenFace)
                {
                    continue;
                }

                var horizontalMargin = Mathf.Max(directOcclusionMargin, m_camera.pixelWidth * 0.18f);
                var verticalMargin = Mathf.Max(directOcclusionMargin, m_camera.pixelHeight * 0.23f);
                if (playerPoint.x >= minimum.x - horizontalMargin && playerPoint.x <= maximum.x + horizontalMargin &&
                    playerPoint.y >= minimum.y - verticalMargin && playerPoint.y <= maximum.y + verticalMargin)
                {
                    return CutawayReason.TacticalWindow;
                }
            }

            return CutawayReason.None;
        }

        private MeshRenderer _ensureFootprint(ObstacleRenderGroup group)
        {
            if (group.Footprint != null || group.FootprintMaterial == null)
            {
                return group.Footprint;
            }

            var marker = new GameObject(FOOTPRINT_NAME);
            marker.layer = LayerMask.NameToLayer("Ignore Raycast");
            marker.transform.SetParent(transform, false);
            var floorHeight = float.PositiveInfinity;
            foreach (var renderer in group.Renderers)
            {
                if (renderer != null)
                {
                    floorHeight = Mathf.Min(floorHeight, renderer.bounds.min.y);
                }
            }

            if (float.IsPositiveInfinity(floorHeight))
            {
                floorHeight = group.Obstacle != null ? group.Obstacle.transform.position.y : 0f;
            }

            var center = group.Obstacle != null ? group.Obstacle.Center : _rendererCenter(group.Renderers);
            var forward = group.Obstacle != null ? group.Obstacle.ForwardAxis : Vector2.up;
            var ownerHeight = group.Obstacle != null
                ? group.Obstacle.transform.position.y + 0.08f
                : floorHeight + 0.025f;
            var markerHeight = Mathf.Max(floorHeight + 0.025f, ownerHeight);
            marker.transform.position = new Vector3(center.x, markerHeight, center.y);
            marker.transform.rotation = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.y), Vector3.up);
            var halfSize = group.Obstacle != null ? group.Obstacle.ScaledHalfSize : _rendererHalfSize(group.Renderers);
            marker.transform.localScale = new Vector3(halfSize.x * 2f, 1f, halfSize.y * 2f);

            marker.AddComponent<MeshFilter>().sharedMesh = _getFootprintMesh();
            group.Footprint = marker.AddComponent<MeshRenderer>();
            group.Footprint.sharedMaterial = group.FootprintMaterial;
            group.Footprint.shadowCastingMode = ShadowCastingMode.Off;
            group.Footprint.receiveShadows = false;
            group.Footprint.lightProbeUsage = LightProbeUsage.Off;
            group.Footprint.reflectionProbeUsage = ReflectionProbeUsage.Off;
            group.Footprint.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            group.Footprint.enabled = false;
            return group.Footprint;
        }

        private static Vector2 _rendererCenter(IReadOnlyList<Renderer> renderers)
        {
            var bounds = _combinedBounds(renderers);
            return new Vector2(bounds.center.x, bounds.center.z);
        }

        private static Vector2 _rendererHalfSize(IReadOnlyList<Renderer> renderers)
        {
            var bounds = _combinedBounds(renderers);
            return new Vector2(Mathf.Max(0.05f, bounds.extents.x), Mathf.Max(0.05f, bounds.extents.z));
        }

        private static Bounds _combinedBounds(IReadOnlyList<Renderer> renderers)
        {
            var bounds = new Bounds();
            var initialized = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private Mesh _getFootprintMesh()
        {
            if (m_footprintMesh != null)
            {
                return m_footprintMesh;
            }

            m_footprintMesh = new Mesh { name = "Foreground Cutaway Footprint Plane" };
            m_footprintMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f)
            };
            m_footprintMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            m_footprintMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m_footprintMesh.RecalculateNormals();
            m_footprintMesh.RecalculateBounds();
            return m_footprintMesh;
        }

        private void _destroyFootprints()
        {
            foreach (var group in m_groups)
            {
                if (group.Footprint == null)
                {
                    continue;
                }

                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(group.Footprint.gameObject);
                }
                else
                {
                    DestroyImmediate(group.Footprint.gameObject);
                }

                group.Footprint = null;
            }
        }
    }
}
