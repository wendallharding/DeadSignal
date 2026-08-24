using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalDepartureChannelSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/DepartureCapacitorAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/DepartureCapacitorModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/DepartureCapacitorArmor.mat";
        private const string CELL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/DepartureCapacitorCells.mat";
        private const string BEACON_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/DepartureThresholdBeacons.mat";
        private const string CAPACITOR_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/DepartureCapacitor.prefab";
        private const string CHANNEL_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/ExtractionDepartureChannel.prefab";
        private const string OBSTACLE_TEMPLATE_PATH = "Assets/DeadSignal/Resources/Environment/CoolantManifoldAssembly.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";

        private static readonly Vector3 s_channelPosition = new(-7.2f, 0f, -4.2f);

        public static bool HasAssets
        {
            get
            {
                var capacitor = AssetDatabase.LoadAssetAtPath<GameObject>(CAPACITOR_PREFAB_PATH);
                var channel = AssetDatabase.LoadAssetAtPath<GameObject>(CHANNEL_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CELL_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(BEACON_MATERIAL_PATH) != null &&
                       _hasValidCapacitor(capacitor) &&
                       channel != null &&
                       channel.GetComponentsInChildren<AuthoredMapObstacle>().Length == 2;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureModelImport();
            _ensureMaterials();
            _ensureCapacitorPrefab();
            _ensureChannelPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The extraction departure-channel assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the departure-capacitor texture at {TEXTURE_PATH}.");
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
                throw new InvalidOperationException($"Could not find the departure-capacitor model at {MODEL_PATH}.");
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
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "DepartureCapacitorArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.3f);
            armor.SetFloat("_Smoothness", 0.4f);
            EditorUtility.SetDirty(armor);

            var cells = _loadOrCreateMaterial(CELL_MATERIAL_PATH, "DepartureCapacitorCells");
            _configureEmission(cells, new Color(0.01f, 0.7f, 0.94f), 1.65f);

            var beacons = _loadOrCreateMaterial(BEACON_MATERIAL_PATH, "DepartureThresholdBeacons");
            _configureEmission(beacons, new Color(0.28f, 0.88f, 1f), 1.85f);
        }

        private static void _configureEmission(Material material, Color color, float intensity)
        {
            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", color * intensity);
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Smoothness", 0.7f);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
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
                throw new InvalidOperationException("Could not find the URP Lit shader for departure-channel materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureCapacitorPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CAPACITOR_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var obstacleTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(OBSTACLE_TEMPLATE_PATH);
                var instance = PrefabUtility.InstantiatePrefab(obstacleTemplate) as GameObject;
                var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null || modelInstance == null)
                {
                    throw new InvalidOperationException("Could not compose the departure-capacitor model and obstacle template.");
                }

                instance.name = "DepartureCapacitor";
                while (instance.transform.childCount > 0)
                {
                    UnityEngine.Object.DestroyImmediate(instance.transform.GetChild(0).gameObject);
                }

                PrefabUtility.UnpackPrefabInstance(modelInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                while (modelInstance.transform.childCount > 0)
                {
                    modelInstance.transform.GetChild(0).SetParent(instance.transform, false);
                }

                UnityEngine.Object.DestroyImmediate(modelInstance);
                PrefabUtility.SaveAsPrefabAsset(instance, CAPACITOR_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(CAPACITOR_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    throw new InvalidOperationException("The departure capacitor lost its authored-obstacle component.");
                }

                obstacle.Configure(new Vector2(2.3f, 0.42f));
                _assignMaterial(root.transform, "Departure Capacitor Armor", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Departure Capacitor Cells", CELL_MATERIAL_PATH);
                _assignMaterial(root.transform, "Departure Threshold Beacons", BEACON_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, CAPACITOR_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureChannelPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CHANNEL_PREFAB_PATH) != null)
            {
                return;
            }

            var capacitorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAPACITOR_PREFAB_PATH);
            var channel = new GameObject("ExtractionDepartureChannel");
            try
            {
                _addCapacitor(channel.transform, capacitorPrefab, "North Departure Capacitor", new Vector3(0f, 0f, 1.25f), 180f);
                _addCapacitor(channel.transform, capacitorPrefab, "South Departure Capacitor", new Vector3(0f, 0f, -1.25f), 0f);
                PrefabUtility.SaveAsPrefabAsset(channel, CHANNEL_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(channel);
            }
        }

        private static void _addCapacitor(
            Transform parent,
            GameObject capacitorPrefab,
            string objectName,
            Vector3 localPosition,
            float rotationY)
        {
            var capacitor = PrefabUtility.InstantiatePrefab(capacitorPrefab) as GameObject;
            if (capacitor == null)
            {
                throw new InvalidOperationException($"Could not instantiate {objectName} for the departure channel.");
            }

            capacitor.name = objectName;
            capacitor.transform.SetParent(parent, false);
            capacitor.transform.localPosition = localPosition;
            capacitor.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Extraction Departure Channel");
            if (existing == null)
            {
                var channelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CHANNEL_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(channelPrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the extraction departure channel in SampleScene.");
                }

                existing.name = "Extraction Departure Channel";
                existing.transform.position = s_channelPosition;
                existing.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 2)
            {
                throw new InvalidOperationException("The SampleScene departure channel does not contain two authored capacitors.");
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

        private static bool _hasValidCapacitor(GameObject capacitor)
        {
            return capacitor != null &&
                   capacitor.GetComponent<AuthoredMapObstacle>() != null &&
                   capacitor.transform.Find("Departure Capacitor Armor") != null &&
                   capacitor.transform.Find("Departure Capacitor Cells") != null &&
                   capacitor.transform.Find("Departure Threshold Beacons") != null;
        }
    }
}
