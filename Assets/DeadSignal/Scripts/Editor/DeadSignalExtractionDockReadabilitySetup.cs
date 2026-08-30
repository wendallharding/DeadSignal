using System;
using DeadSignal.World;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalExtractionDockReadabilitySetup
    {
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/ExtractionPadAssembly.prefab";
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Environment/ExtractionUplinkStatusGlyph.png";
        private const string MESH_PATH = "Assets/DeadSignal/Resources/Environment/ExtractionUplinkStatusReadability.asset";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/ExtractionDock";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/ExtractionUplinkStatus.mat";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                var readability = prefab?.GetComponent<AuthoredExtractionDockReadability>();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH) != null &&
                       readability is { IsConfigured: true, HasStatusTexture: true } &&
                       prefab.transform.Find("Extraction Uplink Status") != null;
            }
        }

        [MenuItem("DEAD SIGNAL/Setup/Extraction Dock Readability")]
        public static void EnsureAssets()
        {
            _configureTexture();
            var mesh = _ensureMesh();
            var material = _ensureMaterial();
            _upgradePrefab(mesh, material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!HasAssets)
            {
                throw new InvalidOperationException("The Extraction Dock readability assets are incomplete.");
            }
        }

        private static void _configureTexture()
        {
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not find the Extraction Dock status texture at {TEXTURE_PATH}.");
            }

            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Mesh _ensureMesh()
        {
            const int segmentCount = 16;
            var vertices = new Vector3[segmentCount + 1];
            var uv = new Vector2[segmentCount + 1];
            var triangles = new int[segmentCount * 3];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (var index = 0; index < segmentCount; index++)
            {
                var angle = Mathf.PI * 2f * index / segmentCount;
                var x = Mathf.Sin(angle);
                var z = Mathf.Cos(angle);
                vertices[index + 1] = new Vector3(x, 0f, z);
                uv[index + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
                triangles[index * 3] = 0;
                triangles[index * 3 + 1] = index + 1;
                triangles[index * 3 + 2] = (index + 1) % segmentCount + 1;
            }

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
            var createAsset = mesh == null;
            if (createAsset)
            {
                mesh = new Mesh { name = "ExtractionUplinkStatusReadability" };
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (createAsset)
            {
                AssetDatabase.CreateAsset(mesh, MESH_PATH);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Material _ensureMaterial()
        {
            if (!AssetDatabase.IsValidFolder(MATERIAL_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/DeadSignal/Resources/Materials", "ExtractionDock");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("Could not find the URP Lit shader for Extraction Dock readability.");
                }

                material = new Material(shader) { name = "ExtractionUplinkStatus" };
                AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(0.04f, 0.95f, 1f));
            material.SetFloat("_Metallic", 0.24f);
            material.SetFloat("_Smoothness", 0.42f);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.08f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_EMISSION");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void _upgradePrefab(Mesh mesh, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                var status = root.transform.Find("Extraction Uplink Status");
                if (status == null)
                {
                    status = new GameObject("Extraction Uplink Status", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    status.SetParent(root.transform, false);
                }

                status.localPosition = new Vector3(0f, 0.235f, 0f);
                status.localRotation = Quaternion.identity;
                status.localScale = Vector3.one * 1.02f;
                status.GetComponent<MeshFilter>().sharedMesh = mesh;
                status.GetComponent<MeshRenderer>().sharedMaterial = material;
                foreach (var collider in status.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                var beacon = root.transform.Find("Extraction Beacon");
                if (beacon == null)
                {
                    throw new InvalidOperationException("The authored Extraction Beacon is missing from the pad prefab.");
                }

                var readability = root.GetComponent<AuthoredExtractionDockReadability>();
                if (readability == null)
                {
                    readability = root.AddComponent<AuthoredExtractionDockReadability>();
                }
                readability.Configure(status, status.GetComponent<Renderer>(), beacon.GetComponent<Renderer>());
                EditorUtility.SetDirty(readability);
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
