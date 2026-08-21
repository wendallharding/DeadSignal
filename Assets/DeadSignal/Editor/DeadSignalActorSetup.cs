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

        public static bool HasSecurityWardenAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(SECURITY_WARDEN_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_MODEL_PATH) != null &&
            _hasSecurityWardenModelPrefab() &&
            _hasSecurityWardenMaterialAssignments();

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

        private static void _configureMaterial(
            string assetPath,
            string materialName,
            Color baseColor,
            Color emissionColor,
            float smoothness,
            Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Security Warden materials.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = materialName;
            material.color = baseColor;
            material.mainTexture = texture;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
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

            EditorUtility.SetDirty(material);
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

        private static void _assignMaterial(Transform root, string partName, string materialPath)
        {
            var part = root.Find(partName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (part == null || material == null || !part.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not assign {materialPath} to Security Warden part {partName}.");
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

        private static bool _hasSecurityWardenMaterialAssignments()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_PREFAB_PATH);
            return prefab != null &&
                   _hasMaterial(prefab.transform.Find("Warden Chassis"), SECURITY_WARDEN_ARMOR_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Warden Eye"), SECURITY_WARDEN_EYE_MATERIAL_PATH) &&
                   _hasMaterial(prefab.transform.Find("Warden Crown"), SECURITY_WARDEN_CROWN_MATERIAL_PATH);
        }

        private static bool _hasMaterial(Transform part, string materialPath)
        {
            return part != null &&
                   part.TryGetComponent<Renderer>(out var renderer) &&
                   renderer.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }
    }
}
