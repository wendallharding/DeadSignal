using System;
using System.Linq;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSwarmerSetup
    {
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Actors/SwarmerAssembly.prefab";
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Actors/SecuritySwarmerAlbedo.png";
        private const string TUNING_PATH = "Assets/DeadSignal/Resources/Tuning/SwarmerPressureTuning.asset";
        private const string MESH_FOLDER = "Assets/DeadSignal/Resources/Meshes/Actors";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/Actors";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       AssetDatabase.LoadAssetAtPath<SwarmerPressureTuning>(TUNING_PATH) != null &&
                       prefab != null && prefab.GetComponent<SecuritySwarmerPresentation>() != null &&
                       _hasMeshPart(prefab, "Swarmer Body", "SecuritySwarmerBody.asset") &&
                       _hasMeshPart(prefab, "Swarmer Core", "SecuritySwarmerCore.asset") &&
                       _hasMeshPart(prefab, "Swarmer Needle", "SecuritySwarmerNeedle.asset") &&
                       _hasMeshPart(prefab, "Swarmer Tail", "SecuritySwarmerTail.asset");
            }
        }

        [MenuItem("Tools/DEAD SIGNAL/Ensure Swarmer Pressure Assets")]
        public static void EnsureAssets()
        {
            AssetDatabase.Refresh();
            _ensureFolder(MESH_FOLDER);
            _ensureFolder(MATERIAL_FOLDER);
            if (AssetDatabase.LoadAssetAtPath<SwarmerPressureTuning>(TUNING_PATH) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SwarmerPressureTuning>(), TUNING_PATH);
            }

            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing Swarmer texture: {TEXTURE_PATH}");
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            _ensureMeshes();
            _ensureMaterials(AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH));
            _ensurePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets) throw new InvalidOperationException("Security Swarmer presentation assets are incomplete.");
        }

        private static void _ensureMeshes()
        {
            _saveMesh("SecuritySwarmerBody.asset", _createExtrudedMesh("Security Swarmer Directional Body", new[]
            {
                new Vector2(-0.34f, -0.34f), new Vector2(-0.48f, 0.05f), new Vector2(-0.2f, 0.42f),
                new Vector2(0f, 0.58f), new Vector2(0.2f, 0.42f), new Vector2(0.48f, 0.05f),
                new Vector2(0.34f, -0.34f), new Vector2(0f, -0.46f)
            }, 0.16f));
            var coreOutline = Enumerable.Range(0, 8)
                .Select(index => new Vector2(Mathf.Sin(index * Mathf.PI / 4f), Mathf.Cos(index * Mathf.PI / 4f)) * 0.18f)
                .ToArray();
            _saveMesh("SecuritySwarmerCore.asset", _createExtrudedMesh("Security Swarmer Ceramic Core", coreOutline, 0.18f));
            _saveMesh("SecuritySwarmerNeedle.asset", _createExtrudedMesh("Security Swarmer Contact Needle", new[]
            {
                new Vector2(-0.07f, -0.32f), new Vector2(-0.09f, 0.12f), new Vector2(0f, 0.48f),
                new Vector2(0.09f, 0.12f), new Vector2(0.07f, -0.32f), new Vector2(0f, -0.38f)
            }, 0.1f));
            _saveMesh("SecuritySwarmerTail.asset", _createExtrudedMesh("Security Swarmer Graphite Tail", new[]
            {
                new Vector2(-0.2f, -0.18f), new Vector2(-0.28f, 0.08f), new Vector2(0f, 0.3f),
                new Vector2(0.28f, 0.08f), new Vector2(0.2f, -0.18f), new Vector2(0f, -0.28f)
            }, 0.09f));
        }

        private static void _ensureMaterials(Texture2D texture)
        {
            _saveMaterial("SecuritySwarmerArmor.mat", texture, new Vector2(0.62f, 0.72f), new Vector2(0f, 0.28f), 0.48f, 0.08f);
            _saveMaterial("SecuritySwarmerCore.mat", texture, new Vector2(0.34f, 0.4f), new Vector2(0.66f, 0.56f), 0.22f, 0.16f);
            _saveMaterial("SecuritySwarmerNeedle.mat", texture, new Vector2(0.34f, 0.3f), new Vector2(0.62f, 0.28f), 0.36f, 0.7f);
            _saveMaterial("SecuritySwarmerTail.mat", texture, new Vector2(0.4f, 0.3f), new Vector2(0.6f, 0f), 0.58f, 0.03f);
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject("Swarmer Assembly");
            try
            {
                root.AddComponent<SecuritySwarmerPresentation>();
                _createPart(root.transform, "Swarmer Body", "SecuritySwarmerBody.asset",
                    new Vector3(0f, 0.28f, 0f), Vector3.one, "SecuritySwarmerArmor.mat");
                _createPart(root.transform, "Swarmer Core", "SecuritySwarmerCore.asset",
                    new Vector3(0f, 0.43f, -0.04f), Vector3.one, "SecuritySwarmerCore.mat");
                _createPart(root.transform, "Swarmer Needle", "SecuritySwarmerNeedle.asset",
                    new Vector3(0f, 0.26f, 0.46f), Vector3.one, "SecuritySwarmerNeedle.mat");
                _createPart(root.transform, "Swarmer Tail", "SecuritySwarmerTail.asset",
                    new Vector3(0f, 0.23f, -0.42f), Vector3.one, "SecuritySwarmerTail.mat");
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Mesh _createExtrudedMesh(string meshName, Vector2[] outline, float height)
        {
            var count = outline.Length;
            var vertices = new Vector3[count * 2];
            var uv = new Vector2[count * 2];
            var min = outline.Aggregate(Vector2.Min);
            var max = outline.Aggregate(Vector2.Max);
            for (var index = 0; index < count; index++)
            {
                vertices[index] = new Vector3(outline[index].x, height * 0.5f, outline[index].y);
                vertices[index + count] = new Vector3(outline[index].x, -height * 0.5f, outline[index].y);
                var mapped = new Vector2(
                    Mathf.InverseLerp(min.x, max.x, outline[index].x),
                    Mathf.InverseLerp(min.y, max.y, outline[index].y));
                uv[index] = mapped;
                uv[index + count] = mapped;
            }

            var triangles = new int[(count - 2) * 6 + count * 6];
            var cursor = 0;
            for (var index = 1; index < count - 1; index++)
            {
                triangles[cursor++] = 0; triangles[cursor++] = index; triangles[cursor++] = index + 1;
                triangles[cursor++] = count; triangles[cursor++] = count + index + 1; triangles[cursor++] = count + index;
            }
            for (var index = 0; index < count; index++)
            {
                var next = (index + 1) % count;
                triangles[cursor++] = index; triangles[cursor++] = next; triangles[cursor++] = count + next;
                triangles[cursor++] = index; triangles[cursor++] = count + next; triangles[cursor++] = count + index;
            }

            var mesh = new Mesh { name = meshName, vertices = vertices, triangles = triangles, uv = uv };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void _saveMesh(string fileName, Mesh source)
        {
            var path = $"{MESH_FOLDER}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(source, path);
            else
            {
                EditorUtility.CopySerialized(source, existing);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void _saveMaterial(
            string fileName,
            Texture2D texture,
            Vector2 scale,
            Vector2 offset,
            float metallic,
            float emission)
        {
            var path = $"{MATERIAL_FOLDER}/{fileName}";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("Could not find the URP Lit shader for the Swarmer.");
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(fileName) };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", 0.46f);
            material.SetColor("_EmissionColor", Color.white * emission);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
        }

        private static void _createPart(
            Transform parent,
            string objectName,
            string meshName,
            Vector3 position,
            Vector3 scale,
            string materialName)
        {
            var part = new GameObject(objectName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MESH_FOLDER}/{meshName}");
            part.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>($"{MATERIAL_FOLDER}/{materialName}");
        }

        private static bool _hasMeshPart(GameObject prefab, string partName, string meshName)
        {
            var part = prefab != null ? prefab.transform.Find(partName) : null;
            return part != null && part.TryGetComponent<MeshFilter>(out var filter) &&
                   filter.sharedMesh == AssetDatabase.LoadAssetAtPath<Mesh>($"{MESH_FOLDER}/{meshName}") &&
                   part.GetComponent<Collider>() == null;
        }

        private static void _ensureFolder(string path)
        {
            var current = "Assets";
            foreach (var part in path.Split('/').Skip(1))
            {
                var next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
