using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSpineHeroSetup
    {
        private const string SPINE_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineRegion.prefab";
        private const string TRENCH_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineDischargeTrenchRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineHeroAtlas.png";
        private const string SPINE_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CapacitorSpineHeroFinish.asset";
        private const string TRENCH_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/DischargeTrenchHeroFinish.asset";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/SpineHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/SpineHeroAlloy.mat",
            MATERIAL_FOLDER + "/SpineHeroCeramic.mat",
            MATERIAL_FOLDER + "/SpineHeroConductor.mat",
            MATERIAL_FOLDER + "/SpineHeroInsulator.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var spine = AssetDatabase.LoadAssetAtPath<GameObject>(SPINE_PREFAB_PATH);
                var trench = AssetDatabase.LoadAssetAtPath<GameObject>(TRENCH_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SPINE_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(TRENCH_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       _hasFinish(spine, 4) && _hasFinish(trench, 4);
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Spine Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _saveOrReplaceMesh(SPINE_MESH_PATH, _buildSpineMesh());
            _saveOrReplaceMesh(TRENCH_MESH_PATH, _buildTrenchMesh());
            _upgradeTrench(materials);
            _upgradeSpine(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Capacitor Spine hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Spine hero atlas is missing.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "SpineHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "Spine Hero Alloy", texture,
                    new Vector2(0f, 0.5f), 0.72f, 0.28f, new Color(0.48f, 0.5f, 0.52f)),
                _ensureMaterial(s_materialPaths[1], "Spine Hero Ceramic", texture,
                    new Vector2(0.5f, 0.5f), 0.06f, 0.42f, new Color(0.9f, 0.9f, 0.86f)),
                _ensureMaterial(s_materialPaths[2], "Spine Hero Conductor", texture,
                    Vector2.zero, 0.82f, 0.36f, new Color(0.72f, 0.52f, 0.34f)),
                _ensureMaterial(s_materialPaths[3], "Spine Hero Insulator", texture,
                    new Vector2(0.5f, 0f), 0.42f, 0.31f, new Color(0.5f, 0.54f, 0.56f))
            };
        }

        private static Material _ensureMaterial(
            string path,
            string materialName,
            Texture texture,
            Vector2 offset,
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
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Spine finish.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetTextureScale("_BaseMap", Vector2.one * 0.5f);
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", Color.black);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.DisableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh _buildSpineMesh()
        {
            var mesh = new MeshBuilder("CapacitorSpineHeroFinish", 4);

            // A restrained service rim gives the room one deliberate high-voltage deck boundary.
            mesh.AddBox(new Vector3(0f, -0.105f, 4.68f), new Vector3(11.8f, 0.08f, 0.18f), 0f, 0);
            mesh.AddBox(new Vector3(6.68f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 8.8f), 0f, 0);
            mesh.AddBox(new Vector3(-6.68f, -0.105f, 3.7f), new Vector3(0.18f, 0.08f, 1.7f), 0f, 0);
            mesh.AddBox(new Vector3(-6.68f, -0.105f, -3.7f), new Vector3(0.18f, 0.08f, 1.7f), 0f, 0);
            mesh.AddBox(new Vector3(0f, -0.105f, -4.68f), new Vector3(1.3f, 0.08f, 0.18f), 0f, 0);

            // Conductive feeds unify the transfer bank, tower berth, and venting threshold without adding blockers.
            mesh.AddBox(new Vector3(-1.55f, 0.015f, 1.42f), new Vector3(5.1f, 0.1f, 0.11f), 11f, 2);
            mesh.AddBox(new Vector3(-1.55f, 0.015f, -1.42f), new Vector3(5.1f, 0.1f, 0.11f), -11f, 2);
            mesh.AddBox(new Vector3(2.5f, 0.02f, 0f), new Vector3(4.1f, 0.11f, 0.11f), 0f, 2);
            mesh.AddBox(new Vector3(0f, 0.02f, -4.2f), new Vector3(2.1f, 0.11f, 0.11f), 0f, 2);

            // A low ceramic berth frame keeps the existing tower core and interaction side dominant.
            mesh.AddBox(new Vector3(5f, 0.11f, 1.22f), new Vector3(2.8f, 0.22f, 0.16f), 0f, 1);
            mesh.AddBox(new Vector3(5f, 0.11f, -1.22f), new Vector3(2.8f, 0.22f, 0.16f), 0f, 1);
            mesh.AddBox(new Vector3(3.62f, 0.11f, 0f), new Vector3(0.16f, 0.22f, 2.28f), 0f, 1);
            mesh.AddBox(new Vector3(6.38f, 0.11f, 0f), new Vector3(0.16f, 0.22f, 2.28f), 0f, 1);

            // Insulated threshold rails reinforce the powered return without replacing its state glyph.
            mesh.AddBox(new Vector3(-5.75f, 0.04f, -1.35f), new Vector3(1.55f, 0.12f, 0.15f), 0f, 3);
            mesh.AddBox(new Vector3(-5.75f, 0.04f, 1.35f), new Vector3(1.55f, 0.12f, 0.15f), 0f, 3);
            mesh.AddBox(new Vector3(-6.45f, 0.04f, 0f), new Vector3(0.15f, 0.12f, 2.55f), 0f, 3);
            return mesh.Build();
        }

        private static Mesh _buildTrenchMesh()
        {
            var mesh = new MeshBuilder("DischargeTrenchHeroFinish", 4);

            mesh.AddBox(new Vector3(0f, -0.105f, -2.78f), new Vector3(8.4f, 0.08f, 0.18f), 0f, 0);
            mesh.AddBox(new Vector3(-4.68f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 5.2f), 0f, 0);
            mesh.AddBox(new Vector3(4.68f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 5.2f), 0f, 0);

            mesh.AddBox(new Vector3(-3.1f, 0.08f, -1f), new Vector3(2.4f, 0.16f, 0.65f), 28f, 1);
            mesh.AddBox(new Vector3(3.1f, 0.08f, -1f), new Vector3(2.4f, 0.16f, 0.65f), -28f, 1);

            // The central conductor cradle visually owns pressure, while the existing coil remains authoritative cover.
            mesh.AddBox(new Vector3(0f, 0.1f, 1.48f), new Vector3(2.8f, 0.2f, 0.15f), 0f, 2);
            mesh.AddBox(new Vector3(0f, 0.1f, -1.48f), new Vector3(2.8f, 0.2f, 0.15f), 0f, 2);
            mesh.AddBox(new Vector3(-1.48f, 0.1f, 0f), new Vector3(0.15f, 0.2f, 2.8f), 0f, 2);
            mesh.AddBox(new Vector3(1.48f, 0.1f, 0f), new Vector3(0.15f, 0.2f, 2.8f), 0f, 2);

            // A low insulated control plinth leaves the prompt and pressure selector unobstructed.
            mesh.AddBox(new Vector3(0f, 0.035f, -2.08f), new Vector3(2.3f, 0.12f, 0.9f), 0f, 3);
            mesh.AddBox(new Vector3(-1.22f, 0.09f, -2.08f), new Vector3(0.14f, 0.24f, 1.05f), 0f, 1);
            mesh.AddBox(new Vector3(1.22f, 0.09f, -2.08f), new Vector3(0.14f, 0.24f, 1.05f), 0f, 1);

            mesh.AddBox(new Vector3(-3.8f, 0.015f, 0f), new Vector3(0.11f, 0.1f, 4.9f), 0f, 2);
            mesh.AddBox(new Vector3(3.8f, 0.015f, 0f), new Vector3(0.11f, 0.1f, 4.9f), 0f, 2);
            mesh.AddBox(new Vector3(-2.35f, 0.015f, 2.3f), new Vector3(2.8f, 0.1f, 0.11f), 0f, 2);
            mesh.AddBox(new Vector3(2.35f, 0.015f, 2.3f), new Vector3(2.8f, 0.1f, 0.11f), 0f, 2);
            return mesh.Build();
        }

        private static void _upgradeSpine(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(SPINE_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Capacitor Spine Deck", materials[3]);
                foreach (var name in new[]
                         {
                             "Capacitor Spine South West", "Capacitor Spine South Center", "Capacitor Spine South East",
                             "Capacitor Spine North West", "Capacitor Spine North Center", "Capacitor Spine North East",
                             "Capacitor Spine East Bulkhead"
                         })
                {
                    _assignMaterial(root.transform, name, materials[0]);
                }

                _assignMaterial(root.transform, "Capacitor Transfer Bank/Departure Capacitor Armor", materials[1]);
                _assignMaterial(root.transform, "Capacitor Transfer Bank/Departure Capacitor Cells", materials[2]);
                _assignMaterial(root.transform, "North Capacitor Shield/Departure Capacitor Armor", materials[1]);
                _assignMaterial(root.transform, "North Capacitor Shield/Departure Capacitor Cells", materials[2]);
                _assignMaterial(root.transform, "Third Tower Berth/Tower Base", materials[0]);
                _assignMaterial(root.transform, "Third Tower Berth/Tower Column", materials[1]);
                _ensureFinish(root, "Capacitor Spine Hero Finish", SPINE_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, SPINE_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeTrench(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(TRENCH_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Discharge Trench Deck", materials[3]);
                _assignMaterial(root.transform, "Discharge Trench East Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Discharge Trench West Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Discharge Trench South Bulkhead", materials[0]);
                _assignMaterial(root.transform, "West Ceramic Baffle", materials[1]);
                _assignMaterial(root.transform, "East Ceramic Baffle", materials[1]);
                _assignMaterial(root.transform, "Central Discharge Coil", materials[2]);
                _ensureFinish(root, "Discharge Trench Hero Finish", TRENCH_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, TRENCH_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _ensureFinish(GameObject root, string objectName, string meshPath, Material[] materials)
        {
            var part = root.transform.Find(objectName);
            if (part == null)
            {
                part = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                part.SetParent(root.transform, false);
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

            var finish = root.GetComponent<AuthoredSpineHeroFinish>() ?? root.AddComponent<AuthoredSpineHeroFinish>();
            finish.Configure(renderer);
        }

        private static void _assignMaterial(Transform root, string childPath, Material material)
        {
            var child = root.Find(childPath);
            if (child == null || material == null || !child.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not finish the Spine renderer {childPath}.");
            }

            renderer.sharedMaterial = material;
        }

        private static bool _hasFinish(GameObject prefab, int materialCount)
        {
            var finish = prefab != null ? prefab.GetComponent<AuthoredSpineHeroFinish>() : null;
            return finish != null && finish.IsConfigured &&
                   finish.FinishRenderer.sharedMaterials.Length == materialCount &&
                   finish.GetComponentsInChildren<Collider>(true).Length == 0;
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
