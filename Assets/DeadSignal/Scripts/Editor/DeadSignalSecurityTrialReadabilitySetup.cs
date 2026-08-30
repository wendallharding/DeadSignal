using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSecurityTrialReadabilitySetup
    {
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialWingRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialCommitmentStatusPanel.png";
        private const string MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialCommitmentStatusReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/SecurityTrialReadability";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/SecurityTrialCommitmentStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       region?.GetComponent<AuthoredCombatChamber>() is
                       { IsComplete: true, HasCommitmentReadabilityAssets: true };
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Security Trial Commitment Readability")]
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
                throw new InvalidOperationException("The Security Trial commitment readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Security Trial texture at {TEXTURE_PATH}.");
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
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "SecurityTrialReadability");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Security Trial readability.");
                }

                material = new Material(shader) { name = "SecurityTrialCommitmentStatus" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.055f));
            material.SetFloat("_Metallic", 0.34f);
            material.SetFloat("_Smoothness", 0.48f);
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
                mesh = new Mesh { name = "SecurityTrialCommitmentStatusReadability" };
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
                var chamber = root.GetComponent<AuthoredCombatChamber>();
                var commitmentSwitch = root.transform.Find("Commitment Room/Security Trial Breaker");
                if (chamber == null || commitmentSwitch == null)
                {
                    throw new InvalidOperationException("The Security Trial chamber or commitment breaker is missing.");
                }

                var glyph = commitmentSwitch.Find("Commitment Status");
                if (glyph == null)
                {
                    glyph = new GameObject("Commitment Status", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    glyph.SetParent(commitmentSwitch, false);
                }

                glyph.localPosition = new Vector3(0f, 0.58f, 0f);
                glyph.localRotation = Quaternion.identity;
                glyph.localScale = new Vector3(1.22f, 1f, 1.22f);
                glyph.GetComponent<MeshFilter>().sharedMesh = mesh;
                glyph.GetComponent<MeshRenderer>().sharedMaterial = material;
                foreach (var collider in glyph.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                chamber.ConfigureCommitmentReadability(new[] { glyph.GetComponent<Renderer>() }, glyph);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
