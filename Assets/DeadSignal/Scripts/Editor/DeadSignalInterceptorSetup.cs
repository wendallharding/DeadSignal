using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeadSignal.World;

namespace DeadSignal.Editor
{
    public static class DeadSignalInterceptorSetup
    {
        private const string ACTOR_PREFAB_PATH = "Assets/DeadSignal/Resources/Actors/SecurityInterceptorAssembly.prefab";
        private const string INTERCEPTOR_TEXTURE_PATH = "Assets/DeadSignal/Resources/Actors/SecurityInterceptorAlbedo.png";
        private const string INTERCEPTOR_MESH_FOLDER = "Assets/DeadSignal/Resources/Meshes/Actors";
        private const string INTERCEPTOR_MATERIAL_FOLDER = "Assets/DeadSignal/Resources/Materials/Actors";
        private const string SUPPRESSOR_PREFAB_PATH = "Assets/DeadSignal/Resources/Actors/SecuritySuppressorAssembly.prefab";
        private const string ENTRANCE_PREFAB_PATH = "Assets/DeadSignal/Resources/Environment/InterceptorEntryGate.prefab";
        private const string SCENE_PATH = "Assets/DeadSignal/Scenes/SampleScene.unity";
        private const string ARMOR_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SecurityWardenArmor.mat";
        private const string RED_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/SecurityWardenEye.mat";
        private const string AMBER_MATERIAL_PATH = "Assets/DeadSignal/Resources/Materials/EastVaultEnergy.mat";

        private static readonly Vector3 s_northPosition = new(-16.4f, 0f, 7.1f);
        private static readonly Vector3 s_southPosition = new(1.5f, 0f, -7.5f);

        public static bool HasAssets
        {
            get
            {
                var actor = AssetDatabase.LoadAssetAtPath<GameObject>(ACTOR_PREFAB_PATH);
                var entrance = AssetDatabase.LoadAssetAtPath<GameObject>(ENTRANCE_PREFAB_PATH);
                return _hasActorParts(actor) &&
                       _hasSuppressorParts(AssetDatabase.LoadAssetAtPath<GameObject>(SUPPRESSOR_PREFAB_PATH)) &&
                       entrance != null &&
                       entrance.GetComponent<AuthoredInterceptorEntrance>() != null;
            }
        }

        public static void EnsureAssets()
        {
            _ensureInterceptorPresentationAssets();
            _ensureActorPrefab();
            _ensureSuppressorPrefab();
            _ensureEntrancePrefab();
            _ensureScenePlacements();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!HasAssets)
            {
                throw new InvalidOperationException("Security Interceptor assets are incomplete.");
            }
        }

        private static void _ensureActorPrefab()
        {
            var root = new GameObject("SecurityInterceptorAssembly");
            try
            {
                _createMeshPart(root.transform, "Interceptor Chassis", "SecurityInterceptorChassis.asset",
                    new Vector3(0f, 0.3f, 0f), Vector3.one, "SecurityInterceptorArmor.mat");
                _createMeshPart(root.transform, "Interceptor Blade Left", "SecurityInterceptorBlade.asset",
                    new Vector3(-0.55f, 0.24f, 0f), Vector3.one, "SecurityInterceptorCeramic.mat");
                _createMeshPart(root.transform, "Interceptor Blade Right", "SecurityInterceptorBlade.asset",
                    new Vector3(0.55f, 0.24f, 0f), new Vector3(-1f, 1f, 1f), "SecurityInterceptorRail.mat");
                _createMeshPart(root.transform, "Interceptor Core", "SecurityInterceptorCore.asset",
                    new Vector3(0f, 0.43f, -0.32f), Vector3.one, "SecurityInterceptorCore.mat");
                PrefabUtility.SaveAsPrefabAsset(root, ACTOR_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureInterceptorPresentationAssets()
        {
            _ensureFolder(INTERCEPTOR_MESH_FOLDER);
            _ensureFolder(INTERCEPTOR_MATERIAL_FOLDER);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(INTERCEPTOR_TEXTURE_PATH);
            if (texture == null)
            {
                throw new InvalidOperationException($"Missing Interceptor texture: {INTERCEPTOR_TEXTURE_PATH}");
            }

            _saveMesh("SecurityInterceptorChassis.asset", _createExtrudedMesh("Security Interceptor Chassis", new[]
            {
                new Vector2(-0.5f, -0.56f), new Vector2(-0.48f, 0.22f), new Vector2(0f, 0.7f),
                new Vector2(0.48f, 0.22f), new Vector2(0.5f, -0.56f), new Vector2(0f, -0.38f)
            }, 0.28f));
            _saveMesh("SecurityInterceptorBlade.asset", _createExtrudedMesh("Security Interceptor Charge Blade", new[]
            {
                new Vector2(-0.17f, -0.78f), new Vector2(-0.11f, 0.58f), new Vector2(0.04f, 0.82f),
                new Vector2(0.2f, 0.47f), new Vector2(0.12f, -0.68f)
            }, 0.12f));
            var coreOutline = Enumerable.Range(0, 8)
                .Select(index => new Vector2(Mathf.Sin(index * Mathf.PI / 4f), Mathf.Cos(index * Mathf.PI / 4f)) * 0.22f)
                .ToArray();
            _saveMesh("SecurityInterceptorCore.asset", _createExtrudedMesh("Security Interceptor Capacitor Core", coreOutline, 0.2f));
            _saveMaterial("SecurityInterceptorArmor.mat", texture, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), 0.48f, 0.15f);
            _saveMaterial("SecurityInterceptorCeramic.mat", texture, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0.38f, 0.1f);
            _saveMaterial("SecurityInterceptorRail.mat", texture, new Vector2(0.5f, 0.5f), Vector2.zero, 0.55f, 0.45f);
            _saveMaterial("SecurityInterceptorCore.mat", texture, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), 0.42f, 0.65f);
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
            var path = $"{INTERCEPTOR_MESH_FOLDER}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(source, path);
            else { EditorUtility.CopySerialized(source, existing); UnityEngine.Object.DestroyImmediate(source); }
        }

        private static void _saveMaterial(string fileName, Texture2D texture, Vector2 scale, Vector2 offset, float metallic, float emission)
        {
            var path = $"{INTERCEPTOR_MATERIAL_FOLDER}/{fileName}";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(fileName) };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", 0.52f);
            material.SetColor("_EmissionColor", Color.white * emission);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
        }

