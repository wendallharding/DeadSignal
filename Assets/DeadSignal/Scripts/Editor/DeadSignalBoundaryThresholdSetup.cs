using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalBoundaryThresholdSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/SignalBoundaryThreshold.png";
        private const string MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalBoundaryThreshold.mat";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SignalBoundaryThreshold.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";

        private static readonly Vector3 s_thresholdPosition = new(-6.25f, 0.038f, -3.54f);

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       prefab != null &&
                       prefab.GetComponentsInChildren<Renderer>().Length == 1 &&
                       prefab.GetComponentsInChildren<Collider>().Length == 0;
            }
        }

        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterial();
            _ensurePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Signal boundary-threshold assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal boundary texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Unlit shader for the Signal boundary threshold.");
                }

                material = new Material(shader) { name = "SignalBoundaryThreshold" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH));
            EditorUtility.SetDirty(material);
        }

        private static void _ensurePrefab()
        {
            var threshold = GameObject.CreatePrimitive(PrimitiveType.Quad);
            try
            {
                threshold.name = "SignalBoundaryThreshold";
                UnityEngine.Object.DestroyImmediate(threshold.GetComponent<Collider>());
                threshold.transform.rotation = Quaternion.Euler(90f, -35f, 0f);
                threshold.transform.localScale = new Vector3(2.2f, 3.2f, 1f);
                var renderer = threshold.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                PrefabUtility.SaveAsPrefabAsset(threshold, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(threshold);
            }
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Signal Boundary Threshold");
            if (existing == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the Signal boundary threshold in SampleScene.");
                }

                existing.name = "Signal Boundary Threshold";
                existing.transform.position = s_thresholdPosition;
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.GetComponentsInChildren<Renderer>().Length != 1 ||
                existing.GetComponentsInChildren<Collider>().Length != 0 ||
                Vector3.Distance(existing.transform.position, s_thresholdPosition) > 0.01f)
            {
                throw new InvalidOperationException("The SampleScene Signal boundary threshold is misplaced or blocks movement.");
            }
        }
    }
}
