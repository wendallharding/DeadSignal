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
        private const float WARDEN_COLLISION_RADIUS = 0.54f;
        private const float SAPPER_COLLISION_RADIUS = 0.42f;

        private static readonly float s_wardenProjectileHitRadius = Mathf.Sqrt(0.9f);
        private static readonly float s_sapperProjectileHitRadius = Mathf.Sqrt(0.75f);

        private readonly RunModel m_model;
        private readonly RunMetrics m_metrics;
        private readonly DeadSignalWorld m_world;
        private readonly ICombatFeedback m_combatFeedback;
        private readonly IDeadSignalAudio m_audio;
        private readonly SignalBoltPresentationTuning m_projectileTuning;
        private readonly ThreatBalanceTuning m_tuning;
        private readonly Action<string> m_showFeedback;
        private readonly List<Projectile> m_projectiles = new();

        private float m_wardenHealth;
        private float m_sapperHealth;
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
            SignalBoltPresentationTuning projectileTuning,
            ThreatBalanceTuning tuning,
            Action<string> showFeedback)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_combatFeedback = combatFeedback;
            m_audio = audio;
            m_projectileTuning = projectileTuning;
            m_tuning = tuning;
            m_showFeedback = showFeedback;
            m_wardenHealth = tuning.WardenHealth;
            m_sapperHealth = tuning.SapperHealth;
        }

        public bool IsSapperLatched => m_sapperLatched;
        public bool IsSapperAlive => m_sapperHealth > 0f;
        public bool IsWardenAlive => m_wardenHealth > 0f;
        public float WardenHealth => m_wardenHealth;
        public float WardenMaximumHealth => m_tuning.WardenHealth;
        public float SapperMaximumHealth => m_tuning.SapperHealth;
        public float WardenSignalReward => m_tuning.WardenSignalReward;
        public float SapperSignalReward => m_tuning.SapperSignalReward;
        public float SapperPulseInterval => m_tuning.SapperPulseInterval;
        public float SapperHealth => m_sapperHealth;
        public bool LastShotBlockedByEnvironment { get; private set; }
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

            m_shotCooldown = m_projectileTuning.FireCooldown;
            m_metrics.RecordShot();
            m_audio.Play(DeadSignalAudioCue.Fire);
            LastShotBlockedByEnvironment = false;
            var shot = m_world.CreateSignalBolt(direction);
            m_projectiles.Add(new Projectile(shot, direction.normalized, m_projectileTuning.Lifetime));
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
            var distance = delta.magnitude;
            if (distance > 0.05f)
            {
                m_world.Warden.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }

            if (distance > m_tuning.WardenAttackDistance)
            {
                var desired = m_world.Warden.position + delta.normalized * (m_tuning.WardenSpeed * dt);
                m_world.Warden.position = m_world.ResolveMovement(
                    m_world.Warden.position,
                    desired,
                    WARDEN_COLLISION_RADIUS,
                    m_model.ShortcutOpen);
            }
            else if (m_wardenAttackCooldown <= 0f)
            {
                m_wardenAttackCooldown = m_tuning.WardenAttackCooldown;
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

            m_world.SapperTelegraph.SetThreatState(true, m_sapperLatched, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            m_world.SapperCore.Rotate(Vector3.up, (m_sapperLatched ? 260f : 120f) * dt, Space.Self);
            if (!m_sapperLatched)
            {
                var delta = m_world.TowerPosition - m_world.Sapper.position;
                delta.y = 0f;
                var distance = delta.magnitude;
                if (distance > 0.05f)
                {
                    m_world.Sapper.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                }

                if (distance > m_tuning.SapperLatchDistance)
                {
                    var desired = m_world.Sapper.position + delta.normalized * (m_tuning.SapperSpeed * dt);
                    m_world.Sapper.position = m_world.ResolveMovement(
                        m_world.Sapper.position,
                        desired,
                        SAPPER_COLLISION_RADIUS,
                        m_model.ShortcutOpen);
                    return;
                }

                m_sapperLatched = true;
                m_sapperPulseCooldown = m_tuning.SapperFirstPulseDelay;
                m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
                m_showFeedback("SAPPER LATCHED - PURGE IT");
            }

            m_sapperPulseCooldown = Mathf.Max(0f, m_sapperPulseCooldown - dt);
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            var pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.18f;
            m_world.SapperCore.localScale = Vector3.Scale(
                m_world.SapperCoreBaseScale,
                new Vector3(pulse, 1f, pulse));
            if (m_sapperPulseCooldown > 0f)
            {
                return;
            }

            m_sapperPulseCooldown = m_tuning.SapperPulseInterval;
            m_model.TakeSapperPulse();
            m_metrics.RecordSapperPulse();
            m_combatFeedback.PlaySapperImpact(m_world.TowerPosition + Vector3.up * 0.65f);
            m_audio.Play(DeadSignalAudioCue.SapperPulse);
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            m_world.SapperTelegraph.NotifyPulse();
            m_showFeedback($"SAPPER DRAIN  -{RunModel.SapperPulseCost:0} SIGNAL");
        }

        private void _tickProjectiles(float dt)
        {
            for (int index = m_projectiles.Count - 1; index >= 0; index--)
            {
                var shot = m_projectiles[index];
                shot.Life -= dt;
                var start = shot.Visual.transform.position;
                var end = start + shot.Direction * (m_projectileTuning.Speed * dt);
                var hitWarden = _tryGetThreatHitFraction(
                    start, end, m_world.Warden, m_wardenHealth, s_wardenProjectileHitRadius, out var wardenHitFraction);
                var hitSapper = _tryGetThreatHitFraction(
                    start, end, m_world.Sapper, m_sapperHealth, s_sapperProjectileHitRadius, out var sapperHitFraction);
                var hitObstacle = m_world.TryGetProjectileObstacleHit(
                    start,
                    end,
                    m_projectileTuning.CollisionRadius,
                    m_model.ShortcutOpen,
                    out var obstacleHitFraction);
                var nearestThreatFraction = Mathf.Min(
                    hitWarden ? wardenHitFraction : float.PositiveInfinity,
                    hitSapper ? sapperHitFraction : float.PositiveInfinity);
                if (hitObstacle && obstacleHitFraction < nearestThreatFraction)
                {
                    LastShotBlockedByEnvironment = true;
                    shot.Visual.transform.position = Vector3.Lerp(start, end, obstacleHitFraction);
                    m_combatFeedback.PlayEnvironmentImpact(shot.Visual.transform.position + Vector3.up * 0.03f);
                    UnityEngine.Object.Destroy(shot.Visual);
                    m_projectiles.RemoveAt(index);
                    continue;
                }

                shot.Visual.transform.position = end;
                if (hitWarden && hitSapper)
                {
                    hitWarden = wardenHitFraction <= sapperHitFraction;
                    hitSapper = !hitWarden;
                }
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

        private static bool _tryGetThreatHitFraction(
            Vector3 start,
            Vector3 end,
            Transform threat,
            float health,
            float radius,
            out float hitFraction)
        {
            hitFraction = float.PositiveInfinity;
            if (!threat.gameObject.activeSelf || health <= 0f)
            {
                return false;
            }

            var center = threat.position + Vector3.up * 0.3f;
            if (ProjectileCollision.TryGetCircleHitFraction(start, end, center, radius, out hitFraction))
            {
                return true;
            }

            if (Vector3.SqrMagnitude(end - center) >= radius * radius)
            {
                return false;
            }

            hitFraction = 1f;
            return true;
        }

        private void _hitWarden()
        {
            m_wardenHealth -= 1f;
            m_combatFeedback.PlaySignalImpact(m_world.Warden.position + Vector3.up * 0.65f, m_wardenHealth <= 0f);
            m_audio.Play(DeadSignalAudioCue.SignalImpact);
            if (m_wardenHealth <= 0f)
            {
                var restored = m_model.RestoreSignal(m_tuning.WardenSignalReward);
                m_metrics.RecordThreatPurge(restored);
                m_world.PurgeWarden();
                m_combatFeedback.PlaySignalRecovery(m_world.Warden.position + Vector3.up * 0.55f);
                m_showFeedback($"WARDEN PURGED  +{restored:0} SIGNAL");
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
                var restored = m_model.RestoreSignal(m_tuning.SapperSignalReward);
                m_metrics.RecordThreatPurge(restored);
                m_world.PurgeSapper();
                m_combatFeedback.PlaySignalRecovery(m_world.Sapper.position + Vector3.up * 0.55f);
                m_showFeedback($"SAPPER PURGED  +{restored:0} SIGNAL");
                return;
            }

            m_showFeedback("SAPPER SHELL HIT");
        }

        private sealed class Projectile
        {
            public Projectile(GameObject visual, Vector3 direction, float lifetime)
            {
                Visual = visual;
                Direction = direction;
                Life = lifetime;
            }

            public GameObject Visual { get; }
            public Vector3 Direction { get; }
            public float Life { get; set; }
        }
    }
}