        private static void _createMeshPart(Transform parent, string objectName, string meshName, Vector3 position, Vector3 scale, string materialName)
        {
            var part = new GameObject(objectName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{INTERCEPTOR_MESH_FOLDER}/{meshName}");
            part.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>($"{INTERCEPTOR_MATERIAL_FOLDER}/{materialName}");
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

        private static void _ensureEntrancePrefab()
        {
            var root = new GameObject("InterceptorEntryGate");
            try
            {
                root.AddComponent<AuthoredInterceptorEntrance>();
                _createPart(root.transform, "Gate Rail Left", PrimitiveType.Cube,
                    new Vector3(-0.85f, 0.22f, 0f), new Vector3(0.18f, 0.44f, 1.8f), ARMOR_MATERIAL_PATH);
                _createPart(root.transform, "Gate Rail Right", PrimitiveType.Cube,
                    new Vector3(0.85f, 0.22f, 0f), new Vector3(0.18f, 0.44f, 1.8f), ARMOR_MATERIAL_PATH);
                _createPart(root.transform, "Gate Warning Bar", PrimitiveType.Cube,
                    new Vector3(0f, 0.08f, 0f), new Vector3(1.45f, 0.04f, 0.22f), RED_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, ENTRANCE_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureSuppressorPrefab()
        {
            var root = new GameObject("SecuritySuppressorAssembly");
            try
            {
                _createPart(root.transform, "Suppressor Chassis", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.34f, 0f), new Vector3(0.9f, 0.22f, 0.9f), ARMOR_MATERIAL_PATH);
                _createPart(root.transform, "Suppressor Emitter Left", PrimitiveType.Cube,
                    new Vector3(-0.58f, 0.38f, 0f), new Vector3(0.18f, 0.18f, 0.92f), RED_MATERIAL_PATH);
                _createPart(root.transform, "Suppressor Emitter Right", PrimitiveType.Cube,
                    new Vector3(0.58f, 0.38f, 0f), new Vector3(0.18f, 0.18f, 0.92f), RED_MATERIAL_PATH);
                _createPart(root.transform, "Suppressor Core", PrimitiveType.Sphere,
                    new Vector3(0f, 0.58f, 0f), new Vector3(0.3f, 0.22f, 0.3f), AMBER_MATERIAL_PATH);
                PrefabUtility.SaveAsPrefabAsset(root, SUPPRESSOR_PREFAB_PATH);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void _ensureScenePlacements()
        {
            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            _ensureScenePlacement(scene, "North Interceptor Flank Gate", s_northPosition, 0);
            _ensureScenePlacement(scene, "South Interceptor Flank Gate", s_southPosition, 1);
            EditorSceneManager.SaveScene(scene);
        }

        private static void _ensureScenePlacement(Scene scene, string objectName, Vector3 position, int priority)
        {
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == objectName);
            if (existing == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENTRANCE_PREFAB_PATH);
                existing = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (existing == null)
                {
                    throw new InvalidOperationException($"Could not place {objectName} in SampleScene.");
                }

                existing.name = objectName;
            }

            existing.transform.position = position;
            existing.transform.rotation = priority == 0 ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            existing.GetComponent<AuthoredInterceptorEntrance>().Configure(priority);
        }

        private static void _createPart(
            Transform parent,
            string objectName,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            string materialPath)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Missing Interceptor material: {materialPath}");
            }

            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static bool _hasActorParts(GameObject actor)
        {
            return actor != null &&
                   actor.transform.Find("Interceptor Chassis") != null &&
                   actor.transform.Find("Interceptor Blade Left") != null &&
                   actor.transform.Find("Interceptor Blade Right") != null &&
                   actor.transform.Find("Interceptor Core") != null;
        }

        private static bool _hasSuppressorParts(GameObject actor)
        {
            return actor != null &&
                   actor.transform.Find("Suppressor Chassis") != null &&
                   actor.transform.Find("Suppressor Emitter Left") != null &&
                   actor.transform.Find("Suppressor Emitter Right") != null &&
                   actor.transform.Find("Suppressor Core") != null;
        }
    }
}
