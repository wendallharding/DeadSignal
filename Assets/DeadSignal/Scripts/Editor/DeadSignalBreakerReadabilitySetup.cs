using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalBreakerReadabilitySetup
    {
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBreakerGalleryRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/BreakerDistributionStatusPanel.png";
        private const string MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/BreakerDistributionStatusReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/ConvergenceBreakerGallery";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/BreakerDistributionStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       region?.GetComponent<AuthoredBreakerResetObjective>() is
                       { IsConfigured: true, HasReadabilityAssets: true };
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Breaker Distribution Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            var material = _ensureMaterial();
            var mesh = _ensureMesh();
            _upgradeRegion(mesh, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Breaker distribution readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Breaker status texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material _ensureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Breaker readability.");
                }

                material = new Material(shader) { name = "BreakerDistributionStatus" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.055f));
            material.SetFloat("_Metallic", 0.32f);
            material.SetFloat("_Smoothness", 0.46f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.08f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh _ensureMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = "BreakerDistributionStatusReadability" };
            }

            mesh.vertices = new[]
            {
                new Vector3(-0.72f, 0f, -0.5f), new Vector3(-0.5f, 0f, -0.72f),
                new Vector3(0.5f, 0f, -0.72f), new Vector3(0.72f, 0f, -0.5f),
                new Vector3(0.72f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.72f),
                new Vector3(-0.5f, 0f, 0.72f), new Vector3(-0.72f, 0f, 0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0.15f), new Vector2(0.15f, 0f), new Vector2(0.85f, 0f), new Vector2(1f, 0.15f),
                new Vector2(1f, 0.85f), new Vector2(0.85f, 1f), new Vector2(0.15f, 1f), new Vector2(0f, 0.85f)
            };
            mesh.triangles = new[] { 0, 7, 6, 0, 6, 1, 1, 6, 2, 2, 6, 5, 2, 5, 3, 3, 5, 4 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (createAsset)
            {
                AssetDatabase.CreateAsset(mesh, MESH_PATH);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static void _upgradeRegion(Mesh mesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(REGION_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredBreakerResetObjective>();
                if (objective == null || root.transform.Find("Breaker Bank Assembly") == null)
                {
                    throw new InvalidOperationException("The Breaker reset authority or bank is missing.");
                }

                var status = root.transform.Find("Breaker Distribution Status");
                if (status == null)
                {
                    status = new GameObject(
                        "Breaker Distribution Status", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    status.SetParent(root.transform, false);
                }

                status.localPosition = new Vector3(-0.55f, 0.255f, 0f);
                status.localRotation = Quaternion.identity;
                status.localScale = new Vector3(0.78f, 1f, 0.78f);
                status.GetComponent<MeshFilter>().sharedMesh = mesh;
                status.GetComponent<MeshRenderer>().sharedMaterial = material;
                foreach (var collider in status.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                objective.ConfigureReadability(new[] { status.GetComponent<Renderer>() }, status);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

    }
}
