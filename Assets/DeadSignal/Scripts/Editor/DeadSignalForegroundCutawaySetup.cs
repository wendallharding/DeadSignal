using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalForegroundCutawaySetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/ForegroundCutawayFootprint.png";
        private const string MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/ForegroundCutawayFootprint.mat";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null;

        public static void EnsureAssets()
        {
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the foreground-cutaway texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/DeadSignal/Resources/Materials/RuntimeParticleTemplate.mat");
                if (template == null)
                {
                    throw new InvalidOperationException("The runtime particle material template is missing.");
                }

                material = new Material(template) { name = "ForegroundCutawayFootprint" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(0.72f, 0.82f, 0.86f, 0.72f));
            material.SetColor("_Color", new Color(0.72f, 0.82f, 0.86f, 0.72f));
            material.SetFloat("_Cutoff", 0.12f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The foreground-cutaway footprint assets are incomplete.");
            }
        }
    }
}
