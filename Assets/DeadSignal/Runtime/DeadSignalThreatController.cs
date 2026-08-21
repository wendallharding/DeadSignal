using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Owns enemy state, projectile lifetime, and combat interactions for one run.
    /// </summary>
    internal sealed class DeadSignalThreatController
    {
        public const float SAPPER_PULSE_INTERVAL = 1.35f;

        private const float WARDEN_COLLISION_RADIUS = 0.54f;
        private const float SAPPER_COLLISION_RADIUS = 0.42f;
        private const float SAPPER_LATCH_DISTANCE = 1.25f;
        private const float SAPPER_FIRST_PULSE_DELAY = 1.6f;

        private readonly RunModel m_model;
        private readonly RunMetrics m_metrics;
        private readonly DeadSignalWorld m_world;
        private readonly ICombatFeedback m_combatFeedback;
        private readonly IDeadSignalAudio m_audio;
        private readonly Action<string> m_showFeedback;
        private readonly List<Projectile> m_projectiles = new();

        private float m_wardenHealth = 3f;
        private float m_sapperHealth = 2f;
        private float m_wardenAttackCooldown;
        private float m_sapperPulseCooldown;
        private float m_shotCooldown;
        private bool m_sapperLatched;

        public DeadSignalThreatController(
            RunModel model,
            RunMetrics metrics,
            DeadSignalWorld world,
            ICombatFeedback combatFeedback,
            IDeadSignalAudio audio,
            Action<string> showFeedback)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_combatFeedback = combatFeedback;
            m_audio = audio;
            m_showFeedback = showFeedback;
        }

        public bool IsSapperLatched => m_sapperLatched;
        public bool IsSapperAlive => m_sapperHealth > 0f;
        public float SapperPulseCooldown => m_sapperPulseCooldown;
        public bool CanFire => m_shotCooldown <= 0f;

        public void TickCooldown(float dt)
        {
            m_shotCooldown = Mathf.Max(0f, m_shotCooldown - dt);
        }

        public void Tick(float dt)
        {
            _tickWarden(dt);
            _tickSapper(dt);
            _tickProjectiles(dt);
        }

        public void TryFire(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = m_world.Player.forward;
            }

            if (!m_model.TrySpend(RunModel.ShotCost))
            {
                m_showFeedback("INSUFFICIENT SIGNAL");
                return;
            }

            m_shotCooldown = 0.16f;
            m_metrics.RecordShot();
            m_audio.Play(DeadSignalAudioCue.Fire);
            var shot = m_world.CreateSignalBolt(direction);
            m_projectiles.Add(new Projectile(shot, direction.normalized));
        }

        private void _tickWarden(float dt)
        {
            if (!m_model.TowerOnline || m_wardenHealth <= 0f)
            {
                return;
            }

            m_wardenAttackCooldown = Mathf.Max(0f, m_wardenAttackCooldown - dt);
            var delta = m_world.Player.position - m_world.Warden.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > 0.05f)
            {
                m_world.Warden.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }

            if (distance > 1.05f)
            {
                var desired = m_world.Warden.position + delta.normalized * (2.15f * dt);
                m_world.Warden.position = m_world.ResolveMovement(
                    m_world.Warden.position,
                    desired,
                    WARDEN_COLLISION_RADIUS,
                    m_model.ShortcutOpen);
            }
            else if (m_wardenAttackCooldown <= 0f)
            {
                m_wardenAttackCooldown = 0.9f;
                m_model.TakeSecurityHit();
                m_metrics.RecordSecurityHit();
                m_combatFeedback.PlaySecurityImpact(m_world.Player.position + Vector3.up * 0.58f);
                m_audio.Play(DeadSignalAudioCue.SecurityImpact);
                m_showFeedback("SECURITY IMPACT  −18 SIGNAL");
            }
        }

        private void _tickSapper(float dt)
        {
            if (!m_model.TowerOnline || m_sapperHealth <= 0f)
            {
                return;
            }

            m_world.SapperTelegraph.SetThreatState(true, m_sapperLatched, m_sapperPulseCooldown, SAPPER_PULSE_INTERVAL);
            m_world.SapperCore.Rotate(Vector3.up, (m_sapperLatched ? 260f : 120f) * dt, Space.Self);
            if (!m_sapperLatched)
            {
                var delta = m_world.TowerPosition - m_world.Sapper.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance > 0.05f)
                {
                    m_world.Sapper.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                }

                if (distance > SAPPER_LATCH_DISTANCE)
                {
                    var desired = m_world.Sapper.position + delta.normalized * (1.8f * dt);
                    m_world.Sapper.position = m_world.ResolveMovement(
                        m_world.Sapper.position,
                        desired,
                        SAPPER_COLLISION_RADIUS,
                        m_model.ShortcutOpen);
                    return;
                }

                m_sapperLatched = true;
                m_sapperPulseCooldown = SAPPER_FIRST_PULSE_DELAY;
                m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, SAPPER_PULSE_INTERVAL);
                m_showFeedback("SAPPER LATCHED - PURGE IT");
            }

            m_sapperPulseCooldown = Mathf.Max(0f, m_sapperPulseCooldown - dt);
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, SAPPER_PULSE_INTERVAL);
            float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.18f;
            m_world.SapperCore.localScale = new Vector3(0.42f * pulse, 0.1f, 0.42f * pulse);
            if (m_sapperPulseCooldown > 0f)
            {
                return;
            }

            m_sapperPulseCooldown = SAPPER_PULSE_INTERVAL;
            m_model.TakeSapperPulse();
            m_metrics.RecordSapperPulse();
            m_combatFeedback.PlaySapperImpact(m_world.TowerPosition + Vector3.up * 0.65f);
            m_audio.Play(DeadSignalAudioCue.SapperPulse);
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, SAPPER_PULSE_INTERVAL);
            m_world.SapperTelegraph.NotifyPulse();
            m_showFeedback($"SAPPER DRAIN  -{RunModel.SapperPulseCost:0} SIGNAL");
        }

        private void _tickProjectiles(float dt)
        {
            for (int index = m_projectiles.Count - 1; index >= 0; index--)
            {
                var shot = m_projectiles[index];
                shot.Life -= dt;
                shot.Visual.transform.position += shot.Direction * (13.5f * dt);

                bool hitWarden = m_world.Warden.gameObject.activeSelf && m_wardenHealth > 0f &&
                                  Vector3.SqrMagnitude(shot.Visual.transform.position - (m_world.Warden.position + Vector3.up * 0.3f)) < 0.9f;
                bool hitSapper = m_world.Sapper.gameObject.activeSelf && m_sapperHealth > 0f &&
                                  Vector3.SqrMagnitude(shot.Visual.transform.position - (m_world.Sapper.position + Vector3.up * 0.3f)) < 0.75f;
                if (hitWarden)
                {
                    _hitWarden();
                }

                if (hitSapper)
                {
                    _hitSapper();
                }

                if (hitWarden || hitSapper || shot.Life <= 0f)
                {
                    UnityEngine.Object.Destroy(shot.Visual);
                    m_projectiles.RemoveAt(index);
                }
            }
        }

        private void _hitWarden()
        {
            m_wardenHealth -= 1f;
            m_combatFeedback.PlaySignalImpact(m_world.Warden.position + Vector3.up * 0.65f, m_wardenHealth <= 0f);
            m_audio.Play(DeadSignalAudioCue.SignalImpact);
            if (m_wardenHealth <= 0f)
            {
                m_world.PurgeWarden();
                m_showFeedback("SECURITY NODE PURGED");
                return;
            }

            m_showFeedback("SECURITY ARMOR HIT");
        }

        private void _hitSapper()
        {
            m_sapperHealth -= 1f;
            m_combatFeedback.PlaySignalImpact(m_world.Sapper.position + Vector3.up * 0.58f, m_sapperHealth <= 0f);
            m_audio.Play(DeadSignalAudioCue.SignalImpact);
            if (m_sapperHealth <= 0f)
            {
                m_world.PurgeSapper();
                m_showFeedback("SIGNAL SAPPER PURGED");
                return;
            }

            m_showFeedback("SAPPER SHELL HIT");
        }

        private sealed class Projectile
        {
            public Projectile(GameObject visual, Vector3 direction)
            {
                Visual = visual;
                Direction = direction;
                Life = 1.5f;
            }

            public GameObject Visual { get; }
            public Vector3 Direction { get; }
            public float Life { get; set; }
        }
    }
}
