using System;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalCentralInstallationSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string VAULT_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";

        [MenuItem("DEAD SIGNAL/Setup/Central Payload Installation")]
        public static void EnsureAssets()
        {
            _ensureVaultRouteGate();
            _ensureSceneObjective();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void _ensureVaultRouteGate()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(VAULT_PREFAB_PATH);
            try
            {
                var gate = prefabRoot.transform.Find("Central Relay Route Gate")?.gameObject;
                if (gate == null)
                {
                    gate = new GameObject("Central Relay Route Gate");
                    gate.transform.SetParent(prefabRoot.transform, false);
                }

                gate.transform.localPosition = new Vector3(-3.15f, 0f, 0f);
                gate.transform.localRotation = Quaternion.identity;
                gate.transform.localScale = Vector3.one;
                if (gate.TryGetComponent<Renderer>(out var legacyRenderer))
                {
                    UnityEngine.Object.DestroyImmediate(legacyRenderer);
                }
                if (gate.TryGetComponent<MeshFilter>(out var legacyMeshFilter))
                {
                    UnityEngine.Object.DestroyImmediate(legacyMeshFilter);
                }

                _ensureCube(gate.transform, "Gate Visual", new Vector3(0f, 0.7f, 0f),
                    new Vector3(0.24f, 1.4f, 2.3f), AMBER_MATERIAL_PATH);
                var obstacle = gate.GetComponent<AuthoredMapObstacle>() ?? gate.AddComponent<AuthoredMapObstacle>();
                obstacle.Configure(new Vector2(0.16f, 1.15f));

                var routeOpen = _ensureCube(prefabRoot.transform, "Central Relay Route Open",
                    new Vector3(-2.85f, 0.06f, 0f), new Vector3(0.75f, 0.06f, 2.3f), CYAN_MATERIAL_PATH);
                var transferObjective = prefabRoot.GetComponent<AuthoredTransferVaultObjective>();
                if (transferObjective == null)
                {
                    throw new InvalidOperationException("The East Transfer Vault has no authored assembly objective.");
                }

                transferObjective.ConfigureRouteGate(gate, routeOpen);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, VAULT_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void _ensureSceneObjective()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var vault = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Optional East Salvage Vault");
            if (references == null || vault == null)
            {
                throw new InvalidOperationException("Central installation requires scene references and the East Transfer Vault.");
            }

            var routeGate = vault.transform.Find("Central Relay Route Gate")?.gameObject;
            var routeOpen = vault.transform.Find("Central Relay Route Open")?.gameObject;
            if (routeGate == null || routeOpen == null)
            {
                throw new InvalidOperationException("The East Transfer Vault route gate did not import into the scene.");
            }

            var objectiveRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == "Central Payload Installation");
            if (objectiveRoot == null)
            {
                objectiveRoot = new GameObject("Central Payload Installation");
                SceneManager.MoveGameObjectToScene(objectiveRoot, scene);
            }

            objectiveRoot.transform.position = references.TowerPosition;
            objectiveRoot.transform.rotation = Quaternion.identity;
            objectiveRoot.transform.localScale = Vector3.one;

            var anchor = _ensureAnchor(objectiveRoot.transform, "Central Payload Installation Anchor",
                new Vector3(0f, 0f, -1.25f));
            var available = _ensureRing(objectiveRoot.transform, "Central Payload Install Available", AMBER_MATERIAL_PATH);
            var installed = _ensureRing(objectiveRoot.transform, "Central Payload Installed", CYAN_MATERIAL_PATH);
            var objective = objectiveRoot.GetComponent<AuthoredCentralInstallationObjective>() ??
                            objectiveRoot.AddComponent<AuthoredCentralInstallationObjective>();
            objective.Configure(anchor, available, installed);
            EditorUtility.SetDirty(objectiveRoot);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform _ensureAnchor(Transform parent, string objectName, Vector3 localPosition)
        {
            var anchor = parent.Find(objectName);
            if (anchor == null)
            {
                var anchorObject = new GameObject(objectName);
                anchorObject.transform.SetParent(parent, false);
                anchor = anchorObject.transform;
            }

            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static GameObject _ensureRing(Transform parent, string objectName, string materialPath)
        {
            var ring = parent.Find(objectName)?.gameObject;
            if (ring == null)
            {
                ring = new GameObject(objectName);
                ring.transform.SetParent(parent, false);
            }

            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.identity;
            ring.transform.localScale = Vector3.one;
            _ensureCube(ring.transform, "North Rail", new Vector3(0f, 0.08f, 1.1f),
                new Vector3(2.4f, 0.08f, 0.12f), materialPath);
            _ensureCube(ring.transform, "South Rail", new Vector3(0f, 0.08f, -1.1f),
                new Vector3(2.4f, 0.08f, 0.12f), materialPath);
            _ensureCube(ring.transform, "East Rail", new Vector3(1.1f, 0.08f, 0f),
                new Vector3(0.12f, 0.08f, 2.4f), materialPath);
            _ensureCube(ring.transform, "West Rail", new Vector3(-1.1f, 0.08f, 0f),
                new Vector3(0.12f, 0.08f, 2.4f), materialPath);
            return ring;
        }

        private static GameObject _ensureCube(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            string materialPath)
        {
            var cube = parent.Find(objectName)?.gameObject;
            if (cube == null)
            {
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = objectName;
                cube.transform.SetParent(parent, false);
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            return cube;
        }
    }
}
