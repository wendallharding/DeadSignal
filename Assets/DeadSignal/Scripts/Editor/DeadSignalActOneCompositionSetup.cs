using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Editor
{
    public static class DeadSignalActOneCompositionSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/ActOneComposition.prefab";
        private const string APRON_MESH_PATH = "Assets/DeadSignal/Resources/Environment/ActOneUnderdeckAprons.asset";
        private const string SHADOW_MESH_PATH = "Assets/DeadSignal/Resources/Environment/ActOneShadowBacks.asset";
        private const string BRACE_MESH_PATH = "Assets/DeadSignal/Resources/Environment/ActOneCeramicBraces.asset";
        private const string APRON_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationSteel.mat";
        private const string SHADOW_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationBlack.mat";
        private const string BRACE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/MaintenanceBulkhead.mat";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string COMPOSITION_NAME = "Act I Station Composition";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var composition = prefab != null ? prefab.GetComponent<AuthoredActOneComposition>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(APRON_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SHADOW_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(BRACE_MESH_PATH) != null &&
                       composition != null && composition.IsConfigured &&
                       prefab.GetComponentsInChildren<Collider>(true).Length == 0;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Act I Presentation Composition")]
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
                throw new InvalidOperationException("The Act I presentation composition is incomplete.");
            }
        }

        private static Mesh _buildAprons()
        {
            var builder = new MeshBuilder("ActOneUnderdeckAprons");
            builder.AddBox(new Vector3(-0.6f, -0.72f, 0.4f), new Vector3(8.2f, 0.48f, 7.2f));
            builder.AddBox(new Vector3(-5.8f, -0.71f, 7.2f), new Vector3(7.2f, 0.42f, 5.8f));
            builder.AddBox(new Vector3(9.7f, -0.71f, 6.3f), new Vector3(5.8f, 0.42f, 5.4f));
            builder.AddBox(new Vector3(10.4f, -0.71f, -6.4f), new Vector3(6f, 0.42f, 5.8f));
            builder.AddBox(new Vector3(16.7f, -0.71f, 0f), new Vector3(7.4f, 0.42f, 7.3f));
            builder.AddBox(new Vector3(-3.35f, -0.73f, 4.7f), new Vector3(2.1f, 0.3f, 3.5f));
            builder.AddBox(new Vector3(5.6f, -0.73f, 4.55f), new Vector3(5.6f, 0.3f, 1.7f));
            builder.AddBox(new Vector3(6f, -0.73f, -4.6f), new Vector3(5.8f, 0.3f, 1.7f));
            builder.AddBox(new Vector3(13.45f, -0.73f, 0f), new Vector3(2.1f, 0.3f, 2.4f));
            return builder.Build();
        }

        private static Mesh _buildShadowBacks()
        {
            var builder = new MeshBuilder("ActOneShadowBacks");
            builder.AddBox(new Vector3(-4.85f, -0.66f, 0.4f), new Vector3(0.34f, 0.92f, 7.2f));
            builder.AddBox(new Vector3(-5.8f, -0.66f, 10.15f), new Vector3(7.2f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(-9.55f, -0.66f, 7.2f), new Vector3(0.34f, 0.92f, 5.8f));
            builder.AddBox(new Vector3(9.7f, -0.66f, 9.15f), new Vector3(5.8f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(12.75f, -0.66f, 6.3f), new Vector3(0.34f, 0.92f, 5.4f));
            builder.AddBox(new Vector3(10.4f, -0.66f, -9.45f), new Vector3(6f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(13.55f, -0.66f, -6.4f), new Vector3(0.34f, 0.92f, 5.8f));
            builder.AddBox(new Vector3(16.7f, -0.66f, 3.8f), new Vector3(7.4f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(16.7f, -0.66f, -3.8f), new Vector3(7.4f, 0.92f, 0.34f));
            builder.AddBox(new Vector3(20.55f, -0.66f, 0f), new Vector3(0.34f, 0.92f, 7.3f));
            return builder.Build();
        }

        private static Mesh _buildCeramicBraces()
        {
            var builder = new MeshBuilder("ActOneCeramicBraces");
            _addBracePair(builder, new Vector3(-5.8f, -0.2f, 10.13f), new Vector3(5.7f, 0.12f, 0.18f));
            _addBracePair(builder, new Vector3(9.7f, -0.2f, 9.13f), new Vector3(4.4f, 0.12f, 0.18f));
            _addBracePair(builder, new Vector3(10.4f, -0.2f, -9.43f), new Vector3(4.6f, 0.12f, 0.18f));
            _addBracePair(builder, new Vector3(16.7f, -0.2f, 3.78f), new Vector3(5.6f, 0.12f, 0.18f));
            _addBracePair(builder, new Vector3(16.7f, -0.2f, -3.78f), new Vector3(5.6f, 0.12f, 0.18f));
            return builder.Build();
        }

        private static void _addBracePair(MeshBuilder builder, Vector3 center, Vector3 span)
        {
            var halfSpan = span.x * 0.5f;
            builder.AddBox(center + Vector3.left * halfSpan, new Vector3(0.18f, 0.65f, span.z));
            builder.AddBox(center + Vector3.right * halfSpan, new Vector3(0.18f, 0.65f, span.z));
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
                root.AddComponent<AuthoredActOneComposition>().Configure(sections);
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
                throw new InvalidOperationException("Could not place the Act I presentation composition in SampleScene.");
            }

            instance.name = COMPOSITION_NAME;
            _resizeCentralInstallationMarkers();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _resizeCentralInstallationMarkers()
        {
            var installation = UnityEngine.Object.FindFirstObjectByType<AuthoredCentralInstallationObjective>(
                FindObjectsInactive.Include);
            if (installation == null)
            {
                throw new InvalidOperationException("The authored scene is missing Central installation markers.");
            }

            foreach (var markerName in new[] { "Central Payload Install Available", "Central Payload Installed" })
            {
                var marker = installation.transform.Find(markerName);
                if (marker == null)
                {
                    throw new InvalidOperationException($"The Central installation is missing {markerName}.");
                }

                _resizeRail(marker, "North Rail", new Vector3(0f, 0.08f, 0.82f), new Vector3(1.85f, 0.08f, 0.1f));
                _resizeRail(marker, "South Rail", new Vector3(0f, 0.08f, -0.82f), new Vector3(1.85f, 0.08f, 0.1f));
                _resizeRail(marker, "East Rail", new Vector3(0.82f, 0.08f, 0f), new Vector3(0.1f, 0.08f, 1.85f));
                _resizeRail(marker, "West Rail", new Vector3(-0.82f, 0.08f, 0f), new Vector3(0.1f, 0.08f, 1.85f));
            }
        }

        private static void _resizeRail(Transform parent, string railName, Vector3 position, Vector3 scale)
        {
            var rail = parent.Find(railName);
            if (rail == null)
            {
                throw new InvalidOperationException($"The Central installation is missing {parent.name}/{railName}.");
            }

            rail.localPosition = position;
            rail.localScale = scale;
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
