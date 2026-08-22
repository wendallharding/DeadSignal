using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalSignalSpineSetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/SignalSpineInlay.png";
        private const string MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SignalSpineInlay.mat";
        private const string INLAY_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SignalSpineInlay.prefab";
        private const string ROUTE_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/OpeningSignalSpine.prefab";
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

        private static readonly Vector3[] s_routePositions =
        {
            new(-7.7f, 0.035f, -4.55f),
            new(-6.05f, 0.035f, -3.4f),
            new(-4.4f, 0.035f, -2.25f),
            new(-2.75f, 0.035f, -1.1f),
            new(-1.1f, 0.035f, 0.05f)
        };

        public static bool HasAssets
        {
            get
            {
                var inlay = AssetDatabase.LoadAssetAtPath<GameObject>(INLAY_PREFAB_PATH);
                var route = AssetDatabase.LoadAssetAtPath<GameObject>(ROUTE_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       inlay != null &&
                       inlay.GetComponentsInChildren<Renderer>().Length == 1 &&
                       inlay.GetComponentsInChildren<Collider>().Length == 0 &&
                       route != null &&
                       route.transform.childCount == s_routePositions.Length;
            }
        }

        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterial();
            _ensureInlayPrefab();
            _ensureRoutePrefab();
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The opening Signal-spine assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Signal-spine texture at {TEXTURE_PATH}.");
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
                    throw new InvalidOperationException("Could not find the URP Unlit shader for the Signal spine.");
                }

                material = new Material(shader) { name = "SignalSpineInlay" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH));
            material.SetTextureScale("_BaseMap", new Vector2(-1f, -1f));
            material.SetTextureOffset("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(material);
        }

        private static void _ensureInlayPrefab()
        {
            var inlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            try
            {
                inlay.name = "SignalSpineInlay";
                UnityEngine.Object.DestroyImmediate(inlay.GetComponent<Collider>());
                inlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                inlay.transform.localScale = new Vector3(1.45f, 1.45f, 1f);
                var renderer = inlay.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                PrefabUtility.SaveAsPrefabAsset(inlay, INLAY_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inlay);
            }
        }

        private static void _ensureRoutePrefab()
        {
            var route = new GameObject("OpeningSignalSpine");
            try
            {
                var inlayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(INLAY_PREFAB_PATH);
                for (var index = 0; index < s_routePositions.Length; index++)
                {
                    var inlay = PrefabUtility.InstantiatePrefab(inlayPrefab) as GameObject;
                    if (inlay == null)
                    {
                        throw new InvalidOperationException("Could not instantiate a Signal-spine inlay.");
                    }

                    inlay.name = $"Signal Spine Inlay {index + 1}";
                    inlay.transform.SetParent(route.transform, false);
                    inlay.transform.position = s_routePositions[index];
                    inlay.transform.rotation = Quaternion.Euler(90f, 125f, 0f);
                }

                PrefabUtility.SaveAsPrefabAsset(route, ROUTE_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(route);
            }
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Opening Signal Spine");
            if (existing == null)
            {
                var routePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ROUTE_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(routePrefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException("Could not place the opening Signal spine in SampleScene.");
                }

                existing.name = "Opening Signal Spine";
                EditorSceneManager.SaveScene(scene);
            }

            if (existing.transform.childCount != s_routePositions.Length ||
                existing.GetComponentsInChildren<Collider>().Length != 0)
            {
                throw new InvalidOperationException("The SampleScene Signal spine is incomplete or blocks movement.");
            }
        }
    }
}
