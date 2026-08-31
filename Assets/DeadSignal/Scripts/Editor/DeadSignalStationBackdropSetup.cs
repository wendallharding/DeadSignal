using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationBackdropSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/StationUnderdeckAlbedo.png";
        private const string MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/StationUnderdeck.mat";
        private const string STRUCTURE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/StationUnderdeckRibs.mat";
        private const string STRUCTURE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/StationUnderdeckStructure.asset";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/StationUnderdeckBackdrop.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string BACKDROP_NAME = "Station Underdeck Backdrop";

        private static readonly Vector2 s_coverage = new(210f, 270f);

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var backdrop = prefab != null ? prefab.GetComponent<AuthoredStationBackdrop>() : null;
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(STRUCTURE_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(STRUCTURE_MESH_PATH) != null &&
                       backdrop != null && backdrop.Coverage == s_coverage &&
                       backdrop.StructureRenderers is { Length: 1 } &&
                       prefab.GetComponentInChildren<Collider>() == null;
            }
        }

        public static void EnsureAssets()
        {
            AssetDatabase.Refresh();
            _configureTextureImport();
            var material = _ensureMaterial();
            _ensureStructureMaterial();
            _ensureMesh(STRUCTURE_MESH_PATH, _buildStructure());
            _ensurePrefab(material);
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored station underdeck backdrop is incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the station underdeck texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material _ensureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/DeadSignal/Resources/Materials/RuntimeLitTemplate.mat");
                if (template == null)
                {
                    throw new InvalidOperationException("The runtime Lit material template is missing.");
                }

                material = new Material(template) { name = "StationUnderdeck" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(15f, 10f));
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", new Vector2(15f, 10f));
            material.SetColor("_BaseColor", new Color(0.46f, 0.5f, 0.56f, 1f));
            material.SetFloat("_Metallic", 0.18f);
            material.SetFloat("_Smoothness", 0.16f);
            material.SetColor("_EmissionColor", new Color(0.05f, 0.07f, 0.1f, 1f));
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _ensureStructureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(STRUCTURE_MATERIAL_PATH);
            if (material == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/DeadSignal/Resources/Materials/RuntimeLitTemplate.mat");
                if (template == null)
                {
                    throw new InvalidOperationException("The runtime Lit material template is missing.");
                }

                material = new Material(template) { name = "StationUnderdeckRibs" };
                AssetDatabase.CreateAsset(material, STRUCTURE_MATERIAL_PATH);
            }

            material.SetColor("_BaseColor", new Color(0.22f, 0.27f, 0.34f, 1f));
            material.SetFloat("_Metallic", 0.32f);
            material.SetFloat("_Smoothness", 0.22f);
            material.SetColor("_EmissionColor", new Color(0.035f, 0.055f, 0.08f, 1f));
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
        }

        private static void _ensurePrefab(Material material)
        {
            var instance = new GameObject(BACKDROP_NAME);
            try
            {
                var basePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                basePlane.name = "Recessed Underdeck Surface";
                basePlane.transform.SetParent(instance.transform, false);
                basePlane.transform.localPosition = new Vector3(0f, -1.1f, 0f);
                basePlane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                basePlane.transform.localScale = new Vector3(s_coverage.x, s_coverage.y, 1f);
                UnityEngine.Object.DestroyImmediate(basePlane.GetComponent<Collider>());
                var baseRenderer = basePlane.GetComponent<Renderer>();
                _configureRenderer(baseRenderer, material, false);

                var structure = new GameObject("Modular Underdeck Ribs", typeof(MeshFilter), typeof(MeshRenderer));
                structure.transform.SetParent(instance.transform, false);
                structure.GetComponent<MeshFilter>().sharedMesh =
                    AssetDatabase.LoadAssetAtPath<Mesh>(STRUCTURE_MESH_PATH);
                var structureRenderer = structure.GetComponent<MeshRenderer>();
                _configureRenderer(structureRenderer,
                    AssetDatabase.LoadAssetAtPath<Material>(STRUCTURE_MATERIAL_PATH), true);

                instance.AddComponent<AuthoredStationBackdrop>().Configure(s_coverage, new[] { structureRenderer });
                PrefabUtility.SaveAsPrefabAsset(instance, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static Mesh _buildStructure()
        {
            var builder = new MeshBuilder("StationUnderdeckStructure");
            const float y = -0.86f;
            const float depth = 0.18f;
            const float ribWidth = 0.28f;
            const float horizontalSpan = 198f;
            const float verticalSpan = 258f;

            for (var x = -96f; x <= 96f; x += 12f)
            {
                builder.AddBox(new Vector3(x, y, 0f), new Vector3(ribWidth, depth, verticalSpan));
            }

            for (var z = -126f; z <= 126f; z += 12f)
            {
                builder.AddBox(new Vector3(0f, y, z), new Vector3(horizontalSpan, depth, ribWidth));
            }

            for (var x = -90f; x <= 90f; x += 12f)
            {
                builder.AddBox(new Vector3(x, y - 0.02f, 0f),
                    new Vector3(ribWidth * 0.45f, depth * 0.65f, verticalSpan));
            }

            for (var z = -120f; z <= 120f; z += 12f)
            {
                builder.AddBox(new Vector3(0f, y - 0.02f, z),
                    new Vector3(horizontalSpan, depth * 0.65f, ribWidth * 0.45f));
            }

            return builder.Build();
        }

        private static void _configureRenderer(Renderer renderer, Material material, bool receiveShadows)
        {
            if (material == null)
            {
                throw new InvalidOperationException("The station underdeck material is missing.");
            }

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
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

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var environment = GameObject.Find(ENVIRONMENT_PATH);
            if (environment == null)
            {
                throw new InvalidOperationException($"The authored scene is missing {ENVIRONMENT_PATH}.");
            }

            var existing = environment.transform.Find(BACKDROP_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the station underdeck backdrop in SampleScene.");
            }

            instance.name = BACKDROP_NAME;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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
