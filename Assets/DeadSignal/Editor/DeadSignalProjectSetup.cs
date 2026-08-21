using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalProjectSetup
    {
        private const string RESOURCES_FOLDER = "Assets/DeadSignal/Resources";
        private const string MATERIALS_FOLDER = RESOURCES_FOLDER + "/Materials";
        private const string ENVIRONMENT_FOLDER = RESOURCES_FOLDER + "/Environment";
        private const string REFLEX_SETTINGS_PATH = RESOURCES_FOLDER + "/ReflexSettings.asset";
        private const string RUNTIME_LIT_MATERIAL_PATH = MATERIALS_FOLDER + "/RuntimeLitTemplate.mat";
        private const string RUNTIME_PARTICLE_MATERIAL_PATH = MATERIALS_FOLDER + "/RuntimeParticleTemplate.mat";
        private const string TOWER_ACTIVATION_SWEEP_PATH = RESOURCES_FOLDER + "/VFX/TowerNetworkActivationSweep.png";
        private const string MAINTENANCE_DECK_TEXTURE_PATH = ENVIRONMENT_FOLDER + "/MaintenanceDeckPanel.png";
        private const string MAINTENANCE_DECK_PREFAB_PATH = ENVIRONMENT_FOLDER + "/MaintenanceDeckModule.prefab";
        private const string MAINTENANCE_BULKHEAD_TEXTURE_PATH = ENVIRONMENT_FOLDER + "/MaintenanceBulkheadPanel.png";
        private const string MAINTENANCE_ROOM_SHELL_PREFAB_PATH = ENVIRONMENT_FOLDER + "/MaintenanceRoomShell.prefab";
        private const string SIGNAL_TOWER_TEXTURE_PATH = ENVIRONMENT_FOLDER + "/SignalTowerHousingPanel.png";
        private const string SIGNAL_TOWER_PREFAB_PATH = ENVIRONMENT_FOLDER + "/SignalTowerAssembly.prefab";
        private const string EXTRACTION_DOCK_TEXTURE_PATH = ENVIRONMENT_FOLDER + "/ExtractionDockPanel.png";
        private const string EXTRACTION_PAD_PREFAB_PATH = ENVIRONMENT_FOLDER + "/ExtractionPadAssembly.prefab";
        private const string CREATE_REFLEX_SETTINGS_MENU = "Assets/Create/Reflex/Settings";

        public static bool HasReflexSettings =>
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(REFLEX_SETTINGS_PATH) != null;

        public static bool HasRuntimeMaterialTemplates =>
            AssetDatabase.LoadAssetAtPath<Material>(RUNTIME_LIT_MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(RUNTIME_PARTICLE_MATERIAL_PATH) != null;

        public static bool HasMaintenanceDeckAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(MAINTENANCE_DECK_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(MAINTENANCE_DECK_PREFAB_PATH) != null;

        public static bool HasMaintenanceRoomShellAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(MAINTENANCE_BULKHEAD_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(MAINTENANCE_ROOM_SHELL_PREFAB_PATH) != null;

        public static bool HasSignalTowerAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(SIGNAL_TOWER_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(SIGNAL_TOWER_PREFAB_PATH) != null;

        public static bool HasExtractionPadAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(EXTRACTION_DOCK_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(EXTRACTION_PAD_PREFAB_PATH) != null;

        public static void EnsureReflexSettings()
        {
            if (HasReflexSettings)
            {
                return;
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(RESOURCES_FOLDER);
            if (!EditorApplication.ExecuteMenuItem(CREATE_REFLEX_SETTINGS_MENU))
            {
                throw new InvalidOperationException($"Could not execute the Reflex settings menu at {CREATE_REFLEX_SETTINGS_MENU}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasReflexSettings)
            {
                throw new InvalidOperationException($"Reflex settings were not created at {REFLEX_SETTINGS_PATH}.");
            }
        }

        public static void EnsureRuntimeMaterialTemplates()
        {
            if (!AssetDatabase.IsValidFolder(MATERIALS_FOLDER))
            {
                AssetDatabase.CreateFolder(RESOURCES_FOLDER, "Materials");
            }

            _ensureMaterial(RUNTIME_LIT_MATERIAL_PATH, "Universal Render Pipeline/Lit");
            _ensureMaterial(RUNTIME_PARTICLE_MATERIAL_PATH, "Universal Render Pipeline/Particles/Unlit");
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureTowerActivationSweepTexture()
        {
            var importer = AssetImporter.GetAtPath(TOWER_ACTIVATION_SWEEP_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the tower activation sweep at {TOWER_ACTIVATION_SWEEP_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        public static void EnsureMaintenanceDeckAssets()
        {
            var importer = AssetImporter.GetAtPath(MAINTENANCE_DECK_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the maintenance deck texture at {MAINTENANCE_DECK_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(MAINTENANCE_DECK_PREFAB_PATH) == null)
            {
                var module = GameObject.CreatePrimitive(PrimitiveType.Cube);
                module.name = "Maintenance Deck Module";
                var collider = module.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                PrefabUtility.SaveAsPrefabAsset(module, MAINTENANCE_DECK_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(module);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasMaintenanceDeckAssets)
            {
                throw new InvalidOperationException("The maintenance deck texture and prefab were not created successfully.");
            }
        }

        public static void EnsureMaintenanceRoomShellAssets()
        {
            var importer = AssetImporter.GetAtPath(MAINTENANCE_BULKHEAD_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the maintenance bulkhead texture at {MAINTENANCE_BULKHEAD_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(MAINTENANCE_ROOM_SHELL_PREFAB_PATH) == null)
            {
                var shell = new GameObject("Maintenance Room Shell");
                var bulkheads = new GameObject("Bulkheads");
                bulkheads.transform.SetParent(shell.transform, false);
                _createPrefabCube("North Bulkhead", new Vector3(0f, 0.25f, 9.1f),
                    new Vector3(27.8f, 0.8f, 0.5f), bulkheads.transform);
                _createPrefabCube("South Bulkhead", new Vector3(0f, 0.25f, -9.1f),
                    new Vector3(27.8f, 0.8f, 0.5f), bulkheads.transform);
                _createPrefabCube("East Bulkhead", new Vector3(13.7f, 0.25f, 0f),
                    new Vector3(0.5f, 0.8f, 18.7f), bulkheads.transform);
                _createPrefabCube("West Bulkhead", new Vector3(-13.7f, 0.25f, 0f),
                    new Vector3(0.5f, 0.8f, 18.7f), bulkheads.transform);

                var sockets = new GameObject("Machine Sockets");
                sockets.transform.SetParent(shell.transform, false);
                Vector3[] machineLocations =
                {
                    new(-11.6f, 0f, 6.8f), new(-8.8f, 0f, 6.9f), new(10.8f, 0f, 6.8f),
                    new(11.2f, 0f, -6.7f), new(4.8f, 0f, -7.1f), new(-3.8f, 0f, 7.1f)
                };
                for (int i = 0; i < machineLocations.Length; i++)
                {
                    var socket = new GameObject($"Machine Socket {i + 1:00}");
                    socket.transform.SetParent(sockets.transform, false);
                    socket.transform.localPosition = machineLocations[i];
                }

                PrefabUtility.SaveAsPrefabAsset(shell, MAINTENANCE_ROOM_SHELL_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(shell);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasMaintenanceRoomShellAssets)
            {
                throw new InvalidOperationException("The maintenance bulkhead texture and room-shell prefab were not created successfully.");
            }
        }

        public static void EnsureSignalTowerAssets()
        {
            var importer = AssetImporter.GetAtPath(SIGNAL_TOWER_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal tower texture at {SIGNAL_TOWER_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(SIGNAL_TOWER_PREFAB_PATH) == null)
            {
                var tower = new GameObject("Signal Tower Assembly");
                _createPrefabPrimitive("Tower Base", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f),
                    new Vector3(2.2f, 0.25f, 2.2f), tower.transform);
                _createPrefabPrimitive("Tower Column", PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f),
                    new Vector3(0.8f, 1.35f, 0.8f), tower.transform);
                _createPrefabPrimitive("Tower Core", PrimitiveType.Cylinder, new Vector3(0f, 1.65f, 0f),
                    new Vector3(1.35f, 0.22f, 1.35f), tower.transform);

                PrefabUtility.SaveAsPrefabAsset(tower, SIGNAL_TOWER_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(tower);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasSignalTowerAssets)
            {
                throw new InvalidOperationException("The Signal tower texture and assembly prefab were not created successfully.");
            }
        }

        public static void EnsureExtractionPadAssets()
        {
            var importer = AssetImporter.GetAtPath(EXTRACTION_DOCK_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the extraction dock texture at {EXTRACTION_DOCK_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(EXTRACTION_PAD_PREFAB_PATH) == null)
            {
                var extractionPad = new GameObject("Extraction Pad Assembly");
                _createPrefabPrimitive("Extraction Plinth", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
                    new Vector3(3.2f, 0.08f, 3.2f), extractionPad.transform);
                _createPrefabPrimitive("Extraction Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f),
                    new Vector3(2.55f, 0.08f, 2.55f), extractionPad.transform);
                _createPrefabPrimitive("Extraction Center", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f),
                    new Vector3(2.1f, 0.08f, 2.1f), extractionPad.transform);
                _createPrefabPrimitive("Extraction Beacon", PrimitiveType.Cube, new Vector3(0f, 0.7f, 1.5f),
                    new Vector3(0.22f, 1.4f, 0.22f), extractionPad.transform);

                PrefabUtility.SaveAsPrefabAsset(extractionPad, EXTRACTION_PAD_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(extractionPad);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasExtractionPadAssets)
            {
                throw new InvalidOperationException("The extraction dock texture and assembly prefab were not created successfully.");
            }
        }

        private static void _ensureMaterial(string assetPath, string shaderName)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(assetPath) != null)
            {
                return;
            }

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Could not find required shader {shaderName}.");
            }

            AssetDatabase.CreateAsset(new Material(shader), assetPath);
        }

        private static void _createPrefabCube(string objectName, Vector3 position, Vector3 scale, Transform parent)
        {
            _createPrefabPrimitive(objectName, PrimitiveType.Cube, position, scale, parent);
        }

        private static void _createPrefabPrimitive(
            string objectName,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Transform parent)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }
    }
}
