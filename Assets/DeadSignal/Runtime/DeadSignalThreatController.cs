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
        private const float INTERCEPTOR_COLLISION_RADIUS = 0.46f;
        private const float SUPPRESSOR_COLLISION_RADIUS = 0.48f;

        private static readonly float s_wardenProjectileHitRadius = Mathf.Sqrt(0.9f);
        private static readonly float s_sapperProjectileHitRadius = Mathf.Sqrt(0.75f);
        private static readonly float s_interceptorProjectileHitRadius = Mathf.Sqrt(0.78f);
        private static readonly float s_suppressorProjectileHitRadius = Mathf.Sqrt(0.82f);

        private readonly RunModel m_model;
        private readonly RunMetrics m_metrics;
        private readonly DeadSignalWorld m_world;
        private readonly ICombatFeedback m_combatFeedback;
        private readonly IDeadSignalAudio m_audio;
        private readonly SignalBoltPresentationTuning m_projectileTuning;
        private readonly ThreatBalanceTuning m_tuning;
        private readonly SignalOverclockChoice m_overclockChoice;
        private readonly SignalOverclockTuning m_overclockTuning;
        private readonly Action<string> m_showFeedback;
        private readonly Func<float> m_rewardExtractionPurge;
        private readonly SecurityEscalationDirector m_director;
        private readonly List<Projectile> m_projectiles = new();

        private float m_wardenHealth;
        private float m_sapperHealth;
        private float m_interceptorHealth;
        private float m_suppressorHealth;
        private float m_wardenAttackCooldown;
        private float m_sapperPulseCooldown;
        private float m_shotCooldown;
        private bool m_sapperLatched;
        private float m_interceptorChargeCountdown;
        private float m_interceptorDashRemaining;
        private float m_interceptorHitCooldown;
        private Vector3 m_interceptorDashDirection;
        private Vector3 m_interceptorDashTarget;
        private Vector3 m_interceptorCutoffTarget;
        private bool m_extractionPressure;
        private float m_suppressorWarningCountdown;
        private float m_suppressorFieldCountdown;
        private float m_suppressorFieldCooldown;
        private float m_suppressorPulseCountdown;
        private Vector3 m_suppressorFieldCenter;

        public DeadSignalThreatController(
            RunModel model,
            RunMetrics metrics,
            DeadSignalWorld world,
            ICombatFeedback combatFeedback,
            IDeadSignalAudio audio,
            SignalBoltPresentationTuning projectileTuning,
            ThreatBalanceTuning tuning,
            SignalOverclockChoice overclockChoice,
            SignalOverclockTuning overclockTuning,
            Action<string> showFeedback,
            Func<float> rewardExtractionPurge)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_combatFeedback = combatFeedback;
            m_audio = audio;
            m_projectileTuning = projectileTuning;
            m_tuning = tuning;
            m_overclockChoice = overclockChoice;
            m_overclockTuning = overclockTuning;
            m_director = new SecurityEscalationDirector(
                tuning.ReinforcementEntryDelay,
                tuning.ReinforcementSafeDistance,
                UnityEngine.Random.Range(0, 2) == 1,
                tuning.DeadZoneTraceDuration);
            m_showFeedback = showFeedback;
            m_rewardExtractionPurge = rewardExtractionPurge;
            m_wardenHealth = tuning.WardenHealth;
            m_sapperHealth = tuning.SapperHealth;
        }

        public bool IsSapperLatched => m_sapperLatched;
        public bool IsSapperAlive => m_sapperHealth > 0f;
        public bool IsWardenAlive => m_wardenHealth > 0f;
        public bool IsInterceptorAlive => m_interceptorHealth > 0f;
        public bool IsSuppressorAlive => m_suppressorHealth > 0f;
        public float WardenHealth => m_wardenHealth;
        public float WardenMaximumHealth => m_tuning.WardenHealth;
        public float SapperMaximumHealth => m_tuning.SapperHealth;
        public float WardenSignalReward => m_tuning.WardenSignalReward;
        public float SapperSignalReward => m_tuning.SapperSignalReward;
        public float SapperPulseInterval => m_tuning.SapperPulseInterval;
        public float SapperHealth => m_sapperHealth;
        public float InterceptorHealth => m_interceptorHealth;
        public float InterceptorMaximumHealth => m_tuning.InterceptorHealth;
        public float InterceptorSignalReward => m_tuning.InterceptorSignalReward;
        public float SuppressorHealth => m_suppressorHealth;
        public float SuppressorMaximumHealth => m_tuning.SuppressorHealth;
        public float SuppressorSignalReward => m_tuning.SuppressorSignalReward;
        public bool IsInterceptorCharging => m_interceptorChargeCountdown > 0f;
        public Vector3 InterceptorCutoffTarget => m_interceptorCutoffTarget;
        public bool IsSuppressorFieldActive => m_suppressorFieldCountdown > 0f;
        public bool IsSuppressorFieldWarningActive => m_suppressorWarningCountdown > 0f;
        public Vector3 SuppressorFieldCenter => m_suppressorFieldCenter;
        public bool IsPlayerSuppressed => IsSuppressorFieldActive &&
                                          DeadSignalWorld.FlatDistance(m_world.Player.position, m_suppressorFieldCenter) <=
                                          m_tuning.SuppressorFieldRadius;
        public float PlayerMovementMultiplier => IsPlayerSuppressed ? m_tuning.SuppressorMovementMultiplier : 1f;
        public bool LastShotBlockedByEnvironment { get; private set; }
        public float SapperPulseCooldown => m_sapperPulseCooldown;
        public bool CanFire => m_shotCooldown <= 0f;
        public int EscalationTier => m_director.EscalationTier;
        public int ReinforcementsRemaining => m_director.ReinforcementsRemaining;
        public float ReinforcementEntryCountdown => m_director.EntryCountdown;
        public bool IsDeadZoneTraceActive => m_director.IsDeadZoneTraceActive;
        public float DeadZoneTraceSecondsRemaining => m_director.DeadZoneTraceSecondsRemaining;
        public SecurityReinforcement PendingReinforcement => m_director.PendingReinforcement;

        public void BeginExtractionPressure()
        {
            m_extractionPressure = true;
        }

        public void TickCooldown(float dt)
        {
            m_shotCooldown = Mathf.Max(0f, m_shotCooldown - dt);
        }

        public void Tick(float dt, bool playerPowered)
        {
            _tickDirector(dt, playerPowered);
            _tickInterceptor(dt);
            _tickSuppressor(dt);
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

        private void _tickDirector(float dt, bool playerPowered)
        {
            var traceWasCompleted = m_director.IsDeadZoneTraceCompleted;
            var reinforcement = m_director.Tick(
                dt,
                m_model.TowerOnline,
                playerPowered,
                m_model.Salvage,
                m_extractionPressure,
                IsInterceptorAlive,
                IsWardenAlive,
                IsSapperAlive,
                IsSuppressorAlive,
                m_world.GetSafestInterceptorEntryDistance(m_world.Player.position),
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.Warden.position),
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.Sapper.position),
                m_world.GetSafestInterceptorEntryDistance(m_world.Player.position));
            if (!traceWasCompleted && m_director.IsDeadZoneTraceCompleted)
            {
                m_showFeedback("SECURITY TRACE COMPLETE — INTERCEPTOR DISPATCHED");
            }

            if (reinforcement == SecurityReinforcement.Interceptor)
            {
                m_interceptorHealth = m_tuning.InterceptorHealth;
                m_interceptorChargeCountdown = 0f;
                m_interceptorDashRemaining = 0f;
                m_interceptorHitCooldown = 0f;
                m_world.DeployInterceptorReinforcement();
                m_showFeedback("FLANK GATES OPEN — INTERCEPTOR INBOUND");
            }
            else if (reinforcement == SecurityReinforcement.Warden)
            {
                m_wardenHealth = m_tuning.WardenHealth;
                m_wardenAttackCooldown = m_tuning.WardenAttackCooldown;
                m_world.DeployWardenReinforcement();
                m_showFeedback("SECURITY BAY OPEN — WARDEN REINFORCEMENT");
            }
            else if (reinforcement == SecurityReinforcement.Sapper)
            {
                m_sapperHealth = m_tuning.SapperHealth;
                m_sapperLatched = false;
                m_sapperPulseCooldown = 0f;
                m_world.DeploySapperReinforcement(m_tuning.SapperPulseInterval);
                m_showFeedback("SIPHON CRADLE OPEN — SAPPER REINFORCEMENT");
            }
            else if (reinforcement == SecurityReinforcement.Suppressor)
            {
                m_suppressorHealth = m_tuning.SuppressorHealth;
                m_suppressorFieldCountdown = 0f;
                m_suppressorFieldCooldown = 0f;
                m_suppressorPulseCountdown = 0f;
                m_world.DeploySuppressorReinforcement();
                m_showFeedback("FLANK GATES OPEN — SUPPRESSOR INBOUND");
                _beginSuppressorWarning(m_world.Player.position, "SUPPRESSION SWEEP LOCKED — LEAVE THE RING");
            }
        }

        private void _tickSuppressor(float dt)
        {
            if (!m_model.TowerOnline || m_suppressorHealth <= 0f)
            {
                return;
            }

            m_world.SuppressorCore.Rotate(Vector3.up, 220f * dt, Space.Self);
            if (m_suppressorFieldCountdown > 0f)
            {
                m_suppressorFieldCountdown = Mathf.Max(0f, m_suppressorFieldCountdown - dt);
                m_suppressorPulseCountdown = Mathf.Max(0f, m_suppressorPulseCountdown - dt);
                m_world.SetSuppressorFieldAt(true, true, m_tuning.SuppressorFieldRadius, m_suppressorFieldCenter);
                if (IsPlayerSuppressed && m_suppressorPulseCountdown <= 0f)
                {
                    m_suppressorPulseCountdown = m_tuning.SuppressorPulseInterval;
                    if (!_tryAbsorbThreatDamage("SUPPRESSION PULSE"))
                    {
                        m_model.TakeSuppressionPulse(m_tuning.SuppressorSignalDrain);
                        m_showFeedback($"SUPPRESSION FIELD  −{m_tuning.SuppressorSignalDrain:0} SIGNAL — BREAK OUT");
                    }
                }

                if (m_suppressorFieldCountdown <= 0f)
                {
                    m_suppressorFieldCooldown = m_tuning.SuppressorFieldCooldown;
                    m_world.SetSuppressorField(false, false, m_tuning.SuppressorFieldRadius);
                }

                return;
            }

            if (m_suppressorWarningCountdown > 0f)
            {
                m_suppressorWarningCountdown = Mathf.Max(0f, m_suppressorWarningCountdown - dt);
                m_world.SetSuppressorFieldAt(true, false, m_tuning.SuppressorFieldRadius, m_suppressorFieldCenter);
                if (m_suppressorWarningCountdown <= 0f)
                {
                    m_suppressorFieldCountdown = m_tuning.SuppressorFieldDuration;
                    m_suppressorPulseCountdown = 0f;
                }

                return;
            }

            m_suppressorFieldCooldown = Mathf.Max(0f, m_suppressorFieldCooldown - dt);
            var anchor = InterceptorTactics.CalculateCutoffPoint(
                m_world.Player.position,
                m_world.ExtractionPosition,
                0.5f);
            var delta = anchor - m_world.Suppressor.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                m_world.Suppressor.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }

            if (delta.magnitude > m_tuning.SuppressorAnchorDistance)
            {
                var desired = m_world.Suppressor.position + delta.normalized * (m_tuning.SuppressorApproachSpeed * dt);
                m_world.Suppressor.position = m_world.ResolveMovement(
                    m_world.Suppressor.position,
                    desired,
                    SUPPRESSOR_COLLISION_RADIUS,
                    m_model.ShortcutOpen);
                return;
            }

            if (m_suppressorFieldCooldown <= 0f)
            {
                _beginSuppressorWarning(m_world.Suppressor.position, "SUPPRESSION FIELD PRIMING — LEAVE THE RING");
            }
        }

        private void _beginSuppressorWarning(Vector3 center, string feedback)
        {
            center.y = 0f;
            m_suppressorFieldCenter = center;
            m_suppressorWarningCountdown = m_tuning.SuppressorWarningDuration;
            m_world.SetSuppressorFieldAt(true, false, m_tuning.SuppressorFieldRadius, m_suppressorFieldCenter);
            m_showFeedback(feedback);
        }

        private void _tickInterceptor(float dt)
        {
            if (!m_model.TowerOnline || m_interceptorHealth <= 0f)
            {
                return;
            }

            m_interceptorHitCooldown = Mathf.Max(0f, m_interceptorHitCooldown - dt);
            m_world.InterceptorCore.Rotate(Vector3.up, 320f * dt, Space.Self);
            if (m_interceptorDashRemaining > 0f)
            {
                m_interceptorDashRemaining = Mathf.Max(0f, m_interceptorDashRemaining - dt);
                var desired = m_world.Interceptor.position + m_interceptorDashDirection * (m_tuning.InterceptorDashSpeed * dt);
                m_world.Interceptor.position = m_world.ResolveMovement(
                    m_world.Interceptor.position,
                    desired,
                    INTERCEPTOR_COLLISION_RADIUS,
                    m_model.ShortcutOpen);
                _tryApplyInterceptorHit();
                return;
            }

            if (m_interceptorChargeCountdown > 0f)
            {
                m_interceptorChargeCountdown = Mathf.Max(0f, m_interceptorChargeCountdown - dt);
                m_world.SetInterceptorTelegraph(true, m_interceptorDashTarget);
                _faceInterceptor(m_interceptorDashTarget);
                if (m_interceptorChargeCountdown <= 0f)
                {
                    var dashDelta = m_interceptorDashTarget - m_world.Interceptor.position;
                    dashDelta.y = 0f;
                    m_interceptorDashDirection = dashDelta.sqrMagnitude > 0.01f ? dashDelta.normalized : m_world.Interceptor.forward;
                    m_interceptorDashRemaining = m_tuning.InterceptorDashDuration;
                    m_world.SetInterceptorTelegraph(false, m_interceptorDashTarget);
                }

                return;
            }

            m_interceptorCutoffTarget = _calculateInterceptorCutoffTarget();
            var delta = m_interceptorCutoffTarget - m_world.Interceptor.position;
            delta.y = 0f;
            _faceInterceptor(m_interceptorCutoffTarget);
            if (delta.magnitude > m_tuning.InterceptorChargeDistance)
            {
                var desired = m_world.Interceptor.position + delta.normalized * (m_tuning.InterceptorApproachSpeed * dt);
                m_world.Interceptor.position = m_world.ResolveMovement(
                    m_world.Interceptor.position,
                    desired,
                    INTERCEPTOR_COLLISION_RADIUS,
                    m_model.ShortcutOpen);
                return;
            }

            m_interceptorDashTarget = m_world.Player.position;
            m_interceptorChargeCountdown = m_tuning.InterceptorChargeDuration;
            m_world.SetInterceptorTelegraph(true, m_interceptorDashTarget);
            m_showFeedback("INTERCEPTOR LOCK — BREAK THE LINE");
        }

        private Vector3 _calculateInterceptorCutoffTarget()
        {
            if (m_suppressorHealth > 0f &&
                (m_suppressorWarningCountdown > 0f || (m_suppressorFieldCountdown > 0f && IsPlayerSuppressed)))
            {
                return InterceptorTactics.CalculateSuppressionExitPoint(
                    m_suppressorFieldCenter,
                    m_world.Player.position,
                    m_world.Interceptor.position,
                    m_tuning.SuppressorFieldRadius,
                    m_tuning.InterceptorSuppressionExitMargin);
            }

            return InterceptorTactics.CalculateCutoffPoint(
                m_world.Player.position,
                m_world.ExtractionPosition,
                m_tuning.InterceptorCutoffFraction);
        }

        private void _faceInterceptor(Vector3 target)
        {
            var delta = target - m_world.Interceptor.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                m_world.Interceptor.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }
        }

        private void _tryApplyInterceptorHit()
        {
            if (m_interceptorHitCooldown > 0f ||
                DeadSignalWorld.FlatDistance(m_world.Interceptor.position, m_world.Player.position) > m_tuning.InterceptorHitDistance)
            {
                return;
            }

            m_interceptorHitCooldown = m_tuning.InterceptorHitCooldown;
            if (_tryAbsorbThreatDamage("INTERCEPTOR IMPACT"))
            {
                return;
            }

            m_model.TakeSecurityHit();
            m_metrics.RecordSecurityHit();
            m_combatFeedback.PlaySecurityImpact(m_world.Player.position + Vector3.up * 0.58f);
            m_audio.Play(DeadSignalAudioCue.SecurityImpact);
            m_showFeedback($"INTERCEPTOR IMPACT  −{RunModel.SecurityHitCost:0} SIGNAL");
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
                if (_tryAbsorbThreatDamage("WARDEN IMPACT"))
                {
                    return;
                }

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
            m_combatFeedback.PlaySapperImpact(m_world.TowerPosition + Vector3.up * 0.65f);
            m_audio.Play(DeadSignalAudioCue.SapperPulse);
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            m_world.SapperTelegraph.NotifyPulse();
            if (!_tryAbsorbThreatDamage("SAPPER PULSE"))
            {
                m_model.TakeSapperPulse();
                m_metrics.RecordSapperPulse();
                m_showFeedback($"SAPPER DRAIN  -{RunModel.SapperPulseCost:0} SIGNAL");
            }
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
                var hitInterceptor = _tryGetThreatHitFraction(
                    start,
                    end,
                    m_world.Interceptor,
                    m_interceptorHealth,
                    s_interceptorProjectileHitRadius,
                    out var interceptorHitFraction);
                var hitSuppressor = _tryGetThreatHitFraction(
                    start,
                    end,
                    m_world.Suppressor,
                    m_suppressorHealth,
                    s_suppressorProjectileHitRadius,
                    out var suppressorHitFraction);
                var hitObstacle = m_world.TryGetProjectileObstacleHit(
                    start,
                    end,
                    m_projectileTuning.CollisionRadius,
                    m_model.ShortcutOpen,
                    out var obstacleHitFraction);
                var nearestThreatFraction = Mathf.Min(
                    Mathf.Min(
                        hitWarden ? wardenHitFraction : float.PositiveInfinity,
                        hitSapper ? sapperHitFraction : float.PositiveInfinity),
                    Mathf.Min(
                        hitInterceptor ? interceptorHitFraction : float.PositiveInfinity,
                        hitSuppressor ? suppressorHitFraction : float.PositiveInfinity));
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
                if (hitWarden && wardenHitFraction <= sapperHitFraction && wardenHitFraction <= interceptorHitFraction &&
                    wardenHitFraction <= suppressorHitFraction)
                {
                    var hitPosition = m_world.Warden.position + Vector3.up * 0.55f;
                    _hitWarden();
                    _tryChainArc(ThreatTarget.Warden, hitPosition);
                }
                else if (hitSapper && sapperHitFraction <= interceptorHitFraction && sapperHitFraction <= suppressorHitFraction)
                {
                    var hitPosition = m_world.Sapper.position + Vector3.up * 0.5f;
                    _hitSapper();
                    _tryChainArc(ThreatTarget.Sapper, hitPosition);
                }
                else if (hitInterceptor && interceptorHitFraction <= suppressorHitFraction)
                {
                    var hitPosition = m_world.Interceptor.position + Vector3.up * 0.5f;
                    _hitInterceptor();
                    _tryChainArc(ThreatTarget.Interceptor, hitPosition);
                }
                else if (hitSuppressor)
                {
                    var hitPosition = m_world.Suppressor.position + Vector3.up * 0.5f;
                    _hitSuppressor();
                    _tryChainArc(ThreatTarget.Suppressor, hitPosition);
                }

                if (hitWarden || hitSapper || hitInterceptor || hitSuppressor || shot.Life <= 0f)
                {
                    UnityEngine.Object.Destroy(shot.Visual);
                    m_projectiles.RemoveAt(index);
                }
            }
        }

        private void _tryChainArc(ThreatTarget source, Vector3 start)
        {
            if (m_overclockChoice.Selected != SignalOverclock.ChainArc)
            {
                return;
            }

            var target = ThreatTarget.None;
            var nearestDistance = m_overclockTuning.ChainArcRadius;
            _considerChainTarget(ThreatTarget.Warden, source, m_world.Warden, m_wardenHealth, start, ref target, ref nearestDistance);
            _considerChainTarget(ThreatTarget.Sapper, source, m_world.Sapper, m_sapperHealth, start, ref target, ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Interceptor, source, m_world.Interceptor, m_interceptorHealth, start, ref target, ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Suppressor, source, m_world.Suppressor, m_suppressorHealth, start, ref target, ref nearestDistance);
            if (target == ThreatTarget.None)
            {
                return;
            }

            var targetPosition = _getThreatPosition(target) + Vector3.up * 0.5f;
            m_combatFeedback.PlayChainArc(start, targetPosition);
            switch (target)
            {
                case ThreatTarget.Warden:
                    _hitWarden();
                    break;
                case ThreatTarget.Sapper:
                    _hitSapper();
                    break;
                case ThreatTarget.Interceptor:
                    _hitInterceptor();
                    break;
                case ThreatTarget.Suppressor:
                    _hitSuppressor();
                    break;
            }
        }

        private static void _considerChainTarget(
            ThreatTarget candidate,
            ThreatTarget source,
            Transform transform,
            float health,
            Vector3 start,
            ref ThreatTarget target,
            ref float nearestDistance)
        {
            if (candidate == source || health <= 0f || !transform.gameObject.activeSelf)
            {
                return;
            }

            var distance = DeadSignalWorld.FlatDistance(start, transform.position);
            if (distance > nearestDistance)
            {
                return;
            }

            nearestDistance = distance;
            target = candidate;
        }

        private Vector3 _getThreatPosition(ThreatTarget target)
        {
            return target switch
            {
                ThreatTarget.Warden => m_world.Warden.position,
                ThreatTarget.Sapper => m_world.Sapper.position,
                ThreatTarget.Interceptor => m_world.Interceptor.position,
                ThreatTarget.Suppressor => m_world.Suppressor.position,
                _ => Vector3.zero
            };
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
                m_showFeedback($"WARDEN PURGED  +{restored:0} SIGNAL{_purgeRewardText()}");
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
                m_showFeedback($"SAPPER PURGED  +{restored:0} SIGNAL{_purgeRewardText()}");
                return;
            }

            m_showFeedback("SAPPER SHELL HIT");
        }

        private void _hitInterceptor()
        {
            m_interceptorHealth -= 1f;
            m_combatFeedback.PlaySignalImpact(m_world.Interceptor.position + Vector3.up * 0.5f, m_interceptorHealth <= 0f);
            m_audio.Play(DeadSignalAudioCue.SignalImpact);
            if (m_interceptorHealth <= 0f)
            {
                var restored = m_model.RestoreSignal(m_tuning.InterceptorSignalReward);
                m_metrics.RecordThreatPurge(restored);
                m_world.PurgeInterceptor();
                m_combatFeedback.PlaySignalRecovery(m_world.Interceptor.position + Vector3.up * 0.5f);
                m_showFeedback($"INTERCEPTOR PURGED  +{restored:0} SIGNAL{_purgeRewardText()}");
                return;
            }

            m_showFeedback("INTERCEPTOR ARMOR HIT");
        }

        private void _hitSuppressor()
        {
            m_suppressorHealth -= 1f;
            m_combatFeedback.PlaySignalImpact(m_world.Suppressor.position + Vector3.up * 0.5f, m_suppressorHealth <= 0f);
            m_audio.Play(DeadSignalAudioCue.SignalImpact);
            if (m_suppressorHealth <= 0f)
            {
                var restored = m_model.RestoreSignal(m_tuning.SuppressorSignalReward);
                m_metrics.RecordThreatPurge(restored);
                m_world.PurgeSuppressor();
                m_combatFeedback.PlaySignalRecovery(m_world.Suppressor.position + Vector3.up * 0.5f);
                m_showFeedback($"SUPPRESSOR PURGED  +{restored:0} SIGNAL{_purgeRewardText()}");
                return;
            }

            m_showFeedback("SUPPRESSOR ARMOR HIT");
        }

        private bool _tryAbsorbThreatDamage(string threatName)
        {
            if (!m_overclockChoice.TryAbsorbThreatDamage())
            {
                return false;
            }

            m_combatFeedback.PlaySignalRecovery(m_world.Player.position + Vector3.up * 0.58f);
            m_audio.Play(DeadSignalAudioCue.SignalImpact);
            m_showFeedback($"FEEDBACK SHIELD — {threatName} NEGATED  //  PURGE TO RECHARGE");
            return true;
        }

        private string _purgeRewardText()
        {
            var shieldText = m_overclockChoice.NotifyThreatPurged() ? "  //  SHIELD RECHARGED" : string.Empty;
            var uplinkAcceleration = m_rewardExtractionPurge();
            var uplinkText = uplinkAcceleration > 0f ? $"  //  UPLINK +{uplinkAcceleration:0.##} SEC" : string.Empty;
            return shieldText + uplinkText;
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

        private enum ThreatTarget
        {
            None,
            Warden,
            Sapper,
            Interceptor,
            Suppressor
        }
    }
}
