using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalRelayFoundryHeroSetup
    {
        private const string PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryTurbineAlbedo.png";
        private const string STRUCTURE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryHeroStructure.asset";
        private const string POWER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayFoundryHeroPower.asset";
        private const string POWER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayNetworkStatus.mat";
        private const string AMBER_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayFoundryAmber.mat";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/RelayFoundryHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/RelayFoundryHeroDeck.mat",
            MATERIAL_FOLDER + "/RelayFoundryHeroArmor.mat",
            MATERIAL_FOLDER + "/RelayFoundryHeroCeramic.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var finish = prefab != null ? prefab.GetComponent<AuthoredRelayFoundryHeroFinish>() : null;
                return AssetDatabase.LoadAssetAtPath<Mesh>(STRUCTURE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(POWER_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       AssetDatabase.LoadAssetAtPath<Material>(POWER_MATERIAL_PATH) != null &&
                       finish != null && finish.IsConfigured &&
                       finish.StructureRenderer.sharedMaterials.Length == 3 &&
                       finish.PowerRenderer.sharedMaterials.Length == 1 &&
                       finish.GetComponentsInChildren<Collider>(true).Length == 0 &&
                       _hasFocalMachineryFinish(prefab);
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Relay Foundry Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _ensureMeshes();
            _upgradePrefab(materials);
            DeadSignalActTwoReadabilitySetup.EnsureAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Relay Foundry hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Relay Foundry hero atlas is missing.");
            }

            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "RelayFoundryHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "Relay Foundry Hero Deck", texture,
                    new Vector2(0.03f, 0.34f), new Vector2(0.42f, 0.2f), 0.5f, 0.22f,
                    new Color(0.52f, 0.57f, 0.6f)),
                _ensureMaterial(s_materialPaths[1], "Relay Foundry Hero Armor", texture,
                    new Vector2(0.02f, 0.54f), new Vector2(0.44f, 0.42f), 0.72f, 0.34f,
                    new Color(0.36f, 0.4f, 0.43f)),
                _ensureMaterial(s_materialPaths[2], "Relay Foundry Hero Ceramic", texture,
                    new Vector2(0.55f, 0.55f), new Vector2(0.4f, 0.4f), 0.12f, 0.3f,
                    new Color(0.86f, 0.88f, 0.86f))
            };
        }

        private static Material _ensureMaterial(
            string path,
            string materialName,
            Texture texture,
            Vector2 offset,
            Vector2 scale,
            float metallic,
            float smoothness,
            Color baseColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Relay Foundry finish.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetTextureScale("_BaseMap", scale);
            material.SetColor("_BaseColor", baseColor);
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
            var structure = new MeshBuilder("RelayFoundryHeroStructure", 3);
            structure.AddBox(new Vector3(-0.5f, -0.105f, -2.7f), new Vector3(5.8f, 0.08f, 0.18f), 0f, 0);
            structure.AddBox(new Vector3(-0.5f, -0.105f, 2.7f), new Vector3(5.8f, 0.08f, 0.18f), 0f, 0);
            structure.AddBox(new Vector3(4f, -0.1f, -1.82f), new Vector3(0.22f, 0.09f, 2.35f), 0f, 0);
            structure.AddBox(new Vector3(-3.16f, 0.01f, 0f), new Vector3(0.2f, 0.24f, 4.8f), 0f, 1);
            structure.AddBox(new Vector3(2.16f, 0.01f, 0f), new Vector3(0.2f, 0.24f, 4.8f), 0f, 1);
            structure.AddBox(new Vector3(4.25f, 0.02f, -1.67f), new Vector3(3.4f, 0.22f, 0.18f), 0f, 1);
            structure.AddBox(new Vector3(4.25f, 0.02f, 1.67f), new Vector3(3.4f, 0.22f, 0.18f), 0f, 1);
            structure.AddBox(new Vector3(7.82f, 0.02f, 0f), new Vector3(0.28f, 0.28f, 3.1f), 0f, 1);
            structure.AddBox(new Vector3(7.06f, 0.03f, 5.82f), new Vector3(1.42f, 0.3f, 0.22f), 0f, 2);
            structure.AddBox(new Vector3(7.06f, 0.03f, -5.82f), new Vector3(1.42f, 0.3f, 0.22f), 0f, 2);
            structure.AddBox(new Vector3(2.55f, 0.04f, 0f), new Vector3(0.18f, 0.32f, 2.6f), 0f, 2);
            structure.AddBox(new Vector3(5.95f, 0.04f, 0f), new Vector3(0.18f, 0.32f, 2.6f), 0f, 2);
            _saveOrReplaceMesh(STRUCTURE_MESH_PATH, structure.Build());

            var power = new MeshBuilder("RelayFoundryHeroPower", 1);
            power.AddBox(new Vector3(1.45f, -0.045f, -0.98f), new Vector3(2.2f, 0.045f, 0.1f), 0f, 0);
            power.AddBox(new Vector3(1.45f, -0.045f, 0f), new Vector3(2.2f, 0.045f, 0.1f), 0f, 0);
            power.AddBox(new Vector3(1.45f, -0.045f, 0.98f), new Vector3(2.2f, 0.045f, 0.1f), 0f, 0);
            power.AddBox(new Vector3(4.25f, 0.145f, -1.57f), new Vector3(2.8f, 0.04f, 0.07f), 0f, 0);
            power.AddBox(new Vector3(4.25f, 0.145f, 1.57f), new Vector3(2.8f, 0.04f, 0.07f), 0f, 0);
            power.AddBox(new Vector3(7.66f, 0.17f, 0f), new Vector3(0.045f, 0.04f, 2.7f), 0f, 0);
            _saveOrReplaceMesh(POWER_MESH_PATH, power.Build());
        }

        private static void _upgradePrefab(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                var structure = _ensurePart(root.transform, "Relay Foundry Hero Structure", STRUCTURE_MESH_PATH,
                    materials);
                var power = _ensurePart(root.transform, "Relay Foundry Hero Power", POWER_MESH_PATH,
                    new[] { AssetDatabase.LoadAssetAtPath<Material>(POWER_MATERIAL_PATH) });
                var finish = root.GetComponent<AuthoredRelayFoundryHeroFinish>() ??
                             root.AddComponent<AuthoredRelayFoundryHeroFinish>();
                finish.Configure(structure, power);
                _refinishFocalMachinery(root.transform, materials);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static MeshRenderer _ensurePart(Transform parent, string name, string meshPath, Material[] materials)
        {
            var part = parent.Find(name);
            if (part == null)
            {
                part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                part.SetParent(parent, false);
            }

            part.localPosition = Vector3.zero;
            part.localRotation = Quaternion.identity;
            part.localScale = Vector3.one;
            part.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            foreach (var collider in part.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return renderer;
        }

        private static void _refinishFocalMachinery(Transform root, Material[] materials)
        {
            var turbine = root.Find("Relay Induction Turbine");
            var tower = root.Find("Relay Tower Assembly");
            var power = AssetDatabase.LoadAssetAtPath<Material>(POWER_MATERIAL_PATH);
            var amber = AssetDatabase.LoadAssetAtPath<Material>(AMBER_MATERIAL_PATH);
            if (turbine == null || tower == null || power == null || amber == null)
            {
                throw new InvalidOperationException("The Relay Foundry focal machinery is incomplete.");
            }

            foreach (var renderer in turbine.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = renderer.name.Contains("Ceramic", StringComparison.OrdinalIgnoreCase)
                    ? materials[2]
                    : renderer.name.Contains("Rotor", StringComparison.OrdinalIgnoreCase)
                        ? power
                        : renderer.name.Contains("Crown", StringComparison.OrdinalIgnoreCase)
                            ? amber
                            : materials[1];
            }

            tower.Find("Tower Base").GetComponent<Renderer>().sharedMaterial = materials[1];
            tower.Find("Tower Column").GetComponent<Renderer>().sharedMaterial = materials[2];
        }

        private static bool _hasFocalMachineryFinish(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            var turbine = prefab.transform.Find("Relay Induction Turbine");
            var tower = prefab.transform.Find("Relay Tower Assembly");
            if (turbine == null || tower == null)
            {
                return false;
            }

            var renderers = turbine.GetComponentsInChildren<Renderer>(true);
            return renderers.Length == 6 &&
                   renderers.Any(renderer => renderer.name.Contains("Rotor", StringComparison.OrdinalIgnoreCase) &&
                                             AssetDatabase.GetAssetPath(renderer.sharedMaterial) == POWER_MATERIAL_PATH) &&
                   AssetDatabase.GetAssetPath(tower.Find("Tower Base").GetComponent<Renderer>().sharedMaterial) ==
                   s_materialPaths[1] &&
                   AssetDatabase.GetAssetPath(tower.Find("Tower Column").GetComponent<Renderer>().sharedMaterial) ==
                   s_materialPaths[2];
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
