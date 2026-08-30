using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSpineCoreReadabilitySetup
    {
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineCoreInstallationStatusPanel.png";
        private const string MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineCoreInstallationStatusReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/CapacitorSpine";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/SpineCoreInstallationStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       region?.GetComponent<AuthoredSpineCoreInstallationObjective>() is
                       { IsConfigured: true, HasReadabilityAssets: true } &&
                       region.transform.Find("Spine Core Installation Status") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Spine Core Installation Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            var mesh = _ensureMesh();
            var material = _ensureMaterial();
            _upgradeRegion(mesh, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Spine core installation readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Spine core status texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Mesh _ensureMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = "SpineCoreInstallationStatusReadability" };
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

        private static Material _ensureMaterial()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "CapacitorSpine");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Spine core readability.");
                }

                material = new Material(shader) { name = "SpineCoreInstallationStatus" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.055f));
            material.SetFloat("_Metallic", 0.38f);
            material.SetFloat("_Smoothness", 0.46f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.08f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _upgradeRegion(Mesh mesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(REGION_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredSpineCoreInstallationObjective>();
                if (objective == null)
                {
                    throw new InvalidOperationException("The Spine core installation objective is missing.");
                }

                var status = root.transform.Find("Spine Core Installation Status");
                if (status == null)
                {
                    status = new GameObject(
                        "Spine Core Installation Status", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    status.SetParent(root.transform, false);
                }

                status.localPosition = new Vector3(5f, 0.075f, -2.05f);
                status.localRotation = Quaternion.identity;
                status.localScale = new Vector3(1.15f, 1f, 1.15f);
                status.GetComponent<MeshFilter>().sharedMesh = mesh;
                status.GetComponent<MeshRenderer>().sharedMaterial = material;
                foreach (var collider in status.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                objective.ConfigureReadability(status.GetComponent<Renderer>(), status);
                EditorUtility.SetDirty(objective);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
