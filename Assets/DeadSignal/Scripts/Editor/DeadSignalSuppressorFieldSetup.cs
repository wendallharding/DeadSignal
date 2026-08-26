using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSuppressorFieldSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/SuppressorFieldActive.png";

        public static bool HasAssets => AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null;

        public static void EnsureAssets()
        {
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Suppressor field texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Suppressor field texture was not imported successfully.");
            }
        }
    }
}
