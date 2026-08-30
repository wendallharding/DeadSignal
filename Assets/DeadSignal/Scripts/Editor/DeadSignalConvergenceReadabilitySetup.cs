using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalConvergenceReadabilitySetup
    {
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceCalibrationStatusPanel.png";
        private const string MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceCalibrationStatusReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/ConvergenceChamber";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/ConvergenceCalibrationStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       region?.GetComponent<AuthoredConvergenceCalibrationObjective>() is
                       { IsConfigured: true, HasReadabilityAssets: true };
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Convergence Calibration Readability")]
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
                throw new InvalidOperationException("The Convergence calibration readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the calibration status texture at {TEXTURE_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "ConvergenceChamber");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for calibration readability.");
                }

                material = new Material(shader) { name = "ConvergenceCalibrationStatus" };
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
                mesh = new Mesh { name = "ConvergenceCalibrationStatusReadability" };
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
                var objective = root.GetComponent<AuthoredConvergenceCalibrationObjective>();
                var console = root.transform.Find("Convergence Calibration Console");
                if (objective == null || console == null)
                {
                    throw new InvalidOperationException("The Convergence calibration authority is missing.");
                }

                var status = console.Find("Convergence Calibration Status");
                if (status == null)
                {
                    status = new GameObject(
                        "Convergence Calibration Status",
                        typeof(MeshFilter),
                        typeof(MeshRenderer)).transform;
                    status.SetParent(console, false);
                }

                status.localPosition = new Vector3(0f, 0.515f, 0f);
                status.localRotation = Quaternion.identity;
                status.localScale = new Vector3(0.72f, 1f, 0.72f);
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
