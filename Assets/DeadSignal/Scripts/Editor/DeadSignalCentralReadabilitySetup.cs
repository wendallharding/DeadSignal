using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalCentralReadabilitySetup
    {
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string TOWER_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/SignalTowerAssembly.prefab";
        private const string VAULT_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/EastSalvageVault.prefab";
        private const string STATUS_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralMachineryStatusPanel.png";
        private const string HERO_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerHeroAtlas.png";
        private const string BASE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerBaseReadability.asset";
        private const string COLUMN_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerColumnReadability.asset";
        private const string CORE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerCoreReadability.asset";
        private const string ASSEMBLER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/TransferVaultAssemblerReadability.asset";
        private const string HERO_FINISH_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CentralTowerHeroFinish.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/CentralReadability";
        private const string STATUS_MATERIAL_PATH = MATERIAL_FOLDER + "/CentralMachineryStatus.mat";
        private const string HERO_GRAPHITE_MATERIAL_PATH = MATERIAL_FOLDER + "/CentralHeroGraphite.mat";
        private const string HERO_CERAMIC_MATERIAL_PATH = MATERIAL_FOLDER + "/CentralHeroCeramic.mat";
        private const string HERO_AMBER_MATERIAL_PATH = MATERIAL_FOLDER + "/CentralHeroAmber.mat";
        private const string HERO_DECK_MATERIAL_PATH = MATERIAL_FOLDER + "/CentralHeroDeck.mat";
        private const string TOWER_HOUSING_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/WorldPalette/SignalTowerHousing.mat";
        private const string VAULT_ARMOR_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/EastVaultArmor.mat";

        public static bool HasAssets =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(BASE_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(COLUMN_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(CORE_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(ASSEMBLER_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Mesh>(HERO_FINISH_MESH_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(HERO_GRAPHITE_MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(HERO_CERAMIC_MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(HERO_AMBER_MATERIAL_PATH) != null &&
            AssetDatabase.LoadAssetAtPath<Material>(HERO_DECK_MATERIAL_PATH) != null;

        [MenuItem("DEAD SIGNAL/Setup/Central Machinery Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _configureHeroTexture();
            _ensureFolder();
            var statusMaterial = _ensureStatusMaterial();
            var heroMaterials = _ensureHeroMaterials();
            _ensureMesh(BASE_MESH_PATH, _buildTowerBase());
            _ensureMesh(COLUMN_MESH_PATH, _buildTowerColumn());
            _ensureMesh(CORE_MESH_PATH, _buildTowerCore());
            _ensureMesh(ASSEMBLER_MESH_PATH, _buildTransferAssembler());
            _ensureMesh(HERO_FINISH_MESH_PATH, _buildHeroFinish());
            _upgradeTowerPrefab(statusMaterial);
            _upgradeTransferVault(statusMaterial);
            _bindSceneReadability(heroMaterials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Central machinery readability assets are incomplete.");
            }
        }

        private static void _configureHeroTexture()
        {
            var importer = AssetImporter.GetAtPath(HERO_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Central hero atlas at {HERO_TEXTURE_PATH}.");
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

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(STATUS_TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Central machinery texture at {STATUS_TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void _ensureFolder()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "CentralReadability");
            }
        }

        private static Material _ensureStatusMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "CentralMachineryStatus"
                };
                AssetDatabase.CreateAsset(material, STATUS_MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(STATUS_TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(0.02f, 0.75f, 0.9f));
            material.SetFloat("_Metallic", 0.35f);
            material.SetFloat("_Smoothness", 0.42f);
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material[] _ensureHeroMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(HERO_TEXTURE_PATH);
            return new[]
            {
                _ensureHeroMaterial(HERO_GRAPHITE_MATERIAL_PATH, "Central Hero Graphite", texture,
                    new Vector2(0f, 0.52f), 0.72f, 0.38f, Color.black),
                _ensureHeroMaterial(HERO_CERAMIC_MATERIAL_PATH, "Central Hero Ceramic", texture,
                    new Vector2(0.52f, 0.52f), 0.24f, 0.28f, Color.black),
                _ensureHeroMaterial(HERO_AMBER_MATERIAL_PATH, "Central Hero Amber", texture,
                    new Vector2(0f, 0f), 0.58f, 0.34f, new Color(0.36f, 0.12f, 0.015f)),
                _ensureHeroMaterial(HERO_DECK_MATERIAL_PATH, "Central Hero Deck", texture,
                    new Vector2(0.52f, 0f), 0.66f, 0.24f, new Color(0.008f, 0.06f, 0.075f))
            };
        }

        private static Material _ensureHeroMaterial(string path, string name, Texture texture, Vector2 offset,
            float metallic, float smoothness, Color emission)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(0.48f, 0.48f));
            material.SetTextureOffset("_BaseMap", offset);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _ensureMesh(string path, Mesh generated)
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

        private static void _upgradeTowerPrefab(Material statusMaterial)
        {
            var root = PrefabUtility.LoadPrefabContents(TOWER_PREFAB_PATH);
            try
            {
                var housing = AssetDatabase.LoadAssetAtPath<Material>(TOWER_HOUSING_MATERIAL_PATH);
                _assignMesh(root.transform, "Tower Base", BASE_MESH_PATH, housing);
                _assignMesh(root.transform, "Tower Column", COLUMN_MESH_PATH, housing);
                _assignMesh(root.transform, "Tower Core", CORE_MESH_PATH, statusMaterial);
                PrefabUtility.SaveAsPrefabAsset(root, TOWER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeTransferVault(Material statusMaterial)
        {
            var root = PrefabUtility.LoadPrefabContents(VAULT_PREFAB_PATH);
            try
            {
                var rotor = root.transform.Find("Transfer Assembler Rotor")?.gameObject;
                if (rotor == null)
                {
                    rotor = new GameObject("Transfer Assembler Rotor");
                    rotor.transform.SetParent(root.transform, false);
                    rotor.AddComponent<MeshFilter>();
                    rotor.AddComponent<MeshRenderer>();
                }

                rotor.transform.localPosition = new Vector3(0.65f, 0.16f, 0f);
                rotor.transform.localRotation = Quaternion.identity;
                rotor.transform.localScale = Vector3.one;
                rotor.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ASSEMBLER_MESH_PATH);
                rotor.GetComponent<MeshRenderer>().sharedMaterials = new[]
                {
                    AssetDatabase.LoadAssetAtPath<Material>(VAULT_ARMOR_MATERIAL_PATH),
                    statusMaterial
                };
                root.GetComponent<AuthoredTransferVaultObjective>().ConfigureReadability(
                    rotor.GetComponent<MeshRenderer>(), rotor.transform);
                PrefabUtility.SaveAsPrefabAsset(root, VAULT_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _bindSceneReadability(Material[] heroMaterials)
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var references = UnityEngine.Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var installation = UnityEngine.Object.FindFirstObjectByType<AuthoredCentralInstallationObjective>();
            if (references == null || installation == null)
            {
                throw new InvalidOperationException("Central readability requires scene references and installation markers.");
            }

            var tower = references.SignalTower;
            var readability = tower.GetComponent<AuthoredCentralTowerReadability>() ??
                              tower.AddComponent<AuthoredCentralTowerReadability>();
            var socketRenderers = installation.GetComponentsInChildren<Renderer>(true);
            var statusRenderer = tower.transform.Find("Tower Core").GetComponent<Renderer>();
            statusRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH);
            tower.transform.Find("Tower Base").GetComponent<Renderer>().sharedMaterial = heroMaterials[0];
            tower.transform.Find("Tower Column").GetComponent<Renderer>().sharedMaterial = heroMaterials[1];
            readability.Configure(statusRenderer, socketRenderers);

            var heroFinish = tower.transform.Find("Central Tower Hero Finish")?.gameObject;
            if (heroFinish == null)
            {
                heroFinish = new GameObject("Central Tower Hero Finish");
                heroFinish.transform.SetParent(tower.transform, false);
                heroFinish.AddComponent<MeshFilter>();
                heroFinish.AddComponent<MeshRenderer>();
                heroFinish.AddComponent<AuthoredCentralHeroFinish>();
            }

            heroFinish.transform.localPosition = Vector3.zero;
            heroFinish.transform.localRotation = Quaternion.identity;
            heroFinish.transform.localScale = Vector3.one;
            heroFinish.GetComponent<MeshFilter>().sharedMesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(HERO_FINISH_MESH_PATH);
            var heroRenderer = heroFinish.GetComponent<MeshRenderer>();
            heroRenderer.sharedMaterials = heroMaterials;

            var consoleRenderers = references.StationMachines.transform.Cast<Transform>()
                .OrderBy(child => (child.position - references.TowerPosition).sqrMagnitude)
                .Take(2)
                .SelectMany(child => child.GetComponentsInChildren<MeshRenderer>(true))
                .ToArray();
            for (var index = 0; index < consoleRenderers.Length; index++)
            {
                consoleRenderers[index].sharedMaterial = index % 2 == 0 ? heroMaterials[0] : heroMaterials[2];
            }

            heroFinish.GetComponent<AuthoredCentralHeroFinish>().Configure(heroRenderer, consoleRenderers);
            EditorUtility.SetDirty(tower);
            EditorUtility.SetDirty(heroFinish);
            EditorUtility.SetDirty(installation);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _assignMesh(Transform root, string childName, string meshPath, Material material)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException($"The Signal Tower prefab is missing {childName}.");
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            child.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            child.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh _buildTowerBase()
        {
            var builder = new MeshBuilder("CentralTowerBaseReadability");
            builder.AddPrism(Vector3.zero, 12, 1.18f, 1.02f, 0.32f, 0);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                builder.AddBox(direction * 1.16f + Vector3.up * 0.08f, new Vector3(0.42f, 0.16f, 0.62f), angle, 0);
            }

            return builder.Build();
        }

        private static Mesh _buildTowerColumn()
        {
            var builder = new MeshBuilder("CentralTowerColumnReadability");
            builder.AddPrism(new Vector3(0f, 0.78f, 0f), 8, 0.52f, 0.38f, 1.25f, 0);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                builder.AddBox(direction * 0.48f + Vector3.up * 0.72f, new Vector3(0.16f, 0.82f, 0.16f), angle, 0);
            }

            return builder.Build();
        }

        private static Mesh _buildTowerCore()
        {
            var builder = new MeshBuilder("CentralTowerCoreReadability");
            builder.AddPrism(new Vector3(0f, 1.55f, 0f), 12, 0.76f, 0.76f, 0.24f, 0);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                builder.AddBox(direction * 0.78f + Vector3.up * 1.55f, new Vector3(0.28f, 0.12f, 0.38f), angle, 0);
            }

            return builder.Build();
        }

        private static Mesh _buildTransferAssembler()
        {
            var builder = new MeshBuilder("TransferVaultAssemblerReadability", 2);
            builder.AddBox(Vector3.zero, new Vector3(1.55f, 0.2f, 1.85f), 0f, 0);
            builder.AddBox(new Vector3(-0.58f, 0.2f, 0f), new Vector3(0.18f, 0.28f, 1.55f), 0f, 0);
            builder.AddBox(new Vector3(0.58f, 0.2f, 0f), new Vector3(0.18f, 0.28f, 1.55f), 0f, 0);
            builder.AddPrism(new Vector3(0f, 0.28f, 0f), 8, 0.46f, 0.38f, 0.18f, 1);
            return builder.Build();
        }

        private static Mesh _buildHeroFinish()
        {
            var builder = new MeshBuilder("CentralTowerHeroFinish", 4);
            builder.AddPrism(new Vector3(0f, -0.005f, 0f), 16, 2.35f, 2.22f, 0.12f, 3);
            builder.AddPrism(new Vector3(0f, 0.065f, 0f), 16, 1.5f, 1.44f, 0.08f, 1);

            for (var index = 0; index < 8; index++)
            {
                var angle = index * 45f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                builder.AddBox(direction * 1.92f + Vector3.up * 0.105f,
                    new Vector3(0.1f, 0.025f, 0.68f), angle, 2);
            }

            builder.AddBox(new Vector3(-1.63f, 0.12f, 0f), new Vector3(0.22f, 0.12f, 2.45f), 0f, 0);
            builder.AddBox(new Vector3(1.63f, 0.12f, 0f), new Vector3(0.22f, 0.12f, 2.45f), 0f, 0);
            builder.AddBox(new Vector3(-1.63f, 0.195f, 0f), new Vector3(0.06f, 0.025f, 2.1f), 0f, 2);
            builder.AddBox(new Vector3(1.63f, 0.195f, 0f), new Vector3(0.06f, 0.025f, 2.1f), 0f, 2);
            return builder.Build();
        }

        private sealed class MeshBuilder
        {
            public MeshBuilder(string name, int subMeshCount = 1)
            {
                m_name = name;
                for (var index = 0; index < subMeshCount; index++)
                {
                    m_triangles.Add(new List<int>());
                }
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

                var faces = new[]
                {
                    0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                    0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5,
                    3, 7, 6, 3, 6, 2, 0, 1, 5, 0, 5, 4
                };
                var start = m_vertices.Count;
                m_vertices.AddRange(corners);
                m_uvs.AddRange(new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                });
                foreach (var index in faces)
                {
                    m_triangles[subMesh].Add(start + index);
                }
            }

            public void AddPrism(Vector3 center, int sides, float bottomRadius, float topRadius, float height, int subMesh)
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

                var bottomCenter = m_vertices.Count;
                m_vertices.Add(center + Vector3.down * halfHeight);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                var topCenter = m_vertices.Count;
                m_vertices.Add(center + Vector3.up * halfHeight);
                m_uvs.Add(new Vector2(0.5f, 0.5f));
                for (var index = 0; index < sides; index++)
                {
                    var next = (index + 1) % sides;
                    m_triangles[subMesh].AddRange(new[]
                    {
                        start + index * 2, start + next * 2, start + next * 2 + 1,
                        start + index * 2, start + next * 2 + 1, start + index * 2 + 1,
                        bottomCenter, start + next * 2, start + index * 2,
                        topCenter, start + index * 2 + 1, start + next * 2 + 1
                    });
                }
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
            private readonly List<List<int>> m_triangles = new();
        }
    }
}
