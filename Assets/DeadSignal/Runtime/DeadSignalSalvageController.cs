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

        public DeadSignalSalvageController(
            RunModel model,
            DeadSignalWorld world,
            IDeadSignalAudio audio,
            SalvagePresentationTuning tuning,
            Action<string> showFeedback)
        {
            m_model = model;
            m_world = world;
            m_audio = audio;
            m_tuning = tuning;
            m_showFeedback = showFeedback;
        }

        public void Tick(float dt)
        {
            foreach (var pickup in m_world.SalvagePickups)
            {
                if (!pickup.activeSelf)
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
                m_audio.Play(DeadSignalAudioCue.Salvage);
                m_showFeedback($"SALVAGE SECURED  {m_model.Salvage}/{RunModel.SalvageRequired}");
            }
        }
    }
}
