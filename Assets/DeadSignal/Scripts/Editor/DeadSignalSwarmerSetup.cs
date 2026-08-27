using System;
using DeadSignal.Combat;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSwarmerSetup
    {
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Actors/SwarmerAssembly.prefab";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SwarmerPressureTuning.asset";
        private const string RED_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SecurityRed.mat";
        private const string WHITE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/DroneWhite.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                return prefab != null && prefab.transform.Find("Swarmer Body") != null &&
                       prefab.transform.Find("Swarmer Core") != null &&
                       prefab.transform.Find("Swarmer Needle") != null &&
                       AssetDatabase.LoadAssetAtPath<SwarmerPressureTuning>(TUNING_PATH) != null;
            }
        }

        [MenuItem("Tools/DEAD SIGNAL/Ensure Swarmer Pressure Assets")]
        public static void EnsureAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<SwarmerPressureTuning>(TUNING_PATH) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SwarmerPressureTuning>(), TUNING_PATH);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null)
            {
                var red = _material(RED_MATERIAL_PATH);
                var white = _material(WHITE_MATERIAL_PATH);
                var amber = _material(AMBER_MATERIAL_PATH);
                var root = new GameObject("Swarmer Assembly");
                _part(root.transform, "Swarmer Body", PrimitiveType.Cube, Vector3.zero,
                    new Vector3(0.58f, 0.16f, 0.58f), Quaternion.Euler(0f, 45f, 0f), red);
                _part(root.transform, "Swarmer Core", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0f),
                    Vector3.one * 0.26f, Quaternion.identity, white);
                _part(root.transform, "Swarmer Needle", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.42f),
                    new Vector3(0.12f, 0.12f, 0.38f), Quaternion.identity, amber);
                _part(root.transform, "Swarmer Tail", PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.34f),
                    new Vector3(0.3f, 0.08f, 0.16f), Quaternion.identity, red);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Swarmer prefab or pressure tuning asset is incomplete.");
            }
        }

        private static Material _material(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing Swarmer material: {path}");
            }
            return material;
        }

        private static void _part(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }
    }
}
