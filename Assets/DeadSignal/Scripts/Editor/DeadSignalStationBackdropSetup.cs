using System;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalStationBackdropSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/StationUnderdeckAlbedo.png";
        private const string MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/StationUnderdeck.mat";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/StationUnderdeckBackdrop.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string ENVIRONMENT_PATH = "DEAD SIGNAL — Authored World/Environment";
        private const string BACKDROP_NAME = "Station Underdeck Backdrop";

        private static readonly Vector2 s_coverage = new(150f, 100f);

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var backdrop = prefab != null ? prefab.GetComponent<AuthoredStationBackdrop>() : null;
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       backdrop != null && backdrop.Coverage == s_coverage &&
                       prefab.GetComponentInChildren<Renderer>() != null &&
                       prefab.GetComponentInChildren<Collider>() == null;
            }
        }

        public static void EnsureAssets()
        {
            AssetDatabase.Refresh();
            _configureTextureImport();
            var material = _ensureMaterial();
            _ensurePrefab(material);
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The authored station underdeck backdrop is incomplete.");
            }
        }

        private static void _configureTextureImport()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the station underdeck texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material _ensureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/DeadSignal/Resources/Materials/RuntimeLitTemplate.mat");
                if (template == null)
                {
                    throw new InvalidOperationException("The runtime Lit material template is missing.");
                }

                material = new Material(template) { name = "StationUnderdeck" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(15f, 10f));
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", new Vector2(15f, 10f));
            material.SetColor("_BaseColor", new Color(0.46f, 0.5f, 0.56f, 1f));
            material.SetFloat("_Metallic", 0.18f);
            material.SetFloat("_Smoothness", 0.16f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _ensurePrefab(Material material)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Quad);
            try
            {
                instance.name = BACKDROP_NAME;
                instance.transform.localPosition = new Vector3(0f, -1.1f, 0f);
                instance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                instance.transform.localScale = new Vector3(s_coverage.x, s_coverage.y, 1f);
                UnityEngine.Object.DestroyImmediate(instance.GetComponent<Collider>());
                var renderer = instance.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                instance.AddComponent<AuthoredStationBackdrop>().Configure(s_coverage);
                PrefabUtility.SaveAsPrefabAsset(instance, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var environment = GameObject.Find(ENVIRONMENT_PATH);
            if (environment == null)
            {
                throw new InvalidOperationException($"The authored scene is missing {ENVIRONMENT_PATH}.");
            }

            var existing = environment.transform.Find(BACKDROP_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var instance = PrefabUtility.InstantiatePrefab(prefab, environment.transform) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Could not place the station underdeck backdrop in SampleScene.");
            }

            instance.name = BACKDROP_NAME;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
