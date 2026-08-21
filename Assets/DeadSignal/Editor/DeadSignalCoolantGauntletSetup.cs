using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalCoolantGauntletSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/CoolantGauntletAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/CoolantBaffleModel.fbx";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/CoolantBaffleArmor.mat";
        private const string FIN_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/CoolantBaffleFins.mat";
        private const string PIPE_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/CoolantBafflePipes.mat";
        private const string LIGHT_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/CoolantBaffleLights.mat";
        private const string BAFFLE_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/CoolantBaffle.prefab";
        private const string GAUNTLET_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SoutheastCoolantGauntlet.prefab";
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

        private static readonly Vector3 s_salvagePosition = new(10.4f, 0f, -6.4f);

        public static bool HasAssets
        {
            get
            {
                var baffle = AssetDatabase.LoadAssetAtPath<GameObject>(BAFFLE_PREFAB_PATH);
                var gauntlet = AssetDatabase.LoadAssetAtPath<GameObject>(GAUNTLET_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(FIN_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(PIPE_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(LIGHT_MATERIAL_PATH) != null &&
                       _hasValidBaffle(baffle) &&
                       gauntlet != null &&
                       gauntlet.GetComponentsInChildren<AuthoredMapObstacle>().Length == 2;
            }
        }

        public static void EnsureAssets()
        {
            _configureTextureImport();
            _configureModelImport();
            _ensureMaterials();
            _ensureBafflePrefab();
            _ensureGauntletPrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The southeast coolant-gauntlet assets are incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the coolant-gauntlet texture at {TEXTURE_PATH}.");
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
                throw new InvalidOperationException($"Could not find the coolant-baffle model at {MODEL_PATH}.");
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
            var armor = _loadOrCreateMaterial(ARMOR_MATERIAL_PATH, "CoolantBaffleArmor");
            armor.SetColor("_BaseColor", Color.white);
            armor.SetTexture("_BaseMap", texture);
            armor.SetFloat("_Metallic", 0.42f);
            armor.SetFloat("_Smoothness", 0.38f);
            EditorUtility.SetDirty(armor);

            var fins = _loadOrCreateMaterial(FIN_MATERIAL_PATH, "CoolantBaffleFins");
            fins.SetColor("_BaseColor", new Color(0.76f, 0.75f, 0.68f));
            fins.SetFloat("_Metallic", 0.1f);
            fins.SetFloat("_Smoothness", 0.28f);
            EditorUtility.SetDirty(fins);

            var pipes = _loadOrCreateMaterial(PIPE_MATERIAL_PATH, "CoolantBafflePipes");
            pipes.SetColor("_BaseColor", new Color(0.42f, 0.18f, 0.07f));
            pipes.SetFloat("_Metallic", 0.72f);
            pipes.SetFloat("_Smoothness", 0.52f);
            EditorUtility.SetDirty(pipes);

            var lights = _loadOrCreateMaterial(LIGHT_MATERIAL_PATH, "CoolantBaffleLights");
            var lightColor = new Color(0.01f, 0.68f, 0.9f);
            lights.SetColor("_BaseColor", lightColor);
            lights.SetColor("_EmissionColor", lightColor * 1.5f);
            lights.SetFloat("_Metallic", 0.08f);
            lights.SetFloat("_Smoothness", 0.72f);
            lights.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(lights);
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
                throw new InvalidOperationException("Could not find the URP Lit shader for the coolant-gauntlet materials.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureBafflePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BAFFLE_PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported coolant-baffle model.");
                }

                instance.name = "CoolantBaffle";
                PrefabUtility.SaveAsPrefabAsset(instance, BAFFLE_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var root = PrefabUtility.LoadPrefabContents(BAFFLE_PREFAB_PATH);
            try
            {
                var obstacle = root.GetComponent<AuthoredMapObstacle>();
                if (obstacle == null)
                {
                    obstacle = root.AddComponent<AuthoredMapObstacle>();
                }

                obstacle.Configure(new Vector2(2.12f, 0.51f));
                _assignMaterial(root.transform, "Coolant Baffle Armor", ARMOR_MATERIAL_PATH);
                _assignMaterial(root.transform, "Coolant Baffle Fins", FIN_MATERIAL_PATH);
                _assignMaterial(root.transform, "Coolant Baffle Pipes", PIPE_MATERIAL_PATH);
                _assignMaterial(root.transform, "Coolant Baffle Lights", LIGHT_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, BAFFLE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureGauntletPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GAUNTLET_PREFAB_PATH) != null)
            {
                return;
            }

            var bafflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BAFFLE_PREFAB_PATH);
            var gauntlet = new GameObject("SoutheastCoolantGauntlet");
            try
            {
                _addBaffle(gauntlet.transform, bafflePrefab, "Northwest Coolant Baffle", new Vector3(-1f, 0f, 1.35f));
                _addBaffle(gauntlet.transform, bafflePrefab, "Southeast Coolant Baffle", new Vector3(1f, 0f, -1.35f));
                PrefabUtility.SaveAsPrefabAsset(gauntlet, GAUNTLET_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gauntlet);
            }
        }

        private static void _addBaffle(Transform parent, GameObject prefab, string objectName, Vector3 localPosition)
        {
            var baffle = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (baffle == null)
            {
                throw new InvalidOperationException($"Could not instantiate {objectName} for the coolant gauntlet.");
            }

            baffle.name = objectName;
            baffle.transform.SetParent(parent, false);
            baffle.transform.localPosition = localPosition;
            baffle.transform.localRotation = Quaternion.identity;
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Southeast Coolant Gauntlet");
            if (existing == null)
            {
                var gauntletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GAUNTLET_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(gauntletPrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the coolant gauntlet in SampleScene.");
                }

                existing.name = "Southeast Coolant Gauntlet";
                existing.transform.position = s_salvagePosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.transform.position != s_salvagePosition ||
                existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 2)
            {
                throw new InvalidOperationException("The SampleScene coolant gauntlet is not placed around the southeast salvage cache.");
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

        private static bool _hasValidBaffle(GameObject baffle)
        {
            return baffle != null &&
                   baffle.GetComponent<AuthoredMapObstacle>() != null &&
                   baffle.transform.Find("Coolant Baffle Armor") != null &&
                   baffle.transform.Find("Coolant Baffle Fins") != null &&
                   baffle.transform.Find("Coolant Baffle Pipes") != null &&
                   baffle.transform.Find("Coolant Baffle Lights") != null;
        }
    }
}
