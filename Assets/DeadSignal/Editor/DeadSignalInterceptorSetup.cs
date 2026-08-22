using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalInterceptorSetup
    {
        private const string ACTOR_PREFAB_PATH = "Assets/DeadSignal/Resources/Actors/SecurityInterceptorAssembly.prefab";
        private const string ENTRANCE_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/InterceptorEntryGate.prefab";
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SecurityWardenArmor.mat";
        private const string RED_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SecurityWardenEye.mat";
        private const string AMBER_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/EastVaultEnergy.mat";

        private static readonly Vector3 s_northPosition = new(-16.4f, 0f, 7.1f);
        private static readonly Vector3 s_southPosition = new(1.5f, 0f, -7.5f);

        public static bool HasAssets
        {
            get
            {
                var actor = AssetDatabase.LoadAssetAtPath<GameObject>(ACTOR_PREFAB_PATH);
                var entrance = AssetDatabase.LoadAssetAtPath<GameObject>(ENTRANCE_PREFAB_PATH);
                return _hasActorParts(actor) &&
                       entrance != null &&
                       entrance.GetComponent<AuthoredInterceptorEntrance>() != null;
            }
        }

        public static void EnsureAssets()
        {
            _ensureActorPrefab();
            _ensureEntrancePrefab();
            _ensureScenePlacements();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Security Interceptor assets are incomplete.");
            }
        }

        private static void _ensureActorPrefab()
        {
            var root = new GameObject("SecurityInterceptorAssembly");
            try
            {
                _createPart(root.transform, "Interceptor Chassis", PrimitiveType.Cube,
                    new Vector3(0f, 0.3f, 0f), new Vector3(0.75f, 0.28f, 1.1f), ARMOR_MATERIAL_PATH);
                _createPart(root.transform, "Interceptor Blade Left", PrimitiveType.Cube,
                    new Vector3(-0.56f, 0.22f, 0f), new Vector3(0.16f, 0.12f, 1.5f), RED_MATERIAL_PATH);
                _createPart(root.transform, "Interceptor Blade Right", PrimitiveType.Cube,
                    new Vector3(0.56f, 0.22f, 0f), new Vector3(0.16f, 0.12f, 1.5f), RED_MATERIAL_PATH);
                _createPart(root.transform, "Interceptor Core", PrimitiveType.Sphere,
                    new Vector3(0f, 0.42f, -0.3f), new Vector3(0.26f, 0.18f, 0.26f), AMBER_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, ACTOR_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureEntrancePrefab()
        {
            var root = new GameObject("InterceptorEntryGate");
            try
            {
                root.AddComponent<AuthoredInterceptorEntrance>();
                _createPart(root.transform, "Gate Rail Left", PrimitiveType.Cube,
                    new Vector3(-0.85f, 0.22f, 0f), new Vector3(0.18f, 0.44f, 1.8f), ARMOR_MATERIAL_PATH);
                _createPart(root.transform, "Gate Rail Right", PrimitiveType.Cube,
                    new Vector3(0.85f, 0.22f, 0f), new Vector3(0.18f, 0.44f, 1.8f), ARMOR_MATERIAL_PATH);
                _createPart(root.transform, "Gate Warning Bar", PrimitiveType.Cube,
                    new Vector3(0f, 0.08f, 0f), new Vector3(1.45f, 0.04f, 0.22f), RED_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, ENTRANCE_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureScenePlacements()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            _ensureScenePlacement(scene, "North Interceptor Flank Gate", s_northPosition, 0);
            _ensureScenePlacement(scene, "South Interceptor Flank Gate", s_southPosition, 1);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _ensureScenePlacement(Scene scene, string objectName, Vector3 position, int priority)
        {
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == objectName);
            if (existing == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENTRANCE_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException($"Could not place {objectName} in SampleScene.");
                }

                existing.name = objectName;
            }

            existing.transform.position = position;
            existing.transform.rotation = priority == 0 ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            existing.GetComponent<AuthoredInterceptorEntrance>().Configure(priority);
        }

        private static void _createPart(
            Transform parent,
            string objectName,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            string materialPath)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing Interceptor material: {materialPath}");
            }

            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static bool _hasActorParts(GameObject actor)
        {
            return actor != null &&
                   actor.transform.Find("Interceptor Chassis") != null &&
                   actor.transform.Find("Interceptor Blade Left") != null &&
                   actor.transform.Find("Interceptor Blade Right") != null &&
                   actor.transform.Find("Interceptor Core") != null;
        }
    }
}
