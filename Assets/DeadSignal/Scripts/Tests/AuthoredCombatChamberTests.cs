using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class AuthoredCombatChamberTests
    {
        [Test]
        public void StateFlow_OpensLocksClearsAndCollectsReward()
        {
            var root = new GameObject("Combat Chamber Test");
            try
            {
                var commitmentSwitch = _child(root.transform, "Switch", Vector3.zero);
                var threshold = _child(root.transform, "Threshold", new Vector3(0f, 0f, 2f));
                var entryDoor = _child(root.transform, "Entry Door", Vector3.zero).gameObject;
                var rewardDoor = _child(root.transform, "Reward Door", Vector3.zero).gameObject;
                var reward = _child(root.transform, "Reward", new Vector3(0f, 0f, 8f)).gameObject;
                var clearedSignal = _child(root.transform, "Cleared Signal", Vector3.zero).gameObject;
                var scenario = _scenario(root.transform);
                var chamber = root.AddComponent<AuthoredCombatChamber>();
                chamber.Configure(commitmentSwitch, threshold, entryDoor, rewardDoor, reward, clearedSignal,
                    scenario, 1.8f, 0.75f, 20f);

                Assert.That(chamber.State, Is.EqualTo(CombatChamberState.Dormant));
                Assert.That(entryDoor.activeSelf, Is.True);
                Assert.That(rewardDoor.activeSelf, Is.True);
                Assert.That(clearedSignal.activeSelf, Is.False);
                Assert.That(chamber.TryArm(new Vector3(0f, 0f, 1f)), Is.True);
                Assert.That(entryDoor.activeSelf, Is.False);
                Assert.That(chamber.TryBeginLockdown(new Vector3(0f, 0f, 3f)), Is.True);
                Assert.That(chamber.Phase, Is.EqualTo(1));
                Assert.That(entryDoor.activeSelf, Is.True);
                Assert.That(chamber.AdvancePhase(), Is.True);
                Assert.That(chamber.AdvancePhase(), Is.True);
                Assert.That(chamber.AdvancePhase(), Is.False);

                chamber.Complete();

                Assert.That(chamber.State, Is.EqualTo(CombatChamberState.Cleared));
                Assert.That(entryDoor.activeSelf, Is.False);
                Assert.That(rewardDoor.activeSelf, Is.False);
                Assert.That(clearedSignal.activeSelf, Is.True);
                Assert.That(chamber.TryCollectReward(new Vector3(0f, 0f, 8f)), Is.True);
                Assert.That(chamber.RewardAvailable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static AuthoredCombatScenario _scenario(Transform parent)
        {
            var root = _child(parent, "Scenario", Vector3.zero);
            var scenario = root.gameObject.AddComponent<AuthoredCombatScenario>();
            scenario.Configure(
                _child(root, "Player", Vector3.zero),
                _child(root, "Camera", Vector3.zero),
                _child(root, "Warden", Vector3.left),
                _child(root, "Sapper", Vector3.right),
                _child(root, "Interceptor", Vector3.back),
                _child(root, "Suppressor", Vector3.forward),
                new Vector2(-4f, -4f),
                new Vector2(4f, 4f));
            return scenario;
        }

        private static Transform _child(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }
    }
}
