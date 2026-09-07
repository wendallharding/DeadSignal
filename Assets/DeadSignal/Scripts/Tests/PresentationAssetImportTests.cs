using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class PresentationAssetImportTests
    {
        private const string RESOURCES_ROOT = "Assets/DeadSignal/Resources";
        private const string RUNTIME_SOURCE_ROOT = "Assets/DeadSignal/Scripts/Runtime";
        private const string SCENES_ROOT = "Assets/DeadSignal/Scenes";

        [Test]
        public void PresentationAssets_HaveStableGuidsImportersAndResolvableDependencies()
        {
            var paths = _findPresentationAssetPaths();
            var guids = new HashSet<string>(StringComparer.Ordinal);
            var failures = new List<string>();

            foreach (var path in paths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrWhiteSpace(guid) || !guids.Add(guid))
                {
                    failures.Add($"Missing or duplicate GUID: {path} ({guid})");
                }

                if (AssetImporter.GetAtPath(path) == null)
                {
                    failures.Add($"Missing importer: {path}");
                }

                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    failures.Add($"Main asset does not load: {path}");
                }

                foreach (var dependencyPath in AssetDatabase.GetDependencies(path, true))
                {
                    if (AssetDatabase.LoadMainAssetAtPath(dependencyPath) == null)
                    {
                        failures.Add($"Unresolved dependency: {path} -> {dependencyPath}");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            Assert.That(paths.Length, Is.GreaterThan(500), "The audit did not discover the expected presentation asset set.");
        }

        [Test]
        public void ResourceKeys_AreUniquePerImportedType()
        {
            var collisions = _findPresentationAssetPaths()
                .Where(path => path.StartsWith(RESOURCES_ROOT + "/", StringComparison.Ordinal))
                .Select(path => new
                {
                    Key = Path.ChangeExtension(path.Substring(RESOURCES_ROOT.Length + 1), null),
                    AssetType = AssetDatabase.LoadMainAssetAtPath(path)?.GetType(),
                    Path = path
                })
                .Where(entry => entry.AssetType != null)
                .GroupBy(entry => $"{entry.Key}|{entry.AssetType.FullName}", StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => string.Join(", ", group.Select(entry => entry.Path)))
                .ToArray();

            Assert.That(collisions, Is.Empty, "Ambiguous Resources keys:\n" + string.Join("\n", collisions));
        }

        [Test]
        public void RuntimeResourceLoadContracts_ResolveToPackagedAssets()
        {
            var resourceLoadPattern = new Regex(
                "Resources\\.Load<(?<type>[^>]+)>\\s*\\(\\s*\"(?<path>[^\"\\r\\n]+)\"\\s*\\)",
                RegexOptions.CultureInvariant);
            var typesByName = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(_getLoadableTypes)
                .Where(type => type != null)
                .GroupBy(type => type.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var failures = new List<string>();
            var contractCount = 0;

            foreach (var sourcePath in Directory.GetFiles(RUNTIME_SOURCE_ROOT, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(sourcePath);
                foreach (Match match in resourceLoadPattern.Matches(source))
                {
                    contractCount++;
                    var typeName = match.Groups["type"].Value.Trim().Split('.').Last();
                    var resourcePath = match.Groups["path"].Value;
                    if (!typesByName.TryGetValue(typeName, out var assetType))
                    {
                        failures.Add($"Unknown Resources type {typeName}: {sourcePath} -> {resourcePath}");
                        continue;
                    }

                    if (Resources.Load(resourcePath, assetType) == null)
                    {
                        failures.Add($"Missing Resources asset: {sourcePath} -> {assetType.Name} {resourcePath}");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            Assert.That(contractCount, Is.GreaterThan(150), "The audit did not discover the expected runtime Resources contracts.");
        }

        [Test]
        public void StaticPresentationModels_UseLeanDeterministicImportSettings()
        {
            var failures = new List<string>();
            var modelPaths = AssetDatabase.FindAssets("t:Model", new[] { RESOURCES_ROOT })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            foreach (var path in modelPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    failures.Add($"Missing model importer: {path}");
                    continue;
                }

                if (importer.addCollider || importer.importAnimation || importer.importCameras || importer.importLights ||
                    importer.materialImportMode != ModelImporterMaterialImportMode.None || importer.isReadable ||
                    importer.meshCompression != ModelImporterMeshCompression.Low)
                {
                    failures.Add(
                        $"{path}: collider={importer.addCollider}, animation={importer.importAnimation}, " +
                        $"camera={importer.importCameras}, lights={importer.importLights}, " +
                        $"materials={importer.materialImportMode}, readable={importer.isReadable}, " +
                        $"compression={importer.meshCompression}");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            Assert.That(modelPaths.Length, Is.EqualTo(18), "Review newly added or removed presentation models explicitly.");
        }

        [Test]
        public void BuildScenesAndResources_PreserveWindowsInclusionRoots()
        {
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            var failures = enabledScenes
                .Where(scene => string.IsNullOrWhiteSpace(scene.path) ||
                                AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null)
                .Select(scene => $"Missing enabled scene: {scene.path}")
                .ToList();

            var resources = _findPresentationAssetPaths()
                .Where(path => path.StartsWith(RESOURCES_ROOT + "/", StringComparison.Ordinal))
                .ToArray();
            if (resources.Length == 0)
            {
                failures.Add("No Resources presentation assets were discovered for player inclusion.");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            Assert.That(enabledScenes.Select(scene => scene.path), Does.Contain("Assets/DeadSignal/Scenes/SampleScene.unity"));
        }

        private static string[] _findPresentationAssetPaths()
        {
            return AssetDatabase.FindAssets(string.Empty, new[] { RESOURCES_ROOT, SCENES_ROOT })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<Type> _getLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }
}
