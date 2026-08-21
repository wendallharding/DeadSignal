using System;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalActorSetup
    {
        public static void EnsureSecurityWardenAssets()
        {
            var importer = AssetImporter.GetAtPath(SECURITY_WARDEN_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Security Warden texture at {SECURITY_WARDEN_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_PREFAB_PATH) == null)
            {
                var warden = new GameObject("Security Warden Assembly");
                _createPrimitive("Warden Chassis", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f),
                    new Vector3(1.15f, 0.55f, 1.15f), warden.transform);
                _createPrimitive("Warden Eye", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.59f),
                    new Vector3(0.68f, 0.16f, 0.06f), warden.transform);
                _createPrimitive("Warden Crown", PrimitiveType.Cylinder, new Vector3(0f, 0.76f, 0f),
                    new Vector3(0.68f, 0.12f, 0.68f), warden.transform);

                PrefabUtility.SaveAsPrefabAsset(warden, SECURITY_WARDEN_PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(warden);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(SECURITY_WARDEN_TEXTURE_PATH) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(SECURITY_WARDEN_PREFAB_PATH) == null)
            {
                throw new InvalidOperationException("The Security Warden texture and assembly prefab were not created successfully.");
            }
        }

        private static void _createPrimitive(
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

        private const string SECURITY_WARDEN_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Actors/SecurityWardenPanel.png";
        private const string SECURITY_WARDEN_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Actors/SecurityWardenAssembly.prefab";
    }
}
