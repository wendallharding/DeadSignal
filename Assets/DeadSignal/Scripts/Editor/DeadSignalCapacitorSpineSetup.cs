using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalCapacitorSpineSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab";
        private const string RELAY_FOUNDRY_PATH = "Assets/DeadSignal/Resources/Environment/RelayFoundryRegion.prefab";
        private const string CAPACITOR_PATH = "Assets/DeadSignal/Resources/Environment/DepartureCapacitor.prefab";
        private const string TOWER_PATH = "Assets/DeadSignal/Resources/Environment/SignalTowerAssembly.prefab";
        private const string DECAL_PATH = "Assets/DeadSignal/Resources/Environment/CapacitorSpineRouteDecal.png";
        private const string ACTIVATION_DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineActivationDecal.png";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/CapacitorSpine";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/CapacitorSpineRouteDecal.mat";
        private const string ACTIVATION_DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/CapacitorSpineActivationDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string DORMANT_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDormant.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";

        private static readonly Vector3 s_regionPosition = new(42.5f, 0f, 0f);

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var foundry = AssetDatabase.LoadAssetAtPath<GameObject>(RELAY_FOUNDRY_PATH);
                return prefab != null && foundry != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(ACTIVATION_DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(ACTIVATION_DECAL_MATERIAL_PATH) != null &&
                       prefab.GetComponentsInChildren<AuthoredMapObstacle>().Length == 8 &&
                       prefab.GetComponentsInChildren<AuthoredSalvageSocket>().Length == 1 &&
                       prefab.transform.Find("Capacitor Transfer Bank") != null &&
                       prefab.transform.Find("Third Tower Berth") != null &&
                       prefab.transform.Find("Spine Signal Lines") != null &&
                       prefab.transform.Find("Capacitor Spine Activation Decal") != null &&
                       prefab.transform.Find("Capacitor Spine Route Decal") != null &&
                       foundry.transform.Find("Foundry East Bulkhead") == null &&
                       foundry.transform.Find("Foundry East North") != null &&
                       foundry.transform.Find("Foundry East Center") != null &&
                       foundry.transform.Find("Foundry East South") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Capacitor Spine Region")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _openFoundryEastApproaches(materials.Armor);
            _ensurePrefab(materials);
            _ensureScenePlacement();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var foundry = AssetDatabase.LoadAssetAtPath<GameObject>(RELAY_FOUNDRY_PATH);
                throw new InvalidOperationException(
                    $"The Capacitor Spine region assets are incomplete: prefab={prefab != null}, " +
                    $"obstacles={prefab?.GetComponentsInChildren<AuthoredMapObstacle>().Length ?? -1}, " +
                    $"sockets={prefab?.GetComponentsInChildren<AuthoredSalvageSocket>().Length ?? -1}, " +
                    $"bank={prefab?.transform.Find("Capacitor Transfer Bank") != null}, " +
                    $"berth={prefab?.transform.Find("Third Tower Berth") != null}, " +
                    $"decal={prefab?.transform.Find("Capacitor Spine Route Decal") != null}, " +
                    $"foundry={foundry != null}, oldEast={foundry?.transform.Find("Foundry East Bulkhead") != null}, " +
                    $"north={foundry?.transform.Find("Foundry East North") != null}, " +
                    $"center={foundry?.transform.Find("Foundry East Center") != null}, " +
                    $"south={foundry?.transform.Find("Foundry East South") != null}.");
            }
        }

        private static void _configureDecalImport()
        {
            _configureDecalImport(DECAL_PATH);
            _configureDecalImport(ACTIVATION_DECAL_PATH);
        }

        private static void _configureDecalImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Capacitor Spine decal at {path}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterialDirectory()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_DIRECTORY))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "CapacitorSpine");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decalMaterial = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decalMaterial == null)
            {
                decalMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "CapacitorSpineRouteDecal"
                };
                AssetDatabase.CreateAsset(decalMaterial, DECAL_MATERIAL_PATH);
            }

            decalMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH));
            decalMaterial.SetColor("_BaseColor", Color.white);
            decalMaterial.SetFloat("_Surface", 1f);
            decalMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            decalMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            decalMaterial.SetFloat("_ZWrite", 0f);
            decalMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            decalMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(decalMaterial);

            var activationDecalMaterial = AssetDatabase.LoadAssetAtPath<Material>(ACTIVATION_DECAL_MATERIAL_PATH);
            if (activationDecalMaterial == null)
            {
                activationDecalMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "CapacitorSpineActivationDecal"
                };
                AssetDatabase.CreateAsset(activationDecalMaterial, ACTIVATION_DECAL_MATERIAL_PATH);
            }

            activationDecalMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ACTIVATION_DECAL_PATH));
            activationDecalMaterial.SetColor("_BaseColor", Color.white);
            activationDecalMaterial.SetFloat("_Surface", 1f);
            activationDecalMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            activationDecalMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            activationDecalMaterial.SetFloat("_ZWrite", 0f);
            activationDecalMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            activationDecalMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(activationDecalMaterial);

            return new Materials
            {
                Armor = AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH),
                Deck = AssetDatabase.LoadAssetAtPath<Material>(DECK_MATERIAL_PATH),
                Dormant = AssetDatabase.LoadAssetAtPath<Material>(DORMANT_MATERIAL_PATH),
                Cyan = AssetDatabase.LoadAssetAtPath<Material>(CYAN_MATERIAL_PATH),
                Decal = decalMaterial,
                ActivationDecal = activationDecalMaterial
            };
        }

        private static void _openFoundryEastApproaches(Material armor)
        {
            var root = PrefabUtility.LoadPrefabContents(RELAY_FOUNDRY_PATH);
            try
            {
                foreach (var name in new[]
                         {
                             "Foundry East Bulkhead", "Foundry East North", "Foundry East Center", "Foundry East South"
                         })
                {
                    var existing = root.transform.Find(name);
                    if (existing != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    }
                }

                _wall(root.transform, "Foundry East North", new Vector3(8f, 0.5f, 5.5f),
                    new Vector3(0.35f, 1f, 3f), armor, true);
                _wall(root.transform, "Foundry East Center", new Vector3(8f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 3f), armor, true);
                _wall(root.transform, "Foundry East South", new Vector3(8f, 0.5f, -5.5f),
                    new Vector3(0.35f, 1f, 3f), armor, true);
                PrefabUtility.SaveAsPrefabAsset(root, RELAY_FOUNDRY_PATH);
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
                _upgradePrefab(materials);
                return;
            }

            if (materials.Armor == null || materials.Deck == null || materials.Dormant == null)
            {
                throw new InvalidOperationException("The Relay Foundry materials required by the Capacitor Spine are missing.");
            }

            var root = new GameObject("CapacitorSpineRegion");
            try
            {
                _wall(root.transform, "Capacitor Spine Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(14f, 0.55f, 10f), materials.Deck, false);
                _wall(root.transform, "Capacitor Spine North Bulkhead", new Vector3(0f, 0.5f, 5f),
                    new Vector3(14f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Capacitor Spine South Bulkhead", new Vector3(0f, 0.5f, -5f),
                    new Vector3(14f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Capacitor Spine East Bulkhead", new Vector3(7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 10f), materials.Armor, true);

                _addCapacitor(root.transform, "Capacitor Transfer Bank", new Vector3(-0.6f, 0f, 0f),
                    Quaternion.Euler(0f, 90f, 0f), new Vector2(1.75f, 1.5f));
                _addCapacitor(root.transform, "North Capacitor Shield", new Vector3(2.6f, 0f, 3.35f),
                    Quaternion.identity, new Vector2(1.2f, 0.45f));

                var towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TOWER_PATH);
                var tower = (GameObject)PrefabUtility.InstantiatePrefab(towerPrefab, root.transform);
                tower.name = "Third Tower Berth";
                tower.transform.localPosition = new Vector3(5f, 0f, 0f);
                tower.transform.Find("Tower Core").GetComponent<Renderer>().sharedMaterial = materials.Dormant;
                _obstacle(root.transform, "Third Tower Berth Bounds", new Vector3(5f, 0f, 0f), new Vector2(0.9f, 0.9f));

                var salvageSocket = new GameObject("Capacitor Spine Salvage Socket");
                salvageSocket.transform.SetParent(root.transform, false);
                salvageSocket.transform.localPosition = new Vector3(4.65f, 0f, -3.35f);
                salvageSocket.AddComponent<AuthoredSalvageSocket>();

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Capacitor Spine Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(-5.15f, -0.12f, 0f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);
                decal.transform.localScale = Vector3.one * 4.6f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            _upgradePrefab(materials);
        }

        private static void _upgradePrefab(Materials materials)
        {
            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                var oldLines = root.transform.Find("Spine Signal Lines");
                if (oldLines != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldLines.gameObject);
                }

                var signalLines = new GameObject("Spine Signal Lines");
                signalLines.transform.SetParent(root.transform, false);
                _wall(signalLines.transform, "North Feed", new Vector3(2.25f, -0.1f, 1.5f),
                    new Vector3(4.6f, 0.04f, 0.12f), materials.Cyan, false);
                _wall(signalLines.transform, "South Feed", new Vector3(2.25f, -0.1f, -1.5f),
                    new Vector3(4.6f, 0.04f, 0.12f), materials.Cyan, false);
                _wall(signalLines.transform, "Tower Feed", new Vector3(4.5f, -0.1f, 0f),
                    new Vector3(0.12f, 0.04f, 3f), materials.Cyan, false);
                signalLines.SetActive(false);

                var oldDecal = root.transform.Find("Capacitor Spine Activation Decal");
                if (oldDecal != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldDecal.gameObject);
                }

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Capacitor Spine Activation Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(5f, -0.105f, -2.05f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 2.8f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.ActivationDecal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _addCapacitor(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector2 halfSize)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAPACITOR_PATH);
            var capacitor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            capacitor.name = name;
            capacitor.transform.localPosition = position;
            capacitor.transform.localRotation = rotation;
            capacitor.AddComponent<AuthoredMapObstacle>().Configure(halfSize);
        }

        private static void _ensureScenePlacement()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Capacitor Spine Region");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (existing == null || PrefabUtility.GetCorrespondingObjectFromSource(existing) != prefab)
            {
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }

                existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                existing.name = "Capacitor Spine Region";
            }

            existing.transform.position = s_regionPosition;
            existing.transform.rotation = Quaternion.identity;
            existing.transform.localScale = Vector3.one;

            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(references);
            serialized.FindProperty("m_arenaHalfExtents").vector2Value = new Vector2(50.5f, 8.8f);
            serialized.FindProperty("m_spineTowerAnchor").objectReferenceValue = existing.transform.Find("Third Tower Berth");
            serialized.FindProperty("m_capacitorSpine").objectReferenceValue = existing;
            serialized.FindProperty("m_spineTower").objectReferenceValue = existing.transform.Find("Third Tower Berth").gameObject;
            serialized.FindProperty("m_spineSignalRouting").objectReferenceValue = existing.transform.Find("Spine Signal Lines").gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(references);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject _wall(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool obstacle)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            if (obstacle)
            {
                result.AddComponent<AuthoredMapObstacle>().Configure(Vector2.one * 0.5f);
            }

            return result;
        }

        private static void _obstacle(Transform parent, string name, Vector3 position, Vector2 halfSize)
        {
            var obstacle = new GameObject(name);
            obstacle.transform.SetParent(parent, false);
            obstacle.transform.localPosition = position;
            obstacle.AddComponent<AuthoredMapObstacle>().Configure(halfSize);
        }

        private sealed class Materials
        {
            public Material Armor;
            public Material Deck;
            public Material Dormant;
            public Material Cyan;
            public Material Decal;
            public Material ActivationDecal;
        }
    }
}
