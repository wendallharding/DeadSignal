using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Editor
{
    public static class DeadSignalSuppressorSetup
    {
        private const string PREFAB_PATH = "Assets/DeadSignal/Resources/Actors/SecuritySuppressorAssembly.prefab";
        private const string TEXTURE_PATH = "Assets/DeadSignal/Resources/Actors/SecuritySuppressorAlbedo.png";
        private const string MESH_FOLDER = "Assets/DeadSignal/Resources/Meshes/Actors";
        private const string MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/Actors";

        public static bool HasAssets
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_PATH) != null &&
                       _hasMeshPart(prefab, "Suppressor Chassis", "SecuritySuppressorChassis.asset") &&
                       _hasMeshPart(prefab, "Suppressor Emitter Left", "SecuritySuppressorEmitter.asset") &&
                       _hasMeshPart(prefab, "Suppressor Emitter Right", "SecuritySuppressorEmitter.asset") &&
                       _hasMeshPart(prefab, "Suppressor Core", "SecuritySuppressorCore.asset");
            }
        }

        public static void EnsureAssets()
        {
            AssetDatabase.Refresh();
            _ensureFolder(MESH_FOLDER);
            _ensureFolder(MATERIAL_FOLDER);
            var importer = AssetImporter.GetAtPath(TEXTURE_PATH) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing Suppressor texture: {TEXTURE_PATH}");
            importer.alphaIsTransparency = false;
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
            if (!HasAssets) throw new InvalidOperationException("Security Suppressor presentation assets are incomplete.");
        }

        private static void _ensureMeshes()
        {
            _saveMesh("SecuritySuppressorChassis.asset", _createExtrudedMesh("Security Suppressor Directional Chassis", new[]
            {
                new Vector2(-0.52f, -0.52f), new Vector2(-0.62f, 0.18f), new Vector2(-0.34f, 0.55f),
                new Vector2(0f, 0.76f), new Vector2(0.34f, 0.55f), new Vector2(0.62f, 0.18f),
                new Vector2(0.52f, -0.52f), new Vector2(0f, -0.68f)
            }, 0.26f));
            _saveMesh("SecuritySuppressorEmitter.asset", _createExtrudedMesh("Security Suppressor Field Projector", new[]
            {
                new Vector2(-0.14f, -0.54f), new Vector2(-0.18f, 0.32f), new Vector2(0f, 0.66f),
                new Vector2(0.18f, 0.32f), new Vector2(0.14f, -0.54f), new Vector2(0f, -0.68f)
            }, 0.16f));
            var coreOutline = Enumerable.Range(0, 8)
                .Select(index => new Vector2(Mathf.Sin(index * Mathf.PI / 4f), Mathf.Cos(index * Mathf.PI / 4f)) * 0.24f)
                .ToArray();
            _saveMesh("SecuritySuppressorCore.asset", _createExtrudedMesh("Security Suppressor Vulnerable Core", coreOutline, 0.22f));
        }

        private static void _ensureMaterials(Texture2D texture)
        {
            _saveMaterial("SecuritySuppressorArmor.mat", texture, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), 0.52f, 0.04f);
            _saveMaterial("SecuritySuppressorProjector.mat", texture, new Vector2(0.5f, 0.5f), Vector2.zero, 0.36f, 0.75f);
            _saveMaterial("SecuritySuppressorCore.mat", texture, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0.24f, 0.08f);
        }

        private static void _ensurePrefab()
        {
            var root = new GameObject("SecuritySuppressorAssembly");
            try
            {
                _createPart(root.transform, "Suppressor Chassis", "SecuritySuppressorChassis.asset",
                    new Vector3(0f, 0.34f, 0f), Vector3.one, "SecuritySuppressorArmor.mat");
                _createPart(root.transform, "Suppressor Emitter Left", "SecuritySuppressorEmitter.asset",
                    new Vector3(-0.54f, 0.42f, -0.02f), Vector3.one, "SecuritySuppressorProjector.mat");
                _createPart(root.transform, "Suppressor Emitter Right", "SecuritySuppressorEmitter.asset",
                    new Vector3(0.54f, 0.42f, -0.02f), new Vector3(-1f, 1f, 1f), "SecuritySuppressorProjector.mat");
                _createPart(root.transform, "Suppressor Core", "SecuritySuppressorCore.asset",
                    new Vector3(0f, 0.57f, -0.16f), Vector3.one, "SecuritySuppressorCore.mat");
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

        private static void _saveMaterial(string fileName, Texture2D texture, Vector2 scale, Vector2 offset, float metallic, float emission)
        {
            var path = $"{MATERIAL_FOLDER}/{fileName}";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("Could not find the URP Lit shader for the Suppressor.");
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(fileName) };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", 0.48f);
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
            return part != null &&
                   part.TryGetComponent<MeshFilter>(out var filter) &&
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
