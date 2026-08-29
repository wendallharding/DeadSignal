using System;
using DeadSignal.Presentation;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalWeaponTransformationFeedbackSetup
    {
        private const string PIERCING_GLYPH_PATH =
            "Assets/DeadSignal/Resources/VFX/PiercingPulseTransformationGlyph.png";
        private const string RICOCHET_GLYPH_PATH =
            "Assets/DeadSignal/Resources/VFX/ControlledRicochetTransformationGlyph.png";
        private const string TUNING_PATH =
            "Assets/DeadSignal/Resources/Tuning/WeaponTransformationFeedbackTuning.asset";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(PIERCING_GLYPH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(RICOCHET_GLYPH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<WeaponTransformationFeedbackTuning>(TUNING_PATH) != null;

        [MenuItem("Dead Signal/Setup/Ensure Weapon Transformation Feedback")]
        public static void EnsureAssets()
        {
            _configureTexture(PIERCING_GLYPH_PATH);
            _configureTexture(RICOCHET_GLYPH_PATH);

            if (AssetDatabase.LoadAssetAtPath<WeaponTransformationFeedbackTuning>(TUNING_PATH) == null)
            {
                var tuning = ScriptableObject.CreateInstance<WeaponTransformationFeedbackTuning>();
                AssetDatabase.CreateAsset(tuning, TUNING_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Weapon transformation feedback assets were not imported successfully.");
            }
        }

        private static void _configureTexture(string texturePath)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the weapon transformation glyph at {texturePath}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }
}
