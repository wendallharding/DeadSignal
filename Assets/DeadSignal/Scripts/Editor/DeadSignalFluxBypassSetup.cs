using System;
using UnityEditor;
using UnityEngine;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalFluxBypassSetup
    {
        private const string GALLERY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string CHAMBER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceChamberRegion.prefab";
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/FluxBypassRegion.prefab";
        private const string LANDMARK_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ConvergenceBusbar.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/FluxBypassRouteDecal.png";
        private const string MATERIAL_DIRECTORY = "Assets/DeadSignal/Resources/Materials/FluxBypass";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/FluxBypassRouteDecal.mat";
        private const string ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryArmor.mat";
        private const string DECK_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryDeck.mat";
        private const string CYAN_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalCyan.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SalvageAmber.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var gallery = AssetDatabase.LoadAssetAtPath<GameObject>(GALLERY_PREFAB_PATH);
                var chamber = AssetDatabase.LoadAssetAtPath<GameObject>(CHAMBER_PREFAB_PATH);
                return region != null && gallery != null && chamber != null &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       region.GetComponentsInChildren<AuthoredMapObstacle>().Length == 8 &&
                       region.GetComponent<AuthoredPoweredTerritory>() != null &&
                       region.GetComponent<AuthoredFluxShuntObjective>()?.IsConfigured == true &&
                       gallery.transform.Find("Flux Bypass Region") != null &&
                       gallery.GetComponentsInChildren<AuthoredMapObstacle>().Length >= 28 &&
                       chamber.transform.Find("Convergence West Bulkhead") == null &&
                       chamber.transform.Find("Convergence West South") != null &&
                       chamber.transform.Find("Convergence West North") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Flux Bypass")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var materials = _ensureMaterials();
            _ensureRegionPrefab(materials);
            _openChamberThreshold(materials);
            _integrateWithGallery(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored Flux Bypass is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the bypass decal at {DECAL_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "FluxBypass");
            }
        }

        private static Materials _ensureMaterials()
        {
            var decal = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (decal == null)
            {
                decal = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "FluxBypassRouteDecal"
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
                Amber = AssetDatabase.LoadAssetAtPath<Material>(AMBER_MATERIAL_PATH),
                Decal = decal
            };
        }

        private static void _ensureRegionPrefab(Materials materials)
        {
            var root = new GameObject("FluxBypassRegion");
            try
            {
                _wall(root.transform, "Flux Bypass Deck", new Vector3(0f, -0.42f, 0f),
                    new Vector3(7f, 0.55f, 11.5f), materials.Deck, false);
                _wall(root.transform, "Flux West Bulkhead", new Vector3(-3.5f, 0.5f, 0f),
                    new Vector3(0.35f, 1f, 11.5f), materials.Armor, true);
                _wall(root.transform, "Flux North Bulkhead", new Vector3(0f, 0.5f, 5.75f),
                    new Vector3(7f, 1f, 0.35f), materials.Armor, true);
                _wall(root.transform, "Flux East South", new Vector3(3.5f, 0.5f, -5.125f),
                    new Vector3(0.35f, 1f, 1.25f), materials.Armor, true);
                _wall(root.transform, "Flux East Center", new Vector3(3.5f, 0.5f, -0.25f),
                    new Vector3(0.35f, 1f, 3.5f), materials.Armor, true);
                _wall(root.transform, "Flux East North", new Vector3(3.5f, 0.5f, 4.875f),
                    new Vector3(0.35f, 1f, 1.75f), materials.Armor, true);

                _wall(root.transform, "South Flux Deflector", new Vector3(-0.6f, 0.5f, -1.5f),
                    new Vector3(2.8f, 1f, 0.45f), materials.Armor, true).transform.localRotation =
                    Quaternion.Euler(0f, 32f, 0f);
                _wall(root.transform, "North Flux Deflector", new Vector3(0.6f, 0.5f, 4.8f),
                    new Vector3(2.8f, 1f, 0.45f), materials.Armor, true).transform.localRotation =
                    Quaternion.Euler(0f, -32f, 0f);

                var landmarkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LANDMARK_PREFAB_PATH);
                var landmark = (GameObject)PrefabUtility.InstantiatePrefab(landmarkPrefab, root.transform);
                landmark.name = "Flux Shunt Regulator";
                landmark.transform.localPosition = new Vector3(-0.25f, 0f, 0.75f);
                landmark.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                landmark.transform.localScale = Vector3.one * 0.82f;

                var interactionAnchor = new GameObject("Flux Shunt Interaction Anchor");
                interactionAnchor.transform.SetParent(root.transform, false);
                interactionAnchor.transform.localPosition = new Vector3(1.45f, 0f, 0.75f);

                var routing = new GameObject("Flux Bypass Signal Lines");
                routing.transform.SetParent(root.transform, false);
                _wall(routing.transform, "Flux Return Trunk", new Vector3(-2.55f, -0.1f, 0f),
                    new Vector3(0.12f, 0.04f, 9.5f), materials.Cyan, false);
                _wall(routing.transform, "Flux Gallery Feed", new Vector3(0.45f, -0.1f, -3.25f),
                    new Vector3(5.8f, 0.04f, 0.12f), materials.Cyan, false);
                _wall(routing.transform, "Flux Chamber Feed", new Vector3(0.45f, -0.1f, 2.75f),
                    new Vector3(5.8f, 0.04f, 0.12f), materials.Cyan, false);

                var available = new GameObject("Flux Shunt Available");
                available.transform.SetParent(root.transform, false);
                available.transform.localPosition = new Vector3(-0.25f, 0f, 0.75f);
                _markerPart(available.transform, "Amber Shunt Ring", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.08f, 0f), new Vector3(1.05f, 0.025f, 1.05f), materials.Amber);
                _markerPart(available.transform, "Amber Shunt Lever", PrimitiveType.Cube,
                    new Vector3(0f, 0.58f, 0f), new Vector3(0.16f, 0.7f, 0.16f), materials.Amber);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Flux Bypass Route Decal";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, -0.35f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 4.1f;
                decal.GetComponent<Renderer>().sharedMaterial = materials.Decal;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                root.AddComponent<AuthoredPoweredTerritory>().Configure(
                    PoweredTerritorySource.SpineTower, new Vector2(3.15f, 5.4f), null);
                root.AddComponent<AuthoredFluxShuntObjective>().Configure(interactionAnchor.transform, available, routing);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _openChamberThreshold(Materials materials)
        {
            var chamber = PrefabUtility.LoadPrefabContents(CHAMBER_PREFAB_PATH);
            try
            {
                _remove(chamber.transform, "Convergence West Bulkhead");
                _replaceWall(chamber.transform, "Convergence West South", new Vector3(-7f, 0.5f, -3.375f),
                    new Vector3(0.35f, 1f, 1.25f), materials.Armor);
                _replaceWall(chamber.transform, "Convergence West North", new Vector3(-7f, 0.5f, 1.875f),
                    new Vector3(0.35f, 1f, 4.25f), materials.Armor);
                PrefabUtility.SaveAsPrefabAsset(chamber, CHAMBER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(chamber);
            }
        }

        private static void _integrateWithGallery(Materials materials)
        {
            var gallery = PrefabUtility.LoadPrefabContents(GALLERY_PREFAB_PATH);
            try
            {
                _remove(gallery.transform, "Induction Gallery West Bulkhead");
                _replaceWall(gallery.transform, "Induction Gallery West South", new Vector3(-7f, 0.5f, -1.875f),
                    new Vector3(0.35f, 1f, 3.25f), materials.Armor);
                _replaceWall(gallery.transform, "Induction Gallery West North", new Vector3(-7f, 0.5f, 2.875f),
                    new Vector3(0.35f, 1f, 1.25f), materials.Armor);

                _remove(gallery.transform, "Convergence Chamber Region");
                var chamberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CHAMBER_PREFAB_PATH);
                var chamber = (GameObject)PrefabUtility.InstantiatePrefab(chamberPrefab, gallery.transform);
                chamber.name = "Convergence Chamber Region";
                chamber.transform.localPosition = new Vector3(0f, 0f, 8.5f);

                _remove(gallery.transform, "Flux Bypass Region");
                var regionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var bypass = (GameObject)PrefabUtility.InstantiatePrefab(regionPrefab, gallery.transform);
                bypass.name = "Flux Bypass Region";
                bypass.transform.localPosition = new Vector3(-10.5f, 0f, 4.25f);
                PrefabUtility.SaveAsPrefabAsset(gallery, GALLERY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(gallery);
            }
        }

        private static void _remove(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void _replaceWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            _remove(parent, name);
            _wall(parent, name, position, scale, material, true);
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

        private static void _markerPart(Transform parent, string name, PrimitiveType primitiveType,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private sealed class Materials
        {
            public Material Armor;
            public Material Deck;
            public Material Cyan;
            public Material Amber;
            public Material Decal;
        }
    }
}
