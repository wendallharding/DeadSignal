using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class AuthoredMapObstacleTests
    {
        [Test]
        public void WorldHalfSize_AccountsForSceneScaleAndRotation()
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
                Assert.That(obstacle.WorldHalfSize.x, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(obstacle.WorldHalfSize.y, Is.EqualTo(1f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
