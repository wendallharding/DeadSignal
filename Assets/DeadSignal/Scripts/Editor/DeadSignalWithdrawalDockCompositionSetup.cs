using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalWithdrawalDockCompositionSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/WithdrawalDockComposition.prefab";
        private const string APRON_MESH_PATH = "Assets/DeadSignal/Resources/Environment/WithdrawalDockAprons.asset";
        private const string SHADOW_MESH_PATH = "Assets/DeadSignal/Resources/Environment/WithdrawalDockShadowBacks.asset";
        private const string FRAME_MESH_PATH = "Assets/DeadSignal/Resources/Environment/WithdrawalDockEdgeFrames.asset";
        private const string APRON_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationSteel.mat";
        private const string SHADOW_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationBlack.mat";
        private const string FRAME_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/MaintenanceBulkhead.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string COMPOSITION_NAME = "Withdrawal and Dock Composition";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var composition = prefab != null ? prefab.GetComponent<AuthoredWithdrawalDockComposition>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(APRON_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SHADOW_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(FRAME_MESH_PATH) != null &&
                       composition != null && composition.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Withdrawal and Dock Presentation Composition")]
        public static void EnsureAssets()
        {
            _ensureMesh(APRON_MESH_PATH, _buildAprons());
            _ensureMesh(SHADOW_MESH_PATH, _buildShadowBacks());
            _ensureMesh(FRAME_MESH_PATH, _buildEdgeFrames());
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The withdrawal and Dock presentation composition is incomplete.");
            }
        }

        private static Mesh _buildAprons()
        {
            var builder = new MeshBuilder("WithdrawalDockAprons");
            builder.AddBox(new Vector3(-9.2f, -0.71f, -5.6f), new Vector3(9.6f, 0.42f, 9.6f));
            builder.AddBox(new Vector3(-5.8f, -0.71f, -2.9f), new Vector3(7.2f, 0.42f, 5.4f), -35f);
            return builder.Build();
        }

        private static Mesh _buildShadowBacks()
        {
            var builder = new MeshBuilder("WithdrawalDockShadowBacks");
            builder.AddBox(new Vector3(-14f, -0.66f, -5.6f), new Vector3(0.34f, 0.92f, 9.6f));
            builder.AddBox(new Vector3(-9.2f, -0.66f, -10.4f), new Vector3(9.6f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(-3.35f, -0.66f, -4.6f), new Vector3(0.34f, 0.92f, 4.8f), -35f);
            return builder.Build();
        }

        private static Mesh _buildEdgeFrames()
        {
            var builder = new MeshBuilder("WithdrawalDockEdgeFrames");
            _addBracePair(builder, new Vector3(-13.82f, -0.2f, -5.6f), Vector3.forward, 7.6f);
            _addBracePair(builder, new Vector3(-9.2f, -0.2f, -10.22f), Vector3.right, 7.6f);
            _addBracePair(builder, new Vector3(-3.52f, -0.2f, -4.55f),
                Quaternion.Euler(0f, -35f, 0f) * Vector3.forward, 3.6f);
            return builder.Build();
        }

        private static void _addBracePair(MeshBuilder builder, Vector3 center, Vector3 axis, float span)
        {
            var offset = axis.normalized * (span * 0.42f);
            builder.AddBox(center - offset, new Vector3(0.28f, 0.68f, 0.28f));
            builder.AddBox(center + offset, new Vector3(0.28f, 0.68f, 0.28f));
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject(COMPOSITION_NAME);
            try
            {
                var sections = new[]
                {
                    _addSection(root.transform, "Withdrawal and Dock Underdeck Aprons", APRON_MESH_PATH, APRON_MATERIAL_PATH),
                    _addSection(root.transform, "Withdrawal and Dock Shadow Backs", SHADOW_MESH_PATH, SHADOW_MATERIAL_PATH),
                    _addSection(root.transform, "Withdrawal and Dock Edge Frames", FRAME_MESH_PATH, FRAME_MATERIAL_PATH)
                };
                root.AddComponent<AuthoredWithdrawalDockComposition>().Configure(sections);
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

            var existing = environment.transform.Find(COMPOSITION_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the withdrawal and Dock composition in SampleScene.");
            }

            instance.name = COMPOSITION_NAME;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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
