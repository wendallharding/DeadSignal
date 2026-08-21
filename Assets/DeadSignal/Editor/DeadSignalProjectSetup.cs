using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalProjectSetup
    {
        private const string RESOURCES_FOLDER = "Assets/DeadSignal/Resources";
        private const string MATERIALS_FOLDER = RESOURCES_FOLDER + "/Materials";
        private const string REFLEX_SETTINGS_PATH = RESOURCES_FOLDER + "/ReflexSettings.asset";
        private const string RUNTIME_LIT_MATERIAL_PATH = MATERIALS_FOLDER + "/RuntimeLitTemplate.mat";
        private const string RUNTIME_PARTICLE_MATERIAL_PATH = MATERIALS_FOLDER + "/RuntimeParticleTemplate.mat";
        private const string TOWER_ACTIVATION_SWEEP_PATH = RESOURCES_FOLDER + "/VFX/TowerNetworkActivationSweep.png";
        private const string CREATE_REFLEX_SETTINGS_MENU = "Assets/Create/Reflex/Settings";

        public static bool HasReflexSettings =>
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(REFLEX_SETTINGS_PATH) != null;

        public static bool HasRuntimeMaterialTemplates =>
            AssetDatabase.LoadAssetAtPath<Material>(RUNTIME_LIT_MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(RUNTIME_PARTICLE_MATERIAL_PATH) != null;

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
    }
}
