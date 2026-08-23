using NUnit.Framework;
using UnityEngine;
using DeadSignal.Combat;
using DeadSignal.Missions;

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
                Assert.That(tuning.WardenSapperScreenDistance, Is.InRange(2.5f, 3.5f));
                Assert.That(tuning.WardenSapperScreenBreakDistance,
                    Is.GreaterThan(tuning.WardenAttackDistance).And.LessThan(tuning.WardenSapperScreenDistance));
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
                Assert.That(tuning.StableExtractionPurgeAcceleration, Is.InRange(0.75f, 1f));
                Assert.That(tuning.StableExtractionPurgeAcceleration, Is.LessThan(tuning.SuppressorWarningDuration),
                    "One purge should reward combat without bypassing the readable suppression response.");
                Assert.That(tuning.OverdriveExtractionPurgeAcceleration, Is.InRange(0.1f, 0.4f));
                Assert.That(tuning.StableExtractionPurgeAcceleration,
                    Is.GreaterThan(tuning.OverdriveExtractionPurgeAcceleration * 3f),
                    "Stable should be the deliberate combat route while Overdrive remains the short evasion route.");
                Assert.That(tuning.OverdriveSuppressionLeadDistance,
                    Is.GreaterThan(tuning.SuppressorFieldRadius),
                    "The predictive sweep should begin beyond the drone so changing course can avoid it.");
                Assert.That(tuning.InterceptorHealth, Is.GreaterThan(0));
                Assert.That(tuning.InterceptorChargeDuration, Is.GreaterThanOrEqualTo(0.5f));
                Assert.That(tuning.InterceptorDashSpeed, Is.GreaterThan(tuning.InterceptorApproachSpeed));
                Assert.That(tuning.InterceptorDashRecoveryDuration, Is.InRange(0.5f, 1f));
                Assert.That(tuning.InterceptorCrashRecoveryDuration,
                    Is.GreaterThan(tuning.InterceptorDashRecoveryDuration).And.LessThanOrEqualTo(2f));
                Assert.That(tuning.InterceptorSuppressionExitMargin, Is.InRange(0.25f, 1f));
                Assert.That(tuning.InterceptorSapperFlankDistance, Is.InRange(3f, 4.5f));
                Assert.That(tuning.InterceptorSapperFlankBreakDistance,
                    Is.GreaterThan(tuning.WardenSapperScreenBreakDistance)
                        .And.LessThan(tuning.InterceptorSapperFlankDistance));
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
        public void OpeningSuppressionCenter_StableLocksPlayerWhileOverdriveLeadsRetreat()
        {
            var extraction = new Vector3(-9f, 0f, -5f);
            var player = new Vector3(-6f, 0f, -1f);

            var stable = InterceptorTactics.CalculateOpeningSuppressionCenter(
                player, extraction, ExtractionUplinkMode.Stable, 3.5f);
            var overdrive = InterceptorTactics.CalculateOpeningSuppressionCenter(
                player, extraction, ExtractionUplinkMode.Overdrive, 3.5f);

            Assert.That(stable, Is.EqualTo(player));
            Assert.That(Vector3.Distance(player, overdrive), Is.EqualTo(3.5f).Within(0.001f));
            Assert.That(Vector3.Dot((overdrive - player).normalized, (player - extraction).normalized),
                Is.GreaterThan(0.999f));
        }

        [Test]
        public void OpeningSuppressionCenter_OverdriveAtDockDoesNotInventDirection()
        {
            var dock = new Vector3(-9f, 0f, -5f);

            Assert.That(InterceptorTactics.CalculateOpeningSuppressionCenter(
                dock, dock, ExtractionUplinkMode.Overdrive, 3.5f), Is.EqualTo(dock));
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
                Assert.That(tuning.OverdriveSynergySurgeDuration, Is.InRange(1f, 3f));
                Assert.That(tuning.OverdriveSynergySpeedMultiplier, Is.InRange(1.1f, 1.3f));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
