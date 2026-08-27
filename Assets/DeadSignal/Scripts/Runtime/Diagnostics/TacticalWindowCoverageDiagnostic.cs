using System.Collections.Generic;
using UnityEngine;

namespace DeadSignal.Diagnostics
{
    public readonly struct TacticalWindowCoverage
    {
        public TacticalWindowCoverage(string rendererName, float windowCoverage, Rect screenRect)
        {
            RendererName = rendererName;
            WindowCoverage = windowCoverage;
            ScreenRect = screenRect;
        }

        public string RendererName { get; }
        public float WindowCoverage { get; }
        public Rect ScreenRect { get; }
    }

    /// <summary>Measures authored renderer coverage without changing runtime presentation.</summary>
    public static class TacticalWindowCoverageDiagnostic
    {
        public static IReadOnlyList<TacticalWindowCoverage> Measure(
            Camera camera,
            IEnumerable<Renderer> renderers,
            float windowWidth = 0.4f,
            float windowHeight = 0.55f)
        {
            var results = new List<TacticalWindowCoverage>();
            if (camera == null || renderers == null)
            {
                return results;
            }

            var tacticalWindow = new Rect(
                (1f - windowWidth) * 0.5f,
                (1f - windowHeight) * 0.5f,
                windowWidth,
                windowHeight);

            foreach (var candidate in renderers)
            {
                if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy ||
                    !_tryProjectBounds(camera, candidate.bounds, out var screenRect))
                {
                    continue;
                }

                var intersection = _intersect(screenRect, tacticalWindow);
                if (intersection.width <= 0f || intersection.height <= 0f)
                {
                    continue;
                }

                var coverage = intersection.width * intersection.height /
                               (tacticalWindow.width * tacticalWindow.height);
                results.Add(new TacticalWindowCoverage(_hierarchyPath(candidate.transform), coverage, screenRect));
            }

            results.Sort((left, right) => right.WindowCoverage.CompareTo(left.WindowCoverage));
            return results;
        }

        private static bool _tryProjectBounds(Camera camera, Bounds bounds, out Rect screenRect)
        {
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var extents = bounds.extents;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var point = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(extents, new Vector3(x, y, z)));
                        if (point.z <= 0f)
                        {
                            screenRect = default;
                            return false;
                        }

                        minimum = Vector2.Min(minimum, point);
                        maximum = Vector2.Max(maximum, point);
                    }
                }
            }

            screenRect = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
            return true;
        }

        private static Rect _intersect(Rect left, Rect right)
        {
            return Rect.MinMaxRect(
                Mathf.Max(left.xMin, right.xMin),
                Mathf.Max(left.yMin, right.yMin),
                Mathf.Min(left.xMax, right.xMax),
                Mathf.Min(left.yMax, right.yMax));
        }

        private static string _hierarchyPath(Transform target)
        {
            var path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = $"{target.name}/{path}";
            }

            return path;
        }
    }
}
