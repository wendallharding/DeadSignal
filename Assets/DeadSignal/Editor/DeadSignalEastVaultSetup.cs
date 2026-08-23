using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.Salvage;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalEastVaultSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/EastSalvageVaultAlbedo.png";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/EastSalvageVaultModel.fbx";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab";
        private const string SHELL_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/MaintenanceRoomShell.prefab";
        private const string DECK_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/EastVaultDeck.mat";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/EastVaultArmor.mat";
        private const string COPPER_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/EastVaultCopper.mat";
        private const string ENERGY_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/EastVaultEnergy.mat";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SalvagePresentationTuning.asset";
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

        private static readonly Vector3 s_roomPosition = new(16.7f, 0f, 0f);

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<SalvagePresentationTuning>(TUNING_PATH) != null &&
            _hasValidPrefab() &&
            _hasOpenEastDoorway();

        [MenuItem("DEAD SIGNAL/Setup Optional East Salvage Vault")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _configureModel();
            _ensureMaterials();
            _ensurePrefab();
            _assignMaterials();
            _ensureSalvageTuning();
            _ensureEastDoorway();
            _ensureScenePlacement();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The optional east salvage-vault assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the east-vault texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _configureModel()
        {
            var importer = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the east-vault model at {MODEL_PATH}.");
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
            _configureMaterial(DECK_MATERIAL_PATH, "EastVaultDeck", Color.white, Color.black, 0.42f, texture);
            _configureMaterial(ARMOR_MATERIAL_PATH, "EastVaultArmor", Color.white, Color.black, 0.36f, texture);
            _configureMaterial(COPPER_MATERIAL_PATH, "EastVaultCopper", new Color(0.72f, 0.42f, 0.24f), Color.black, 0.48f, texture);
            _configureMaterial(ENERGY_MATERIAL_PATH, "EastVaultEnergy", new Color(1f, 0.25f, 0.02f),
                new Color(3.2f, 0.42f, 0.02f), 0.18f, null);
        }

        private static void _configureMaterial(
            string path,
            string materialName,
            Color baseColor,
            Color emissionColor,
            float smoothness,
            Texture2D texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for east-vault materials.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = baseColor;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            material.SetColor("_EmissionColor", emissionColor);
            if (emissionColor.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            material.mainTexture = texture;
            material.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(material);
        }

        private static void _ensurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported east-vault model.");
                }

                instance.name = "East Salvage Vault";
                PrefabUtility.SaveAsPrefabAsset(instance, PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                foreach (var importedObstacle in prefabRoot.GetComponentsInChildren<AuthoredMapObstacle>())
                {
                    UnityEngine.Object.DestroyImmediate(importedObstacle);
                }

                _ensureObstacle(prefabRoot.transform, "Vault North Wall Bounds", new Vector3(0f, 0f, -3.15f),
                    new Vector2(3.3f, 0.16f));
                _ensureObstacle(prefabRoot.transform, "Vault South Wall Bounds", new Vector3(0f, 0f, 3.15f),
                    new Vector2(3.3f, 0.16f));
                _ensureObstacle(prefabRoot.transform, "Vault East Wall Bounds", new Vector3(-3.15f, 0f, 0f),
                    new Vector2(0.16f, 3.3f));
                _ensureObstacle(prefabRoot.transform, "Vault West North Gate Bounds", new Vector3(3.15f, 0f, -2.15f),
                    new Vector2(0.16f, 1f));
                _ensureObstacle(prefabRoot.transform, "Vault West South Gate Bounds", new Vector3(3.15f, 0f, 2.15f),
                    new Vector2(0.16f, 1f));
                _ensureObstacle(prefabRoot.transform, "Vault Route Splitter Bounds", new Vector3(-0.45f, 0f, 0f),
                    new Vector2(0.55f, 1.25f));

                var socket = prefabRoot.transform.Find("Optional Salvage Socket");
                if (socket == null)
                {
                    var socketObject = new GameObject("Optional Salvage Socket");
                    socketObject.transform.SetParent(prefabRoot.transform, false);
                    socket = socketObject.transform;
                }

                // The imported FBX basis mirrors Blender X; scene placement rotates the module so its doorway faces west.
                socket.localPosition = new Vector3(-2f, 0f, 0f);
                socket.localRotation = Quaternion.identity;
                if (!socket.TryGetComponent<AuthoredSalvageSocket>(out _))
                {
                    socket.gameObject.AddComponent<AuthoredSalvageSocket>();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void _assignMaterials()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                _assignMaterial(prefabRoot.transform, "Vault Floor", DECK_MATERIAL_PATH);
                foreach (var partName in new[]
                         {
                             "Vault North Wall", "Vault South Wall", "Vault East Wall",
                             "Vault West North Gate", "Vault West South Gate"
                         })
                {
                    _assignMaterial(prefabRoot.transform, partName, ARMOR_MATERIAL_PATH);
                }

                _assignMaterial(prefabRoot.transform, "Vault Route Splitter", COPPER_MATERIAL_PATH);
                _assignMaterial(prefabRoot.transform, "Vault Energy Guides", ENERGY_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void _ensureSalvageTuning()
        {
            if (AssetDatabase.LoadAssetAtPath<SalvagePresentationTuning>(TUNING_PATH) != null)
            {
                return;
            }

            var tuning = ScriptableObject.CreateInstance<SalvagePresentationTuning>();
            AssetDatabase.CreateAsset(tuning, TUNING_PATH);
        }

        private static void _ensureEastDoorway()
        {
            var shellRoot = PrefabUtility.LoadPrefabContents(SHELL_PREFAB_PATH);
            try
            {
                var bulkheads = shellRoot.transform.Find("Bulkheads");
                if (bulkheads == null)
                {
                    throw new InvalidOperationException("The maintenance room shell has no Bulkheads root.");
                }

                var oldEastWall = bulkheads.Find("East Bulkhead");
                Material material = null;
                if (oldEastWall != null)
                {
                    if (oldEastWall.TryGetComponent<Renderer>(out var renderer))
                    {
                        material = renderer.sharedMaterial;
                    }

                    UnityEngine.Object.DestroyImmediate(oldEastWall.gameObject);
                }

                _ensureDoorwaySegment(bulkheads, "East Bulkhead North", 5.425f, material);
                _ensureDoorwaySegment(bulkheads, "East Bulkhead South", -5.425f, material);
                PrefabUtility.SaveAsPrefabAsset(shellRoot, SHELL_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(shellRoot);
            }
        }

        private static void _ensureDoorwaySegment(Transform parent, string objectName, float zPosition, Material material)
        {
            var segment = parent.Find(objectName);
            if (segment == null)
            {
                var segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segmentObject.name = objectName;
                segmentObject.transform.SetParent(parent, false);
                var collider = segmentObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                segment = segmentObject.transform;
            }

            segment.localPosition = new Vector3(13.7f, 0.25f, zPosition);
            segment.localRotation = Quaternion.identity;
            segment.localScale = new Vector3(0.5f, 0.8f, 7.85f);
            if (material != null && segment.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
            }

            var obstacle = segment.GetComponent<AuthoredMapObstacle>();
            if (obstacle == null)
            {
                obstacle = segment.gameObject.AddComponent<AuthoredMapObstacle>();
            }

            obstacle.Configure(new Vector2(0.5f, 0.5f));
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Optional East Salvage Vault");
            if (existing == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the optional east salvage vault in SampleScene.");
                }

                existing.name = "Optional East Salvage Vault";
            }

            existing.transform.position = s_roomPosition;
            existing.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            existing.transform.localScale = Vector3.one;
            EditorSceneManager.SaveScene(scene);

            if (existing.GetComponentsInChildren<AuthoredMapObstacle>().Length != 6 ||
                existing.GetComponentsInChildren<AuthoredSalvageSocket>().Length != 1)
            {
                throw new InvalidOperationException("The scene-authored east vault is missing blockers or its salvage socket.");
            }
        }

        private static void _ensureObstacle(Transform root, string objectName, Vector3 localPosition, Vector2 halfSize)
        {
            var bounds = root.Find(objectName);
            if (bounds == null)
            {
                var boundsObject = new GameObject(objectName);
                boundsObject.transform.SetParent(root, false);
                bounds = boundsObject.transform;
            }

            bounds.localPosition = localPosition;
            bounds.localRotation = Quaternion.identity;
            bounds.localScale = Vector3.one;
            var obstacle = bounds.GetComponent<AuthoredMapObstacle>();
            if (obstacle == null)
            {
                obstacle = bounds.gameObject.AddComponent<AuthoredMapObstacle>();
            }

            obstacle.Configure(halfSize);
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

        private static bool _hasValidPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null || prefab.GetComponentsInChildren<AuthoredMapObstacle>().Length != 6 ||
                prefab.GetComponentsInChildren<AuthoredSalvageSocket>().Length != 1)
            {
                return false;
            }

            var meshes = prefab.GetComponentsInChildren<MeshFilter>();
            return meshes.Length == 8 && meshes.All(filter => filter.sharedMesh != null) &&
                   prefab.GetComponentsInChildren<Collider>().Length == 0;
        }

        private static bool _hasOpenEastDoorway()
        {
            var shell = AssetDatabase.LoadAssetAtPath<GameObject>(SHELL_PREFAB_PATH);
            var bulkheads = shell != null ? shell.transform.Find("Bulkheads") : null;
            return bulkheads != null &&
                   bulkheads.Find("East Bulkhead") == null &&
                   bulkheads.Find("East Bulkhead North")?.GetComponent<AuthoredMapObstacle>() != null &&
                   bulkheads.Find("East Bulkhead South")?.GetComponent<AuthoredMapObstacle>() != null;
        }
    }
}
