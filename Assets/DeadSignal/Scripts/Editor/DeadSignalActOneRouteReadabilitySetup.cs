using System;
using System.Collections.Generic;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadSignal.Editor
{
    public static class DeadSignalActOneRouteReadabilitySetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string RELAY_FORK_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/NorthwestRelayFork.prefab";
        private const string SHORTCUT_GATE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/ShortcutGateAssembly.prefab";
        private const string VAULT_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab";
        private const string RELAY_STATUS_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkStatusPanel.png";
        private const string SHORTCUT_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/ShortcutGatePanel.png";
        private const string RELAY_CONSOLE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkConsoleReadability.asset";
        private const string RELAY_PANEL_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkPanelReadability.asset";
        private const string RELAY_SELECTOR_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkSelectorReadability.asset";
        private const string DOOR_THRESHOLD_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RouteDoorThresholdReadability.asset";
        private const string RELAY_STATUS_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayForkStatus.mat";
        private const string DOOR_STATUS_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RouteDoorThresholdStatus.mat";
        private const string RELAY_ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayBankArmor.mat";

        public static bool HasAssets
        {
            get
            {
                var relayFork = AssetDatabase.LoadAssetAtPath<GameObject>(RELAY_FORK_PREFAB_PATH);
                var shortcut = AssetDatabase.LoadAssetAtPath<GameObject>(SHORTCUT_GATE_PREFAB_PATH);
                var vault = AssetDatabase.LoadAssetAtPath<GameObject>(VAULT_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(RELAY_STATUS_TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(RELAY_CONSOLE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(RELAY_PANEL_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(RELAY_SELECTOR_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(DOOR_THRESHOLD_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(RELAY_STATUS_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DOOR_STATUS_MATERIAL_PATH) != null &&
                       relayFork != null && relayFork.TryGetComponent<AuthoredRelayForkObjective>(out var relayObjective) &&
                       relayObjective.HasReadabilityAssets &&
                       shortcut != null && shortcut.TryGetComponent<AuthoredRouteDoorReadability>(out var shortcutDoor) &&
                       shortcutDoor.IsConfigured &&
                       vault != null && vault.TryGetComponent<AuthoredTransferVaultObjective>(out var vaultObjective) &&
                       vaultObjective.IsRouteConfigured;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Act I Route Readability")]
        public static void EnsureAssets()
        {
            _configureTexture(RELAY_STATUS_TEXTURE_PATH, "Relay Fork status");
            _ensureMaterials();
            _ensureMeshes();
            _upgradeRelayFork();
            _upgradeCentralShortcut();
            _upgradeRelayRouteGate();
            _saveSceneBindings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Act I route-readability assets are incomplete.");
            }
        }

        private static void _configureTexture(string path, string label)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the {label} texture at {path}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterials()
        {
            var relayStatus = _loadOrCreateMaterial(RELAY_STATUS_MATERIAL_PATH, "RelayForkStatus");
            var relayTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RELAY_STATUS_TEXTURE_PATH);
            relayStatus.SetTexture("_BaseMap", relayTexture);
            relayStatus.SetTexture("_EmissionMap", relayTexture);
            relayStatus.SetColor("_BaseColor", Color.white);
            relayStatus.SetColor("_EmissionColor", new Color(1f, 0.42f, 0.04f));
            relayStatus.SetFloat("_Metallic", 0.36f);
            relayStatus.SetFloat("_Smoothness", 0.42f);
            relayStatus.EnableKeyword("_EMISSION");
            relayStatus.enableInstancing = true;
            EditorUtility.SetDirty(relayStatus);

            var doorStatus = _loadOrCreateMaterial(DOOR_STATUS_MATERIAL_PATH, "RouteDoorThresholdStatus");
            var shortcutTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SHORTCUT_TEXTURE_PATH);
            doorStatus.SetTexture("_BaseMap", shortcutTexture);
            doorStatus.SetTexture("_EmissionMap", shortcutTexture);
            doorStatus.SetColor("_BaseColor", new Color(0.72f, 0.08f, 0.045f));
            doorStatus.SetColor("_EmissionColor", new Color(0.72f, 0.08f, 0.045f) * 0.52f);
            doorStatus.SetFloat("_Metallic", 0.48f);
            doorStatus.SetFloat("_Smoothness", 0.4f);
            doorStatus.EnableKeyword("_EMISSION");
            doorStatus.enableInstancing = true;
            EditorUtility.SetDirty(doorStatus);
        }

        private static Material _loadOrCreateMaterial(string path, string materialName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("Could not find the URP Lit shader for Act I route readability.");
            }

            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void _ensureMeshes()
        {
            var console = new MeshBuilder("RelayForkConsoleReadability");
            console.AddBox(new Vector3(0f, 0.12f, 0f), new Vector3(1.45f, 0.24f, 1.15f));
            console.AddBox(new Vector3(-0.62f, 0.24f, 0f), new Vector3(0.18f, 0.34f, 1.05f));
            console.AddBox(new Vector3(0.62f, 0.24f, 0f), new Vector3(0.18f, 0.34f, 1.05f));
            _saveOrReplaceMesh(RELAY_CONSOLE_MESH_PATH, console.Build());

            var panel = new MeshBuilder("RelayForkPanelReadability");
            panel.AddBox(new Vector3(0f, 0.285f, 0f), new Vector3(1.08f, 0.05f, 0.82f));
            _saveOrReplaceMesh(RELAY_PANEL_MESH_PATH, panel.Build());

            var selector = new MeshBuilder("RelayForkSelectorReadability");
            selector.AddPrism(new Vector3(0f, 0.36f, 0f), 8, 0.24f, 0.2f, 0.09f);
            selector.AddBox(new Vector3(0f, 0.405f, 0.25f), new Vector3(0.1f, 0.08f, 0.34f));
            _saveOrReplaceMesh(RELAY_SELECTOR_MESH_PATH, selector.Build());

            var threshold = new MeshBuilder("RouteDoorThresholdReadability");
            threshold.AddBox(new Vector3(0f, 0.1f, -1.28f), new Vector3(0.62f, 0.2f, 0.24f));
            threshold.AddBox(new Vector3(0f, 0.1f, 1.28f), new Vector3(0.62f, 0.2f, 0.24f));
            threshold.AddBox(new Vector3(0f, 0.1f, 0f), new Vector3(0.22f, 0.12f, 2.35f));
            threshold.AddBox(new Vector3(0f, 1.42f, -1.28f), new Vector3(0.5f, 2.65f, 0.22f));
            threshold.AddBox(new Vector3(0f, 1.42f, 1.28f), new Vector3(0.5f, 2.65f, 0.22f));
            threshold.AddBox(new Vector3(0f, 2.72f, 0f), new Vector3(0.5f, 0.18f, 2.78f));
            _saveOrReplaceMesh(DOOR_THRESHOLD_MESH_PATH, threshold.Build());
        }

        private static void _upgradeRelayFork()
        {
            var root = PrefabUtility.LoadPrefabContents(RELAY_FORK_PREFAB_PATH);
            try
            {
                var console = _ensureMeshPart(root.transform, "Relay Routing Console", new Vector3(0f, 0f, 1.85f),
                    RELAY_CONSOLE_MESH_PATH, RELAY_ARMOR_MATERIAL_PATH);
                var panel = _ensureMeshPart(console, "Relay Routing Status Panel", Vector3.zero,
                    RELAY_PANEL_MESH_PATH, RELAY_STATUS_MATERIAL_PATH);
                var selector = _ensureMeshPart(console, "Relay Routing Selector", Vector3.zero,
                    RELAY_SELECTOR_MESH_PATH, RELAY_STATUS_MATERIAL_PATH);
                var objective = root.GetComponent<AuthoredRelayForkObjective>();
                if (objective == null)
                {
                    throw new InvalidOperationException("The Relay Fork has no authored objective.");
                }

                objective.ConfigureReadability(
                    new[] { panel.GetComponent<Renderer>(), selector.GetComponent<Renderer>() }, selector);
                PrefabUtility.SaveAsPrefabAsset(root, RELAY_FORK_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeCentralShortcut()
        {
            var root = PrefabUtility.LoadPrefabContents(SHORTCUT_GATE_PREFAB_PATH);
            try
            {
                var slab = root.transform.Find("Signal Shortcut Gate")?.gameObject;
                var openMarker = root.transform.Find("Shortcut Gate Signal")?.gameObject;
                var threshold = _ensureMeshPart(root.transform, "Central Shortcut Threshold", Vector3.zero,
                    DOOR_THRESHOLD_MESH_PATH, DOOR_STATUS_MATERIAL_PATH);
                if (slab == null || openMarker == null)
                {
                    throw new InvalidOperationException("The Central shortcut is missing its blocker or route signal.");
                }

                var readability = root.GetComponent<AuthoredRouteDoorReadability>() ??
                                  root.AddComponent<AuthoredRouteDoorReadability>();
                readability.Configure(slab, openMarker, threshold.GetComponent<Renderer>());
                PrefabUtility.SaveAsPrefabAsset(root, SHORTCUT_GATE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeRelayRouteGate()
        {
            var root = PrefabUtility.LoadPrefabContents(VAULT_PREFAB_PATH);
            try
            {
                var slab = root.transform.Find("Central Relay Route Gate")?.gameObject;
                var openMarker = root.transform.Find("Central Relay Route Open")?.gameObject;
                var threshold = _ensureMeshPart(root.transform, "Central Relay Route Threshold",
                    new Vector3(-3.15f, 0f, 0f), DOOR_THRESHOLD_MESH_PATH, DOOR_STATUS_MATERIAL_PATH);
                var objective = root.GetComponent<AuthoredTransferVaultObjective>();
                if (slab == null || openMarker == null || objective == null)
                {
                    throw new InvalidOperationException("The Central Relay Route Gate is incomplete.");
                }

                var readability = root.GetComponent<AuthoredRouteDoorReadability>() ??
                                  root.AddComponent<AuthoredRouteDoorReadability>();
                readability.Configure(slab, openMarker, threshold.GetComponent<Renderer>());
                objective.ConfigureRouteGate(slab, openMarker, readability);
                PrefabUtility.SaveAsPrefabAsset(root, VAULT_PREFAB_PATH);
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

        private static void _saveSceneBindings()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _saveOrReplaceMesh(string path, Mesh mesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return;
            }

            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name)
            {
                m_name = name;
            }

            public void AddBox(Vector3 center, Vector3 size)
            {
                var half = size * 0.5f;
                var corners = new[]
                {
                    new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                    new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z),
                    new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
                    new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z)
                };
                var faces = new[]
                {
                    0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                    0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5,
                    3, 7, 6, 3, 6, 2, 0, 1, 5, 0, 5, 4
                };
                var start = m_vertices.Count;
                foreach (var corner in corners)
                {
                    m_vertices.Add(center + corner);
                }
                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                foreach (var index in faces)
                {
                    m_triangles.Add(start + index);
                }
            }

            public void AddPrism(Vector3 center, int sides, float bottomRadius, float topRadius, float height)
            {
                var start = m_vertices.Count;
                var halfHeight = height * 0.5f;
                for (var index = 0; index < sides; index++)
                {
                    var angle = index * Mathf.PI * 2f / sides;
                    var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    m_vertices.Add(center + direction * bottomRadius + Vector3.down * halfHeight);
                    m_vertices.Add(center + direction * topRadius + Vector3.up * halfHeight);
                    m_uvs.Add(new Vector2(index / (float)sides, 0f));
                    m_uvs.Add(new Vector2(index / (float)sides, 1f));
                }

                for (var index = 0; index < sides; index++)
                {
                    var next = (index + 1) % sides;
                    m_triangles.AddRange(new[]
                    {
                        start + index * 2, start + next * 2 + 1, start + next * 2,
                        start + index * 2, start + index * 2 + 1, start + next * 2 + 1
                    });
                }
            }

            public Mesh Build()
            {
                var mesh = new Mesh { name = m_name };
                mesh.SetVertices(m_vertices);
                mesh.SetUVs(0, m_uvs);
                mesh.SetTriangles(m_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }

            private readonly string m_name;
            private readonly List<Vector3> m_vertices = new();
            private readonly List<Vector2> m_uvs = new();
            private readonly List<int> m_triangles = new();
        }
    }
}
