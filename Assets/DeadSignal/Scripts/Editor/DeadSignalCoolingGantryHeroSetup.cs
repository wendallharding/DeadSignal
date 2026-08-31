using System;
using System.Collections.Generic;
using System.Linq;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalCoolingGantryHeroSetup
    {
        private const string REGION_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayCoolingGantryRegion.prefab";
        private const string LANDMARK_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/RelayHeatExchanger.prefab";
        private const string TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/CoolingGantryHeroAtlas.png";
        private const string MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/CoolingGantryHeroFinish.asset";
        private const string STATUS_MATERIAL_PATH =
            "Assets/DeadSignal/Resources/Materials/RelayFoundry/RelayNetworkStatus.mat";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/CoolingGantryHeroFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/CoolingGantryHeroDeck.mat",
            MATERIAL_FOLDER + "/CoolingGantryHeroCeramic.mat",
            MATERIAL_FOLDER + "/CoolingGantryHeroCopper.mat",
            MATERIAL_FOLDER + "/CoolingGantryHeroVent.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var region = AssetDatabase.LoadAssetAtPath<GameObject>(REGION_PREFAB_PATH);
                var landmark = AssetDatabase.LoadAssetAtPath<GameObject>(LANDMARK_PREFAB_PATH);
                var finish = region != null ? region.GetComponent<AuthoredCoolingGantryHeroFinish>() : null;
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       finish != null && finish.IsConfigured &&
                       finish.FinishRenderer.sharedMaterials.Length == 4 &&
                       finish.GetComponentsInChildren<Collider>(true).Length == 0 &&
                       _hasLandmarkFinish(landmark);
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Cooling Gantry Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture();
            _ensureMaterialFolder();
            var materials = _ensureMaterials();
            _ensureMesh();
            _refinishLandmark(materials);
            _upgradeRegion(materials);
            DeadSignalActTwoReadabilitySetup.EnsureAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The Cooling Gantry hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("The Cooling Gantry hero atlas is missing.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "CoolingGantryHeroFinish");
            }
        }

        private static Material[] _ensureMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "Cooling Gantry Hero Deck", texture,
                    new Vector2(0f, 0.5f), 0.7f, 0.25f, new Color(0.48f, 0.52f, 0.55f), Color.black),
                _ensureMaterial(s_materialPaths[1], "Cooling Gantry Hero Ceramic", texture,
                    new Vector2(0.5f, 0.5f), 0.08f, 0.38f, new Color(0.9f, 0.92f, 0.9f), Color.black),
                _ensureMaterial(s_materialPaths[2], "Cooling Gantry Hero Copper", texture,
                    Vector2.zero, 0.75f, 0.3f, new Color(0.68f, 0.5f, 0.34f), Color.black),
                _ensureMaterial(s_materialPaths[3], "Cooling Gantry Hero Vent", texture,
                    new Vector2(0.5f, 0f), 0.55f, 0.34f, new Color(0.48f, 0.66f, 0.72f),
                    new Color(0.02f, 0.12f, 0.16f))
            };
        }

        private static Material _ensureMaterial(
            string path,
            string materialName,
            Texture texture,
            Vector2 offset,
            float metallic,
            float smoothness,
            Color baseColor,
            Color emissionColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for the Cooling Gantry finish.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetTextureScale("_BaseMap", Vector2.one * 0.5f);
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emissionColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emissionColor.maxColorComponent > 0f)
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

        private static void _ensureMesh()
        {
            var mesh = new MeshBuilder("CoolingGantryHeroFinish", 4);

            // Deck borders and two approach guides make the return route readable without narrowing it.
            mesh.AddBox(new Vector3(-5.48f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 7.4f), 0f, 0);
            mesh.AddBox(new Vector3(5.48f, -0.105f, 0f), new Vector3(0.18f, 0.08f, 7.4f), 0f, 0);
            mesh.AddBox(new Vector3(0f, -0.105f, -3.78f), new Vector3(10.8f, 0.08f, 0.18f), 0f, 0);
            mesh.AddBox(new Vector3(-4.25f, -0.1f, 3.78f), new Vector3(2.3f, 0.08f, 0.18f), 0f, 0);
            mesh.AddBox(new Vector3(4.25f, -0.1f, 3.78f), new Vector3(2.3f, 0.08f, 0.18f), 0f, 0);

            // A low processing bed and ceramic guards frame the existing payload socket and prompt.
            mesh.AddBox(new Vector3(3.75f, -0.02f, -2.55f), new Vector3(2.15f, 0.18f, 1.65f), 0f, 3);
            mesh.AddBox(new Vector3(2.64f, 0.15f, -2.55f), new Vector3(0.16f, 0.42f, 1.82f), 0f, 1);
            mesh.AddBox(new Vector3(4.86f, 0.15f, -2.55f), new Vector3(0.16f, 0.42f, 1.82f), 0f, 1);
            mesh.AddBox(new Vector3(3.75f, 0.12f, -3.43f), new Vector3(2.38f, 0.34f, 0.14f), 0f, 1);

            // Copper feeds describe processing flow from the exchanger to the bed.
            mesh.AddBox(new Vector3(1.88f, 0.03f, -0.42f), new Vector3(0.12f, 0.12f, 3.85f), -31f, 2);
            mesh.AddBox(new Vector3(3.08f, 0.035f, -1.72f), new Vector3(2.6f, 0.12f, 0.12f), 0f, 2);
            mesh.AddBox(new Vector3(-4.95f, 0.02f, 0f), new Vector3(0.12f, 0.1f, 6.5f), 0f, 2);
            mesh.AddBox(new Vector3(4.95f, 0.02f, 0.45f), new Vector3(0.12f, 0.1f, 5.2f), 0f, 2);

            // Cold vent banks and guard rails distinguish stabilization from Foundry installation.
            mesh.AddBox(new Vector3(-3.6f, 0.04f, -3.58f), new Vector3(2.1f, 0.14f, 0.32f), 0f, 3);
            mesh.AddBox(new Vector3(-1.15f, 0.04f, -3.58f), new Vector3(2.1f, 0.14f, 0.32f), 0f, 3);
            mesh.AddBox(new Vector3(1.3f, 0.04f, -3.58f), new Vector3(2.1f, 0.14f, 0.32f), 0f, 3);
            mesh.AddBox(new Vector3(-1.55f, 0.18f, 2.35f), new Vector3(2.35f, 0.16f, 0.14f), 31f, 1);
            mesh.AddBox(new Vector3(1.55f, 0.18f, -1.95f), new Vector3(2.35f, 0.16f, 0.14f), -31f, 1);

            _saveOrReplaceMesh(MESH_PATH, mesh.Build());
        }

        private static void _refinishLandmark(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(LANDMARK_PREFAB_PATH);
            try
            {
                var statusMaterial = AssetDatabase.LoadAssetAtPath<Material>(STATUS_MATERIAL_PATH);
                _assignMaterial(root.transform, "Exchanger armored plinth", materials[0]);
                _assignMaterial(root.transform, "Exchanger ceramic spine", materials[1]);
                _assignMaterial(root.transform, "West coolant coil", statusMaterial);
                _assignMaterial(root.transform, "East coolant coil", statusMaterial);
                _assignMaterial(root.transform, "South copper manifold", materials[2]);
                _assignMaterial(root.transform, "North ceramic manifold", materials[1]);
                PrefabUtility.SaveAsPrefabAsset(root, LANDMARK_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _upgradeRegion(Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(REGION_PREFAB_PATH);
            try
            {
                _assignMaterial(root.transform, "Cooling Gantry Deck", materials[0]);
                _assignMaterial(root.transform, "Cooling Gantry South Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Cooling Gantry West Bulkhead", materials[0]);
                _assignMaterial(root.transform, "Cooling Gantry East Bulkhead", materials[0]);
                _assignMaterial(root.transform, "West Ceramic Deflector", materials[1]);
                _assignMaterial(root.transform, "East Copper Deflector", materials[2]);

                var part = root.transform.Find("Cooling Gantry Hero Finish");
                if (part == null)
                {
                    part = new GameObject("Cooling Gantry Hero Finish", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    part.SetParent(root.transform, false);
                }

                part.localPosition = Vector3.zero;
                part.localRotation = Quaternion.identity;
                part.localScale = Vector3.one;
                part.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
                var renderer = part.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                foreach (var collider in part.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                var finish = root.GetComponent<AuthoredCoolingGantryHeroFinish>() ??
                             root.AddComponent<AuthoredCoolingGantryHeroFinish>();
                finish.Configure(renderer);
                PrefabUtility.SaveAsPrefabAsset(root, REGION_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void _assignMaterial(Transform root, string childName, Material material)
        {
            var child = root.Find(childName);
            if (child == null || material == null || !child.TryGetComponent<Renderer>(out var renderer))
            {
                throw new InvalidOperationException($"Could not finish the Cooling Gantry renderer {childName}.");
            }

            renderer.sharedMaterial = material;
        }

        private static bool _hasLandmarkFinish(GameObject landmark)
        {
            if (landmark == null)
            {
                return false;
            }

            var renderers = landmark.GetComponentsInChildren<Renderer>(true);
            return renderers.Length == 6 &&
                   renderers.Count(renderer => renderer.name.Contains("coolant coil", StringComparison.OrdinalIgnoreCase) &&
                                               AssetDatabase.GetAssetPath(renderer.sharedMaterial) ==
                                               STATUS_MATERIAL_PATH) == 2 &&
                   AssetDatabase.GetAssetPath(landmark.transform.Find("Exchanger armored plinth")
                       .GetComponent<Renderer>().sharedMaterial) == s_materialPaths[0] &&
                   AssetDatabase.GetAssetPath(landmark.transform.Find("South copper manifold")
                       .GetComponent<Renderer>().sharedMaterial) == s_materialPaths[2];
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
