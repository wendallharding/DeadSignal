using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalWardenBaySetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/WardenBayAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/SecurityBlastShieldModel.fbx";
        private const string ROUTE_MARKER_MODEL_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityBayRouteMarkerModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SecurityShieldArmor.mat";
        private const string BRACE_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SecurityShieldBraces.mat";
        private const string WARNING_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SecurityShieldWarnings.mat";
        private const string ROUTE_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/DepartureThresholdBeacons.mat";
        private const string SHIELD_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SecurityBlastShield.prefab";
        private const string ROUTE_MARKER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityBayRouteMarker.prefab";
        private const string BAY_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/WardenStagingBay.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";

        private static readonly Vector3 s_wardenPosition = new(6.8f, 0f, 4.7f);

        public static bool HasAssets
        {
            get
            {
                var shield = AssetDatabase.LoadAssetAtPath<GameObject>(SHIELD_PREFAB_PATH);
                var routeMarker = AssetDatabase.LoadAssetAtPath<GameObject>(ROUTE_MARKER_PREFAB_PATH);
                var bay = AssetDatabase.LoadAssetAtPath<GameObject>(BAY_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(ROUTE_MARKER_MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(BRACE_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(WARNING_MATERIAL_PATH) != null &&
                       _hasValidShield(shield) &&
                       routeMarker != null &&
                       bay != null &&
                       bay.GetComponentsInChildren<AuthoredMapObstacle>().Length == 3 &&
                       bay.transform.Find("North Bypass Entry Marker") != null &&
                       bay.transform.Find("North Bypass Exit Marker") != null;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureModelImport(MODEL_PATH, "security-shield");
            _configureModelImport(ROUTE_MARKER_MODEL_PATH, "bay route-marker");
            _ensureMaterials();
            _ensureShieldPrefab();
            _ensureRouteMarkerPrefab();
            _ensureBayPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Security Warden staging-bay assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Warden-bay texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _configureModelImport(string assetPath, string assetDescription)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the {assetDescription} model at {assetPath}.");
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
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "SecurityShieldArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.5f);
            armor.SetFloat("_Smoothness", 0.38f);
            EditorUtility.SetDirty(armor);

            var braces = _loadOrCreateMaterial(BRACE_MATERIAL_PATH, "SecurityShieldBraces");
            braces.SetColor("_BaseColor", new Color(0.78f, 0.76f, 0.7f));
            braces.SetFloat("_Metallic", 0.1f);
            braces.SetFloat("_Smoothness", 0.32f);
            EditorUtility.SetDirty(braces);

            var warnings = _loadOrCreateMaterial(WARNING_MATERIAL_PATH, "SecurityShieldWarnings");
            var warningColor = new Color(0.78f, 0.01f, 0.02f);
            warnings.SetColor("_BaseColor", warningColor);
            warnings.SetColor("_EmissionColor", warningColor * 1.6f);
            warnings.SetFloat("_Metallic", 0.08f);
            warnings.SetFloat("_Smoothness", 0.72f);
            warnings.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(warnings);
        }

        private static Material _loadOrCreateMaterial(string path, string materialName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Could not find the URP Lit shader for the Warden-bay materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureShieldPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SHIELD_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported security blast-shield model.");
                }

                instance.name = "SecurityBlastShield";
                PrefabUtility.SaveAsPrefabAsset(instance, SHIELD_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(SHIELD_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    obstacle = root.AddComponent<AuthoredMapObstacle>();
                }

                obstacle.Configure(new Vector2(1.6f, 0.45f));
                _assignMaterial(root.transform, "Security Shield Armor", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Security Shield Braces", BRACE_MATERIAL_PATH);
                _assignMaterial(root.transform, "Security Shield Warning Lenses", WARNING_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, SHIELD_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureRouteMarkerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ROUTE_MARKER_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ROUTE_MARKER_MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported bay route-marker model.");
                }

                instance.name = "SecurityBayRouteMarker";
                PrefabUtility.SaveAsPrefabAsset(instance, ROUTE_MARKER_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(ROUTE_MARKER_PREFAB_PATH);
            try
            {
                var routeMaterial = AssetDatabase.LoadAssetAtPath<Material>(ROUTE_MATERIAL_PATH);
                var renderers = root.GetComponentsInChildren<Renderer>();
                if (routeMaterial == null || renderers.Length == 0)
                {
                    throw new InvalidOperationException("The bay route marker is missing its renderer or Signal material.");
                }

                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = routeMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, ROUTE_MARKER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureBayPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BAY_PREFAB_PATH) == null)
            {
                var emptyBay = new GameObject("WardenStagingBay");
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(emptyBay, BAY_PREFAB_PATH);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(emptyBay);
                }
            }

            var shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SHIELD_PREFAB_PATH);
            var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROUTE_MARKER_PREFAB_PATH);
            var bay = PrefabUtility.LoadPrefabContents(BAY_PREFAB_PATH);
            try
            {
                _ensurePrefabChild(bay.transform, shieldPrefab, "North Security Shield",
                    new Vector3(-0.2f, 0f, 1.15f), 0f, new Vector3(0.45f, 1f, 1f));
                _ensurePrefabChild(bay.transform, shieldPrefab, "South Security Shield",
                    new Vector3(0.65f, 0f, -1.35f), 0f, new Vector3(0.82f, 1f, 1f));
                _ensurePrefabChild(bay.transform, shieldPrefab, "East Security Shield",
                    new Vector3(1.8f, 0f, 0f), 90f, new Vector3(0.72f, 1f, 1f));
                _ensurePrefabChild(bay.transform, markerPrefab, "North Bypass Entry Marker",
                    new Vector3(-1.45f, 0.08f, 2.15f), 8f, new Vector3(0.9f, 0.9f, 0.9f));
                _ensurePrefabChild(bay.transform, markerPrefab, "North Bypass Exit Marker",
                    new Vector3(0.95f, 0.08f, 1.95f), 0f, new Vector3(0.9f, 0.9f, 0.9f));
                PrefabUtility.SaveAsPrefabAsset(bay, BAY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(bay);
            }
        }

        private static void _ensurePrefabChild(
            Transform parent,
            GameObject prefab,
            string objectName,
            Vector3 localPosition,
            float rotationY,
            Vector3 localScale)
        {
            var child = parent.Find(objectName);
            if (child == null)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException($"Could not instantiate {objectName} for the Warden bay.");
                }

                instance.name = objectName;
                instance.transform.SetParent(parent, false);
                child = instance.transform;
            }

            child.localPosition = localPosition;
            child.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            child.localScale = localScale;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Security Warden Staging Bay");
            if (existing == null)
            {
                var bayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BAY_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(bayPrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the Security Warden staging bay in SampleScene.");
                }

                existing.name = "Security Warden Staging Bay";
                existing.transform.position = s_wardenPosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.transform.position != s_wardenPosition ||
                existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 3)
            {
                throw new InvalidOperationException("The SampleScene staging bay is not placed around the Warden spawn.");
            }
        }

        private static void _assignMaterial(Transform root, string partName, string materialPath)
        {
            var part = root.Find(partName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (part == null || material == null || !part.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not assign {materialPath} to {partName}.");
            }

            renderer.sharedMaterial = material;
        }

        private static bool _hasValidShield(GameObject shield)
        {
            return shield != null &&
                   shield.GetComponent<AuthoredMapObstacle>() != null &&
                   shield.transform.Find("Security Shield Armor") != null &&
                   shield.transform.Find("Security Shield Braces") != null &&
                   shield.transform.Find("Security Shield Warning Lenses") != null;
        }
    }
}
