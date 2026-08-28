using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.Missions;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalSapperCradleSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/SapperCradleAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/SapperSiphonPylonModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SapperCradleArmor.mat";
        private const string CERAMIC_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SapperCradleCeramic.mat";
        private const string ENERGY_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SapperCradleEnergy.mat";
        private const string PYLON_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SapperSiphonPylon.prefab";
        private const string CRADLE_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SignalSapperCradle.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";

        private static readonly Vector3 s_sapperPosition = new(-10.8f, 0f, 5.7f);

        public static bool HasAssets
        {
            get
            {
                var pylon = AssetDatabase.LoadAssetAtPath<GameObject>(PYLON_PREFAB_PATH);
                var cradle = AssetDatabase.LoadAssetAtPath<GameObject>(CRADLE_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CERAMIC_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ENERGY_MATERIAL_PATH) != null &&
                       _hasValidPylon(pylon) &&
                       cradle != null &&
                       cradle.TryGetComponent<AuthoredWithdrawalPursuitLandmark>(out var landmark) &&
                       landmark.IsConfigured && landmark.Phase == PoweredWithdrawalPhase.SapperCradle &&
                       cradle.GetComponentsInChildren<AuthoredMapObstacle>().Length == 2;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureModelImport();
            _ensureMaterials();
            _ensurePylonPrefab();
            _ensureCradlePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Signal Sapper service-cradle assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Sapper-cradle texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _configureModelImport()
        {
            var importer = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the siphon-pylon model at {MODEL_PATH}.");
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
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "SapperCradleArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.42f);
            armor.SetFloat("_Smoothness", 0.34f);
            EditorUtility.SetDirty(armor);

            var ceramic = _loadOrCreateMaterial(CERAMIC_MATERIAL_PATH, "SapperCradleCeramic");
            ceramic.SetColor("_BaseColor", new Color(0.74f, 0.72f, 0.7f));
            ceramic.SetFloat("_Metallic", 0.08f);
            ceramic.SetFloat("_Smoothness", 0.34f);
            EditorUtility.SetDirty(ceramic);

            var energy = _loadOrCreateMaterial(ENERGY_MATERIAL_PATH, "SapperCradleEnergy");
            var energyColor = new Color(0.9f, 0.01f, 0.46f);
            energy.SetColor("_BaseColor", energyColor);
            energy.SetColor("_EmissionColor", energyColor * 2f);
            energy.SetFloat("_Metallic", 0.04f);
            energy.SetFloat("_Smoothness", 0.78f);
            energy.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(energy);
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
                throw new InvalidOperationException("Could not find the URP Lit shader for the Sapper-cradle materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensurePylonPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PYLON_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported siphon-pylon model.");
                }

                instance.name = "SapperSiphonPylon";
                PrefabUtility.SaveAsPrefabAsset(instance, PYLON_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(PYLON_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    obstacle = root.AddComponent<AuthoredMapObstacle>();
                }

                obstacle.Configure(new Vector2(1.32f, 0.45f));
                _assignMaterial(root.transform, "Sapper Cradle Armor", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Sapper Cradle Ceramic", CERAMIC_MATERIAL_PATH);
                _assignMaterial(root.transform, "Sapper Cradle Energy", ENERGY_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, PYLON_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureCradlePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CRADLE_PREFAB_PATH) == null)
            {
                var emptyCradle = new GameObject("SignalSapperCradle");
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(emptyCradle, CRADLE_PREFAB_PATH);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(emptyCradle);
                }
            }

            var pylonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PYLON_PREFAB_PATH);
            var cradle = PrefabUtility.LoadPrefabContents(CRADLE_PREFAB_PATH);
            try
            {
                var landmark = cradle.GetComponent<AuthoredWithdrawalPursuitLandmark>();
                if (landmark == null)
                {
                    landmark = cradle.AddComponent<AuthoredWithdrawalPursuitLandmark>();
                }
                landmark.Configure(PoweredWithdrawalPhase.SapperCradle);
                _ensurePylon(cradle.transform, pylonPrefab, "North Siphon Pylon",
                    new Vector3(0f, 0f, 1.3f), 0f);
                _ensurePylon(cradle.transform, pylonPrefab, "West Siphon Pylon",
                    new Vector3(-1.3f, 0f, 0f), 90f);
                PrefabUtility.SaveAsPrefabAsset(cradle, CRADLE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(cradle);
            }
        }

        private static void _ensurePylon(
            Transform parent,
            GameObject pylonPrefab,
            string objectName,
            Vector3 localPosition,
            float rotationY)
        {
            var pylon = parent.Find(objectName);
            if (pylon == null)
            {
                var instance = PrefabUtility.InstantiatePrefab(pylonPrefab) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException($"Could not instantiate {objectName} for the Sapper cradle.");
                }

                instance.name = objectName;
                instance.transform.SetParent(parent, false);
                pylon = instance.transform;
            }

            pylon.localPosition = localPosition;
            pylon.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            pylon.localScale = Vector3.one;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Signal Sapper Service Cradle");
            if (existing == null)
            {
                var cradlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CRADLE_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(cradlePrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the Signal Sapper service cradle in SampleScene.");
                }

                existing.name = "Signal Sapper Service Cradle";
                existing.transform.position = s_sapperPosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.transform.position != s_sapperPosition ||
                existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 2)
            {
                throw new InvalidOperationException("The SampleScene Sapper cradle is not placed around the Sapper spawn.");
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

        private static bool _hasValidPylon(GameObject pylon)
        {
            return pylon != null &&
                   pylon.GetComponent<AuthoredMapObstacle>() != null &&
                   pylon.transform.Find("Sapper Cradle Armor") != null &&
                   pylon.transform.Find("Sapper Cradle Ceramic") != null &&
                   pylon.transform.Find("Sapper Cradle Energy") != null;
        }
    }
}
