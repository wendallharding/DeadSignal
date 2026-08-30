using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalRelayTransferHeroSetup
    {
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayTransferHeroAtlas.png";
        private const string RELAY_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayForkHeroFinish.asset";
        private const string TRANSFER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/TransferVaultHeroFinish.asset";
        private const string RELAY_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/NorthwestRelayFork.prefab";
        private const string TRANSFER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/RelayTransferFinish";
        private const string GRAPHITE_MATERIAL_PATH = MATERIAL_FOLDER + "/RelayTransferGraphite.mat";
        private const string CERAMIC_MATERIAL_PATH = MATERIAL_FOLDER + "/RelayTransferCeramic.mat";
        private const string COPPER_MATERIAL_PATH = MATERIAL_FOLDER + "/RelayTransferCopper.mat";
        private const string DECK_MATERIAL_PATH = MATERIAL_FOLDER + "/RelayTransferDeck.mat";

        public static bool HasAssets
        {
            get
            {
                var relay = AssetDatabase.LoadAssetAtPath<GameObject>(RELAY_PREFAB_PATH);
                var transfer = AssetDatabase.LoadAssetAtPath<GameObject>(TRANSFER_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(RELAY_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(TRANSFER_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(GRAPHITE_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(CERAMIC_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(COPPER_MATERIAL_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(DECK_MATERIAL_PATH) != null &&
                       relay != null && relay.GetComponentInChildren<AuthoredRelayTransferHeroFinish>(true) != null &&
                       transfer != null && transfer.GetComponentInChildren<AuthoredRelayTransferHeroFinish>(true) != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Relay Fork and Transfer Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _ensureMeshes();
            _upgradeRelayPrefab(materials);
            _upgradeTransferPrefab(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Relay Fork and Transfer Vault hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Relay/Transfer hero atlas at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "RelayTransferFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(GRAPHITE_MATERIAL_PATH, "Relay Transfer Graphite", texture,
                    new Vector2(0f, 0.52f), 0.72f, 0.36f),
                _ensureMaterial(CERAMIC_MATERIAL_PATH, "Relay Transfer Ceramic", texture,
                    new Vector2(0.52f, 0.52f), 0.12f, 0.28f),
                _ensureMaterial(COPPER_MATERIAL_PATH, "Relay Transfer Copper", texture,
                    new Vector2(0f, 0f), 0.78f, 0.46f),
                _ensureMaterial(DECK_MATERIAL_PATH, "Relay Transfer Deck", texture,
                    new Vector2(0.52f, 0f), 0.54f, 0.2f)
            };
        }

        private static Material _ensureMaterial(
            string path,
            string materialName,
            Texture texture,
            Vector2 offset,
            float metallic,
            float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Relay/Transfer materials.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(0.48f, 0.48f));
            material.SetTextureOffset("_BaseMap", offset);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", Color.black);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.DisableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _ensureMeshes()
        {
            var relay = new MeshBuilder("RelayForkHeroFinish", 4);
            relay.AddBox(new Vector3(0f, 0.035f, 0f), new Vector3(5.8f, 0.07f, 5.2f), 0f, 3);
            relay.AddBox(new Vector3(-2.68f, 0.12f, 0f), new Vector3(0.2f, 0.24f, 4.8f), 0f, 0);
            relay.AddBox(new Vector3(2.68f, 0.12f, 0f), new Vector3(0.2f, 0.24f, 4.8f), 0f, 0);
            relay.AddBox(new Vector3(0f, 0.13f, -2.32f), new Vector3(5.2f, 0.22f, 0.2f), 0f, 1);
            relay.AddBox(new Vector3(-1.22f, 0.11f, 0.35f), new Vector3(0.16f, 0.14f, 3.4f), -22f, 2);
            relay.AddBox(new Vector3(1.22f, 0.11f, 0.35f), new Vector3(0.16f, 0.14f, 3.4f), 22f, 2);
            relay.AddBox(new Vector3(0f, 0.12f, 1.76f), new Vector3(2.3f, 0.16f, 0.18f), 0f, 2);
            relay.AddBox(new Vector3(0f, 0.15f, -1.55f), new Vector3(1.5f, 0.2f, 0.28f), 0f, 1);
            _saveOrReplaceMesh(RELAY_MESH_PATH, relay.Build());

            var transfer = new MeshBuilder("TransferVaultHeroFinish", 4);
            transfer.AddBox(new Vector3(0f, 0.035f, 0f), new Vector3(6.3f, 0.07f, 5.2f), 0f, 3);
            transfer.AddBox(new Vector3(0f, 0.12f, 2.38f), new Vector3(5.8f, 0.2f, 0.2f), 0f, 0);
            transfer.AddBox(new Vector3(0f, 0.12f, -2.38f), new Vector3(5.8f, 0.2f, 0.2f), 0f, 0);
            transfer.AddBox(new Vector3(2.86f, 0.12f, 0f), new Vector3(0.2f, 0.24f, 4.5f), 0f, 1);
            transfer.AddBox(new Vector3(-2.86f, 0.13f, 1.75f), new Vector3(0.2f, 0.26f, 0.7f), 0f, 1);
            transfer.AddBox(new Vector3(-2.86f, 0.13f, -1.75f), new Vector3(0.2f, 0.26f, 0.7f), 0f, 1);
            transfer.AddBox(new Vector3(0.65f, 0.11f, 0f), new Vector3(0.18f, 0.14f, 4.0f), 0f, 2);
            transfer.AddBox(new Vector3(-0.2f, 0.11f, 0.9f), new Vector3(1.7f, 0.14f, 0.16f), 0f, 2);
            transfer.AddBox(new Vector3(-0.2f, 0.11f, -0.9f), new Vector3(1.7f, 0.14f, 0.16f), 0f, 2);
            transfer.AddBox(new Vector3(-2.72f, 0.18f, 0f), new Vector3(0.28f, 0.3f, 2.4f), 0f, 0);
            _saveOrReplaceMesh(TRANSFER_MESH_PATH, transfer.Build());
        }

        private static void _upgradeRelayPrefab(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(RELAY_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredRelayForkObjective>();
                if (objective == null)
                {
                    throw new InvalidOperationException("The Relay Fork prefab has no authored objective.");
                }

                var finish = _ensureFinish(root.transform, "Relay Fork Hero Finish", RELAY_MESH_PATH, materials);
                var component = finish.GetComponent<AuthoredRelayTransferHeroFinish>() ??
                                finish.gameObject.AddComponent<AuthoredRelayTransferHeroFinish>();
                component.ConfigureRelay(finish.GetComponent<MeshRenderer>(), objective);
                objective.ConfigureHeroFinish(component);
                PrefabUtility.SaveAsPrefabAsset(root, RELAY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeTransferPrefab(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(TRANSFER_PREFAB_PATH);
            try
            {
                var objective = root.GetComponent<AuthoredTransferVaultObjective>();
                if (objective == null)
                {
                    throw new InvalidOperationException("The Transfer Vault prefab has no authored objective.");
                }

                var finish = _ensureFinish(root.transform, "Transfer Vault Hero Finish", TRANSFER_MESH_PATH, materials);
                var component = finish.GetComponent<AuthoredRelayTransferHeroFinish>() ??
                                finish.gameObject.AddComponent<AuthoredRelayTransferHeroFinish>();
                component.ConfigureTransfer(finish.GetComponent<MeshRenderer>(), objective);
                objective.ConfigureHeroFinish(component);
                PrefabUtility.SaveAsPrefabAsset(root, TRANSFER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform _ensureFinish(Transform parent, string objectName, string meshPath, Material[] materials)
        {
            var finish = parent.Find(objectName);
            if (finish == null)
            {
                var finishObject = new GameObject(objectName);
                finishObject.transform.SetParent(parent, false);
                finishObject.AddComponent<MeshFilter>();
                finishObject.AddComponent<MeshRenderer>();
                finish = finishObject.transform;
            }

            finish.localPosition = Vector3.zero;
            finish.localRotation = Quaternion.identity;
            finish.localScale = Vector3.one;
            finish.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            finish.GetComponent<MeshRenderer>().sharedMaterials = materials;
            return finish;
        }

        private static void _saveOrReplaceMesh(string path, Mesh generated)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return;
            }

            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name, int subMeshCount)
            {
                m_name = name;
                m_triangles = Enumerable.Range(0, subMeshCount).Select(_ => new List<int>()).ToList();
            }

            public void AddBox(Vector3 center, Vector3 size, float yaw, int subMesh)
            {
                var half = size * 0.5f;
                var rotation = Quaternion.Euler(0f, yaw, 0f);
                var corners = new[]
                {
                    new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                    new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z),
                    new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
                    new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z)
                };
                for (var index = 0; index < corners.Length; index++)
                {
                    corners[index] = center + rotation * corners[index];
                }

                var start = m_vertices.Count;
                m_vertices.AddRange(corners);
                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                m_triangles[subMesh].AddRange(new[]
                {
                    start, start + 2, start + 1, start, start + 3, start + 2,
                    start + 4, start + 5, start + 6, start + 4, start + 6, start + 7,
                    start, start + 4, start + 7, start, start + 7, start + 3,
                    start + 1, start + 2, start + 6, start + 1, start + 6, start + 5,
                    start + 3, start + 7, start + 6, start + 3, start + 6, start + 2,
                    start, start + 1, start + 5, start, start + 5, start + 4
                });
            }

            public Mesh Build()
            {
                var mesh = new Mesh { name = m_name };
                mesh.SetVertices(m_vertices);
                mesh.SetUVs(0, m_uvs);
                mesh.subMeshCount = m_triangles.Count;
                for (var index = 0; index < m_triangles.Count; index++)
                {
                    mesh.SetTriangles(m_triangles[index], index);
                }
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }

            private readonly string m_name;
            private readonly List<Vector3> m_vertices = new();
            private readonly List<Vector2> m_uvs = new();
            private readonly List<List<int>> m_triangles;
        }
    }
}
