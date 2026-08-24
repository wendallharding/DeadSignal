using System;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalRelayFoundrySetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/RelayFoundryRegion.prefab";
        private const string MODEL_PATH = "Assets/DeadSignal/Resources/Environment/RelayFoundryTurbineModel.fbx";
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/RelayFoundryTurbineAlbedo.png";
        private const string ROUTE_DECAL_PATH = "Assets/DeadSignal/Resources/Environment/RelayFoundryRouteDecal.png";
        private const string WEAPON_DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryWeaponCalibrationDecal.png";
        private const string LOCKDOWN_DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryLockdownDecal.png";
        private const string EAST_VAULT_PATH = "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab";
        private const string TOWER_PATH = "Assets/DeadSignal/Resources/Environment/SignalTowerAssembly.prefab";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/RelayFoundry";

        private static readonly Vector3 s_regionPosition = new(27.5f, 0f, 0f);

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(ROUTE_DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(WEAPON_DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(LOCKDOWN_DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH) != null && prefab != null &&
                       prefab.GetComponentsInChildren<AuthoredMapObstacle>().Length == 6 &&
                       prefab.GetComponentsInChildren<AuthoredInterceptorEntrance>().Length == 2 &&
                       prefab.transform.Find("Foundry Route Split Decal") != null &&
                       prefab.transform.Find("Relay Weapon Calibration Decal") != null &&
                       prefab.transform.Find("Foundry North Lockdown Decal") != null &&
                       prefab.transform.Find("Foundry South Lockdown Decal") != null &&
                       prefab.transform.Find("Relay Tower Assembly") != null &&
                       prefab.transform.Find("Relay Return Bulkhead") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Relay Foundry Region")]
        public static void EnsureAssets()
        {
            _configureImports();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _openEastVaultExit();
            _ensurePrefab(materials);
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Relay Foundry region assets are incomplete.");
            }
        }

        private static void _configureImports()
        {
            var texture = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            var routeDecal = AssetImporter.GetAtPath(ROUTE_DECAL_PATH) as TextureImporter;
            var weaponDecal = AssetImporter.GetAtPath(WEAPON_DECAL_PATH) as TextureImporter;
            var lockdownDecal = AssetImporter.GetAtPath(LOCKDOWN_DECAL_PATH) as TextureImporter;
            var model = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (texture == null || routeDecal == null || weaponDecal == null || lockdownDecal == null || model == null)
            {
                throw new InvalidOperationException("Relay Foundry source texture or FBX is missing.");
            }

            texture.mipmapEnabled = true;
            texture.maxTextureSize = 2048;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.textureCompression = TextureImporterCompression.CompressedHQ;
            texture.SaveAndReimport();
            routeDecal.alphaIsTransparency = true;
            routeDecal.mipmapEnabled = true;
            routeDecal.maxTextureSize = 2048;
            routeDecal.wrapMode = TextureWrapMode.Clamp;
            routeDecal.textureCompression = TextureImporterCompression.CompressedHQ;
            routeDecal.SaveAndReimport();
            weaponDecal.alphaIsTransparency = true;
            weaponDecal.mipmapEnabled = true;
            weaponDecal.maxTextureSize = 2048;
            weaponDecal.wrapMode = TextureWrapMode.Clamp;
            weaponDecal.textureCompression = TextureImporterCompression.CompressedHQ;
            weaponDecal.SaveAndReimport();
            lockdownDecal.alphaIsTransparency = true;
            lockdownDecal.mipmapEnabled = true;
            lockdownDecal.maxTextureSize = 2048;
            lockdownDecal.wrapMode = TextureWrapMode.Clamp;
            lockdownDecal.textureCompression = TextureImporterCompression.CompressedHQ;
            lockdownDecal.SaveAndReimport();
            model.addCollider = false;
            model.importAnimation = false;
            model.importCameras = false;
            model.importLights = false;
            model.materialImportMode = ModelImporterMaterialImportMode.None;
            model.SaveAndReimport();
        }

        private static void _ensureMaterialDirectory()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_DIRECTORY))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "RelayFoundry");
            }
        }

        private static Materials _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new Materials
            {
                Armor = _material("RelayFoundryArmor", Color.white, Color.black, texture),
                Deck = _material("RelayFoundryDeck", new Color(0.32f, 0.37f, 0.4f), Color.black, texture),
                Cyan = _material("RelayFoundryCyan", new Color(0.02f, 0.86f, 1f), new Color(0f, 2.2f, 2.8f), null),
                Amber = _material("RelayFoundryAmber", new Color(1f, 0.4f, 0.035f), new Color(2.2f, 0.42f, 0f), null),
                Dormant = _material("RelayFoundryDormant", new Color(0.16f, 0.025f, 0.03f), new Color(0.14f, 0f, 0f), null),
                RouteDecal = _decalMaterial("RelayFoundryRouteDecal", ROUTE_DECAL_PATH),
                WeaponDecal = _decalMaterial("RelayFoundryWeaponCalibrationDecal", WEAPON_DECAL_PATH),
                LockdownDecal = _decalMaterial("RelayFoundryLockdownDecal", LOCKDOWN_DECAL_PATH)
            };
        }

        private static Material _decalMaterial(string name, string texturePath)
        {
            var path = $"{MATERIAL_DIRECTORY}/{name}.mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
                AssetDatabase.CreateAsset(result, path);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            result.SetTexture("_BaseMap", texture);
            result.SetColor("_BaseColor", Color.white);
            result.SetFloat("_Surface", 1f);
            result.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            result.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            result.SetFloat("_ZWrite", 0f);
            result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            result.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(result);
            return result;
        }

        private static Material _material(string name, Color color, Color emission, Texture texture)
        {
            var path = $"{MATERIAL_DIRECTORY}/{name}.mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(result, path);
            }
            result.color = color;
            result.SetColor("_BaseColor", color);
            result.SetColor("_EmissionColor", emission);
            result.SetFloat("_Smoothness", 0.34f);
            result.mainTexture = texture;
            result.SetTexture("_BaseMap", texture);
            if (emission.maxColorComponent > 0f) result.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(result);
            return result;
        }

        private static void _openEastVaultExit()
        {
            var root = PrefabUtility.LoadPrefabContents(EAST_VAULT_PATH);
            try
            {
                var visual = root.transform.Find("Vault East Wall");
                if (visual != null && !visual.gameObject.activeSelf &&
                    root.transform.Find("Vault East Wall Bounds") == null &&
                    root.transform.Find("Vault East Exit North Bounds") != null &&
                    root.transform.Find("Vault East Exit South Bounds") != null)
                {
                    return;
                }

                var wallMaterial = visual != null ? visual.GetComponent<Renderer>()?.sharedMaterial : null;
                if (visual != null) visual.gameObject.SetActive(false);
                var oldBounds = root.transform.Find("Vault East Wall Bounds");
                if (oldBounds != null) UnityEngine.Object.DestroyImmediate(oldBounds.gameObject);
                foreach (var name in new[]
                         {
                             "Vault East Exit North", "Vault East Exit North Bounds", "Vault East Exit Divider",
                             "Vault East Exit South", "Vault East Exit South Bounds"
                         })
                {
                    var existing = root.transform.Find(name);
                    if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
                _wall(root.transform, "Vault East Exit North", new Vector3(-3.15f, 0.5f, 2.75f), new Vector3(0.3f, 1f, 0.8f), wallMaterial, false);
                _obstacle(root.transform, "Vault East Exit North Bounds", new Vector3(-3.15f, 0f, 2.75f), new Vector2(0.15f, 0.4f));
                _wall(root.transform, "Vault East Exit South", new Vector3(-3.15f, 0.5f, -2.75f), new Vector3(0.3f, 1f, 0.8f), wallMaterial, false);
                _obstacle(root.transform, "Vault East Exit South Bounds", new Vector3(-3.15f, 0f, -2.75f), new Vector2(0.15f, 0.4f));
                PrefabUtility.SaveAsPrefabAsset(root, EAST_VAULT_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensurePrefab(Materials materials)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
            {
                var existingRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
                try
                {
                    var existingDecal = existingRoot.transform.Find("Relay Weapon Calibration Decal");
                    var northLockdown = existingRoot.transform.Find("Foundry North Lockdown Decal");
                    var southLockdown = existingRoot.transform.Find("Foundry South Lockdown Decal");
                    var weaponDecalReady = existingDecal != null &&
                                           AssetDatabase.GetAssetPath(existingDecal.GetComponent<Renderer>()?.sharedMaterial) ==
                                           AssetDatabase.GetAssetPath(materials.WeaponDecal) &&
                                           existingDecal.localPosition == new Vector3(3.75f, -0.12f, -3.25f) &&
                                           existingDecal.localScale == Vector3.one * 3.8f;
                    if (weaponDecalReady &&
                        _isLockdownDecalReady(northLockdown, new Vector3(6.1f, -0.12f, 4.7f), materials.LockdownDecal) &&
                        _isLockdownDecalReady(southLockdown, new Vector3(6.1f, -0.12f, -4.7f), materials.LockdownDecal))
                    {
                        return;
                    }

                    if (!weaponDecalReady && existingDecal != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existingDecal.gameObject);
                    }

                    if (northLockdown != null) UnityEngine.Object.DestroyImmediate(northLockdown.gameObject);
                    if (southLockdown != null) UnityEngine.Object.DestroyImmediate(southLockdown.gameObject);

                    if (!weaponDecalReady)
                    {
                        _createWeaponDecal(existingRoot.transform, materials.WeaponDecal);
                    }
                    _createLockdownDecal(existingRoot.transform, "Foundry North Lockdown Decal",
                        new Vector3(6.1f, -0.12f, 4.7f), materials.LockdownDecal);
                    _createLockdownDecal(existingRoot.transform, "Foundry South Lockdown Decal",
                        new Vector3(6.1f, -0.12f, -4.7f), materials.LockdownDecal);
                    PrefabUtility.SaveAsPrefabAsset(existingRoot, PREFAB_PATH);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(existingRoot);
                }

                return;
            }

            var root = new GameObject("Relay Foundry Region");
            try
            {
                _wall(root.transform, "Foundry Deck", new Vector3(0f, -0.42f, 0f), new Vector3(16f, 0.55f, 14f), materials.Deck, false);
                _wall(root.transform, "Foundry North Bulkhead", new Vector3(0f, 0.5f, 7f), new Vector3(16f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Foundry South Bulkhead", new Vector3(0f, 0.5f, -7f), new Vector3(16f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Foundry East Bulkhead", new Vector3(8f, 0.5f, 0f), new Vector3(0.35f, 1f, 14f), materials.Armor, true);
                _wall(root.transform, "Foundry West North", new Vector3(-8f, 0.5f, 5f), new Vector3(0.35f, 1f, 4f), materials.Armor, true);
                _wall(root.transform, "Foundry West South", new Vector3(-8f, 0.5f, -5f), new Vector3(0.35f, 1f, 4f), materials.Armor, true);

                var routeDecal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                routeDecal.name = "Foundry Route Split Decal";
                routeDecal.transform.SetParent(root.transform, false);
                routeDecal.transform.localPosition = new Vector3(-5.35f, -0.13f, 0f);
                routeDecal.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);
                routeDecal.transform.localScale = Vector3.one * 4.8f;
                routeDecal.GetComponent<Renderer>().sharedMaterial = materials.RouteDecal;
                UnityEngine.Object.DestroyImmediate(routeDecal.GetComponent<Collider>());

                _createWeaponDecal(root.transform, materials.WeaponDecal);
                _createLockdownDecal(root.transform, "Foundry North Lockdown Decal",
                    new Vector3(6.1f, -0.12f, 4.7f), materials.LockdownDecal);
                _createLockdownDecal(root.transform, "Foundry South Lockdown Decal",
                    new Vector3(6.1f, -0.12f, -4.7f), materials.LockdownDecal);

                var turbineModel = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
                var turbine = (GameObject)PrefabUtility.InstantiatePrefab(turbineModel, root.transform);
                turbine.name = "Relay Induction Turbine";
                turbine.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
                foreach (var renderer in turbine.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = materials.Armor;
                _obstacle(turbine.transform, "Turbine Bounds", Vector3.zero, new Vector2(2.55f, 2.55f));

                var towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TOWER_PATH);
                var tower = (GameObject)PrefabUtility.InstantiatePrefab(towerPrefab, root.transform);
                tower.name = "Relay Tower Assembly";
                tower.transform.localPosition = new Vector3(4.25f, 0f, 0f);
                tower.transform.Find("Tower Core").GetComponent<Renderer>().sharedMaterial = materials.Dormant;

                var routing = new GameObject("Relay Signal Lines");
                routing.transform.SetParent(root.transform, false);
                for (var index = -1; index <= 1; index++)
                {
                    var line = _wall(routing.transform, $"Relay Conduit {index + 2}",
                        new Vector3(1.8f, -0.02f, index * 1.4f), new Vector3(5.2f, 0.035f, 0.09f), materials.Cyan, false);
                    UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());
                }
                routing.SetActive(false);

                var gate = _wall(root.transform, "Relay Return Bulkhead", new Vector3(-8f, 0.65f, 0f),
                    new Vector3(0.42f, 1.3f, 1.5f), materials.Amber, false);
                UnityEngine.Object.DestroyImmediate(gate.GetComponent<Collider>());

                var towerAnchor = new GameObject("Relay Tower Anchor").transform;
                towerAnchor.SetParent(root.transform, false);
                towerAnchor.localPosition = tower.transform.localPosition;
                var shortcutAnchor = new GameObject("Relay Shortcut Anchor").transform;
                shortcutAnchor.SetParent(root.transform, false);
                shortcutAnchor.localPosition = gate.transform.localPosition;

                _entrance(root.transform, "Foundry North Reinforcement Gate", new Vector3(7.2f, 0f, 5.8f), 10);
                _entrance(root.transform, "Foundry South Reinforcement Gate", new Vector3(7.2f, 0f, -5.8f), 11);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Relay Foundry Region");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(references);
            if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing) == prefab &&
                serialized.FindProperty("m_relayFoundry").objectReferenceValue == existing)
            {
                return;
            }

            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            existing.name = "Relay Foundry Region";
            existing.transform.position = s_regionPosition;

            serialized.FindProperty("m_relayTowerAnchor").objectReferenceValue = existing.transform.Find("Relay Tower Anchor");
            serialized.FindProperty("m_relayShortcutAnchor").objectReferenceValue = existing.transform.Find("Relay Shortcut Anchor");
            serialized.FindProperty("m_arenaHalfExtents").vector2Value = new Vector2(36f, 8.8f);
            serialized.FindProperty("m_relayFoundry").objectReferenceValue = existing;
            serialized.FindProperty("m_relayTower").objectReferenceValue = existing.transform.Find("Relay Tower Assembly").gameObject;
            serialized.FindProperty("m_relaySignalRouting").objectReferenceValue = existing.transform.Find("Relay Signal Lines").gameObject;
            serialized.FindProperty("m_relayShortcutGate").objectReferenceValue = existing.transform.Find("Relay Return Bulkhead").gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(references);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _createWeaponDecal(Transform parent, Material material)
        {
            var weaponDecal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            weaponDecal.name = "Relay Weapon Calibration Decal";
            weaponDecal.transform.SetParent(parent, false);
            weaponDecal.transform.localPosition = new Vector3(3.75f, -0.12f, -3.25f);
            weaponDecal.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);
            weaponDecal.transform.localScale = Vector3.one * 3.8f;
            weaponDecal.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(weaponDecal.GetComponent<Collider>());
        }

        private static bool _isLockdownDecalReady(Transform decal, Vector3 position, Material material)
        {
            return decal != null && decal.localPosition == position && decal.localScale == Vector3.one * 2.6f &&
                   AssetDatabase.GetAssetPath(decal.GetComponent<Renderer>()?.sharedMaterial) ==
                   AssetDatabase.GetAssetPath(material);
        }

        private static void _createLockdownDecal(Transform parent, string name, Vector3 position, Material material)
        {
            var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.name = name;
            decal.transform.SetParent(parent, false);
            decal.transform.localPosition = position;
            decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            decal.transform.localScale = Vector3.one * 2.6f;
            decal.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());
        }

        private static GameObject _wall(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool obstacle)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            if (material != null) result.GetComponent<Renderer>().sharedMaterial = material;
            var collider = result.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            if (obstacle) result.AddComponent<AuthoredMapObstacle>().Configure(new Vector2(0.5f, 0.5f));
            return result;
        }

        private static void _obstacle(Transform parent, string name, Vector3 position, Vector2 halfSize)
        {
            var result = new GameObject(name);
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.AddComponent<AuthoredMapObstacle>().Configure(halfSize);
        }

        private static void _entrance(Transform parent, string name, Vector3 position, int priority)
        {
            var result = new GameObject(name);
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.AddComponent<AuthoredInterceptorEntrance>().Configure(priority);
        }

        private sealed class Materials
        {
            public Material Armor;
            public Material Deck;
            public Material Cyan;
            public Material Amber;
            public Material Dormant;
            public Material RouteDecal;
            public Material WeaponDecal;
            public Material LockdownDecal;
        }
    }
}
