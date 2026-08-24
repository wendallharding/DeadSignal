using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalSalvageAnnexSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnexAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnexBarrierModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SalvageAnnexArmor.mat";
        private const string HAZARD_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SalvageAnnexHazard.mat";
        private const string CONDUIT_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SalvageAnnexConduit.mat";
        private const string BARRIER_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnexBarrier.prefab";
        private const string ANNEX_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SalvageAnnex.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";

        private static readonly Vector3 s_salvagePosition = new(9.7f, 0f, 6.3f);

        public static bool HasAssets
        {
            get
            {
                var barrier = AssetDatabase.LoadAssetAtPath<GameObject>(BARRIER_PREFAB_PATH);
                var annex = AssetDatabase.LoadAssetAtPath<GameObject>(ANNEX_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(HAZARD_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CONDUIT_MATERIAL_PATH) != null &&
                       _hasValidBarrier(barrier) &&
                       annex != null &&
                       annex.GetComponentsInChildren<AuthoredMapObstacle>().Length == 3;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureModelImport();
            _ensureMaterials();
            _ensureBarrierPrefab();
            _ensureAnnexPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The salvage-annex assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the salvage-annex texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _configureModelImport()
        {
            var importer = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the salvage-annex model at {MODEL_PATH}.");
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
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "SalvageAnnexArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.38f);
            armor.SetFloat("_Smoothness", 0.32f);
            EditorUtility.SetDirty(armor);

            var hazard = _loadOrCreateMaterial(HAZARD_MATERIAL_PATH, "SalvageAnnexHazard");
            hazard.SetColor("_BaseColor", new Color(0.88f, 0.42f, 0.035f));
            hazard.SetFloat("_Metallic", 0.28f);
            hazard.SetFloat("_Smoothness", 0.36f);
            EditorUtility.SetDirty(hazard);

            var conduit = _loadOrCreateMaterial(CONDUIT_MATERIAL_PATH, "SalvageAnnexConduit");
            var conduitColor = new Color(0.01f, 0.68f, 0.9f);
            conduit.SetColor("_BaseColor", conduitColor);
            conduit.SetColor("_EmissionColor", conduitColor * 1.45f);
            conduit.SetFloat("_Metallic", 0.12f);
            conduit.SetFloat("_Smoothness", 0.68f);
            conduit.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(conduit);
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
                throw new InvalidOperationException("Could not find the URP Lit shader for the salvage-annex materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureBarrierPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BARRIER_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported salvage-annex barrier model.");
                }

                instance.name = "SalvageAnnexBarrier";
                PrefabUtility.SaveAsPrefabAsset(instance, BARRIER_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(BARRIER_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    obstacle = root.AddComponent<AuthoredMapObstacle>();
                }

                obstacle.Configure(new Vector2(2.15f, 0.39f));
                _assignMaterial(root.transform, "Salvage Annex Armor", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Salvage Annex Hazard Rail", HAZARD_MATERIAL_PATH);
                _assignMaterial(root.transform, "Salvage Annex Conduit", CONDUIT_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, BARRIER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureAnnexPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ANNEX_PREFAB_PATH) == null)
            {
                var emptyAnnex = new GameObject("SalvageAnnex");
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(emptyAnnex, ANNEX_PREFAB_PATH);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(emptyAnnex);
                }
            }

            var barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BARRIER_PREFAB_PATH);
            var annex = PrefabUtility.LoadPrefabContents(ANNEX_PREFAB_PATH);
            try
            {
                _ensureBarrier(annex.transform, barrierPrefab, "North Cargo Barrier", new Vector3(0.75f, 0f, 1.55f), 0f,
                    new Vector3(0.65f, 1f, 1f));
                _ensureBarrier(annex.transform, barrierPrefab, "East Cargo Barrier", new Vector3(1.72f, 0f, 0f), 90f,
                    new Vector3(0.62f, 1f, 1f));
                _ensureBarrier(annex.transform, barrierPrefab, "South Cargo Barrier", new Vector3(0.75f, 0f, -1.35f), 0f,
                    new Vector3(0.65f, 1f, 1f));
                PrefabUtility.SaveAsPrefabAsset(annex, ANNEX_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(annex);
            }
        }

        private static void _ensureBarrier(
            Transform parent,
            GameObject barrierPrefab,
            string objectName,
            Vector3 localPosition,
            float rotationY,
            Vector3 localScale)
        {
            var barrier = parent.Find(objectName);
            if (barrier == null)
            {
                var instance = PrefabUtility.InstantiatePrefab(barrierPrefab) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException($"Could not instantiate {objectName} for the salvage annex.");
                }

                instance.name = objectName;
                instance.transform.SetParent(parent, false);
                barrier = instance.transform;
            }

            barrier.localPosition = localPosition;
            barrier.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            barrier.localScale = localScale;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Northeast Salvage Annex");
            if (existing == null)
            {
                var annexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ANNEX_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(annexPrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the salvage annex in SampleScene.");
                }

                existing.name = "Northeast Salvage Annex";
                existing.transform.position = s_salvagePosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 3)
            {
                throw new InvalidOperationException("The SampleScene salvage annex does not contain three authored barriers.");
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

        private static bool _hasValidBarrier(GameObject barrier)
        {
            return barrier != null &&
                   barrier.GetComponent<AuthoredMapObstacle>() != null &&
                   barrier.transform.Find("Salvage Annex Armor") != null &&
                   barrier.transform.Find("Salvage Annex Hazard Rail") != null &&
                   barrier.transform.Find("Salvage Annex Conduit") != null;
        }
    }
}
