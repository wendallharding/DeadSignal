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
                Assert.That(tuning.DeadZoneTraceDuration, Is.InRange(6f, 12f));
                Assert.That(tuning.ReinforcementEntryDelay, Is.GreaterThanOrEqualTo(2f));
                Assert.That(tuning.ReinforcementSafeDistance, Is.GreaterThan(4f));
                Assert.That(tuning.ExtractionUplinkDuration, Is.InRange(6f, 12f));
                Assert.That(tuning.ExtractionOverdriveDuration, Is.LessThan(tuning.ExtractionUplinkDuration));
                Assert.That(tuning.ExtractionOverdriveDuration,
                    Is.GreaterThan(tuning.ReinforcementEntryDelay + tuning.SuppressorWarningDuration + 1f),
                    "The faster link must preserve a meaningful response window after the readable opening sweep.");
                Assert.That(tuning.ExtractionOverdriveSignalCost,
                    Is.InRange(RunModel.ShotCost * 2f, RunModel.SecurityHitCost));
                Assert.That(tuning.ExtractionPurgeAcceleration, Is.InRange(0.5f, 1f));
                Assert.That(tuning.ExtractionPurgeAcceleration, Is.LessThan(tuning.SuppressorWarningDuration),
                    "One purge should reward combat without bypassing the readable suppression response.");
                Assert.That(tuning.InterceptorHealth, Is.GreaterThan(0));
                Assert.That(tuning.InterceptorChargeDuration, Is.GreaterThanOrEqualTo(0.5f));
                Assert.That(tuning.InterceptorDashSpeed, Is.GreaterThan(tuning.InterceptorApproachSpeed));
                Assert.That(tuning.InterceptorSuppressionExitMargin, Is.InRange(0.25f, 1f));
                Assert.That(tuning.InterceptorSignalReward,
                    Is.LessThanOrEqualTo(tuning.InterceptorHealth * RunModel.ShotCost));
                Assert.That(tuning.SuppressorHealth, Is.EqualTo(3));
                Assert.That(tuning.SuppressorWarningDuration, Is.GreaterThanOrEqualTo(0.75f));
                Assert.That(tuning.SuppressorFieldDuration, Is.GreaterThan(tuning.SuppressorWarningDuration));
                Assert.That(tuning.SuppressorFieldRadius, Is.InRange(2.5f, 4f));
                Assert.That(tuning.SuppressorMovementMultiplier, Is.InRange(0.4f, 0.75f));
                Assert.That(tuning.SuppressorSignalReward,
                    Is.LessThanOrEqualTo(tuning.SuppressorHealth * RunModel.ShotCost));
                Assert.That(tuning.ReinforcementEntryDelay + tuning.SuppressorWarningDuration,
                    Is.LessThan(tuning.ExtractionUplinkDuration - 1f),
                    "The promoted Suppressor needs a meaningful active-field response window before extraction completes.");
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void OverclockDefaults_CreateDistinctCombatMobilityAndEconomyBuilds()
        {
            var tuning = ScriptableObject.CreateInstance<SignalOverclockTuning>();
            try
            {
                Assert.That(tuning.ChainArcRadius, Is.InRange(3f, 6f));
                Assert.That(tuning.ThrusterSpeedMultiplier, Is.GreaterThan(1f));
                Assert.That(tuning.ThrusterAccelerationMultiplier, Is.GreaterThan(1f));
                Assert.That(tuning.EmergencyCapacitorThreshold, Is.InRange(20f, 30f));
                Assert.That(tuning.EmergencyCapacitorRestore, Is.GreaterThan(RunModel.SecurityHitCost));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
