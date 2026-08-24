using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalConvergenceChamberSetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string MODEL_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBusbarModel.fbx";
        private const string MODEL_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBusbar.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRouteDecal.png";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/ConvergenceChamber";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/ConvergenceChamberRouteDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";
        private const string WHITE_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/DroneWhite.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";
        private const string RED_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SecurityRed.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var gallery = AssetDatabase.LoadAssetAtPath<GameObject>(GALLERY_PREFAB_PATH);
                return region != null && gallery != null &&
                       AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PREFAB_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       region.GetComponentsInChildren<AuthoredMapObstacle>().Length == 9 &&
                       region.GetComponent<AuthoredPoweredTerritory>() != null &&
                       region.GetComponentInChildren<AuthoredInterceptorEntrance>() != null &&
                       gallery.transform.Find("Convergence Chamber Region") != null &&
                       gallery.GetComponentsInChildren<AuthoredMapObstacle>().Length == 17;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Convergence Chamber")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensureModelPrefab(materials);
            _ensureRegionPrefab(materials);
            _integrateWithGallery(materials);
            _ensureSceneBounds();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Convergence Chamber is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the chamber decal at {DECAL_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "ConvergenceChamber");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "ConvergenceChamberRouteDecal"
                };
                AssetDatabase.CreateAsset(decal, DECAL_MATERIAL_PATH);
            }

            decal.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH));
            decal.SetColor("_BaseColor", Color.white);
            decal.SetFloat("_Surface", 1f);
            decal.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            decal.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            decal.SetFloat("_ZWrite", 0f);
            decal.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            decal.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(decal);

            return new Materials
            {
                Armor = AssetDatabase.LoadAssetAtPath<Material>(ARMOR_MATERIAL_PATH),
                Deck = AssetDatabase.LoadAssetAtPath<Material>(DECK_MATERIAL_PATH),
                Cyan = AssetDatabase.LoadAssetAtPath<Material>(CYAN_MATERIAL_PATH),
                White = AssetDatabase.LoadAssetAtPath<Material>(WHITE_MATERIAL_PATH),
                Amber = AssetDatabase.LoadAssetAtPath<Material>(AMBER_MATERIAL_PATH),
                Red = AssetDatabase.LoadAssetAtPath<Material>(RED_MATERIAL_PATH),
                Decal = decal
            };
        }

        private static void _ensureModelPrefab(Materials materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            if (model == null)
            {
                throw new InvalidOperationException($"Could not find the busbar model at {MODEL_PATH}.");
            }

            var root = new GameObject("ConvergenceBusbar");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Convergence Busbar Model";
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = _materialFor(renderer.name, materials);
                }

                root.AddComponent<AuthoredMapObstacle>().Configure(new Vector2(1.65f, 0.68f));
                PrefabUtility.SaveAsPrefabAsset(root, MODEL_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Material _materialFor(string partName, Materials materials)
        {
            if (partName.Contains("Ceramic", StringComparison.OrdinalIgnoreCase) ||
                partName.Contains("insulator", StringComparison.OrdinalIgnoreCase))
            {
                return materials.White;
            }

            if (partName.Contains("Signal", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Cyan;
            }

            if (partName.Contains("Warning", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Red;
            }

            if (partName.Contains("bus", StringComparison.OrdinalIgnoreCase))
            {
                return materials.Amber;
            }

            return materials.Armor;
        }

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("ConvergenceChamberRegion");
            try
            {
                _wall(root.transform, "Convergence Chamber Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(14f, 0.55f, 8f), materials.Deck, false);
                _wall(root.transform, "Convergence North Bulkhead", new Vector3(0f, 0.5f, 4f),
                    new Vector3(14f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Convergence West Bulkhead", new Vector3(-7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 8f), materials.Armor, true);
                _wall(root.transform, "Convergence East Bulkhead", new Vector3(7f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 8f), materials.Armor, true);
                _wall(root.transform, "Convergence South West", new Vector3(-6.4f, 0.5f, -4f),
                    new Vector3(1.2f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Convergence South Center", new Vector3(0f, 0.5f, -4f),
                    new Vector3(6.4f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Convergence South East", new Vector3(6.4f, 0.5f, -4f),
                    new Vector3(1.2f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "West Convergence Baffle", new Vector3(-3.25f, 0.5f, 1.25f),
                    new Vector3(2.4f, 1f, 0.45f), materials.Armor, true).transform.localRotation =
                    Quaternion.Euler(0f, 28f, 0f);
                _wall(root.transform, "East Convergence Baffle", new Vector3(3.25f, 0.5f, 1.25f),
                    new Vector3(2.4f, 1f, 0.45f), materials.Armor, true).transform.localRotation =
                    Quaternion.Euler(0f, -28f, 0f);

                var busbarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PREFAB_PATH);
                var busbar = (GameObject)PrefabUtility.InstantiatePrefab(busbarPrefab, root.transform);
                busbar.name = "Convergence Busbar Assembly";
                busbar.transform.localPosition = new Vector3(0f, 0f, 0.1f);

                var routing = new GameObject("Convergence Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "West Return Feed", new Vector3(-4.7f, -0.1f, -2f),
                    new Vector3(0.12f, 0.04f, 3.8f), materials.Cyan, false);
                _wall(routing.transform, "East Return Feed", new Vector3(4.7f, -0.1f, -2f),
                    new Vector3(0.12f, 0.04f, 3.8f), materials.Cyan, false);
                _wall(routing.transform, "Crossfeed Return Bus", new Vector3(0f, -0.1f, 3.35f),
                    new Vector3(9.4f, 0.04f, 0.12f), materials.Cyan, false);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Convergence Chamber Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, -1.4f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 4.2f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                var entrance = new GameObject("Convergence Reinforcement Gate");
                entrance.transform.SetParent(root.transform, false);
                entrance.transform.localPosition = new Vector3(0f, 0f, 3.35f);
                entrance.AddComponent<AuthoredInterceptorEntrance>().Configure(5);
                _wall(entrance.transform, "Security Gate Header", new Vector3(0f, 0.8f, 0.28f),
                    new Vector3(2.8f, 0.25f, 0.3f), materials.Red, false);

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.SpineTower, new Vector2(6.65f, 3.65f), routing);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _integrateWithGallery(Materials materials)
        {
            var gallery = PrefabUtility.LoadPrefabContents(GALLERY_PREFAB_PATH);
            try
            {
                var north = gallery.transform.Find("Induction Gallery North Bulkhead");
                if (north != null)
                {
                    UnityEngine.Object.DestroyImmediate(north.gameObject);
                }

                _replaceWall(gallery.transform, "Induction Gallery North West", new Vector3(-6.4f, 0.5f, 3.5f),
                    new Vector3(1.2f, 1f, 0.35f), materials.Armor);
                _replaceWall(gallery.transform, "Induction Gallery North Center", new Vector3(0f, 0.5f, 3.5f),
                    new Vector3(6.4f, 1f, 0.35f), materials.Armor);
                _replaceWall(gallery.transform, "Induction Gallery North East", new Vector3(6.4f, 0.5f, 3.5f),
                    new Vector3(1.2f, 1f, 0.35f), materials.Armor);

                var existing = gallery.transform.Find("Convergence Chamber Region");
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var regionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var chamber = (GameObject)PrefabUtility.InstantiatePrefab(regionPrefab, gallery.transform);
                chamber.name = "Convergence Chamber Region";
                chamber.transform.localPosition = new Vector3(0f, 0f, 8.5f);
                chamber.transform.localRotation = Quaternion.identity;
                chamber.transform.localScale = Vector3.one;
                PrefabUtility.SaveAsPrefabAsset(gallery, GALLERY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(gallery);
            }
        }

        private static void _replaceWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            _wall(parent, name, position, scale, material, true);
        }

        private static void _ensureSceneBounds()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>(FindObjectsInactive.Include);
            var serialized = new SerializedObject(references);
            serialized.FindProperty("m_arenaHalfExtents").vector2Value = new Vector2(50.5f, 21.4f);
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

        private sealed class Materials
        {
            public Material Armor;
            public Material Deck;
            public Material Cyan;
            public Material White;
            public Material Amber;
            public Material Red;
            public Material Decal;
        }
    }
}
