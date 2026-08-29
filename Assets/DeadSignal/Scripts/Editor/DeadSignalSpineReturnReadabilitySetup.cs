using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSpineReturnReadabilitySetup
    {
        private const string SPINE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab";
        private const string DOOR_THRESHOLD_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RouteDoorThresholdReadability.asset";
        private const string DOOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RouteDoorThresholdStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var spine = AssetDatabase.LoadAssetAtPath<GameObject>(SPINE_PREFAB_PATH);
                return spine != null &&
                       spine.TryGetComponent<AuthoredRouteDoorReadability>(out var readability) &&
                       readability.IsConfigured &&
                       spine.transform.Find("Spine Return Threshold") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Spine Return Readability")]
        public static void EnsureAssets()
        {
            _upgradeSpineReturn();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Spine return-gate readability assets are incomplete.");
            }
        }

        private static void _upgradeSpineReturn()
        {
            var root = PrefabUtility.LoadPrefabContents(SPINE_PREFAB_PATH);
            try
            {
                var blockingSlab = root.transform.Find("Capacitor Transfer Bank")?.gameObject;
                var openMarker = root.transform.Find("Spine Signal Lines")?.gameObject;
                if (blockingSlab == null || openMarker == null)
                {
                    throw new InvalidOperationException("The Spine return gate is missing its blocker or powered route.");
                }

                var threshold = root.transform.Find("Spine Return Threshold");
                if (threshold == null)
                {
                    threshold = new GameObject(
                        "Spine Return Threshold", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    threshold.SetParent(root.transform, false);
                }

                threshold.localPosition = new Vector3(-0.6f, 0f, 0f);
                threshold.localRotation = Quaternion.identity;
                threshold.localScale = Vector3.one;
                threshold.GetComponent<MeshFilter>().sharedMesh =
                    AssetDatabase.LoadAssetAtPath<Mesh>(DOOR_THRESHOLD_MESH_PATH);
                threshold.GetComponent<MeshRenderer>().sharedMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(DOOR_MATERIAL_PATH);
                foreach (var collider in threshold.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                var readability = root.GetComponent<AuthoredRouteDoorReadability>() ??
                                  root.AddComponent<AuthoredRouteDoorReadability>();
                readability.Configure(blockingSlab, openMarker, threshold.GetComponent<Renderer>());
                PrefabUtility.SaveAsPrefabAsset(root, SPINE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
