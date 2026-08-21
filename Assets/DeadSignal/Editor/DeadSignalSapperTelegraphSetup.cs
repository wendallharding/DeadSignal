using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSapperTelegraphSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/SapperDrainGlyph.png";
        private const string TUNING_DIRECTORY = "Assets/DeadSignal/Resources/Tuning";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SignalSapperTelegraphTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<SignalSapperTelegraphTuning>(TUNING_PATH) != null;

        public static void EnsureAssets()
        {
            var textureImporter = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (textureImporter == null)
            {
                throw new InvalidOperationException($"Could not find the Sapper drain glyph at {TEXTURE_PATH}.");
            }

            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = true;
            textureImporter.maxTextureSize = 1024;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<SignalSapperTelegraphTuning>(TUNING_PATH) == null)
            {
                if (!AssetDatabase.IsValidFolder(TUNING_DIRECTORY))
                {
                    AssetDatabase.CreateFolder("Assets/DeadSignal/Resources", "Tuning");
                }

                var tuning = ScriptableObject.CreateInstance<SignalSapperTelegraphTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Sapper telegraph texture and tuning asset were not created successfully.");
            }
        }
    }
}
