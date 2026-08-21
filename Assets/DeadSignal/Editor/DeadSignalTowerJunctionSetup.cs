using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalTowerJunctionSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/CoolantManifoldAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/CoolantManifoldModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/CoolantManifoldArmor.mat";
        private const string CONDUIT_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/CoolantManifoldConduit.mat";
        private const string OBSTACLE_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/CoolantManifoldAssembly.prefab";
        private const string JUNCTION_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/TowerApproachJunction.prefab";
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

        private static readonly Vector3 s_towerPosition = new(-0.6f, 0f, 0.4f);

        public static bool HasAssets
        {
            get
            {
                var obstacle = AssetDatabase.LoadAssetAtPath<GameObject>(OBSTACLE_PREFAB_PATH);
                var junction = AssetDatabase.LoadAssetAtPath<GameObject>(JUNCTION_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CONDUIT_MATERIAL_PATH) != null &&
                       _hasValidObstacle(obstacle) &&
                       junction != null &&
                       junction.GetComponentsInChildren<AuthoredMapObstacle>().Length == 3;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureModelImport();
            _ensureMaterials();
            _ensureObstaclePrefab();
            _ensureJunctionPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The tower-approach junction assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the coolant-manifold texture at {TEXTURE_PATH}.");
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
                throw new InvalidOperationException($"Could not find the coolant-manifold model at {MODEL_PATH}.");
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
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "CoolantManifoldArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.42f);
            armor.SetFloat("_Smoothness", 0.38f);
            EditorUtility.SetDirty(armor);

            var conduit = _loadOrCreateMaterial(CONDUIT_MATERIAL_PATH, "CoolantManifoldConduit");
            var conduitColor = new Color(0.01f, 0.72f, 0.92f);
            conduit.SetColor("_BaseColor", conduitColor);
            conduit.SetColor("_EmissionColor", conduitColor * 1.7f);
            conduit.SetFloat("_Metallic", 0.12f);
            conduit.SetFloat("_Smoothness", 0.72f);
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
                throw new InvalidOperationException("Could not find the URP Lit shader for the tower-junction materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureObstaclePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OBSTACLE_PREFAB_PATH);
            if (prefab == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (modelInstance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported coolant-manifold model.");
                }

                modelInstance.name = "CoolantManifoldAssembly";
                PrefabUtility.SaveAsPrefabAsset(modelInstance, OBSTACLE_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(modelInstance);
            }

            var root = PrefabUtility.LoadPrefabContents(OBSTACLE_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    obstacle = root.AddComponent<AuthoredMapObstacle>();
                }

                obstacle.Configure(new Vector2(2.2f, 0.55f));
                _assignMaterial(root.transform, "Coolant Manifold Body", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Coolant Manifold Conduit", CONDUIT_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, OBSTACLE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureJunctionPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(JUNCTION_PREFAB_PATH) != null)
            {
                return;
            }

            var obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OBSTACLE_PREFAB_PATH);
            var junction = new GameObject("TowerApproachJunction");
            try
            {
                _addObstacle(junction.transform, obstaclePrefab, "South Coolant Manifold", new Vector3(0f, 0f, -2.65f), 0f, Vector3.one);
                _addObstacle(junction.transform, obstaclePrefab, "North Coolant Manifold", new Vector3(0f, 0f, 3.05f), 0f,
                    new Vector3(0.72f, 1f, 1f));
                _addObstacle(junction.transform, obstaclePrefab, "East Coolant Manifold", new Vector3(2.55f, 0f, 0.65f), 90f,
                    new Vector3(0.62f, 1f, 1f));
                PrefabUtility.SaveAsPrefabAsset(junction, JUNCTION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(junction);
            }
        }

        private static void _addObstacle(
            Transform parent,
            GameObject obstaclePrefab,
            string objectName,
            Vector3 localPosition,
            float rotationY,
            Vector3 localScale)
        {
            var obstacle = PrefabUtility.InstantiatePrefab(obstaclePrefab) as GameObject;
            if (obstacle == null)
            {
                throw new InvalidOperationException($"Could not instantiate {objectName} for the tower junction.");
            }

            obstacle.name = objectName;
            obstacle.transform.SetParent(parent, false);
            obstacle.transform.localPosition = localPosition;
            obstacle.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            obstacle.transform.localScale = localScale;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Tower Approach Junction");
            if (existing == null)
            {
                var junctionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JUNCTION_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(junctionPrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the tower-approach junction in SampleScene.");
                }

                existing.name = "Tower Approach Junction";
                existing.transform.position = s_towerPosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 3)
            {
                throw new InvalidOperationException("The SampleScene tower junction does not contain three authored obstacles.");
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

        private static bool _hasValidObstacle(GameObject obstacle)
        {
            return obstacle != null &&
                   obstacle.GetComponent<AuthoredMapObstacle>() != null &&
                   obstacle.transform.Find("Coolant Manifold Body") != null &&
                   obstacle.transform.Find("Coolant Manifold Conduit") != null;
        }
    }
}
