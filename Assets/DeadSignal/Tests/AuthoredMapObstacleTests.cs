using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class AuthoredMapObstacleTests
    {
        [Test]
        public void ScaledHalfSizeAndAxes_AccountForSceneScaleAndRotation()
        {
            var root = new GameObject("Authored obstacle test");
            try
            {
                var obstacle = root.AddComponent<AuthoredMapObstacle>();
                obstacle.Configure(new Vector2(2f, 0.5f));
                root.transform.position = new Vector3(3f, 0f, -4f);
                root.transform.localScale = new Vector3(0.5f, 1f, 2f);
                root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                Assert.That(obstacle.Center, Is.EqualTo(new Vector2(3f, -4f)));
                Assert.That(obstacle.ScaledHalfSize.x, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(obstacle.ScaledHalfSize.y, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(obstacle.RightAxis.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(obstacle.RightAxis.y, Is.EqualTo(-1f).Within(0.0001f));
                Assert.That(obstacle.ForwardAxis.x, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(obstacle.ForwardAxis.y, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OverlapsCircle_UsesObjectAlignedBounds()
        {
            var root = new GameObject("Rotated authored obstacle test");
            try
            {
                var obstacle = root.AddComponent<AuthoredMapObstacle>();
                obstacle.Configure(new Vector2(2f, 0.5f));
                root.transform.rotation = Quaternion.Euler(0f, -45f, 0f);

                var insideLongAxis = root.transform.TransformPoint(new Vector3(1.7f, 0f, 0.2f));
                var outsideNarrowAxis = root.transform.TransformPoint(new Vector3(0f, 0f, 0.8f));
                Assert.That(obstacle.OverlapsCircle(insideLongAxis, 0.1f), Is.True);
                Assert.That(obstacle.OverlapsCircle(outsideNarrowAxis, 0.1f), Is.False,
                    "A point inside the old world AABB but outside the rotated box should remain traversable.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TryResolveSlide_ProjectsBlockedMovementAlongRotatedEdge()
        {
            var root = new GameObject("Rotated slide test");
            try
            {
                var obstacle = root.AddComponent<AuthoredMapObstacle>();
                obstacle.Configure(new Vector2(2f, 0.5f));
                root.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

                const float radius = 0.25f;
                var current = root.transform.TransformPoint(new Vector3(0f, 0f, -0.85f));
                var desired = current + root.transform.forward * 0.5f + root.transform.right * 0.7f;

                Assert.That(obstacle.TryResolveSlide(current, desired, radius, out var resolved), Is.True);
                Assert.That(obstacle.OverlapsCircle(resolved, radius), Is.False);
                Assert.That(Vector3.Distance(resolved, current), Is.GreaterThan(0.6f),
                    "Blocked diagonal movement should retain its component along the rotated obstacle edge.");
                Assert.That(Vector3.Dot(resolved - current, root.transform.right), Is.GreaterThan(0.6f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OverlapsCircle_DoesNotSquareOffRotatedObstacleCorners()
        {
            var root = new GameObject("Rotated rounded corner test");
            try
            {
                var obstacle = root.AddComponent<AuthoredMapObstacle>();
                obstacle.Configure(new Vector2(2f, 0.5f));
                root.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

                const float radius = 0.25f;
                var openCorner = root.transform.TransformPoint(new Vector3(2.2f, 0f, 0.7f));

                Assert.That(obstacle.OverlapsCircle(openCorner, radius), Is.False,
                    "Circle collision should keep the rounded clearance around an oriented box corner.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
