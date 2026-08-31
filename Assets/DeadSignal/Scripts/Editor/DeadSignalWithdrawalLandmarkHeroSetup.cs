using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalWithdrawalLandmarkHeroSetup
    {
        private const string WARDEN_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/WardenBayAlbedo.png";
        private const string SAPPER_TEXTURE_PATH =
            "Assets/DeadSignal/Resources/Environment/SapperCradleAlbedo.png";
        private const string WARDEN_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/WardenBayHeroFinish.asset";
        private const string SAPPER_MESH_PATH =
            "Assets/DeadSignal/Resources/Environment/SapperCradleHeroFinish.asset";
        private const string WARDEN_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/WardenStagingBay.prefab";
        private const string SAPPER_PREFAB_PATH =
            "Assets/DeadSignal/Resources/Environment/SignalSapperCradle.prefab";
        private const string MATERIAL_FOLDER =
            "Assets/DeadSignal/Resources/Materials/WithdrawalLandmarkFinish";

        private static readonly string[] s_materialPaths =
        {
            MATERIAL_FOLDER + "/WithdrawalDeck.mat",
            MATERIAL_FOLDER + "/WardenContainmentArmor.mat",
            MATERIAL_FOLDER + "/WardenContainmentCeramic.mat",
            MATERIAL_FOLDER + "/WardenContainmentHazard.mat",
            MATERIAL_FOLDER + "/SapperCradleArmorFinish.mat",
            MATERIAL_FOLDER + "/SapperCradleCeramicFinish.mat",
            MATERIAL_FOLDER + "/SapperCradleConduit.mat"
        };

        public static bool HasAssets
        {
            get
            {
                var warden = AssetDatabase.LoadAssetAtPath<GameObject>(WARDEN_PREFAB_PATH);
                var sapper = AssetDatabase.LoadAssetAtPath<GameObject>(SAPPER_PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Mesh>(WARDEN_MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(SAPPER_MESH_PATH) != null &&
                       s_materialPaths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null) &&
                       _hasFinish(warden, "Warden Bay Hero Finish", 4) &&
                       _hasFinish(sapper, "Sapper Cradle Hero Finish", 4);
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Withdrawal Landmark Hero Finish")]
        public static void EnsureAssets()
        {
            _configureTexture(WARDEN_TEXTURE_PATH);
            _configureTexture(SAPPER_TEXTURE_PATH);
            _ensureMaterialFolder();
            var wardenMaterials = _ensureWardenMaterials();
            var sapperMaterials = _ensureSapperMaterials();
            _ensureMeshes();
            _upgradePrefab(WARDEN_PREFAB_PATH, "Warden Bay Hero Finish", WARDEN_MESH_PATH, wardenMaterials);
            _upgradePrefab(SAPPER_PREFAB_PATH, "Sapper Cradle Hero Finish", SAPPER_MESH_PATH, sapperMaterials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("The withdrawal-landmark hero-finish assets are incomplete.");
            }
        }

        private static void _configureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the withdrawal-landmark texture at {path}.");
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
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "WithdrawalLandmarkFinish");
            }
        }

        private static Material[] _ensureWardenMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(WARDEN_TEXTURE_PATH);
            return new[]
            {
                _ensureMaterial(s_materialPaths[0], "Withdrawal Deck", texture, new Vector2(0.04f, 0.05f),
                    new Vector2(0.42f, 0.42f), 0.5f, 0.2f, Color.white, Color.black),
                _ensureMaterial(s_materialPaths[1], "Warden Containment Armor", texture, new Vector2(0.02f, 0.5f),
                    new Vector2(0.46f, 0.46f), 0.68f, 0.34f, Color.white, Color.black),
                _ensureMaterial(s_materialPaths[2], "Warden Containment Ceramic", texture, new Vector2(0.53f, 0.54f),
                    new Vector2(0.34f, 0.34f), 0.12f, 0.28f, Color.white, Color.black),
                _ensureMaterial(s_materialPaths[3], "Warden Containment Hazard", null, Vector2.zero,
                    Vector2.one, 0.18f, 0.44f, new Color(0.38f, 0.025f, 0.03f), new Color(0.2f, 0.004f, 0.006f))
            };
        }

        private static Material[] _ensureSapperMaterials()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SAPPER_TEXTURE_PATH);
            return new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>(s_materialPaths[0]),
                _ensureMaterial(s_materialPaths[4], "Sapper Cradle Armor Finish", texture,
                    new Vector2(0.02f, 0.52f), new Vector2(0.44f, 0.44f), 0.55f, 0.3f, Color.white, Color.black),
                _ensureMaterial(s_materialPaths[5], "Sapper Cradle Ceramic Finish", texture,
                    new Vector2(0.47f, 0.48f), new Vector2(0.3f, 0.42f), 0.1f, 0.3f, Color.white, Color.black),
                _ensureMaterial(s_materialPaths[6], "Sapper Cradle Conduit", null, Vector2.zero,
                    Vector2.one, 0.12f, 0.5f, new Color(0.32f, 0.025f, 0.18f), new Color(0.18f, 0.005f, 0.09f))
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
            Color baseColor,
            Color emission)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for withdrawal landmarks.");
                }

                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetTextureScale("_BaseMap", scale);
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
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

        private static void _ensureMeshes()
        {
            var warden = new MeshBuilder("WardenBayHeroFinish", 4);
            warden.AddBox(new Vector3(0f, 0.03f, 0f), new Vector3(5.6f, 0.06f, 4.8f), 0f, 0);
            warden.AddBox(new Vector3(-2.66f, 0.1f, 0f), new Vector3(0.18f, 0.2f, 3.5f), 0f, 1);
            warden.AddBox(new Vector3(2.66f, 0.1f, 0f), new Vector3(0.18f, 0.2f, 3.5f), 0f, 1);
            warden.AddBox(new Vector3(0f, 0.08f, -2.24f), new Vector3(3.8f, 0.16f, 0.18f), 0f, 1);
            warden.AddBox(new Vector3(0f, 0.075f, 0f), new Vector3(1.7f, 0.15f, 1.45f), 0f, 2);
            warden.AddBox(new Vector3(-1.02f, 0.1f, 0f), new Vector3(0.18f, 0.2f, 1.7f), 0f, 2);
            warden.AddBox(new Vector3(1.02f, 0.1f, 0f), new Vector3(0.18f, 0.2f, 1.7f), 0f, 2);
            warden.AddBox(new Vector3(0f, 0.16f, -0.79f), new Vector3(1.8f, 0.04f, 0.1f), 0f, 3);
            warden.AddBox(new Vector3(0f, 0.16f, 0.79f), new Vector3(1.8f, 0.04f, 0.1f), 0f, 3);
            warden.AddBox(new Vector3(-1.32f, 0.065f, 1.7f), new Vector3(0.12f, 0.07f, 1.0f), -22f, 3);
            warden.AddBox(new Vector3(1.32f, 0.065f, 1.7f), new Vector3(0.12f, 0.07f, 1.0f), 22f, 3);
            _saveOrReplaceMesh(WARDEN_MESH_PATH, warden.Build());

            var sapper = new MeshBuilder("SapperCradleHeroFinish", 4);
            sapper.AddBox(new Vector3(0f, 0.03f, 0f), new Vector3(5.2f, 0.06f, 4.8f), 0f, 0);
            sapper.AddBox(new Vector3(2.42f, 0.1f, 0f), new Vector3(0.18f, 0.2f, 3.4f), 0f, 1);
            sapper.AddBox(new Vector3(0f, 0.1f, -2.22f), new Vector3(3.8f, 0.2f, 0.18f), 0f, 1);
            sapper.AddBox(new Vector3(0.82f, 0.085f, 0.55f), new Vector3(1.35f, 0.17f, 0.18f), -28f, 2);
            sapper.AddBox(new Vector3(0.82f, 0.085f, -0.55f), new Vector3(1.35f, 0.17f, 0.18f), 28f, 2);
            sapper.AddBox(new Vector3(-0.15f, 0.075f, 0f), new Vector3(0.18f, 0.15f, 1.35f), 0f, 2);
            sapper.AddBox(new Vector3(0.7f, 0.115f, 0f), new Vector3(0.38f, 0.23f, 0.38f), 0f, 2);
            sapper.AddBox(new Vector3(-1.45f, 0.065f, 0f), new Vector3(1.7f, 0.07f, 0.1f), 0f, 3);
            sapper.AddBox(new Vector3(0f, 0.065f, 1.48f), new Vector3(0.1f, 0.07f, 1.6f), 0f, 3);
            sapper.AddBox(new Vector3(0.7f, 0.16f, 0f), new Vector3(0.22f, 0.04f, 0.22f), 0f, 3);
            _saveOrReplaceMesh(SAPPER_MESH_PATH, sapper.Build());
        }

        private static void _upgradePrefab(
            string prefabPath,
            string objectName,
            string meshPath,
            Material[] materials)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var finish = root.transform.Find(objectName);
                if (finish == null)
                {
                    var finishObject = new GameObject(objectName);
                    finishObject.transform.SetParent(root.transform, false);
                    finishObject.AddComponent<MeshFilter>();
                    finishObject.AddComponent<MeshRenderer>();
                    finish = finishObject.transform;
                }

                finish.localPosition = Vector3.zero;
                finish.localRotation = Quaternion.identity;
                finish.localScale = Vector3.one;
                finish.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                var renderer = finish.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool _hasFinish(GameObject prefab, string objectName, int materialCount)
        {
            if (prefab == null)
            {
                return false;
            }

            var finish = prefab.transform.Find(objectName);
            return finish != null &&
                   finish.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null &&
                   finish.TryGetComponent<MeshRenderer>(out var renderer) &&
                   renderer.sharedMaterials.Length == materialCount &&
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
