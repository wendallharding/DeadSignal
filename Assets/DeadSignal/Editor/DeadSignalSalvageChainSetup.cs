using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSalvageChainSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/SalvageChainBurst.png";

        public static bool HasAssets => AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null;

        public static void EnsureAssets()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find salvage-chain art at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (!HasAssets)
            {
                throw new InvalidOperationException("Salvage-chain art was not imported successfully.");
            }
        }
    }
}
