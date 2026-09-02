using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationWallKitSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/StationWallParapetKit.prefab";
        private const string WALL_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationWallFaces.asset";
        private const string CORNER_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationWallCornerCaps.asset";
        private const string PARAPET_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationParapets.asset";
        private const string SUPPORT_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationWallSupports.asset";
        private const string BACK_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationWallBacks.asset";
        private const string END_MESH_PATH = "Assets/DeadSignal/Resources/Environment/StationWallEndPieces.asset";
        private const string BULKHEAD_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/MaintenanceBulkhead.mat";
        private const string STEEL_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationSteel.mat";
        private const string WHITE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/DroneWhite.mat";
        private const string BLACK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationBlack.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string KIT_NAME = "Station Wall and Parapet Kit";
        private const float SECURITY_TRIAL_WEST_WALL_X = 12.5f;
        private const float SECURITY_TRIAL_EAST_WALL_X = 72.5f;

        private static readonly WallSegment[] s_segments =
        {
            new(new Vector2(-5.8f, 10.15f), 7.2f, 0f, Vector2.up, true),
            new(new Vector2(9.7f, 9.15f), 5.8f, 0f, Vector2.up, true),
            new(new Vector2(16.7f, 3.8f), 7.4f, 0f, Vector2.up, true),
            new(new Vector2(20.55f, 0f), 7.3f, 90f, Vector2.right, true),
            new(new Vector2(-9.55f, 7.2f), 5.8f, 90f, Vector2.left, true),
            new(new Vector2(27.5f, 7.3f), 16.6f, 0f, Vector2.up, true),
            new(new Vector2(42.5f, 5.3f), 14.6f, 0f, Vector2.up, true),
            new(new Vector2(49.8f, 0f), 10.6f, 90f, Vector2.right, true),
            new(new Vector2(47.8f, -8f), 6.6f, 90f, Vector2.right, false),
            new(new Vector2(42.5f, 30.3f), 14.6f, 0f, Vector2.up, true),
            new(new Vector2(53f, 30.3f), 7.6f, 0f, Vector2.up, true),
            new(new Vector2(56.8f, 21.25f), 18.1f, 90f, Vector2.right, true),
            new(new Vector2(28.2f, 21.25f), 12.1f, 90f, Vector2.left, true),
            new(new Vector2(SECURITY_TRIAL_WEST_WALL_X, 54f), 36.8f, 90f, Vector2.left, true),
            new(new Vector2(SECURITY_TRIAL_EAST_WALL_X, 54f), 36.8f, 90f, Vector2.right, true),
            new(new Vector2(42.5f, 78.4f), 8.8f, 0f, Vector2.up, true),
            new(new Vector2(-14f, -5.6f), 9.6f, 90f, Vector2.left, false),
            new(new Vector2(-3.35f, -4.6f), 4.8f, -35f, new Vector2(0.82f, 0.57f), false)
        };

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var kit = prefab != null ? prefab.GetComponent<AuthoredStationWallKit>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(WALL_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(CORNER_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(PARAPET_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SUPPORT_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(BACK_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(END_MESH_PATH) != null &&
                       kit != null && kit.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Station Wall and Parapet Kit")]
        public static void EnsureAssets()
        {
            _ensureMesh(WALL_MESH_PATH, _buildWallFaces());
            _ensureMesh(CORNER_MESH_PATH, _buildCornerCaps());
            _ensureMesh(PARAPET_MESH_PATH, _buildParapets());
            _ensureMesh(SUPPORT_MESH_PATH, _buildSupports());
            _ensureMesh(BACK_MESH_PATH, _buildShadowBacks());
            _ensureMesh(END_MESH_PATH, _buildEndPieces());
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored station wall and parapet kit is incomplete.");
            }
        }

        private static Mesh _buildWallFaces()
        {
            var builder = new MeshBuilder("StationWallFaces");
            foreach (var segment in s_segments)
            {
                builder.AddBox(_position(segment.Center, -0.2f),
                    _orientedSize(segment.Length, 0.72f, 0.48f), segment.RotationY);
            }

            return builder.Build();
        }

        private static Mesh _buildCornerCaps()
        {
            var builder = new MeshBuilder("StationWallCornerCaps");
            foreach (var segment in s_segments)
            {
                if (!segment.HasParapet)
                {
                    continue;
                }

                var axis = _axis(segment);
                var offset = axis * (segment.Length * 0.5f - 0.24f);
                builder.AddBox(_position(segment.Center + offset, 0.39f), new Vector3(0.52f, 0.22f, 0.52f), segment.RotationY);
                builder.AddBox(_position(segment.Center - offset, 0.39f), new Vector3(0.52f, 0.22f, 0.52f), segment.RotationY);
            }

            return builder.Build();
        }

        private static Mesh _buildParapets()
        {
            var builder = new MeshBuilder("StationParapets");
            foreach (var segment in s_segments)
            {
                if (!segment.HasParapet)
                {
                    continue;
                }

                var center = segment.Center + segment.Outward * 0.03f;
                builder.AddBox(_position(center, 0.24f),
                    _orientedSize(segment.Length - 0.34f, 0.28f, 0.34f), segment.RotationY);
                builder.AddBox(_position(center, 0.41f),
                    _orientedSize(segment.Length - 0.72f, 0.08f, 0.44f), segment.RotationY);
            }

            return builder.Build();
        }

        private static Mesh _buildSupports()
        {
            var builder = new MeshBuilder("StationWallSupports");
            foreach (var segment in s_segments)
            {
                var axis = _axis(segment);
                var count = Mathf.Max(2, Mathf.CeilToInt(segment.Length / 4f));
                for (var index = 0; index <= count; index++)
                {
                    var offset = Mathf.Lerp(-segment.Length * 0.44f, segment.Length * 0.44f, index / (float)count);
                    var center = segment.Center + axis * offset - segment.Outward * 0.28f;
                    builder.AddBox(_position(center, -0.17f), new Vector3(0.18f, 0.66f, 0.18f), segment.RotationY);
                }
            }

            return builder.Build();
        }

        private static Mesh _buildShadowBacks()
        {
            var builder = new MeshBuilder("StationWallBacks");
            foreach (var segment in s_segments)
            {
                var center = segment.Center - segment.Outward * 0.251f;
                builder.AddBox(_position(center, -0.22f),
                    _orientedSize(segment.Length - 0.7f, 0.34f, 0.025f), segment.RotationY);
            }

            return builder.Build();
        }

        private static Mesh _buildEndPieces()
        {
            var builder = new MeshBuilder("StationWallEndPieces");
            foreach (var segment in s_segments)
            {
                if (segment.HasParapet)
                {
                    continue;
                }

                var axis = _axis(segment);
                var offset = axis * (segment.Length * 0.5f - 0.18f);
                builder.AddBox(_position(segment.Center + offset, -0.06f), new Vector3(0.64f, 0.92f, 0.64f), segment.RotationY);
                builder.AddBox(_position(segment.Center - offset, -0.06f), new Vector3(0.64f, 0.92f, 0.64f), segment.RotationY);
            }

            return builder.Build();
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject(KIT_NAME);
            try
            {
                var sections = new[]
                {
                    _addSection(root.transform, "Wall Faces", WALL_MESH_PATH, BULKHEAD_MATERIAL_PATH),
                    _addSection(root.transform, "Corner Caps", CORNER_MESH_PATH, WHITE_MATERIAL_PATH),
                    _addSection(root.transform, "Parapets", PARAPET_MESH_PATH, STEEL_MATERIAL_PATH),
                    _addSection(root.transform, "Supports", SUPPORT_MESH_PATH, STEEL_MATERIAL_PATH),
                    _addSection(root.transform, "Shadow Backs", BACK_MESH_PATH, BLACK_MATERIAL_PATH),
                    _addSection(root.transform, "End Pieces", END_MESH_PATH, BULKHEAD_MATERIAL_PATH)
                };
                root.AddComponent<AuthoredStationWallKit>().Configure(sections);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Renderer _addSection(Transform parent, string objectName, string meshPath, string materialPath)
        {
            var section = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            section.transform.SetParent(parent, false);
            section.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var renderer = section.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return renderer;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var environment = GameObject.Find(ENVIRONMENT_PATH);
            if (environment == null)
            {
                throw new InvalidOperationException($"The authored scene is missing {ENVIRONMENT_PATH}.");
            }

            var existing = environment.transform.Find(KIT_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the station wall and parapet kit in SampleScene.");
            }

            instance.name = KIT_NAME;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Vector3 _orientedSize(float length, float height, float depth)
        {
            return new Vector3(length, height, depth);
        }

        private static Vector2 _axis(WallSegment segment)
        {
            var rotation = Quaternion.Euler(0f, segment.RotationY, 0f);
            var axis = rotation * Vector3.right;
            return new Vector2(axis.x, axis.z).normalized;
        }

        private static Vector3 _position(Vector2 position, float y)
        {
            return new Vector3(position.x, y, position.y);
        }

        private static void _ensureMesh(string path, Mesh generated)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return;
            }

            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
        }

        private readonly struct WallSegment
        {
            public WallSegment(Vector2 center, float length, float rotationY, Vector2 outward, bool hasParapet)
            {
                Center = center;
                Length = length;
                RotationY = rotationY;
                Outward = outward.normalized;
                HasParapet = hasParapet;
            }

            public Vector2 Center { get; }
            public float Length { get; }
            public float RotationY { get; }
            public Vector2 Outward { get; }
            public bool HasParapet { get; }
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name)
            {
                m_name = name;
            }

            public void AddBox(Vector3 center, Vector3 size, float rotationY = 0f)
            {
                var half = size * 0.5f;
                var rotation = Quaternion.Euler(0f, rotationY, 0f);
                var corners = new[]
                {
                    new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                    new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z),
                    new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
                    new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z)
                };
                var faces = new[]
                {
                    0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                    0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5,
                    3, 7, 6, 3, 6, 2, 0, 1, 5, 0, 5, 4
                };
                var start = m_vertices.Count;
                foreach (var corner in corners)
                {
                    m_vertices.Add(center + rotation * corner);
                }

                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                foreach (var index in faces)
                {
                    m_triangles.Add(start + index);
                }
            }

            public Mesh Build()
            {
                var mesh = new Mesh { name = m_name };
                mesh.SetVertices(m_vertices);
                mesh.SetUVs(0, m_uvs);
                mesh.SetTriangles(m_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }

            private readonly string m_name;
            private readonly List<Vector3> m_vertices = new();
            private readonly List<Vector2> m_uvs = new();
            private readonly List<int> m_triangles = new();
        }
    }
}
