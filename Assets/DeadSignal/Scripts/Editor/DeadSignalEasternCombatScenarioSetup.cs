using System;
using UnityEditor;
using UnityEngine;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalEasternCombatScenarioSetup
    {
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ArcFurnaceRegion.prefab";
        private const string DECAL_PATH =
            "Assets/DeadSignal/Resources/Environment/EasternCombatLabTarget.png";
        private const string MATERIAL_DIRECTORY =
            "Assets/DeadSignal/Resources/Materials/EasternCombatLab";
        private const string DECAL_MATERIAL_PATH = MATERIAL_DIRECTORY + "/EasternCombatLabTarget.mat";

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var scenario = region == null ? null : region.GetComponentInChildren<AuthoredCombatScenario>();
                return scenario != null && scenario.IsComplete &&
                       AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH) != null &&
                       scenario.transform.Find("Combat Lab Target") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup Eastern Combat Scenario")]
        public static void EnsureAssets()
        {
            _configureDecalImport();
            _ensureMaterialDirectory();
            var material = _ensureMaterial();
            _ensureScenarioAnchors(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The scene-authored eastern combat scenario is incomplete.");
            }
        }

        private static void _configureDecalImport()
        {
            var importer = AssetImporter.GetAtPath(DECAL_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the combat-lab decal at {DECAL_PATH}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "EasternCombatLab");
            }
        }

        private static Material _ensureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(DECAL_MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    name = "EasternCombatLabTarget"
                };
                AssetDatabase.CreateAsset(material, DECAL_MATERIAL_PATH);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DECAL_PATH));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _ensureScenarioAnchors(Material material)
        {
            var region = PrefabUtility.LoadPrefabContents(REGION_PREFAB_PATH);
            try
            {
                var existing = region.transform.Find("Eastern Combat Scenario");
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var root = new GameObject("Eastern Combat Scenario");
                root.transform.SetParent(region.transform, false);
                var player = _anchor(root.transform, "Player Anchor", new Vector3(0f, 0f, -2.65f), Vector3.forward);
                var camera = _anchor(root.transform, "Camera Focus", new Vector3(0f, 0f, -0.25f), Vector3.forward);
                var warden = _anchor(root.transform, "Warden Staging", new Vector3(-5f, 0f, 0.4f), Vector3.back);
                var sapper = _anchor(root.transform, "Sapper Staging", new Vector3(5f, 0f, 0.4f), Vector3.back);
                var interceptor = _anchor(root.transform, "Interceptor Staging", new Vector3(-4.3f, 0f, -2.35f), Vector3.right);
                var suppressor = _anchor(root.transform, "Suppressor Staging", new Vector3(4.3f, 0f, -2.35f), Vector3.left);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                decal.name = "Combat Lab Target";
                decal.transform.SetParent(root.transform, false);
                decal.transform.localPosition = new Vector3(0f, -0.105f, -2.65f);
                decal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                decal.transform.localScale = Vector3.one * 3.35f;
                decal.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());

                root.AddComponent<AuthoredCombatScenario>().Configure(
                    player, camera, warden, sapper, interceptor, suppressor,
                    new Vector2(-4.6f, -2.6f), new Vector2(4.6f, 0.5f));
                PrefabUtility.SaveAsPrefabAsset(region, REGION_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(region);
            }
        }

        private static Transform _anchor(Transform parent, string name, Vector3 position, Vector3 forward)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            anchor.localRotation = Quaternion.LookRotation(forward, Vector3.up);
            return anchor;
        }
    }
}
