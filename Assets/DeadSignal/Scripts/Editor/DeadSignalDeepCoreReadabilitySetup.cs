using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalDeepCoreReadabilitySetup
    {
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string FLUX_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/FluxBypassRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/DeepCoreMachineryStatusPanel.png";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/DeepCoreReadability";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/DeepCoreMachineryStatus.mat";
        private const string INDUCTION_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/InductionChargeGlyphReadability.asset";
        private const string FLUX_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/FluxShuntGlyphReadability.asset";

        public static bool HasAssets
        {
            get
            {
                var gallery = AssetDatabase.LoadAssetAtPath<GameObject>(GALLERY_PREFAB_PATH);
                var flux = AssetDatabase.LoadAssetAtPath<GameObject>(FLUX_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(INDUCTION_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(FLUX_MESH_PATH) != null &&
                       gallery?.GetComponent<AuthoredInductionLatticeObjective>()?.HasReadabilityAssets == true &&
                       flux?.GetComponent<AuthoredFluxShuntObjective>()?.HasReadabilityAssets == true;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Deep Core Machinery Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            var material = _ensureMaterial();
            var inductionMesh = _ensureGlyphMesh(INDUCTION_MESH_PATH, "InductionChargeGlyphReadability", 0f, 0.5f);
            var fluxMesh = _ensureGlyphMesh(FLUX_MESH_PATH, "FluxShuntGlyphReadability", 0.5f, 1f);
            _upgradeInduction(inductionMesh, material);
            _upgradeFlux(fluxMesh, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The deep-core machinery readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the deep-core status texture at {TEXTURE_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "DeepCoreReadability");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for deep-core readability.");
                }

                material = new Material(shader) { name = "DeepCoreMachineryStatus" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.055f));
            material.SetFloat("_Metallic", 0.28f);
            material.SetFloat("_Smoothness", 0.42f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.08f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh _ensureGlyphMesh(string path, string name, float minimumU, float maximumU)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = name };
            }

            mesh.vertices = new[]
            {
                new Vector3(-0.72f, 0f, -0.5f), new Vector3(-0.5f, 0f, -0.72f),
                new Vector3(0.5f, 0f, -0.72f), new Vector3(0.72f, 0f, -0.5f),
                new Vector3(0.72f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.72f),
                new Vector3(-0.5f, 0f, 0.72f), new Vector3(-0.72f, 0f, 0.5f)
            };
            var middleU = (minimumU + maximumU) * 0.5f;
            mesh.uv = new[]
            {
                new Vector2(minimumU, 0.15f), new Vector2(Mathf.Lerp(minimumU, middleU, 0.3f), 0f),
                new Vector2(Mathf.Lerp(middleU, maximumU, 0.7f), 0f), new Vector2(maximumU, 0.15f),
                new Vector2(maximumU, 0.85f), new Vector2(Mathf.Lerp(middleU, maximumU, 0.7f), 1f),
                new Vector2(Mathf.Lerp(minimumU, middleU, 0.3f), 1f), new Vector2(minimumU, 0.85f)
            };
            mesh.triangles = new[] { 0, 7, 6, 0, 6, 1, 1, 6, 2, 2, 6, 5, 2, 5, 3, 3, 5, 4 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (createAsset)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static void _upgradeInduction(Mesh mesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(GALLERY_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredInductionLatticeObjective>();
                var parent = root.transform.Find("Induction Lattice Objective");
                if (objective == null || parent == null)
                {
                    throw new InvalidOperationException("The Induction lattice authority is missing.");
                }

                var glyph = _ensureGlyph(parent, "Induction Charge Status", new Vector3(0f, 0.065f, -0.95f), mesh, material);
                glyph.localScale = new Vector3(1.35f, 1f, 1.35f);
                objective.ConfigureReadability(new[] { glyph.GetComponent<Renderer>() }, glyph);
                PrefabUtility.SaveAsPrefabAsset(root, GALLERY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeFlux(Mesh mesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(FLUX_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredFluxShuntObjective>();
                if (objective == null)
                {
                    throw new InvalidOperationException("The Flux shunt authority is missing.");
                }

                var glyph = _ensureGlyph(root.transform, "Flux Shunt Route Status", new Vector3(-0.25f, 0.065f, 0.75f), mesh, material);
                glyph.localScale = new Vector3(1.45f, 1f, 1.45f);
                objective.ConfigureReadability(new[] { glyph.GetComponent<Renderer>() }, glyph);
                PrefabUtility.SaveAsPrefabAsset(root, FLUX_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform _ensureGlyph(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Mesh mesh,
            Material material)
        {
            var glyph = parent.Find(objectName);
            if (glyph == null)
            {
                glyph = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                glyph.SetParent(parent, false);
            }

            glyph.localPosition = localPosition;
            glyph.localRotation = Quaternion.identity;
            glyph.localScale = Vector3.one;
            glyph.GetComponent<MeshFilter>().sharedMesh = mesh;
            glyph.GetComponent<MeshRenderer>().sharedMaterial = material;
            foreach (var collider in glyph.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return glyph;
        }
    }
}
