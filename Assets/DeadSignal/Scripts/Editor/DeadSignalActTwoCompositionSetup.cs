using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalActTwoCompositionSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/ActTwoComposition.prefab";
        private const string APRON_MESH_PATH = "Assets/DeadSignal/Resources/Environment/ActTwoUnderdeckAprons.asset";
        private const string SHADOW_MESH_PATH = "Assets/DeadSignal/Resources/Environment/ActTwoShadowBacks.asset";
        private const string BRACE_MESH_PATH = "Assets/DeadSignal/Resources/Environment/ActTwoCeramicBraces.asset";
        private const string APRON_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationSteel.mat";
        private const string SHADOW_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationBlack.mat";
        private const string BRACE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/MaintenanceBulkhead.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string COMPOSITION_NAME = "Act II Station Composition";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var composition = prefab != null ? prefab.GetComponent<AuthoredActTwoComposition>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(APRON_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SHADOW_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(BRACE_MESH_PATH) != null &&
                       composition != null && composition.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Act II Presentation Composition")]
        public static void EnsureAssets()
        {
            _ensureMesh(APRON_MESH_PATH, _buildAprons());
            _ensureMesh(SHADOW_MESH_PATH, _buildShadowBacks());
            _ensureMesh(BRACE_MESH_PATH, _buildCeramicBraces());
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Act II presentation composition is incomplete.");
            }
        }

        private static Mesh _buildAprons()
        {
            var builder = new MeshBuilder("ActTwoUnderdeckAprons");
            builder.AddBox(new Vector3(27.5f, -0.72f, 0f), new Vector3(16.6f, 0.48f, 14.6f));
            builder.AddBox(new Vector3(27.5f, -0.71f, -11.25f), new Vector3(12.6f, 0.42f, 9.1f));
            builder.AddBox(new Vector3(42.5f, -0.71f, 0f), new Vector3(14.6f, 0.42f, 10.6f));
            builder.AddBox(new Vector3(42.5f, -0.71f, -8f), new Vector3(10.6f, 0.42f, 6.6f));
            builder.AddBox(new Vector3(35.5f, -0.73f, 0f), new Vector3(1.6f, 0.3f, 3.1f));
            builder.AddBox(new Vector3(27.5f, -0.73f, -7f), new Vector3(4.7f, 0.3f, 1.4f));
            builder.AddBox(new Vector3(42.5f, -0.73f, -5f), new Vector3(3.1f, 0.3f, 1.4f));
            return builder.Build();
        }

        private static Mesh _buildShadowBacks()
        {
            var builder = new MeshBuilder("ActTwoShadowBacks");
            builder.AddBox(new Vector3(27.5f, -0.66f, 7.3f), new Vector3(16.6f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(19.2f, -0.66f, 0f), new Vector3(0.34f, 0.92f, 14.6f));
            builder.AddBox(new Vector3(27.5f, -0.66f, -15.8f), new Vector3(12.6f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(21.2f, -0.66f, -11.25f), new Vector3(0.34f, 0.92f, 9.1f));
            builder.AddBox(new Vector3(33.8f, -0.66f, -11.25f), new Vector3(0.34f, 0.92f, 9.1f));
            builder.AddBox(new Vector3(42.5f, -0.66f, 5.3f), new Vector3(14.6f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(49.8f, -0.66f, 0f), new Vector3(0.34f, 0.92f, 10.6f));
            builder.AddBox(new Vector3(42.5f, -0.66f, -11.3f), new Vector3(10.6f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(37.2f, -0.66f, -8f), new Vector3(0.34f, 0.92f, 6.6f));
            builder.AddBox(new Vector3(47.8f, -0.66f, -8f), new Vector3(0.34f, 0.92f, 6.6f));
            return builder.Build();
        }

        private static Mesh _buildCeramicBraces()
        {
            var builder = new MeshBuilder("ActTwoCeramicBraces");
            _addHorizontalBracePair(builder, new Vector3(27.5f, -0.2f, 7.28f), 12.8f);
            _addHorizontalBracePair(builder, new Vector3(27.5f, -0.2f, -15.78f), 9.2f);
            _addHorizontalBracePair(builder, new Vector3(42.5f, -0.2f, 5.28f), 10.8f);
            _addHorizontalBracePair(builder, new Vector3(42.5f, -0.2f, -11.28f), 7.6f);
            _addVerticalBracePair(builder, new Vector3(19.22f, -0.2f, 0f), 10.8f);
            _addVerticalBracePair(builder, new Vector3(49.78f, -0.2f, 0f), 7.4f);
            return builder.Build();
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
                    _addSection(root.transform, "Underdeck Aprons", APRON_MESH_PATH, APRON_MATERIAL_PATH),
                    _addSection(root.transform, "Shadow Backs", SHADOW_MESH_PATH, SHADOW_MATERIAL_PATH),
                    _addSection(root.transform, "Ceramic Braces", BRACE_MESH_PATH, BRACE_MATERIAL_PATH)
                };
                root.AddComponent<AuthoredActTwoComposition>().Configure(sections);
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
                throw new InvalidOperationException("Could not place the Act II presentation composition in SampleScene.");
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
