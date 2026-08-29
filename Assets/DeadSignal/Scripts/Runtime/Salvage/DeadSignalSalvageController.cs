using System;
using UnityEngine;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using DeadSignal.World;

namespace DeadSignal.Salvage
{
    /// <summary>
    /// Owns salvage presentation and collection for a single run.
    /// </summary>
    internal sealed class DeadSignalSalvageController
    {
        private readonly RunModel m_model;
        private readonly DeadSignalWorld m_world;
        private readonly IDeadSignalAudio m_audio;
        private readonly Action<string> m_showFeedback;
        private readonly SalvagePresentationTuning m_tuning;
        private readonly RunMetrics m_metrics;
        private readonly ICombatFeedback m_feedback;
        private readonly IStationStateFeedback m_stationStateFeedback;
        private readonly SignalOverclockChoice m_overclockChoice;
        private readonly SalvageChain m_chain = new();
        private float m_recoveryFieldSecondsRemaining;
        private Vector3 m_recoveryFieldPosition;
        private GameObject m_carriedCargoCoupling;
        private GameObject m_carriedCoolantSeal;

        public int ChainCount => m_chain.Count;
        public float ChainSecondsRemaining => m_chain.SecondsRemaining;
        public bool IsOptionalCacheAvailable =>
            m_model.CanRaidOptionalCache && !m_model.OptionalSalvageSecured && _hasActiveOptionalCache();
        public float OptionalCacheSignalReward => m_tuning.OptionalCacheSignalReward;
        public float OptionalCacheDistance => _optionalCacheDistance();
        public bool IsRecoveryFieldActive => m_recoveryFieldSecondsRemaining > 0f;
        public Vector3 RecoveryFieldPosition => m_recoveryFieldPosition;
        public float RecoveryFieldRadius => m_tuning.RecoveryFieldRadius;

        public DeadSignalSalvageController(
            RunModel model,
            RunMetrics metrics,
            DeadSignalWorld world,
            IDeadSignalAudio audio,
            ICombatFeedback feedback,
            IStationStateFeedback stationStateFeedback,
            SalvagePresentationTuning tuning,
            SignalOverclockChoice overclockChoice,
            Action<string> showFeedback)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_audio = audio;
            m_feedback = feedback;
            m_stationStateFeedback = stationStateFeedback;
            m_tuning = tuning;
            m_overclockChoice = overclockChoice;
            m_showFeedback = showFeedback;
        }

        public void Tick(float dt)
        {
            m_chain.Advance(dt);
            m_recoveryFieldSecondsRemaining = Mathf.Max(0f, m_recoveryFieldSecondsRemaining - dt);
            _tickCargoCouplingWithdrawal();
            _tickCoolantSealRelease();
            foreach (var pickup in m_world.SalvagePickups)
            {
                if (!pickup.activeSelf)
                {
                    continue;
                }

                var isOptionalCache = m_world.IsOptionalCache(pickup);
                if (isOptionalCache && m_model.OptionalSalvageSecured)
                {
                    continue;
                }

                pickup.transform.Rotate(Vector3.up, m_tuning.RotationSpeed * dt, Space.World);
                var hover = m_tuning.HoverHeight +
                            Mathf.Sin(Time.time * m_tuning.HoverFrequency + pickup.transform.position.x) *
                            m_tuning.HoverAmplitude;
                var position = pickup.transform.position;
                position.y = hover;
                pickup.transform.position = position;

                if (DeadSignalWorld.FlatDistance(m_world.Player.position, pickup.transform.position) >= m_tuning.CollectionRadius)
                {
                    continue;
                }

                var region = m_world.GetPayloadRegion(pickup);
                var centralComponent = m_world.GetCentralComponent(pickup);
                if (!isOptionalCache && !m_model.CanCollectPayload(region, centralComponent))
                {
                    continue;
                }

                if (isOptionalCache && !m_model.CanRaidOptionalCache)
                {
                    continue;
                }

                if (!isOptionalCache && region == SignalRegion.Central &&
                    centralComponent == CentralComponentKind.PowerCoupling && m_world.CargoAnnexObjective != null)
                {
                    if (!m_world.CargoAnnexObjective.TryTakeCoupling(
                            m_world.Player.position,
                            m_model.CanCollectPayload(region, centralComponent)))
                    {
                        continue;
                    }

                    pickup.SetActive(false);
                    m_carriedCargoCoupling = pickup;
                    m_audio.Play(DeadSignalAudioCue.Salvage);
                    m_showFeedback("POWER COUPLING RELEASED — WITHDRAW ACROSS THE CYAN THRESHOLD");
                    continue;
                }

                if (!isOptionalCache && region == SignalRegion.Central &&
                    centralComponent == CentralComponentKind.CoolantSeal && m_world.CoolantReclamationObjective != null)
                {
                    if (!m_world.CoolantReclamationObjective.TryReleaseSeal(
                            m_model.CanCollectPayload(region, centralComponent)))
                    {
                        continue;
                    }

                    pickup.SetActive(false);
                    m_carriedCoolantSeal = pickup;
                    m_audio.Play(DeadSignalAudioCue.Salvage);
                    m_showFeedback("COOLANT SEAL RELEASED — EXIT ACROSS THE CYAN THRESHOLD");
                    continue;
                }

                pickup.SetActive(false);
                if (isOptionalCache)
                {
                    var optionalRecovered = m_model.CollectOptionalSalvage(m_tuning.OptionalCacheSignalReward);
                    m_metrics.RecordSalvageSignalRecovered(optionalRecovered);
                    m_audio.Play(DeadSignalAudioCue.Salvage);
                    m_feedback.PlaySalvageChain(pickup.transform.position, RunModel.SalvageRequired + 1);
                    m_stationStateFeedback.Play(pickup.transform.position, StationStateFeedbackKind.Recovery);
                    m_showFeedback($"OPTIONAL CACHE SECURED  +{optionalRecovered:0} SIGNAL — EXTRACT NOW");
                    continue;
                }

                if (!_completeRequiredPayload(pickup, region, centralComponent, pickup.transform.position))
                {
                    pickup.SetActive(true);
                }
            }
        }

