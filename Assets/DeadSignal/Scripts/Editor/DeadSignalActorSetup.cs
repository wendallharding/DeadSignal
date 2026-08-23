using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalActorSetup
    {
        private const string SECURITY_WARDEN_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Actors/SecurityWardenArmorAlbedo.png";
        private const string SECURITY_WARDEN_MODEL_PATH =
            "Assets/DeadSignal/Resources/Actors/SecurityWardenModel.fbx";
        private const string SECURITY_WARDEN_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Actors/SecurityWardenAssembly.prefab";
        private const string SECURITY_WARDEN_ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SecurityWardenArmor.mat";
        private const string SECURITY_WARDEN_EYE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SecurityWardenEye.mat";
        private const string SECURITY_WARDEN_CROWN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SecurityWardenCrown.mat";
        private const string SIGNAL_SAPPER_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Actors/SignalSapperArmorAlbedo.png";
        private const string SIGNAL_SAPPER_MODEL_PATH =
            "Assets/DeadSignal/Resources/Actors/SignalSapperModel.fbx";
        private const string SIGNAL_SAPPER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Actors/SignalSapperAssembly.prefab";
        private const string SIGNAL_SAPPER_ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SignalSapperArmor.mat";
        private const string SIGNAL_SAPPER_FORK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SignalSapperFork.mat";
        private const string SIGNAL_SAPPER_CORE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/SignalSapperCore.mat";

        public static bool HasSecurityWardenAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(SECURITY_WARDEN_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_MODEL_PATH) != null &&
            _hasSecurityWardenModelPrefab() &&
            _hasSecurityWardenMaterialAssignments();

        public static bool HasSignalSapperAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(SIGNAL_SAPPER_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(SIGNAL_SAPPER_MODEL_PATH) != null &&
            _hasSignalSapperModelPrefab() &&
            _hasSignalSapperMaterialAssignments();

        public static void EnsureSecurityWardenAssets()
        {
            var textureImporter = AssetImporter.GetAtPath(SECURITY_WARDEN_TEXTURE_PATH) as TextureImporter;
            if (textureImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Security Warden texture at {SECURITY_WARDEN_TEXTURE_PATH}.");
            }

            textureImporter.alphaIsTransparency = false;
            textureImporter.mipmapEnabled = true;
            textureImporter.maxTextureSize = 1024;
            textureImporter.wrapMode = TextureWrapMode.Repeat;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.SaveAndReimport();

            var modelImporter = AssetImporter.GetAtPath(SECURITY_WARDEN_MODEL_PATH) as ModelImporter;
            if (modelImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Security Warden model at {SECURITY_WARDEN_MODEL_PATH}.");
            }

            modelImporter.addCollider = false;
            modelImporter.importAnimation = false;
            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            modelImporter.meshCompression = ModelImporterMeshCompression.Low;
            modelImporter.optimizeMeshPolygons = true;
            modelImporter.optimizeMeshVertices = true;
            modelImporter.SaveAndReimport();

            _ensureSecurityWardenMaterials(AssetDatabase.LoadAssetAtPath<Texture2D>(SECURITY_WARDEN_TEXTURE_PATH));
            if (!_hasSecurityWardenModelPrefab())
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_MODEL_PATH);
                var warden = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (warden == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported Security Warden model.");
                }

                warden.name = "Security Warden Assembly";
                PrefabUtility.SaveAsPrefabAsset(warden, SECURITY_WARDEN_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(warden);
            }

            _assignSecurityWardenMaterials();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasSecurityWardenAssets)
            {
                throw new InvalidOperationException("The Security Warden model, materials, and prefab were not created successfully.");
            }
        }

        public static void EnsureSignalSapperAssets()
        {
            var textureImporter = AssetImporter.GetAtPath(SIGNAL_SAPPER_TEXTURE_PATH) as TextureImporter;
            if (textureImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Signal Sapper texture at {SIGNAL_SAPPER_TEXTURE_PATH}.");
            }

            textureImporter.alphaIsTransparency = false;
            textureImporter.mipmapEnabled = true;
            textureImporter.maxTextureSize = 1024;
            textureImporter.wrapMode = TextureWrapMode.Repeat;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.SaveAndReimport();

            var modelImporter = AssetImporter.GetAtPath(SIGNAL_SAPPER_MODEL_PATH) as ModelImporter;
            if (modelImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Signal Sapper model at {SIGNAL_SAPPER_MODEL_PATH}.");
            }

            modelImporter.addCollider = false;
            modelImporter.importAnimation = false;
            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            modelImporter.meshCompression = ModelImporterMeshCompression.Low;
            modelImporter.optimizeMeshPolygons = true;
            modelImporter.optimizeMeshVertices = true;
            modelImporter.SaveAndReimport();

            _ensureSignalSapperMaterials(AssetDatabase.LoadAssetAtPath<Texture2D>(SIGNAL_SAPPER_TEXTURE_PATH));
            if (!_hasSignalSapperModelPrefab())
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(SIGNAL_SAPPER_MODEL_PATH);
                var sapper = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (sapper == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported Signal Sapper model.");
                }

                sapper.name = "Signal Sapper Assembly";
                PrefabUtility.SaveAsPrefabAsset(sapper, SIGNAL_SAPPER_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(sapper);
            }

            _assignSignalSapperMaterials();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasSignalSapperAssets)
            {
                throw new InvalidOperationException("The Signal Sapper model, materials, and prefab were not created successfully.");
            }
        }

        private static void _ensureSecurityWardenMaterials(Texture2D armorTexture)
        {
            _configureMaterial(
                SECURITY_WARDEN_ARMOR_MATERIAL_PATH,
                "SecurityWardenArmor",
                new Color(0.2f, 0.22f, 0.25f),
                Color.black,
                0.3f,
                armorTexture);
            _configureMaterial(
                SECURITY_WARDEN_EYE_MATERIAL_PATH,
                "SecurityWardenEye",
                new Color(0.95f, 0.015f, 0.02f),
                new Color(2.8f, 0.01f, 0.015f),
                0.22f,
                null);
            _configureMaterial(
                SECURITY_WARDEN_CROWN_MATERIAL_PATH,
                "SecurityWardenCrown",
                new Color(0.24f, 0.006f, 0.01f),
                new Color(0.38f, 0f, 0.005f),
                0.3f,
                null);
        }

        private static void _ensureSignalSapperMaterials(Texture2D armorTexture)
        {
            _configureMaterial(
                SIGNAL_SAPPER_ARMOR_MATERIAL_PATH,
                "SignalSapperArmor",
                new Color(0.82f, 0.78f, 0.88f),
                Color.black,
                0.38f,
                armorTexture);
            _configureMaterial(
                SIGNAL_SAPPER_FORK_MATERIAL_PATH,
                "SignalSapperFork",
                new Color(0.54f, 0.025f, 0.32f),
                new Color(0.12f, 0.001f, 0.06f),
                0.28f,
                null);
            _configureMaterial(
                SIGNAL_SAPPER_CORE_MATERIAL_PATH,
                "SignalSapperCore",
                new Color(0.78f, 0.035f, 0.48f),
                new Color(0.35f, 0.002f, 0.18f),
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
                    throw new InvalidOperationException("Could not find the URP Lit shader for the authored actor materials.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            if (isNewMaterial)
            {
                material.name = materialName;
                material.color = baseColor;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", baseColor);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", smoothness);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", emissionColor);
                    if (emissionColor.maxColorComponent > 0f)
                    {
                        material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                    }
                }
            }

            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }
            }

            if (isNewMaterial || texture != null)
            {
                EditorUtility.SetDirty(material);
            }
        }

        private static void _assignSecurityWardenMaterials()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(SECURITY_WARDEN_PREFAB_PATH);
            try
            {
                _assignMaterial(prefabRoot.transform, "Warden Chassis", SECURITY_WARDEN_ARMOR_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Warden Eye", SECURITY_WARDEN_EYE_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Warden Crown", SECURITY_WARDEN_CROWN_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, SECURITY_WARDEN_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void _assignSignalSapperMaterials()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(SIGNAL_SAPPER_PREFAB_PATH);
            try
            {
                _assignMaterial(prefabRoot.transform, "Sapper Chassis", SIGNAL_SAPPER_ARMOR_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Sapper Fork Left", SIGNAL_SAPPER_FORK_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Sapper Fork Right", SIGNAL_SAPPER_FORK_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Sapper Drain Core", SIGNAL_SAPPER_CORE_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, SIGNAL_SAPPER_PREFAB_PATH);
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
                throw new InvalidOperationException($"Could not assign {materialPath} to authored actor part {partName}.");
            }

            renderer.sharedMaterial = material;
        }

        private static bool _hasSecurityWardenModelPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_PREFAB_PATH);
            return prefab != null &&
                   _isImportedModelPart(prefab.transform.Find("Warden Chassis")) &&
                   _isImportedModelPart(prefab.transform.Find("Warden Eye")) &&
                   _isImportedModelPart(prefab.transform.Find("Warden Crown"));
        }

        private static bool _isImportedModelPart(Transform part)
        {
            if (part == null || !part.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null)
            {
                return false;
            }

            return AssetDatabase.GetAssetPath(meshFilter.sharedMesh) == SECURITY_WARDEN_MODEL_PATH;
        }

        private static bool _hasSignalSapperModelPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SIGNAL_SAPPER_PREFAB_PATH);
            return prefab != null &&
                   _isImportedModelPart(prefab.transform.Find("Sapper Chassis"), SIGNAL_SAPPER_MODEL_PATH) &&
                   _isImportedModelPart(prefab.transform.Find("Sapper Fork Left"), SIGNAL_SAPPER_MODEL_PATH) &&
                   _isImportedModelPart(prefab.transform.Find("Sapper Fork Right"), SIGNAL_SAPPER_MODEL_PATH) &&
                   _isImportedModelPart(prefab.transform.Find("Sapper Drain Core"), SIGNAL_SAPPER_MODEL_PATH);
        }

        private static bool _isImportedModelPart(Transform part, string modelPath)
        {
            if (part == null || !part.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null)
            {
                return false;
            }

            return AssetDatabase.GetAssetPath(meshFilter.sharedMesh) == modelPath;
        }

        private static bool _hasSecurityWardenMaterialAssignments()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_PREFAB_PATH);
            return prefab != null &&
                   _hasMaterial(prefab.transform.Find("Warden Chassis"), SECURITY_WARDEN_ARMOR_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Warden Eye"), SECURITY_WARDEN_EYE_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Warden Crown"), SECURITY_WARDEN_CROWN_MATERIAL_PATH);
        }

        private static bool _hasSignalSapperMaterialAssignments()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SIGNAL_SAPPER_PREFAB_PATH);
            return prefab != null &&
                   _hasMaterial(prefab.transform.Find("Sapper Chassis"), SIGNAL_SAPPER_ARMOR_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Sapper Fork Left"), SIGNAL_SAPPER_FORK_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Sapper Fork Right"), SIGNAL_SAPPER_FORK_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Sapper Drain Core"), SIGNAL_SAPPER_CORE_MATERIAL_PATH);
        }

        private static bool _hasMaterial(Transform part, string materialPath)
        {
            return part != null &&
                   part.TryGetComponent<Renderer>(out var renderer) &&
                   renderer.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }
    }
}
