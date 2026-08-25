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
        private const string RETURN_DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/DepartureCargoReturnDecal.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/DepartureCapacitorModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/DepartureCapacitorArmor.mat";
        private const string CELL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/DepartureCapacitorCells.mat";
        private const string BEACON_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/DepartureThresholdBeacons.mat";
        private const string RETURN_DECAL_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/DepartureCargoReturnDecal.mat";
        private const string ARMOR_MATERIAL_SOURCE_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/StationBlack.mat";
        private const string AMBER_MATERIAL_SOURCE_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";
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
                       AssetDatabase.LoadAssetAtPath<Texture2D>(RETURN_DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CELL_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(BEACON_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(RETURN_DECAL_MATERIAL_PATH) != null &&
                       _hasValidCapacitor(capacitor) &&
                       channel != null &&
                       channel.GetComponentsInChildren<AuthoredMapObstacle>().Length == 3 &&
                       channel.transform.Find("Departure Cargo Shutter") != null &&
                       channel.transform.Find("Departure Cargo Return Signal") != null;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureReturnDecalImport();
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

        private static void _configureReturnDecalImport()
        {
            var importer = AssetImporter.GetAtPath(RETURN_DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the departure return decal at {RETURN_DECAL_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
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

            var returnDecal = _loadOrCreateMaterial(RETURN_DECAL_MATERIAL_PATH, "DepartureCargoReturnDecal",
                "Universal Render Pipeline/Unlit");
            returnDecal.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RETURN_DECAL_PATH));
            returnDecal.SetColor("_BaseColor", Color.white);
            returnDecal.SetFloat("_Surface", 1f);
            returnDecal.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            returnDecal.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            returnDecal.SetFloat("_ZWrite", 0f);
            returnDecal.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            returnDecal.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(returnDecal);
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

        private static Material _loadOrCreateMaterial(
            string path,
            string materialName,
            string shaderName = "Universal Render Pipeline/Lit")
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Could not find {shaderName} for departure-channel materials.");
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
            var capacitorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAPACITOR_PREFAB_PATH);
            var channel = new GameObject("ExtractionDepartureChannel");
            try
            {
                _addCapacitor(channel.transform, capacitorPrefab, "North Departure Capacitor", new Vector3(0f, 0f, 1.25f), 180f);
                _addCapacitor(channel.transform, capacitorPrefab, "South Departure Capacitor", new Vector3(0f, 0f, -1.25f), 0f);
                _addCargoShutter(channel.transform);
                _addReturnSignal(channel.transform);
                PrefabUtility.SaveAsPrefabAsset(channel, CHANNEL_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(channel);
            }
        }

        private static void _addCargoShutter(Transform parent)
        {
            var armor = AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_SOURCE_PATH);
            var amber = AssetDatabase.LoadAssetAtPath<Material>(AMBER_MATERIAL_SOURCE_PATH);
            if (armor == null || amber == null)
            {
                throw new InvalidOperationException("Could not load departure shutter palette materials.");
            }

            var shutter = new GameObject("Departure Cargo Shutter");
            shutter.transform.SetParent(parent, false);
            shutter.AddComponent<AuthoredMapObstacle>().Configure(new Vector2(0.24f, 0.92f));
            _addCube(shutter.transform, "Cargo Shutter Housing", Vector3.zero, new Vector3(0.48f, 1f, 1.84f), armor);
            _addCube(shutter.transform, "North Cargo Lock", new Vector3(-0.25f, 0.08f, 0.59f),
                new Vector3(0.04f, 0.58f, 0.34f), amber);
            _addCube(shutter.transform, "South Cargo Lock", new Vector3(-0.25f, 0.08f, -0.59f),
                new Vector3(0.04f, 0.58f, 0.34f), amber);
        }

        private static void _addReturnSignal(Transform parent)
        {
            var signal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            signal.name = "Departure Cargo Return Signal";
            signal.transform.SetParent(parent, false);
            signal.transform.localPosition = new Vector3(-0.05f, -0.105f, 0f);
            signal.transform.localRotation = Quaternion.Euler(90f, 90f, 0f);
            signal.transform.localScale = Vector3.one * 2.65f;
            signal.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(RETURN_DECAL_MATERIAL_PATH);
            UnityEngine.Object.DestroyImmediate(signal.GetComponent<Collider>());
        }

        private static void _addCube(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
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

            if (existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 3)
            {
                throw new InvalidOperationException(
                    "The SampleScene departure channel does not contain two authored capacitors and the cargo shutter.");
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
