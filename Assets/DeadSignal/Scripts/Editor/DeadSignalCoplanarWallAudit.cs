using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalCoplanarWallAudit
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const float PLANE_TOLERANCE = 0.005f;
        private const float PARALLEL_TOLERANCE = 0.9999f;
        private const float MINIMUM_OVERLAP = 0.5f;

        [MenuItem("DEAD SIGNAL/Diagnostics/Audit Coplanar Walls")]
        public static void Audit()
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var faces = new List<Face>();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!_isWall(renderer.name))
                {
                    continue;
                }
                _addFaces(renderer, faces);
            }

            var issueCount = 0;
            for (var firstIndex = 0; firstIndex < faces.Count; firstIndex++)
            {
                var first = faces[firstIndex];
                for (var secondIndex = firstIndex + 1; secondIndex < faces.Count; secondIndex++)
                {
                    var second = faces[secondIndex];
                    if (first.Renderer == second.Renderer ||
                        Mathf.Abs(Vector3.Dot(first.Normal, second.Normal)) < PARALLEL_TOLERANCE ||
                        Mathf.Abs(Vector3.Dot(second.Center - first.Center, first.Normal)) > PLANE_TOLERANCE)
                    {
                        continue;
                    }

                    var overlapU = _overlap(first.Center, first.AxisU, first.HalfU, second);
                    var overlapV = _overlap(first.Center, first.AxisV, first.HalfV, second);
                    if (overlapU <= MINIMUM_OVERLAP || overlapV <= MINIMUM_OVERLAP)
                    {
                        continue;
                    }

                    issueCount++;
                    Debug.LogWarning(
                        $"[COPLANAR WALL] {_path(first.Renderer.transform)} <> {_path(second.Renderer.transform)} " +
                        $"overlap={overlapU:0.###}x{overlapV:0.###} plane={first.Center:0.###}");
                }
            }
            Debug.Log($"[COPLANAR WALL AUDIT] issues={issueCount} wallRenderers={faces.Count / 6}");
        }

        private static bool _isWall(string objectName)
        {
            return objectName.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("bulkhead", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("door slab", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void _addFaces(MeshRenderer renderer, ICollection<Face> faces)
        {
            var transform = renderer.transform;
            var right = transform.right.normalized;
            var up = transform.up.normalized;
            var forward = transform.forward.normalized;
            var half = Vector3.Scale(transform.lossyScale, Vector3.one * 0.5f);
            _addFacePair(renderer, faces, transform.position, right, up, forward, half.x, half.y, half.z);
            _addFacePair(renderer, faces, transform.position, up, right, forward, half.y, half.x, half.z);
            _addFacePair(renderer, faces, transform.position, forward, right, up, half.z, half.x, half.y);
        }

        private static void _addFacePair(
            MeshRenderer renderer,
            ICollection<Face> faces,
            Vector3 center,
            Vector3 normal,
            Vector3 axisU,
            Vector3 axisV,
            float normalHalf,
            float halfU,
            float halfV)
        {
            faces.Add(new Face(renderer, center + normal * normalHalf, normal, axisU, axisV, halfU, halfV));
            faces.Add(new Face(renderer, center - normal * normalHalf, -normal, axisU, axisV, halfU, halfV));
        }

        private static float _overlap(Vector3 origin, Vector3 axis, float firstHalf, Face second)
        {
            var secondCenter = Vector3.Dot(second.Center - origin, axis);
            var secondHalf = Mathf.Abs(Vector3.Dot(second.AxisU, axis)) * second.HalfU +
                             Mathf.Abs(Vector3.Dot(second.AxisV, axis)) * second.HalfV;
            return Mathf.Min(firstHalf, secondCenter + secondHalf) -
                   Mathf.Max(-firstHalf, secondCenter - secondHalf);
        }

        private static string _path(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private readonly struct Face
        {
            public Face(
                MeshRenderer renderer,
                Vector3 center,
                Vector3 normal,
                Vector3 axisU,
                Vector3 axisV,
                float halfU,
                float halfV)
            {
                Renderer = renderer;
                Center = center;
                Normal = normal;
                AxisU = axisU;
                AxisV = axisV;
                HalfU = halfU;
                HalfV = halfV;
            }

            public MeshRenderer Renderer { get; }
            public Vector3 Center { get; }
            public Vector3 Normal { get; }
            public Vector3 AxisU { get; }
            public Vector3 AxisV { get; }
            public float HalfU { get; }
            public float HalfV { get; }
        }
    }
}
