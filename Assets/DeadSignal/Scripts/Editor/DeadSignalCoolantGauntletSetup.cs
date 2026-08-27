using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

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
        private const string AMBER_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";
        private const string CYAN_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";
        private const string BAFFLE_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/CoolantBaffle.prefab";
        private const string GAUNTLET_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SoutheastCoolantGauntlet.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";

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
                       gauntlet.GetComponentsInChildren<AuthoredMapObstacle>().Length == 2 &&
                       gauntlet.TryGetComponent<AuthoredCoolantReclamationObjective>(out var objective) &&
                       objective.IsConfigured;
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
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GAUNTLET_PREFAB_PATH) == null)
            {
                var emptyGauntlet = new GameObject("SoutheastCoolantGauntlet");
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(emptyGauntlet, GAUNTLET_PREFAB_PATH);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(emptyGauntlet);
                }
            }

            var bafflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BAFFLE_PREFAB_PATH);
            var gauntlet = PrefabUtility.LoadPrefabContents(GAUNTLET_PREFAB_PATH);
            try
            {
                _ensureBaffle(gauntlet.transform, bafflePrefab, "Northwest Coolant Baffle", new Vector3(-1f, 0f, 1.35f));
                _ensureBaffle(gauntlet.transform, bafflePrefab, "Southeast Coolant Baffle", new Vector3(1f, 0f, -1.35f));
                var firstBaffleAnchor = _ensureAnchor(gauntlet.transform, "First Baffle Thread Anchor", new Vector3(-1.75f, 0f, -0.55f));
                var secondBaffleAnchor = _ensureAnchor(gauntlet.transform, "Second Baffle Thread Anchor", new Vector3(1.75f, 0f, 1.95f));
                var sealSocket = _ensureAnchor(gauntlet.transform, "Coolant Seal Socket", new Vector3(0f, 0f, 2.65f));
                var releaseAnchor = _ensureAnchor(gauntlet.transform, "Coolant Release Anchor", new Vector3(0f, 0f, -2.65f));
                var firstBaffleMarker = _ensureMarker(gauntlet.transform, "First Baffle Route Marker", PrimitiveType.Cylinder,
                    firstBaffleAnchor.localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(0.7f, 0.025f, 0.7f),
                    AMBER_MATERIAL_PATH);
                var secondBaffleMarker = _ensureMarker(gauntlet.transform, "Second Baffle Route Marker", PrimitiveType.Cylinder,
                    secondBaffleAnchor.localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(0.7f, 0.025f, 0.7f),
                    AMBER_MATERIAL_PATH);
                var releaseMarker = _ensureMarker(gauntlet.transform, "Coolant Release Marker", PrimitiveType.Cube,
                    releaseAnchor.localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(3.6f, 0.025f, 0.12f),
                    CYAN_MATERIAL_PATH);
                var stableMarker = _ensureMarker(gauntlet.transform, "Coolant Line Stable Marker", PrimitiveType.Cylinder,
                    sealSocket.localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(0.95f, 0.025f, 0.95f),
                    CYAN_MATERIAL_PATH);
                var objective = gauntlet.GetComponent<AuthoredCoolantReclamationObjective>();
                if (objective == null)
                {
                    objective = gauntlet.AddComponent<AuthoredCoolantReclamationObjective>();
                }
                objective.Configure(firstBaffleAnchor, secondBaffleAnchor, sealSocket, releaseAnchor,
                    firstBaffleMarker.gameObject, secondBaffleMarker.gameObject, releaseMarker.gameObject, stableMarker.gameObject);
                PrefabUtility.SaveAsPrefabAsset(gauntlet, GAUNTLET_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(gauntlet);
            }
        }

        private static void _ensureBaffle(Transform parent, GameObject prefab, string objectName, Vector3 localPosition)
        {
            var baffle = parent.Find(objectName);
            if (baffle == null)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException($"Could not instantiate {objectName} for the coolant gauntlet.");
                }

                instance.name = objectName;
                instance.transform.SetParent(parent, false);
                baffle = instance.transform;
            }

            baffle.localPosition = localPosition;
            baffle.localRotation = Quaternion.identity;
            baffle.localScale = Vector3.one;
        }

        private static Transform _ensureAnchor(Transform parent, string objectName, Vector3 localPosition)
        {
            var anchor = parent.Find(objectName);
            if (anchor == null)
            {
                anchor = new GameObject(objectName).transform;
                anchor.SetParent(parent, false);
            }

            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static Transform _ensureMarker(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            string materialPath)
        {
            var marker = parent.Find(objectName);
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(primitiveType).transform;
                marker.name = objectName;
                marker.SetParent(parent, false);
            }

            marker.localPosition = localPosition;
            marker.localRotation = Quaternion.identity;
            marker.localScale = localScale;
            if (marker.TryGetComponent<Collider>(out var collider))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!marker.TryGetComponent<Renderer>(out var renderer) || material == null)
            {
                throw new InvalidOperationException($"Could not configure {objectName} with {materialPath}.");
            }

            renderer.sharedMaterial = material;
            return marker;
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