        private void _tickCargoCouplingWithdrawal()
        {
            var objective = m_world.CargoAnnexObjective;
            if (objective == null)
            {
                return;
            }

            var objectiveAvailable = m_model.CanCollectPayload(SignalRegion.Central, CentralComponentKind.PowerCoupling);
            objective.ObservePlayer(m_world.Player.position, objectiveAvailable);
            if (m_carriedCargoCoupling == null ||
                !objective.CanCompleteWithdrawal(m_world.Player.position, objectiveAvailable))
            {
                return;
            }

            if (!_completeRequiredPayload(
                    m_carriedCargoCoupling,
                    SignalRegion.Central,
                    CentralComponentKind.PowerCoupling,
                    m_world.Player.position))
            {
                m_carriedCargoCoupling.SetActive(true);
                m_carriedCargoCoupling = null;
                objective.ResetState();
                return;
            }

            m_carriedCargoCoupling = null;
            objective.CompleteWithdrawal();
        }

        private void _tickCoolantSealRelease()
        {
            var objective = m_world.CoolantReclamationObjective;
            if (objective == null)
            {
                return;
            }

            var objectiveAvailable = m_model.CanCollectPayload(SignalRegion.Central, CentralComponentKind.CoolantSeal);
            objective.ObservePlayer(m_world.Player.position, objectiveAvailable);
            if (m_carriedCoolantSeal == null ||
                !objective.CanCompleteRelease(m_world.Player.position, objectiveAvailable))
            {
                return;
            }

            if (!_completeRequiredPayload(
                    m_carriedCoolantSeal,
                    SignalRegion.Central,
                    CentralComponentKind.CoolantSeal,
                    m_world.Player.position))
            {
                m_carriedCoolantSeal.SetActive(true);
                m_carriedCoolantSeal = null;
                objective.ResetState();
                return;
            }

            m_carriedCoolantSeal = null;
            objective.CompleteRelease();
        }

        private bool _completeRequiredPayload(
            GameObject pickup,
            SignalRegion region,
            CentralComponentKind centralComponent,
            Vector3 completionPosition)
        {
            var salvageBeforeCollection = m_model.Salvage;
            if (!m_model.CollectPayload(region, centralComponent))
            {
                return false;
            }

            if (region != SignalRegion.Central)
            {
                m_world.RetirePayloadAlternatives(region, pickup);
            }

            if (region == SignalRegion.Relay)
            {
                m_world.UpdateRelayPayloadPresentation(m_model);
            }

            if (m_model.Salvage == salvageBeforeCollection)
            {
                m_audio.Play(DeadSignalAudioCue.Salvage);
                m_feedback.PlaySalvageChain(completionPosition, 1);
                m_showFeedback("BOTH CENTRAL COMPONENTS SECURED — RELAY ROUTE READY");
                return true;
            }

            m_overclockChoice.NotifySalvageCollected(m_model.Salvage);
            var reward = m_chain.RecordCollection(
                m_tuning.ChainWindow, m_tuning.SecondCacheSignalReward, m_tuning.ThirdCacheSignalReward);
            var recovered = m_model.RestoreSignal(m_tuning.RequiredCacheSignalReward + reward);
            m_recoveryFieldPosition = completionPosition;
            m_recoveryFieldSecondsRemaining = m_tuning.RecoveryFieldDuration;
            m_metrics.RecordSalvageChain(m_chain.Count, recovered);
            m_audio.Play(DeadSignalAudioCue.Salvage);
            m_feedback.PlaySalvageChain(completionPosition, m_chain.Count);
            m_stationStateFeedback.Play(completionPosition, StationStateFeedbackKind.Recovery);
            var rewardText = recovered > 0f
                ? $"  +{recovered:0} SIGNAL  //  SAFE FIELD {m_tuning.RecoveryFieldDuration:0}s"
                : string.Empty;
            m_showFeedback(region == SignalRegion.Relay
                ? "RELAY PAYLOAD STABILIZED — RETURN TO FOUNDRY CALIBRATION"
                : m_overclockChoice.IsPrimaryPending
                ? "SALVAGE CORE UNLOCKED — CHOOSE A PRIMARY OVERCLOCK"
                : m_overclockChoice.IsAuxiliaryPending
                ? "SALVAGE CORE SYNCED — CHOOSE AN AUXILIARY OVERCLOCK"
                : $"{region.ToString().ToUpperInvariant()} PAYLOAD SECURED{rewardText}  " +
                  $"{m_model.Salvage}/{RunModel.SalvageRequired}");
            return true;
        }

        private bool _hasActiveOptionalCache()
        {
            foreach (var pickup in m_world.SalvagePickups)
            {
                if (pickup.activeSelf && m_world.IsOptionalCache(pickup))
                {
                    return true;
                }
            }

            return false;
        }

        private float _optionalCacheDistance()
        {
            var nearestDistance = float.PositiveInfinity;
            foreach (var pickup in m_world.SalvagePickups)
            {
                if (!pickup.activeSelf || !m_world.IsOptionalCache(pickup))
                {
                    continue;
                }

                nearestDistance = Mathf.Min(
                    nearestDistance,
                    DeadSignalWorld.FlatDistance(m_world.Player.position, pickup.transform.position));
            }

            return nearestDistance;
        }
    }
}
