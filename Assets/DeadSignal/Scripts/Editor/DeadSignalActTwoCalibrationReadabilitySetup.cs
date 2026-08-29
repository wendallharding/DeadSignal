using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalActTwoCalibrationReadabilitySetup
    {
        private const string FOUNDRY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryRegion.prefab";
        private const string CALIBRATION_PANEL_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkPanelReadability.asset";
        private const string CALIBRATION_SELECTOR_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkSelectorReadability.asset";
        private const string DOOR_THRESHOLD_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RouteDoorThresholdReadability.asset";
        private const string CALIBRATION_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryWeaponCalibrationDecal.mat";
        private const string DOOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RouteDoorThresholdStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var foundry = AssetDatabase.LoadAssetAtPath<GameObject>(FOUNDRY_PREFAB_PATH);
                return foundry != null &&
                       foundry.TryGetComponent<AuthoredRelayPayloadObjective>(out var objective) &&
                       objective.HasReadabilityAssets &&
                       foundry.transform.Find("Relay Calibration Status Panel") != null &&
                       foundry.transform.Find("Relay Calibration Selector") != null &&
                       foundry.transform.Find("Relay Return Threshold") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Act II Calibration Readability")]
        public static void EnsureAssets()
        {
            _upgradeFoundry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Act II calibration readability assets are incomplete.");
            }
        }

        private static void _upgradeFoundry()
        {
            var root = PrefabUtility.LoadPrefabContents(FOUNDRY_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredRelayPayloadObjective>();
                var blockingSlab = root.transform.Find("Relay Return Bulkhead")?.gameObject;
                var openMarker = root.transform.Find("Relay Signal Lines")?.gameObject;
                if (objective == null || blockingSlab == null || openMarker == null)
                {
                    throw new InvalidOperationException("The Relay calibration or return-bulkhead authority is missing.");
                }

                var panel = _ensureMeshPart(root.transform, "Relay Calibration Status Panel",
                    new Vector3(3.75f, 0.04f, -3.25f), CALIBRATION_PANEL_MESH_PATH, CALIBRATION_MATERIAL_PATH);
                panel.localScale = new Vector3(1.55f, 1f, 1.2f);
                var selector = _ensureMeshPart(root.transform, "Relay Calibration Selector",
                    new Vector3(3.75f, 0.04f, -3.25f), CALIBRATION_SELECTOR_MESH_PATH,
                    CALIBRATION_MATERIAL_PATH);
                selector.localScale = new Vector3(1.25f, 1f, 1.25f);
                var threshold = _ensureMeshPart(root.transform, "Relay Return Threshold",
                    new Vector3(-8f, 0f, 0f), DOOR_THRESHOLD_MESH_PATH, DOOR_MATERIAL_PATH);

                var doorReadability = root.GetComponent<AuthoredRouteDoorReadability>() ??
                                      root.AddComponent<AuthoredRouteDoorReadability>();
                doorReadability.Configure(blockingSlab, openMarker, threshold.GetComponent<Renderer>());
                objective.ConfigureReadability(
                    new[] { panel.GetComponent<Renderer>(), selector.GetComponent<Renderer>() },
                    selector,
                    doorReadability);
                PrefabUtility.SaveAsPrefabAsset(root, FOUNDRY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform _ensureMeshPart(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            string meshPath,
            string materialPath)
        {
            var part = parent.Find(objectName);
            if (part == null)
            {
                part = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                part.SetParent(parent, false);
            }

            part.localPosition = localPosition;
            part.localRotation = Quaternion.identity;
            part.localScale = Vector3.one;
            part.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            part.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            foreach (var collider in part.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return part;
        }
    }
}
