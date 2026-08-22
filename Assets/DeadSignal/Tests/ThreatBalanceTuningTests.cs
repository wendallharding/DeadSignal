using NUnit.Framework;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class ThreatBalanceTuningTests
    {
        [Test]
        public void DefaultRewards_OffsetWardenCostAndMakeUrgentSapperNetPositive()
        {
            var tuning = ScriptableObject.CreateInstance<ThreatBalanceTuning>();
            try
            {
                Assert.That(tuning.WardenSignalReward, Is.GreaterThan(0f));
                Assert.That(tuning.WardenSignalReward, Is.LessThan(tuning.WardenHealth * RunModel.ShotCost));
                Assert.That(tuning.SapperSignalReward, Is.GreaterThan(tuning.SapperHealth * RunModel.ShotCost));
                Assert.That(tuning.SapperPulseInterval, Is.GreaterThan(0f));
                Assert.That(tuning.ReinforcementEntryDelay, Is.GreaterThanOrEqualTo(2f));
                Assert.That(tuning.ReinforcementSafeDistance, Is.GreaterThan(4f));
                Assert.That(tuning.ExtractionUplinkDuration, Is.InRange(6f, 12f));
                Assert.That(tuning.InterceptorHealth, Is.GreaterThan(0));
                Assert.That(tuning.InterceptorChargeDuration, Is.GreaterThanOrEqualTo(0.5f));
                Assert.That(tuning.InterceptorDashSpeed, Is.GreaterThan(tuning.InterceptorApproachSpeed));
                Assert.That(tuning.InterceptorSignalReward,
                    Is.LessThanOrEqualTo(tuning.InterceptorHealth * RunModel.ShotCost));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void OverclockDefaults_CreateDistinctCombatAndMobilityBuilds()
        {
            var tuning = ScriptableObject.CreateInstance<SignalOverclockTuning>();
            try
            {
                Assert.That(tuning.ChainArcRadius, Is.InRange(3f, 6f));
                Assert.That(tuning.ThrusterSpeedMultiplier, Is.GreaterThan(1f));
                Assert.That(tuning.ThrusterAccelerationMultiplier, Is.GreaterThan(1f));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
