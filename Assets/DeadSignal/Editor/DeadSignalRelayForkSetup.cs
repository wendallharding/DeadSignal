using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalRelayForkSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/RelayForkAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/RelayBankModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/RelayBankArmor.mat";
        private const string INSULATOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/RelayBankInsulators.mat";
        private const string COIL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/RelayBankCoils.mat";
        private const string SIGNAL_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/RelayBankSignals.mat";
        private const string BANK_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/RelayBank.prefab";
        private const string FORK_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/NorthwestRelayFork.prefab";
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

        private static readonly Vector3 s_salvagePosition = new(-5.8f, 0f, 7.2f);

        public static bool HasAssets
        {
            get
            {
                var bank = AssetDatabase.LoadAssetAtPath<GameObject>(BANK_PREFAB_PATH);
                var fork = AssetDatabase.LoadAssetAtPath<GameObject>(FORK_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(INSULATOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(COIL_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(SIGNAL_MATERIAL_PATH) != null &&
                       _hasValidBank(bank) &&
                       fork != null &&
                       fork.GetComponentsInChildren<AuthoredMapObstacle>().Length == 2;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureModelImport();
            _ensureMaterials();
            _ensureBankPrefab();
            _ensureForkPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The northwest relay-fork assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the relay-fork texture at {TEXTURE_PATH}.");
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
                throw new InvalidOperationException($"Could not find the relay-bank model at {MODEL_PATH}.");
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
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "RelayBankArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.46f);
            armor.SetFloat("_Smoothness", 0.4f);
            EditorUtility.SetDirty(armor);

            var insulators = _loadOrCreateMaterial(INSULATOR_MATERIAL_PATH, "RelayBankInsulators");
            insulators.SetColor("_BaseColor", new Color(0.78f, 0.76f, 0.67f));
            insulators.SetFloat("_Metallic", 0.08f);
            insulators.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(insulators);

            var coils = _loadOrCreateMaterial(COIL_MATERIAL_PATH, "RelayBankCoils");
            coils.SetColor("_BaseColor", new Color(0.56f, 0.34f, 0.09f));
            coils.SetFloat("_Metallic", 0.76f);
            coils.SetFloat("_Smoothness", 0.5f);
            EditorUtility.SetDirty(coils);

            var signals = _loadOrCreateMaterial(SIGNAL_MATERIAL_PATH, "RelayBankSignals");
            var signalColor = new Color(0.01f, 0.68f, 0.9f);
            signals.SetColor("_BaseColor", signalColor);
            signals.SetColor("_EmissionColor", signalColor * 1.55f);
            signals.SetFloat("_Metallic", 0.08f);
            signals.SetFloat("_Smoothness", 0.74f);
            signals.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(signals);
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
                throw new InvalidOperationException("Could not find the URP Lit shader for the relay-fork materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureBankPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BANK_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported relay-bank model.");
                }

                instance.name = "RelayBank";
                PrefabUtility.SaveAsPrefabAsset(instance, BANK_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(BANK_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    obstacle = root.AddComponent<AuthoredMapObstacle>();
                }

                obstacle.Configure(new Vector2(1.5f, 0.52f));
                _assignMaterial(root.transform, "Relay Bank Armor", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Relay Bank Insulators", INSULATOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Relay Bank Coils", COIL_MATERIAL_PATH);
                _assignMaterial(root.transform, "Relay Bank Signals", SIGNAL_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, BANK_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureForkPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FORK_PREFAB_PATH) != null)
            {
                return;
            }

            var bankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BANK_PREFAB_PATH);
            var fork = new GameObject("NorthwestRelayFork");
            try
            {
                _addBank(fork.transform, bankPrefab, "West Relay Bank", new Vector3(-2.1f, 0f, -0.55f), 22f);
                _addBank(fork.transform, bankPrefab, "East Relay Bank", new Vector3(2.1f, 0f, -0.55f), -22f);
                PrefabUtility.SaveAsPrefabAsset(fork, FORK_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fork);
            }
        }

        private static void _addBank(
            Transform parent,
            GameObject prefab,
            string objectName,
            Vector3 localPosition,
            float rotationY)
        {
            var bank = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (bank == null)
            {
                throw new InvalidOperationException($"Could not instantiate {objectName} for the relay fork.");
            }

            bank.name = objectName;
            bank.transform.SetParent(parent, false);
            bank.transform.localPosition = localPosition;
            bank.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Northwest Relay Fork");
            if (existing == null)
            {
                var forkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FORK_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(forkPrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the northwest relay fork in SampleScene.");
                }

                existing.name = "Northwest Relay Fork";
                existing.transform.position = s_salvagePosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.transform.position != s_salvagePosition ||
                existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 2)
            {
                throw new InvalidOperationException("The SampleScene relay fork is not placed around the northwest salvage cache.");
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

        private static bool _hasValidBank(GameObject bank)
        {
            return bank != null &&
                   bank.GetComponent<AuthoredMapObstacle>() != null &&
                   bank.transform.Find("Relay Bank Armor") != null &&
                   bank.transform.Find("Relay Bank Insulators") != null &&
                   bank.transform.Find("Relay Bank Coils") != null &&
                   bank.transform.Find("Relay Bank Signals") != null;
        }
    }
}
