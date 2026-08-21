using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSapperTelegraphSetup
    {
        private const string PULSE_TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/SapperDrainGlyph.png";
        private const string TETHER_TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/SapperTetherFlow.png";
        private const string TUNING_DIRECTORY = "Assets/DeadSignal/Resources/Tuning";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SignalSapperTelegraphTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(PULSE_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(TETHER_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<SignalSapperTelegraphTuning>(TUNING_PATH) != null;

        public static void EnsureAssets()
        {
            _configureTexture(PULSE_TEXTURE_PATH, TextureWrapMode.Clamp);
            _configureTexture(TETHER_TEXTURE_PATH, TextureWrapMode.Repeat);

            if (AssetDatabase.LoadAssetAtPath<SignalSapperTelegraphTuning>(TUNING_PATH) == null)
            {
                if (!AssetDatabase.IsValidFolder(TUNING_DIRECTORY))
                {
                    AssetDatabase.CreateFolder("Assets/DeadSignal/Resources", "Tuning");
                }

                var tuning = ScriptableObject.CreateInstance<SignalSapperTelegraphTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            var existingTuning = AssetDatabase.LoadAssetAtPath<SignalSapperTelegraphTuning>(TUNING_PATH);
            EditorUtility.SetDirty(existingTuning);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Sapper telegraph textures and tuning asset were not created successfully.");
            }
        }

        private static void _configureTexture(string texturePath, TextureWrapMode wrapMode)
        {
            var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Sapper telegraph texture at {texturePath}.");
            }

            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = true;
            textureImporter.maxTextureSize = 1024;
            textureImporter.wrapMode = wrapMode;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.SaveAndReimport();
        }
    }
}
