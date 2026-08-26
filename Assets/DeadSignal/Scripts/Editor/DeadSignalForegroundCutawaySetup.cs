using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalForegroundCutawaySetup
    {
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/VFX/ForegroundCutawayFootprint.png";
        private const string MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/ForegroundCutawayFootprint.mat";
        private const string AUTHORED_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/VFX/ForegroundCutawayFootprintAuthored.png";
        private const string AUTHORED_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/ForegroundCutawayFootprintAuthored.mat";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string BINDINGS_ROOT_NAME = "Authored Foreground Cutaway Bindings";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Texture2D>(AUTHORED_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(AUTHORED_MATERIAL_PATH) != null;

        public static void EnsureAssets()
        {
            AssetDatabase.Refresh();
            _configureTexture(TEXTURE_PATH);
            _configureTexture(AUTHORED_TEXTURE_PATH);
            _ensureMaterial(MATERIAL_PATH, TEXTURE_PATH, "ForegroundCutawayFootprint");
            _ensureMaterial(AUTHORED_MATERIAL_PATH, AUTHORED_TEXTURE_PATH, "ForegroundCutawayFootprintAuthored");
            _ensureAuthoredSceneBindings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The foreground-cutaway footprint assets are incomplete.");
            }
        }

        private static void _configureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the foreground-cutaway texture at {path}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterial(string materialPath, string texturePath, string materialName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/DeadSignal/Resources/Materials/RuntimeParticleTemplate.mat");
                if (template == null)
                {
                    throw new InvalidOperationException("The runtime particle material template is missing.");
                }

                material = new Material(template) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(0.72f, 0.82f, 0.86f, 0.72f));
            material.SetColor("_Color", new Color(0.72f, 0.82f, 0.86f, 0.72f));
            material.SetFloat("_Cutoff", 0.12f);
            EditorUtility.SetDirty(material);
        }

        private static void _ensureAuthoredSceneBindings()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = GameObject.Find(BINDINGS_ROOT_NAME);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            var bindingRoot = new GameObject(BINDINGS_ROOT_NAME);
            var authoredWorld = GameObject.Find("DEAD SIGNAL — Authored World");
            if (authoredWorld != null)
            {
                bindingRoot.transform.SetParent(authoredWorld.transform, false);
            }

            var obstacles = UnityEngine.Object.FindObjectsByType<AuthoredMapObstacle>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var bindings = 0;
            foreach (var renderer in renderers)
            {
                if (!_requiresExplicitBinding(renderer))
                {
                    continue;
                }

                var owner = _findCollisionOwner(renderer.name, obstacles);
                var bindingObject = new GameObject($"{renderer.name} Cutaway");
                bindingObject.transform.SetParent(bindingRoot.transform, false);
                bindingObject.AddComponent<AuthoredForegroundCutaway>().Configure(owner, renderer);
                bindings++;
            }

            if (bindings < 4)
            {
                throw new InvalidOperationException(
                    $"Expected at least four explicit wall-shell cutaway bindings, but authored {bindings}.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static bool _requiresExplicitBinding(Renderer renderer)
        {
            if (renderer.GetComponentInParent<AuthoredMapObstacle>() != null)
            {
                return false;
            }

            return renderer.name.Contains("Bulkhead", StringComparison.Ordinal) ||
                   renderer.name.Contains("Wall", StringComparison.Ordinal) ||
                   renderer.name == "Cargo Shutter Housing";
        }

        private static AuthoredMapObstacle _findCollisionOwner(
            string rendererName,
            IReadOnlyList<AuthoredMapObstacle> obstacles)
        {
            var expectedName = rendererName == "Cargo Shutter Housing"
                ? "Departure Cargo Shutter"
                : $"{rendererName} Bounds";
            foreach (var obstacle in obstacles)
            {
                if (obstacle.name == expectedName)
                {
                    return obstacle;
                }
            }

            return null;
        }
    }
}
