using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalInductionFluxHeroSetup
    {
        private const string INDUCTION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SpineInductionGalleryRegion.prefab";
        private const string FLUX_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/FluxBypassRegion.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/InductionFluxHeroAtlas.png";
        private const string INDUCTION_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/InductionGalleryHeroFinish.asset";
        private const string FLUX_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/FluxBypassHeroFinish.asset";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/InductionFluxHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/InductionFluxAlloy.mat",
            MATERIAL_FOLDER + "/InductionFluxCeramic.mat",
            MATERIAL_FOLDER + "/InductionFluxConductor.mat",
            MATERIAL_FOLDER + "/InductionFluxDeck.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var induction = AssetDatabase.LoadAssetAtPath<GameObject>(INDUCTION_PREFAB_PATH);
                var flux = AssetDatabase.LoadAssetAtPath<GameObject>(FLUX_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(INDUCTION_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(FLUX_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       _hasFinish(induction, "Induction Gallery Hero Finish") &&
                       _hasFinish(flux, "Flux Bypass Hero Finish");
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Induction and Flux Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _saveOrReplaceMesh(INDUCTION_MESH_PATH, _buildInductionMesh());
            _saveOrReplaceMesh(FLUX_MESH_PATH, _buildFluxMesh());
            _upgradeInduction(materials);
            _upgradeFlux(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Induction Gallery and Flux Bypass hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Induction/Flux hero atlas is missing.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "InductionFluxHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "InductionFluxAlloy", texture,
                    Vector2.up * 0.5f, 0.74f, 0.3f, new Color(0.47f, 0.49f, 0.5f)),
                _ensureMaterial(s_materialPaths[1], "InductionFluxCeramic", texture,
                    new Vector2(0.5f, 0.5f), 0.05f, 0.4f, new Color(0.9f, 0.89f, 0.84f)),
                _ensureMaterial(s_materialPaths[2], "InductionFluxConductor", texture,
                    Vector2.zero, 0.8f, 0.34f, new Color(0.66f, 0.42f, 0.25f)),
                _ensureMaterial(s_materialPaths[3], "InductionFluxDeck", texture,
                    Vector2.right * 0.5f, 0.45f, 0.25f, new Color(0.42f, 0.45f, 0.47f))
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
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Induction/Flux finish.");
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

        private static Mesh _buildInductionMesh()
        {
            var mesh = new MeshBuilder("InductionGalleryHeroFinish", 4);
            var center = new Vector3(0f, 0.04f, 1.55f);

            // A segmented radial cradle makes charging read outward from the lattice without covering its glyph.
            for (var index = 0; index < 12; index++)
            {
                var angle = index * 30f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                mesh.AddBox(center + direction * 1.62f, new Vector3(0.68f, 0.12f, 0.16f), angle, 2);
            }

            mesh.AddBox(new Vector3(-2.65f, 0.06f, 1.55f), new Vector3(1.85f, 0.16f, 0.18f), 0f, 1);
            mesh.AddBox(new Vector3(2.65f, 0.06f, 1.55f), new Vector3(1.85f, 0.16f, 0.18f), 0f, 1);
            mesh.AddBox(new Vector3(0f, 0.06f, -0.85f), new Vector3(0.18f, 0.16f, 2.55f), 0f, 2);
            mesh.AddBox(new Vector3(-4.55f, 0.015f, 0.25f), new Vector3(0.12f, 0.1f, 4.55f), 0f, 2);
            mesh.AddBox(new Vector3(4.55f, 0.015f, 0.25f), new Vector3(0.12f, 0.1f, 4.55f), 0f, 2);

            mesh.AddBox(new Vector3(0f, -0.105f, -3.3f), new Vector3(10.7f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(-6.65f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 6.5f), 0f, 0);
            mesh.AddBox(new Vector3(6.65f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 6.5f), 0f, 0);
            return mesh.Build();
        }

        private static Mesh _buildFluxMesh()
        {
            var mesh = new MeshBuilder("FluxBypassHeroFinish", 4);

            // Parallel feeds and offset chevrons make the shunt read as directional rerouting rather than radial charge.
            mesh.AddBox(new Vector3(-2.45f, 0.02f, 0f), new Vector3(0.13f, 0.11f, 9.6f), 0f, 2);
            mesh.AddBox(new Vector3(-1.9f, 0.02f, 0f), new Vector3(0.13f, 0.11f, 8.7f), 0f, 2);
            mesh.AddBox(new Vector3(0.35f, 0.02f, -3.15f), new Vector3(4.8f, 0.11f, 0.13f), 0f, 2);
            mesh.AddBox(new Vector3(0.35f, 0.02f, 2.7f), new Vector3(4.8f, 0.11f, 0.13f), 0f, 2);
            mesh.AddBox(new Vector3(-0.45f, 0.08f, -1.1f), new Vector3(2.2f, 0.16f, 0.18f), 28f, 2);
            mesh.AddBox(new Vector3(-0.45f, 0.08f, 2.55f), new Vector3(2.2f, 0.16f, 0.18f), -28f, 2);

            mesh.AddBox(new Vector3(-0.25f, 0.08f, -0.55f), new Vector3(2.7f, 0.18f, 0.16f), 0f, 1);
            mesh.AddBox(new Vector3(-0.25f, 0.08f, 2.05f), new Vector3(2.7f, 0.18f, 0.16f), 0f, 1);
            mesh.AddBox(new Vector3(-1.58f, 0.08f, 0.75f), new Vector3(0.16f, 0.18f, 2.5f), 0f, 1);
            mesh.AddBox(new Vector3(1.08f, 0.08f, 0.75f), new Vector3(0.16f, 0.18f, 2.5f), 0f, 1);

            mesh.AddBox(new Vector3(0f, -0.105f, -5.55f), new Vector3(6.5f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(0f, -0.105f, 5.55f), new Vector3(6.5f, 0.08f, 0.18f), 0f, 3);
            mesh.AddBox(new Vector3(-3.35f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 11f), 0f, 0);
            return mesh.Build();
        }

        private static void _upgradeInduction(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(INDUCTION_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Induction Gallery Deck", materials[3]);
                _assignMaterial(root.transform, "Induction Coil/Departure Capacitor Armor", materials[1]);
                _assignMaterial(root.transform, "Induction Coil/Departure Capacitor Cells", materials[2]);
                _assignMaterial(root.transform, "West Deflection Baffle", materials[0]);
                _assignMaterial(root.transform, "East Deflection Baffle", materials[0]);
                _ensureFinish(root, "Induction Gallery Hero Finish", INDUCTION_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, INDUCTION_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeFlux(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(FLUX_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Flux Bypass Deck", materials[3]);
                _assignLandmarkMaterials(root.transform, "Flux Shunt Regulator", materials);
                _assignMaterial(root.transform, "South Flux Deflector", materials[0]);
                _assignMaterial(root.transform, "North Flux Deflector", materials[0]);
                _ensureFinish(root, "Flux Bypass Hero Finish", FLUX_MESH_PATH, materials);
                PrefabUtility.SaveAsPrefabAsset(root, FLUX_PREFAB_PATH);
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

            var finish = root.GetComponent<AuthoredInductionFluxHeroFinish>() ??
                         root.AddComponent<AuthoredInductionFluxHeroFinish>();
            finish.Configure(renderer);
        }

        private static void _assignMaterial(Transform root, string childPath, Material material)
        {
            var child = root.Find(childPath);
            if (child == null || material == null || !child.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not finish the Induction/Flux renderer {childPath}.");
            }

            renderer.sharedMaterial = material;
        }

        private static void _assignLandmarkMaterials(Transform root, string childPath, Material[] materials)
        {
            var child = root.Find(childPath);
            var renderers = child != null ? child.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"Could not finish the Induction/Flux landmark {childPath}.");
            }

            foreach (var renderer in renderers)
            {
                var name = renderer.gameObject.name;
                renderer.sharedMaterial = name.Contains("Insulator", StringComparison.OrdinalIgnoreCase)
                    ? materials[1]
                    : name.Contains("Coil", StringComparison.OrdinalIgnoreCase) ||
                      name.Contains("Bus", StringComparison.OrdinalIgnoreCase)
                        ? materials[2]
                        : materials[0];
            }
        }

        private static bool _hasFinish(GameObject prefab, string objectName)
        {
            var finish = prefab != null ? prefab.GetComponent<AuthoredInductionFluxHeroFinish>() : null;
            return finish != null && finish.IsConfigured && finish.transform.Find(objectName) != null &&
                   finish.FinishRenderer.sharedMaterials.Length == 4 &&
                   finish.FinishRenderer.GetComponents<Collider>().Length == 0;
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
