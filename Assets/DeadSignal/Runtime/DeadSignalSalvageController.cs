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

        public DeadSignalSalvageController(
            RunModel model,
            DeadSignalWorld world,
            IDeadSignalAudio audio,
            Action<string> showFeedback)
        {
            m_model = model;
            m_world = world;
            m_audio = audio;
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

                pickup.transform.Rotate(Vector3.up, 70f * dt, Space.World);
                float hover = 0.06f + Mathf.Sin(Time.time * 3f + pickup.transform.position.x) * 0.04f;
                var position = pickup.transform.position;
                position.y = hover;
                pickup.transform.position = position;

                if (DeadSignalWorld.FlatDistance(m_world.Player.position, pickup.transform.position) >= 0.85f)
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
