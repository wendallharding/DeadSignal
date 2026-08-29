using System;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalActTwoReadabilitySetup
    {
        private const string FOUNDRY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryRegion.prefab";
        private const string STATUS_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayNetworkStatusPanel.png";
        private const string STATUS_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayNetworkStatus.mat";
        private const string PANEL_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkPanelReadability.asset";

        public static bool HasAssets
        {
            get
            {
                var foundry = AssetDatabase.LoadAssetAtPath<GameObject>(FOUNDRY_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH) != null &&
                       foundry != null &&
                       foundry.TryGetComponent<AuthoredRelayNetworkReadability>(out var readability) &&
                       readability.IsConfigured &&
                       foundry.transform.Find("Relay Foundry Network Status") != null &&
                       foundry.transform.Find("Cooling Gantry Network Status") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Act II Relay Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            var statusMaterial = _ensureMaterial();
            _upgradeFoundry(statusMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Act II Relay readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(STATUS_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Relay network status texture at {STATUS_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material _ensureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Relay readability.");
                }

                material = new Material(shader) { name = "RelayNetworkStatus" };
                AssetDatabase.CreateAsset(material, STATUS_MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.055f));
            material.SetFloat("_Metallic", 0.4f);
            material.SetFloat("_Smoothness", 0.44f);
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _upgradeFoundry(Material statusMaterial)
        {
            var root = PrefabUtility.LoadPrefabContents(FOUNDRY_PREFAB_PATH);
            try
            {
                var panelMesh = AssetDatabase.LoadAssetAtPath<Mesh>(PANEL_MESH_PATH);
                var relayPanel = _ensurePanel(root.transform, "Relay Foundry Network Status",
                    new Vector3(4.25f, 0.04f, -1.8f), panelMesh, statusMaterial);
                var gantryPanel = _ensurePanel(root.transform, "Cooling Gantry Network Status",
                    new Vector3(-3.75f, 0.04f, -13.8f), panelMesh, statusMaterial);
                var relayCore = root.transform.Find("Relay Tower Assembly/Tower Core");
                var exchanger = root.transform.Find("Relay Cooling Gantry Region/Relay Heat Exchanger");
                if (relayCore == null || exchanger == null)
                {
                    throw new InvalidOperationException("The Relay Foundry is missing its tower core or heat exchanger.");
                }

                var relayRenderers = new[] { relayCore.GetComponent<Renderer>(), relayPanel };
                var gantryRenderers = exchanger.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.name.Contains("coolant coil", StringComparison.OrdinalIgnoreCase))
                    .Append(gantryPanel)
                    .ToArray();
                if (relayRenderers.Any(renderer => renderer == null) || gantryRenderers.Any(renderer => renderer == null))
                {
                    throw new InvalidOperationException("The Relay readability renderer bindings are incomplete.");
                }

                var readability = root.GetComponent<AuthoredRelayNetworkReadability>() ??
                                  root.AddComponent<AuthoredRelayNetworkReadability>();
                readability.Configure(relayRenderers, gantryRenderers);
                PrefabUtility.SaveAsPrefabAsset(root, FOUNDRY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Renderer _ensurePanel(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Mesh mesh,
            Material material)
        {
            var panel = parent.Find(objectName);
            if (panel == null)
            {
                panel = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                panel.SetParent(parent, false);
            }

            panel.localPosition = localPosition;
            panel.localRotation = Quaternion.identity;
            panel.localScale = new Vector3(1.5f, 1f, 1.15f);
            panel.GetComponent<MeshFilter>().sharedMesh = mesh;
            panel.GetComponent<MeshRenderer>().sharedMaterial = material;
            return panel.GetComponent<Renderer>();
        }
    }
}
