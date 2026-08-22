using System;
using UnityEngine;

namespace DeadSignal
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
        private readonly SignalOverclockChoice m_overclockChoice;
        private readonly SalvageChain m_chain = new();

        public int ChainCount => m_chain.Count;
        public float ChainSecondsRemaining => m_chain.SecondsRemaining;

        public DeadSignalSalvageController(
            RunModel model,
            RunMetrics metrics,
            DeadSignalWorld world,
            IDeadSignalAudio audio,
            ICombatFeedback feedback,
            SalvagePresentationTuning tuning,
            SignalOverclockChoice overclockChoice,
            Action<string> showFeedback)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_audio = audio;
            m_feedback = feedback;
            m_tuning = tuning;
            m_overclockChoice = overclockChoice;
            m_showFeedback = showFeedback;
        }

        public void Tick(float dt)
        {
            m_chain.Advance(dt);
            foreach (var pickup in m_world.SalvagePickups)
            {
                if (!pickup.activeSelf)
                {
                    continue;
                }

                if (m_model.Salvage >= RunModel.SalvageRequired)
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

                pickup.SetActive(false);
                m_model.CollectSalvage();
                m_overclockChoice.NotifySalvageCollected(m_model.Salvage);
                var reward = m_chain.RecordCollection(
                    m_tuning.ChainWindow, m_tuning.SecondCacheSignalReward, m_tuning.ThirdCacheSignalReward);
                var recovered = m_model.RestoreSignal(reward);
                m_metrics.RecordSalvageChain(m_chain.Count, recovered);
                m_audio.Play(DeadSignalAudioCue.Salvage);
                m_feedback.PlaySalvageChain(pickup.transform.position, m_chain.Count);
                var rewardText = recovered > 0f ? $"  +{recovered:0} SIGNAL" : string.Empty;
                m_showFeedback(m_overclockChoice.IsPrimaryPending
                    ? "SALVAGE CORE UNLOCKED — CHOOSE A PRIMARY OVERCLOCK"
                    : m_overclockChoice.IsAuxiliaryPending
                    ? "SALVAGE CORE SYNCED — CHOOSE AN AUXILIARY OVERCLOCK"
                    : $"SALVAGE CHAIN x{m_chain.Count}{rewardText}  {m_model.Salvage}/{RunModel.SalvageRequired}");
            }
        }
    }
}
