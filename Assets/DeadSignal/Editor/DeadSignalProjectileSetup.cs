using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalProjectileSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltModel.fbx";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Projectiles/SignalBoltAssembly.prefab";
        private const string SHELL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalBoltShell.mat";
        private const string ENERGY_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalBoltEnergy.mat";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
            _hasModelPrefab() &&
            _hasMaterialAssignments();

        public static void EnsureAssets()
        {
            _configureTexture();
            _configureModel();
            _ensureMaterials();
            _ensurePrefab();
            _assignMaterials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored Signal bolt texture, model, materials, or prefab are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal bolt texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _configureModel()
        {
            var importer = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal bolt model at {MODEL_PATH}.");
            }

            importer.addCollider = false;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            _configureMaterial(
                SHELL_MATERIAL_PATH,
                "SignalBoltShell",
                new Color(0.78f, 0.82f, 0.82f),
                Color.black,
                0.36f,
                texture);
            _configureMaterial(
                ENERGY_MATERIAL_PATH,
                "SignalBoltEnergy",
                new Color(0.01f, 0.72f, 0.9f),
                new Color(0f, 2.2f, 3.1f),
                0.2f,
                null);
        }

        private static void _configureMaterial(
            string assetPath,
            string materialName,
            Color baseColor,
            Color emissionColor,
            float smoothness,
            Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            var isNewMaterial = material == null;
            if (isNewMaterial)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Signal bolt materials.");
                }

                material = new Material(shader);
                material.name = materialName;
                material.color = baseColor;
                material.SetColor("_BaseColor", baseColor);
                material.SetFloat("_Smoothness", smoothness);
                material.SetColor("_EmissionColor", emissionColor);
                if (emissionColor.maxColorComponent > 0f)
                {
                    material.EnableKeyword("_EMISSION");
                }

                AssetDatabase.CreateAsset(material, assetPath);
            }

            if (texture != null)
            {
                material.mainTexture = texture;
                material.SetTexture("_BaseMap", texture);
            }

            if (isNewMaterial || texture != null)
            {
                EditorUtility.SetDirty(material);
            }
        }

        private static void _ensurePrefab()
        {
            if (_hasModelPrefab())
            {
                return;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            var bolt = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (bolt == null)
            {
                throw new InvalidOperationException("Could not instantiate the imported Signal bolt model.");
            }

            bolt.name = "Signal Bolt Assembly";
            PrefabUtility.SaveAsPrefabAsset(bolt, PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(bolt);
        }

        private static void _assignMaterials()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                _assignMaterial(prefabRoot.transform, "Bolt Shell", SHELL_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Bolt Energy", ENERGY_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void _assignMaterial(Transform root, string partName, string materialPath)
        {
            var part = root.Find(partName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (part == null || material == null || !part.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not assign {materialPath} to Signal bolt part {partName}.");
            }

            renderer.sharedMaterial = material;
        }

        private static bool _hasModelPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            return prefab != null &&
                   _isImportedPart(prefab.transform.Find("Bolt Shell")) &&
                   _isImportedPart(prefab.transform.Find("Bolt Energy"));
        }

        private static bool _isImportedPart(Transform part)
        {
            return part != null &&
                   part.TryGetComponent<MeshFilter>(out var meshFilter) &&
                   meshFilter.sharedMesh != null &&
                   AssetDatabase.GetAssetPath(meshFilter.sharedMesh) == MODEL_PATH;
        }

        private static bool _hasMaterialAssignments()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            return prefab != null &&
                   _hasMaterial(prefab.transform.Find("Bolt Shell"), SHELL_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Bolt Energy"), ENERGY_MATERIAL_PATH);
        }

        private static bool _hasMaterial(Transform part, string materialPath)
        {
            return part != null &&
                   part.TryGetComponent<Renderer>(out var renderer) &&
                   renderer.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }
    }
}
