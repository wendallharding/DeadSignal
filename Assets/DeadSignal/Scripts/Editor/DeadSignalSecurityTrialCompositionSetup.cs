using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalSecurityTrialCompositionSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SecurityTrialComposition.prefab";
        private const string APRON_MESH_PATH = "Assets/DeadSignal/Resources/Environment/SecurityTrialAprons.asset";
        private const string SHADOW_MESH_PATH = "Assets/DeadSignal/Resources/Environment/SecurityTrialShadowBacks.asset";
        private const string FRAME_MESH_PATH = "Assets/DeadSignal/Resources/Environment/SecurityTrialThresholdFrames.asset";
        private const string APRON_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/WorldPalette/StationSteel.mat";
        private const string SHADOW_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/WorldPalette/StationBlack.mat";
        private const string FRAME_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/MaintenanceBulkhead.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string COMPOSITION_NAME = "Security Trial Composition";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var composition = prefab != null ? prefab.GetComponent<AuthoredSecurityTrialComposition>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(APRON_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SHADOW_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(FRAME_MESH_PATH) != null &&
                       composition != null && composition.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Security Trial Presentation Composition")]
        public static void EnsureAssets()
        {
            _ensureMesh(APRON_MESH_PATH, _buildAprons());
            _ensureMesh(SHADOW_MESH_PATH, _buildShadowBacks());
            _ensureMesh(FRAME_MESH_PATH, _buildThresholdFrames());
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Security Trial presentation composition is incomplete.");
            }
        }

        private static Mesh _buildAprons()
        {
            var builder = new MeshBuilder("SecurityTrialAprons");
            builder.AddBox(new Vector3(42.5f, -0.71f, 33f), new Vector3(8.8f, 0.42f, 6.8f));
            builder.AddBox(new Vector3(42.5f, -0.71f, 54f), new Vector3(35.8f, 0.42f, 36.8f));
            builder.AddBox(new Vector3(42.5f, -0.71f, 75f), new Vector3(8.8f, 0.42f, 6.8f));
            return builder.Build();
        }

        private static Mesh _buildShadowBacks()
        {
            var builder = new MeshBuilder("SecurityTrialShadowBacks");
            builder.AddBox(new Vector3(38.1f, -0.66f, 33f), new Vector3(0.34f, 0.92f, 6.8f));
            builder.AddBox(new Vector3(46.9f, -0.66f, 33f), new Vector3(0.34f, 0.92f, 6.8f));
            builder.AddBox(new Vector3(24.6f, -0.66f, 54f), new Vector3(0.34f, 0.92f, 36.8f));
            builder.AddBox(new Vector3(60.4f, -0.66f, 54f), new Vector3(0.34f, 0.92f, 36.8f));
            builder.AddBox(new Vector3(38.1f, -0.66f, 75f), new Vector3(0.34f, 0.92f, 6.8f));
            builder.AddBox(new Vector3(46.9f, -0.66f, 75f), new Vector3(0.34f, 0.92f, 6.8f));
            builder.AddBox(new Vector3(42.5f, -0.66f, 78.4f), new Vector3(8.8f, 0.92f, 0.34f));
            return builder.Build();
        }

        private static Mesh _buildThresholdFrames()
        {
            var builder = new MeshBuilder("SecurityTrialThresholdFrames");
            _addThresholdFrame(builder, 36f);
            _addThresholdFrame(builder, 72f);
            _addVerticalBracePair(builder, new Vector3(24.62f, -0.2f, 54f), 30f);
            _addVerticalBracePair(builder, new Vector3(60.38f, -0.2f, 54f), 30f);
            _addHorizontalBracePair(builder, new Vector3(42.5f, -0.2f, 78.38f), 6.8f);
            return builder.Build();
        }

        private static void _addThresholdFrame(MeshBuilder builder, float z)
        {
            builder.AddBox(new Vector3(40.65f, -0.14f, z), new Vector3(0.22f, 0.58f, 0.24f));
            builder.AddBox(new Vector3(44.35f, -0.14f, z), new Vector3(0.22f, 0.58f, 0.24f));
        }

        private static void _addHorizontalBracePair(MeshBuilder builder, Vector3 center, float span)
        {
            var offset = span * 0.42f;
            builder.AddBox(center + Vector3.left * offset, new Vector3(0.2f, 0.65f, 0.18f));
            builder.AddBox(center + Vector3.right * offset, new Vector3(0.2f, 0.65f, 0.18f));
        }

        private static void _addVerticalBracePair(MeshBuilder builder, Vector3 center, float span)
        {
            var offset = span * 0.42f;
            builder.AddBox(center + Vector3.back * offset, new Vector3(0.18f, 0.65f, 0.2f));
            builder.AddBox(center + Vector3.forward * offset, new Vector3(0.18f, 0.65f, 0.2f));
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject(COMPOSITION_NAME);
            try
            {
                var sections = new[]
                {
                    _addSection(root.transform, "Security Trial Underdeck Aprons", APRON_MESH_PATH, APRON_MATERIAL_PATH),
                    _addSection(root.transform, "Security Trial Shadow Backs", SHADOW_MESH_PATH, SHADOW_MATERIAL_PATH),
                    _addSection(root.transform, "Security Trial Threshold Frames", FRAME_MESH_PATH, FRAME_MATERIAL_PATH)
                };
                root.AddComponent<AuthoredSecurityTrialComposition>().Configure(sections);
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
                throw new InvalidOperationException("Could not place the Security Trial composition in SampleScene.");
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

            public void AddBox(Vector3 center, Vector3 size)
            {
                var half = size * 0.5f;
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
                    m_vertices.Add(center + corner);
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
