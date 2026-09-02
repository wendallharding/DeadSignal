using System;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalSecurityTrialSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string TUNING_SCENE_PATH = "Assets/DeadSignal/Scenes/SecurityTrialCombatTuning.unity";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SecurityTrialWingRegion.prefab";
        private const string FURNACE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRegion.prefab";
        private const string CHAMBER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string WHITE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/DroneWhite.mat";
        private const string RED_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SecurityRed.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";
        private const float ARENA_WIDTH = 60f;
        private const float ARENA_DEPTH = 36f;
        private const float ARENA_HALF_WIDTH = ARENA_WIDTH * 0.5f;
        private const float ARENA_HALF_DEPTH = ARENA_DEPTH * 0.5f;
        private const float ARENA_DOOR_WIDTH = 3.2f;
        private const float ARENA_HORIZONTAL_BULKHEAD_WIDTH = (ARENA_WIDTH - ARENA_DOOR_WIDTH) * 0.5f;
        private const float ARENA_HORIZONTAL_BULKHEAD_CENTER =
            (ARENA_HALF_WIDTH + ARENA_DOOR_WIDTH * 0.5f) * 0.5f;
        private const float GLOBAL_ARENA_HALF_WIDTH = 73f;

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var furnace = AssetDatabase.LoadAssetAtPath<GameObject>(FURNACE_PREFAB_PATH);
                var chamber = AssetDatabase.LoadAssetAtPath<GameObject>(CHAMBER_PREFAB_PATH);
                var gallery = AssetDatabase.LoadAssetAtPath<GameObject>(GALLERY_PREFAB_PATH);
                var authored = region == null ? null : region.GetComponent<AuthoredCombatChamber>();
                var arenaDeck = region == null ? null : region.transform.Find("Lockdown Arena/Arena Deck");
                return authored != null && authored.IsComplete &&
                       arenaDeck != null &&
                       Mathf.Approximately(arenaDeck.localScale.x, ARENA_WIDTH) &&
                       Mathf.Approximately(arenaDeck.localScale.z, ARENA_DEPTH) &&
                       region.transform.Find("Commitment Room") != null &&
                       region.transform.Find("Lockdown Arena") != null &&
                       region.transform.Find("Lockdown Arena/Arena South West Bulkhead") != null &&
                       region.transform.Find("Lockdown Arena/Arena South East Bulkhead") != null &&
                       region.transform.Find("Lockdown Arena/Arena North West Bulkhead") != null &&
                       region.transform.Find("Lockdown Arena/Arena North East Bulkhead") != null &&
                       region.transform.Find("Reward Vault") != null &&
                       furnace != null && furnace.transform.Find("Security Trial Wing Region") != null &&
                       chamber != null && chamber.transform.Find("Arc Furnace Region/Security Trial Wing Region") != null &&
                       gallery != null && gallery.transform.Find(
                           "Convergence Chamber Region/Arc Furnace Region/Security Trial Wing Region") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Security Trial Wing")]
        public static void EnsureAssets()
        {
            var materials = _loadMaterials();
            _ensureRegionPrefab(materials);
            _integrateWithFurnace(materials);
            _refreshParentPrefab(CHAMBER_PREFAB_PATH, "Arc Furnace Region", FURNACE_PREFAB_PATH, new Vector3(0f, 0f, 8.5f));
            _refreshParentPrefab(GALLERY_PREFAB_PATH, "Convergence Chamber Region", CHAMBER_PREFAB_PATH, new Vector3(0f, 0f, 8.5f));
            _ensureSceneBounds();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Security Trial Wing is incomplete.");
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Security Trial Expanded Arena")]
        public static void EnsureExpandedArenaAssets()
        {
            var materials = _loadMaterials();
            _ensureRegionPrefab(materials);
            DeadSignalSecurityTrialReadabilitySetup.EnsureAssets();
            DeadSignalSecurityLockdownReadabilitySetup.EnsureAssets();
            DeadSignalSecurityTrialHeroSetup.EnsureAssets();
            DeadSignalSecurityTrialCompositionSetup.EnsureAssets();
            DeadSignalStationWallKitSetup.EnsureAssets();
            _ensureSceneBounds();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The expanded Security Trial arena is incomplete.");
            }
        }

        private static Materials _loadMaterials()
        {
            var materials = new Materials
            {
                Armor = AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH),
                Deck = AssetDatabase.LoadAssetAtPath<Material>(DECK_MATERIAL_PATH),
                White = AssetDatabase.LoadAssetAtPath<Material>(WHITE_MATERIAL_PATH),
                Red = AssetDatabase.LoadAssetAtPath<Material>(RED_MATERIAL_PATH),
                Cyan = AssetDatabase.LoadAssetAtPath<Material>(CYAN_MATERIAL_PATH),
                Amber = AssetDatabase.LoadAssetAtPath<Material>(AMBER_MATERIAL_PATH)
            };
            if (materials.Armor == null || materials.Deck == null || materials.White == null ||
                materials.Red == null || materials.Cyan == null || materials.Amber == null)
            {
                throw new InvalidOperationException("The Security Trial Wing requires the established world materials.");
            }
            return materials;
        }

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("SecurityTrialWingRegion");
            try
            {
                var commitment = new GameObject("Commitment Room");
                commitment.transform.SetParent(root.transform, false);
                _wall(commitment.transform, "Commitment Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(8f, 0.55f, 6f), materials.Deck, false);
                _wall(commitment.transform, "Commitment West Bulkhead", new Vector3(-4f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 6f), materials.Armor, true);
                _wall(commitment.transform, "Commitment East Bulkhead", new Vector3(4f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 6f), materials.Armor, true);

                var commitmentSwitch = _wall(commitment.transform, "Security Trial Breaker",
                    new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.7f, 1.1f), materials.Amber, false).transform;
                _wall(commitment.transform, "Trial Warning Left", new Vector3(-1.4f, -0.1f, 2.25f),
                    new Vector3(1.8f, 0.04f, 0.12f), materials.Red, false);
                _wall(commitment.transform, "Trial Warning Right", new Vector3(1.4f, -0.1f, 2.25f),
                    new Vector3(1.8f, 0.04f, 0.12f), materials.Red, false);

                var arena = new GameObject("Lockdown Arena");
                arena.transform.SetParent(root.transform, false);
                arena.transform.localPosition = new Vector3(0f, 0f, 21f);
                _wall(arena.transform, "Arena Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(ARENA_WIDTH, 0.55f, ARENA_DEPTH), materials.Deck, false);
                _wall(arena.transform, "Arena West Bulkhead", new Vector3(-ARENA_HALF_WIDTH, 0.5f, 0f),
                    new Vector3(0.35f, 1f, ARENA_DEPTH), materials.Armor, true);
                _wall(arena.transform, "Arena East Bulkhead", new Vector3(ARENA_HALF_WIDTH, 0.5f, 0f),
                    new Vector3(0.35f, 1f, ARENA_DEPTH), materials.Armor, true);
                _wall(arena.transform, "Arena South West Bulkhead",
                    new Vector3(-ARENA_HORIZONTAL_BULKHEAD_CENTER, 0.5f, -ARENA_HALF_DEPTH),
                    new Vector3(ARENA_HORIZONTAL_BULKHEAD_WIDTH, 1f, 0.35f), materials.Armor, true);
                _wall(arena.transform, "Arena South East Bulkhead",
                    new Vector3(ARENA_HORIZONTAL_BULKHEAD_CENTER, 0.5f, -ARENA_HALF_DEPTH),
                    new Vector3(ARENA_HORIZONTAL_BULKHEAD_WIDTH, 1f, 0.35f), materials.Armor, true);
                _wall(arena.transform, "Arena North West Bulkhead",
                    new Vector3(-ARENA_HORIZONTAL_BULKHEAD_CENTER, 0.5f, ARENA_HALF_DEPTH),
                    new Vector3(ARENA_HORIZONTAL_BULKHEAD_WIDTH, 1f, 0.35f), materials.Armor, true);
                _wall(arena.transform, "Arena North East Bulkhead",
                    new Vector3(ARENA_HORIZONTAL_BULKHEAD_CENTER, 0.5f, ARENA_HALF_DEPTH),
                    new Vector3(ARENA_HORIZONTAL_BULKHEAD_WIDTH, 1f, 0.35f), materials.Armor, true);
                _wall(arena.transform, "Arena West Deflector", new Vector3(-12f, 0.5f, 6f),
                    new Vector3(4.5f, 1f, 0.45f), materials.White, true).transform.localRotation =
                    Quaternion.Euler(0f, 28f, 0f);
                _wall(arena.transform, "Arena East Deflector", new Vector3(12f, 0.5f, -6f),
                    new Vector3(4.5f, 1f, 0.45f), materials.White, true).transform.localRotation =
                    Quaternion.Euler(0f, -28f, 0f);
                _wall(arena.transform, "Arena Circuit Spine", new Vector3(0f, -0.1f, 0f),
                    new Vector3(0.14f, 0.04f, 33.5f), materials.Red, false);

                var entryDoor = new GameObject("Lockdown Entry Door");
                entryDoor.transform.SetParent(root.transform, false);
                _wall(entryDoor.transform, "Entry Door Slab", new Vector3(0f, 0.65f, 3f),
                    new Vector3(3.2f, 1.3f, 0.35f), materials.Red, true);
                var rewardDoor = new GameObject("Reward Vault Door");
                rewardDoor.transform.SetParent(root.transform, false);
                _wall(rewardDoor.transform, "Reward Door Slab", new Vector3(0f, 0.65f, 39f),
                    new Vector3(3.2f, 1.3f, 0.35f), materials.Red, true);

                var vault = new GameObject("Reward Vault");
                vault.transform.SetParent(root.transform, false);
                vault.transform.localPosition = new Vector3(0f, 0f, 42f);
                _wall(vault.transform, "Vault Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(8f, 0.55f, 6f), materials.Deck, false);
                _wall(vault.transform, "Vault West Bulkhead", new Vector3(-4f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 6f), materials.Armor, true);
                _wall(vault.transform, "Vault East Bulkhead", new Vector3(4f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 6f), materials.Armor, true);
                _wall(vault.transform, "Vault North Bulkhead", new Vector3(0f, 0.5f, 3f),
                    new Vector3(8f, 1f, 0.35f), materials.Armor, true);
                var reward = _wall(vault.transform, "Trial Capacitor Reward", new Vector3(0f, 0.45f, 0f),
                    new Vector3(1.2f, 0.9f, 1.2f), materials.Amber, false);
                var clearedSignal = _wall(root.transform, "Cleared Return Signal", new Vector3(0f, 0.1f, 21f),
                    new Vector3(0.18f, 0.04f, 41f), materials.Cyan, false);

                var scenarioRoot = new GameObject("Security Trial Combat Scenario");
                scenarioRoot.transform.SetParent(root.transform, false);
                scenarioRoot.transform.localPosition = new Vector3(0f, 0f, 21f);
                var player = _anchor(scenarioRoot.transform, "Player Anchor", new Vector3(0f, 0f, -14.5f));
                var camera = _anchor(scenarioRoot.transform, "Camera Focus", Vector3.zero);
                var warden = _anchor(scenarioRoot.transform, "Warden Anchor", new Vector3(-22f, 0f, 13f));
                var sapper = _anchor(scenarioRoot.transform, "Sapper Anchor", new Vector3(22f, 0f, 13f));
                var interceptor = _anchor(scenarioRoot.transform, "Interceptor Anchor", new Vector3(-22f, 0f, -8f));
                var suppressor = _anchor(scenarioRoot.transform, "Suppressor Anchor", new Vector3(22f, 0f, -8f));
                var scenario = scenarioRoot.AddComponent<AuthoredCombatScenario>();
                scenario.Configure(player, camera, warden, sapper, interceptor, suppressor,
                    new Vector2(-28.5f, -16.5f), new Vector2(28.5f, 16.5f));
                var threshold = _anchor(root.transform, "Lockdown Threshold", new Vector3(0f, 0f, 3f));

                root.AddComponent<AuthoredCombatChamber>().Configure(
                    commitmentSwitch, threshold, entryDoor, rewardDoor, reward, clearedSignal, scenario, 1.8f, 0.75f, 20f);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _integrateWithFurnace(Materials materials)
        {
            var furnace = PrefabUtility.LoadPrefabContents(FURNACE_PREFAB_PATH);
            try
            {
                _remove(furnace.transform, "Arc Furnace North Bulkhead");
                _replaceWall(furnace.transform, "Arc Furnace North West Trial", new Vector3(-4.75f, 0.5f, 4.5f),
                    new Vector3(4.5f, 1f, 0.35f), materials.Armor);
                _replaceWall(furnace.transform, "Arc Furnace North East Trial", new Vector3(4.75f, 0.5f, 4.5f),
                    new Vector3(4.5f, 1f, 0.35f), materials.Armor);
                _remove(furnace.transform, "Security Trial Wing Region");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, furnace.transform);
                instance.name = "Security Trial Wing Region";
                instance.transform.localPosition = new Vector3(0f, 0f, 7.5f);
                PrefabUtility.SaveAsPrefabAsset(furnace, FURNACE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(furnace);
            }
        }

        private static void _refreshParentPrefab(string parentPath, string childName, string childPath, Vector3 position)
        {
            var parent = PrefabUtility.LoadPrefabContents(parentPath);
            try
            {
                _remove(parent.transform, childName);
                var childPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(childPath);
                var child = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab, parent.transform);
                child.name = childName;
                child.transform.localPosition = position;
                PrefabUtility.SaveAsPrefabAsset(parent, parentPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(parent);
            }
        }

        private static void _ensureSceneBounds()
        {
            _ensureSceneBounds(SCENE_PATH);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TUNING_SCENE_PATH) != null)
            {
                _ensureSceneBounds(TUNING_SCENE_PATH);
            }
        }

        private static void _ensureSceneBounds(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(references);
            var boundsProperty = serialized.FindProperty("m_arenaHalfExtents");
            var existing = boundsProperty.vector2Value;
            boundsProperty.vector2Value = new Vector2(
                Mathf.Max(existing.x, GLOBAL_ARENA_HALF_WIDTH),
                Mathf.Max(existing.y, 81f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(references);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform _anchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = position;
            return anchor.transform;
        }

        private static void _replaceWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            _remove(parent, name);
            _wall(parent, name, position, scale, material, true);
        }

        private static void _remove(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        private static GameObject _wall(
            Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool obstacle)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            if (obstacle) result.AddComponent<AuthoredMapObstacle>().Configure(Vector2.one * 0.5f);
            return result;
        }

        private sealed class Materials
        {
            public Material Armor;
            public Material Deck;
            public Material White;
            public Material Red;
            public Material Cyan;
            public Material Amber;
        }
    }
}
