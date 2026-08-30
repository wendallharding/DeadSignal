using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalCoreProcessingReadabilitySetup
    {
        private const string FURNACE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRegion.prefab";
        private const string QUENCH_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchLoopRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/CoreProcessingStatusPanel.png";
        private const string FURNACE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/FurnaceForgeStatusReadability.asset";
        private const string QUENCH_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/QuenchStabilizationStatusReadability.asset";
        private const string THRESHOLD_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RouteDoorThresholdReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/CoreProcessingReadability";
        private const string STATUS_MATERIAL_PATH = MATERIAL_FOLDER + "/CoreProcessingStatus.mat";
        private const string THRESHOLD_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RouteDoorThresholdStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var furnace = AssetDatabase.LoadAssetAtPath<GameObject>(FURNACE_PREFAB_PATH);
                var quench = AssetDatabase.LoadAssetAtPath<GameObject>(QUENCH_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(FURNACE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(QUENCH_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH) != null &&
                       furnace?.GetComponent<AuthoredFurnaceForgeObjective>() is
                       { IsConfigured: true, HasReadabilityAssets: true } &&
                       quench?.GetComponent<AuthoredQuenchStabilizationObjective>() is
                       { IsConfigured: true, HasReadabilityAssets: true } &&
                       quench.GetComponent<AuthoredRouteDoorReadability>()?.IsConfigured == true;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Core Processing Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            var material = _ensureMaterial();
            var furnaceMesh = _ensureGlyphMesh(FURNACE_MESH_PATH, "FurnaceForgeStatusReadability", 0f, 0.5f);
            var quenchMesh = _ensureGlyphMesh(QUENCH_MESH_PATH, "QuenchStabilizationStatusReadability", 0.5f, 1f);
            _upgradeFurnace(furnaceMesh, material);
            _upgradeQuench(quenchMesh, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The core-processing readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the core-processing texture at {TEXTURE_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "CoreProcessingReadability");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for core-processing readability.");
                }

                material = new Material(shader) { name = "CoreProcessingStatus" };
                AssetDatabase.CreateAsset(material, STATUS_MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.055f));
            material.SetFloat("_Metallic", 0.3f);
            material.SetFloat("_Smoothness", 0.44f);
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

        private static void _upgradeFurnace(Mesh mesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(FURNACE_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredFurnaceForgeObjective>();
                if (objective == null || root.transform.Find("Arc Furnace Assembly") == null)
                {
                    throw new InvalidOperationException("The Arc Furnace objective or assembly is missing.");
                }

                var glyph = _ensureGlyph(root.transform, "Furnace Forge Status", new Vector3(0f, 0.19f, -1.2f), mesh, material);
                glyph.localScale = new Vector3(1.55f, 1f, 1.55f);
                objective.ConfigureReadability(new[] { glyph.GetComponent<Renderer>() }, glyph);
                PrefabUtility.SaveAsPrefabAsset(root, FURNACE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeQuench(Mesh mesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(QUENCH_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredQuenchStabilizationObjective>();
                var slab = root.transform.Find("Quench Pressure Shutter")?.gameObject;
                var openMarker = root.transform.Find("Quench Cache Return Signal")?.gameObject;
                if (objective == null || slab == null || openMarker == null)
                {
                    throw new InvalidOperationException("The Quench objective, shutter, or return signal is missing.");
                }

                var glyph = _ensureGlyph(root.transform, "Quench Stabilization Status", new Vector3(1.45f, 0.19f, 0f), mesh, material);
                glyph.localScale = new Vector3(1.45f, 1f, 1.45f);
                objective.ConfigureReadability(new[] { glyph.GetComponent<Renderer>() }, glyph);

                var thresholdMesh = AssetDatabase.LoadAssetAtPath<Mesh>(THRESHOLD_MESH_PATH);
                var thresholdMaterial = AssetDatabase.LoadAssetAtPath<Material>(THRESHOLD_MATERIAL_PATH);
                if (thresholdMesh == null || thresholdMaterial == null)
                {
                    throw new InvalidOperationException("The established route-door threshold assets are missing.");
                }

                var threshold = _ensureGlyph(
                    root.transform,
                    "Quench Pressure Threshold",
                    new Vector3(-1.25f, 0f, 0f),
                    thresholdMesh,
                    thresholdMaterial);
                threshold.localRotation = Quaternion.Euler(0f, 90f, 0f);
                threshold.localScale = new Vector3(1f, 1f, 1.42f);
                var readability = root.GetComponent<AuthoredRouteDoorReadability>() ??
                                  root.AddComponent<AuthoredRouteDoorReadability>();
                readability.Configure(slab, openMarker, threshold.GetComponent<Renderer>());
                PrefabUtility.SaveAsPrefabAsset(root, QUENCH_PREFAB_PATH);
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
