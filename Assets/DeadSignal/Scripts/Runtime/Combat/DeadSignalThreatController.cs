using System;
using System.Collections.Generic;
using UnityEngine;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using DeadSignal.World;

namespace DeadSignal.Combat
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
        private const float EARLY_SAPPER_GRACE_SECONDS = 3.5f;
        private const float SAPPER_HIT_INTERRUPT_SECONDS = 2.25f;

        private static readonly float s_wardenProjectileHitRadius = Mathf.Sqrt(0.9f);
        private static readonly float s_sapperProjectileHitRadius = Mathf.Sqrt(0.75f);
        private static readonly float s_interceptorProjectileHitRadius = Mathf.Sqrt(0.78f);
        private static readonly float s_suppressorProjectileHitRadius = Mathf.Sqrt(0.82f);

        private readonly RunModel m_model;
        private readonly RunMetrics m_metrics;
        private readonly DeadSignalWorld m_world;
        private readonly ICombatFeedback m_combatFeedback;
        private readonly IDirectionalDamageFeedback m_directionalDamageFeedback;
        private readonly IDeadSignalAudio m_audio;
        private readonly SignalBoltPresentationTuning m_projectileTuning;
        private readonly ThreatBalanceTuning m_tuning;
        private readonly SwarmerPressureTuning m_swarmerTuning;
        private readonly SignalOverclockChoice m_overclockChoice;
        private readonly SignalOverclockTuning m_overclockTuning;
        private readonly Action<string> m_showFeedback;
        private readonly Func<float> m_rewardExtractionPurge;
        private readonly SecurityEscalationDirector m_director;
        private readonly SwarmerPressurePopulation m_swarmers;
        private readonly List<Projectile> m_projectiles = new();

        private float m_wardenHealth;
        private float m_sapperHealth;
        private float m_interceptorHealth;
        private float m_suppressorHealth;
        private float m_wardenAttackCooldown;
        private bool m_wardenScreeningSapper;
        private Vector3 m_wardenTacticalTarget;
        private float m_sapperPulseCooldown;
        private float m_shotCooldown;
        private bool m_sapperLatched;
        private float m_interceptorChargeCountdown;
        private float m_interceptorDashRemaining;
        private float m_interceptorRecoveryCountdown;
        private float m_interceptorHitCooldown;
        private Vector3 m_interceptorDashDirection;
        private Vector3 m_interceptorDashTarget;
        private Vector3 m_interceptorCutoffTarget;
        private bool m_interceptorCuttingSapperFlank;
        private bool m_extractionPressure;
        private ExtractionUplinkMode m_extractionUplinkMode;
        private float m_suppressorWarningCountdown;
        private float m_suppressorFieldCountdown;
        private float m_suppressorFieldCooldown;
        private float m_suppressorPulseCountdown;
        private Vector3 m_suppressorFieldCenter;
        private int m_pendingEntryIndex = -1;
        private bool m_debugFrozen;
        private bool m_debugPlayerInvulnerable;
        private bool m_debugScenarioActive;
        private int m_debugScenarioAttackMask;
        private AuthoredCombatScenario m_debugScenario;
        private AuthoredCombatScenario m_combatChamberScenario;
        private bool m_combatChamberActive;
        private int m_combatChamberPhase;

        public DeadSignalThreatController(
            RunModel model,
            RunMetrics metrics,
            DeadSignalWorld world,
            ICombatFeedback combatFeedback,
            IDirectionalDamageFeedback directionalDamageFeedback,
            IDeadSignalAudio audio,
            SignalBoltPresentationTuning projectileTuning,
            ThreatBalanceTuning tuning,
            SwarmerPressureTuning swarmerTuning,
            SignalOverclockChoice overclockChoice,
            SignalOverclockTuning overclockTuning,
            Action<string> showFeedback,
            Func<float> rewardExtractionPurge)
        {
            m_model = model;
            m_metrics = metrics;
            m_world = world;
            m_combatFeedback = combatFeedback;
            m_directionalDamageFeedback = directionalDamageFeedback;
            m_audio = audio;
            m_projectileTuning = projectileTuning;
            m_tuning = tuning;
            m_swarmerTuning = swarmerTuning;
            m_overclockChoice = overclockChoice;
            m_overclockTuning = overclockTuning;
            m_director = new SecurityEscalationDirector(
                tuning.ReinforcementEntryDelay,
                tuning.ReinforcementSafeDistance,
                UnityEngine.Random.Range(0, 2) == 1,
                tuning.DeadZoneTraceDuration,
                tuning.DeadZoneTraceRecoveryRate);
            m_swarmers = new SwarmerPressurePopulation(world, swarmerTuning, _applySwarmerContact, _purgeSwarmer);
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
        public bool IsWardenScreeningSapper => m_wardenScreeningSapper;
        public Vector3 WardenTacticalTarget => m_wardenTacticalTarget;
        public float SapperSignalReward => m_tuning.SapperSignalReward;
        public float SapperPulseInterval => m_tuning.SapperPulseInterval;
        public float SapperSpeed => m_tuning.SapperSpeed;
        public float SignalBoltSpeed => m_projectileTuning.Speed;
        public float SapperHealth => m_sapperHealth;
        public float InterceptorHealth => m_interceptorHealth;
        public float InterceptorMaximumHealth => m_tuning.InterceptorHealth;
        public float InterceptorSignalReward => m_tuning.InterceptorSignalReward;
        public float SuppressorHealth => m_suppressorHealth;
        public float SuppressorMaximumHealth => m_tuning.SuppressorHealth;
        public float SuppressorSignalReward => m_tuning.SuppressorSignalReward;
        public bool IsInterceptorCharging => m_interceptorChargeCountdown > 0f;
        public bool IsInterceptorRecovering => m_interceptorRecoveryCountdown > 0f;
        public float InterceptorRecoverySecondsRemaining => m_interceptorRecoveryCountdown;
        public Vector3 InterceptorCutoffTarget => m_interceptorCutoffTarget;
        public bool IsInterceptorCuttingSapperFlank => m_interceptorCuttingSapperFlank;
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
        public bool IsReinforcementEntryBlocked => m_director.IsEntryBlocked;
        public bool IsDeadZoneTraceActive => m_director.IsDeadZoneTraceActive;
        public bool IsDeadZoneTraceCooling => m_director.IsDeadZoneTraceCooling;
        public float DeadZoneTraceSecondsRemaining => m_director.DeadZoneTraceSecondsRemaining;
        public SecurityReinforcement PendingReinforcement => m_director.PendingReinforcement;
        public ExtractionSuppressionProfile CurrentExtractionSuppressionProfile { get; private set; }
        public int PiercingPulseFollowThroughs { get; private set; }
        public int ControlledRicochets { get; private set; }
        public bool HasSwarmerAssets => m_swarmers.HasAssets;
        public int ActiveSwarmerCount => m_swarmers.ActiveCount;
        public int PeakSwarmerCount => m_swarmers.PeakActiveCount;
        public int SwarmersSpawned => m_swarmers.SpawnedCount;
        public int SwarmersPurged => m_swarmers.PurgedCount;
        public int SwarmerContacts => m_swarmers.ContactCount;
        public IReadOnlyList<Transform> ActiveSwarmers => m_swarmers.ActiveTransforms;
        public bool CanBeginCombatChamber => !IsWardenAlive && !IsSapperAlive && !IsInterceptorAlive &&
                                             !IsSuppressorAlive && m_swarmers.ActiveCount == 0;
        public int DebugScenarioAttackCount
        {
            get
            {
                var count = 0;
                for (var bit = 1; bit <= 16; bit <<= 1)
                {
                    if ((m_debugScenarioAttackMask & bit) != 0)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public void BeginExtractionPressure(ExtractionUplinkMode mode)
        {
            m_extractionPressure = true;
            m_extractionUplinkMode = mode;
            CurrentExtractionSuppressionProfile = InterceptorTactics.ResolveExtractionSuppressionProfile(
                m_model.OptionalSalvageSecured,
                m_overclockChoice.SelectedWeapon);
            if (IsSuppressorAlive && CurrentExtractionSuppressionProfile != ExtractionSuppressionProfile.Standard)
            {
                _beginSuppressorWarning(_calculateExtractionSuppressionCenter(), _extractionSuppressionWarning());
            }
        }

        public void BeginPoweredWithdrawalPursuit(PoweredWithdrawalPhase phase)
        {
            switch (phase)
            {
                case PoweredWithdrawalPhase.WardenBay:
                    m_wardenHealth = m_tuning.WardenHealth;
                    m_wardenAttackCooldown = m_tuning.WardenAttackCooldown;
                    m_world.DeployWardenReinforcement();
                    m_showFeedback("WARDEN BAY ACTIVE — USE THE SHIELDS TO BREAK PURSUIT");
                    break;
                case PoweredWithdrawalPhase.SapperCradle:
                    m_sapperHealth = m_tuning.SapperHealth;
                    m_sapperLatched = false;
                    m_sapperPulseCooldown = m_tuning.SapperFirstPulseDelay;
                    m_world.DeploySapperReinforcement(m_tuning.SapperPulseInterval);
                    m_showFeedback("SAPPER CRADLE ACTIVE — PRIORITIZE THE SIPHON WARNING");
                    break;
            }
        }

        public void SpawnForDebug(SecurityReinforcement reinforcement)
        {
            switch (reinforcement)
            {
                case SecurityReinforcement.Warden:
                    m_wardenHealth = m_tuning.WardenHealth;
                    m_world.DeployWardenReinforcement();
                    break;
                case SecurityReinforcement.Sapper:
                    m_sapperHealth = m_tuning.SapperHealth;
                    m_sapperLatched = false;
                    m_sapperPulseCooldown = m_tuning.SapperFirstPulseDelay;
                    m_world.DeploySapperReinforcement(m_tuning.SapperPulseInterval);
                    break;
                case SecurityReinforcement.Interceptor:
                    m_interceptorHealth = m_tuning.InterceptorHealth;
                    m_world.DeployInterceptorReinforcement();
                    break;
                case SecurityReinforcement.Suppressor:
                    m_suppressorHealth = m_tuning.SuppressorHealth;
                    m_world.DeploySuppressorReinforcement();
                    break;
            }
        }

        public void PurgeForDebug(SecurityReinforcement reinforcement)
        {
            switch (reinforcement)
            {
                case SecurityReinforcement.Warden:
                    while (m_wardenHealth > 0f)
                    {
                        _hitWarden();
                    }
                    break;
                case SecurityReinforcement.Sapper:
                    while (m_sapperHealth > 0f)
                    {
                        _hitSapper();
                    }
                    break;
                case SecurityReinforcement.Interceptor:
                    while (m_interceptorHealth > 0f)
                    {
                        _hitInterceptor();
                    }
                    break;
                case SecurityReinforcement.Suppressor:
                    while (m_suppressorHealth > 0f)
                    {
                        _hitSuppressor();
                    }
                    break;
            }
        }

        public void PurgeSwarmersForDebug() => m_swarmers.PurgeAllForDebug();

        public void SetFrozenForDebug(bool frozen) => m_debugFrozen = frozen;

        public void SetPlayerInvulnerableForDebug(bool invulnerable) => m_debugPlayerInvulnerable = invulnerable;

        public void ConfigureForDebugScenario(AuthoredCombatScenario scenario, bool includeSwarmers)
        {
            ResetDebugScenario();
            if (scenario == null || !scenario.IsComplete)
            {
                return;
            }

            m_debugScenarioActive = true;
            m_debugScenario = scenario;
            m_debugPlayerInvulnerable = true;
            SpawnForDebug(SecurityReinforcement.Warden);
            SpawnForDebug(SecurityReinforcement.Sapper);
            SpawnForDebug(SecurityReinforcement.Interceptor);
            SpawnForDebug(SecurityReinforcement.Suppressor);
            _placeForDebug(m_world.Warden, scenario.WardenAnchor, scenario.PlayerAnchor.position);
            _placeForDebug(m_world.Sapper, scenario.SapperAnchor, scenario.PlayerAnchor.position);
            _placeForDebug(m_world.Interceptor, scenario.InterceptorAnchor, scenario.PlayerAnchor.position);
            _placeForDebug(m_world.Suppressor, scenario.SuppressorAnchor, scenario.PlayerAnchor.position);
            m_wardenAttackCooldown = 1f;
            m_sapperLatched = true;
            m_sapperPulseCooldown = 1.5f;
            m_interceptorDashTarget = scenario.PlayerAnchor.position;
            m_interceptorChargeCountdown = 1.5f;
            m_suppressorFieldCooldown = 1f;
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            if (includeSwarmers)
            {
                m_swarmers.Deploy(scenario);
            }
        }

        public void BeginCombatChamberPhase(AuthoredCombatScenario scenario, int phase)
        {
            if (scenario == null || !scenario.IsComplete || phase < 1 || phase > 3)
            {
                return;
            }

            m_combatChamberActive = true;
            m_combatChamberScenario = scenario;
            m_combatChamberPhase = phase;
            var anchor = phase == 3 ? scenario.SapperAnchor : scenario.WardenAnchor;
            m_swarmers.DeploySingleWave(scenario, anchor, phase == 1 ? 3 : 4);
            if (phase == 2)
            {
                SpawnForDebug(SecurityReinforcement.Warden);
                _placeForDebug(m_world.Warden, scenario.WardenAnchor, scenario.PlayerAnchor.position);
                m_wardenAttackCooldown = 1f;
            }
            else if (phase == 3)
            {
                SpawnForDebug(SecurityReinforcement.Sapper);
                _placeForDebug(m_world.Sapper, scenario.SapperAnchor, scenario.PlayerAnchor.position);
                m_sapperLatched = true;
                m_sapperPulseCooldown = 1.5f;
            }
        }

        public void BeginConvergenceCalibration(
            AuthoredConvergenceCalibrationObjective objective,
            SecurityReinforcement pressureRole)
        {
            if (objective == null || !objective.IsConfigured || pressureRole == SecurityReinforcement.None)
            {
                return;
            }

            SpawnForDebug(pressureRole);
            var pressureActor = pressureRole switch
            {
                SecurityReinforcement.Warden => m_world.Warden,
                SecurityReinforcement.Sapper => m_world.Sapper,
                SecurityReinforcement.Interceptor => m_world.Interceptor,
                SecurityReinforcement.Suppressor => m_world.Suppressor,
                _ => null
            };
            if (pressureActor != null)
            {
                _placeForDebug(pressureActor, objective.PressureAnchor, m_world.Player.position);
            }

            if (pressureRole == SecurityReinforcement.Interceptor)
            {
                m_interceptorDashTarget = m_world.Player.position;
                m_interceptorChargeCountdown = 1.5f;
            }
        }

        public bool IsCombatChamberPhaseCleared()
        {
            if (!m_combatChamberActive || m_swarmers.ActiveCount > 0)
            {
                return false;
            }

            return m_combatChamberPhase switch
            {
                1 => true,
                2 => !IsWardenAlive,
                3 => !IsSapperAlive,
                _ => false
            };
        }

        public void EndCombatChamber()
        {
            m_combatChamberActive = false;
            m_combatChamberScenario = null;
            m_combatChamberPhase = 0;
            m_swarmers.RetireActivePopulation();
        }

        public void ResetDebugScenario()
        {
            m_debugScenarioActive = false;
            m_debugScenario = null;
            m_debugScenarioAttackMask = 0;
            m_debugPlayerInvulnerable = false;
            m_debugFrozen = false;
            m_extractionPressure = false;
            m_extractionUplinkMode = ExtractionUplinkMode.None;
            CurrentExtractionSuppressionProfile = ExtractionSuppressionProfile.Standard;
            m_wardenHealth = 0f;
            m_sapperHealth = 0f;
            m_interceptorHealth = 0f;
            m_suppressorHealth = 0f;
            m_wardenAttackCooldown = 0f;
            m_wardenScreeningSapper = false;
            m_sapperPulseCooldown = 0f;
            m_sapperLatched = false;
            m_interceptorChargeCountdown = 0f;
            m_interceptorDashRemaining = 0f;
            m_interceptorRecoveryCountdown = 0f;
            m_interceptorHitCooldown = 0f;
            m_interceptorCuttingSapperFlank = false;
            m_suppressorWarningCountdown = 0f;
            m_suppressorFieldCountdown = 0f;
            m_suppressorFieldCooldown = 0f;
            m_suppressorPulseCountdown = 0f;
            m_pendingEntryIndex = -1;
            m_shotCooldown = 0f;
            foreach (var projectile in m_projectiles)
            {
                if (projectile.Visual != null)
                {
                    projectile.Visual.SetActive(false);
                    UnityEngine.Object.Destroy(projectile.Visual);
                }
            }
            m_projectiles.Clear();
            m_swarmers.Reset();
            m_world.ResetWardenPresentation();
            m_world.ResetSapperPresentation();
            m_world.ResetInterceptorPresentation();
            m_world.ResetSuppressorPresentation();
            m_world.SetReinforcementEntryWarning(SecurityReinforcement.None, -1, false, 0f);
        }

        public void DamageForDebug(SecurityReinforcement reinforcement)
        {
            switch (reinforcement)
            {
                case SecurityReinforcement.Warden: if (m_wardenHealth > 0f) _hitWarden(); break;
                case SecurityReinforcement.Sapper: if (m_sapperHealth > 0f) _hitSapper(); break;
                case SecurityReinforcement.Interceptor: if (m_interceptorHealth > 0f) _hitInterceptor(); break;
                case SecurityReinforcement.Suppressor: if (m_suppressorHealth > 0f) _hitSuppressor(); break;
            }
        }

        public void ForceAttackForDebug(SecurityReinforcement reinforcement)
        {
            SpawnForDebug(reinforcement);
            switch (reinforcement)
            {
                case SecurityReinforcement.Warden: m_wardenAttackCooldown = 0f; break;
                case SecurityReinforcement.Sapper: m_sapperLatched = true; m_sapperPulseCooldown = 0f; break;
                case SecurityReinforcement.Interceptor: m_interceptorChargeCountdown = 0.01f; break;
                case SecurityReinforcement.Suppressor: m_suppressorFieldCooldown = 0f; break;
            }
        }

        public void TickCooldown(float dt)
        {
            m_shotCooldown = Mathf.Max(0f, m_shotCooldown - dt);
        }

        public void Tick(float dt, bool playerPowered)
        {
            if (m_debugFrozen)
            {
                return;
            }

            if (!m_combatChamberActive)
            {
                _tickDirector(dt, playerPowered);
            }
            _tickInterceptor(dt);
            _tickSuppressor(dt);
            _tickWarden(dt);
            _tickSapper(dt);
            if (m_debugScenarioActive || m_combatChamberActive)
            {
                m_swarmers.Tick(dt, m_model.ShortcutOpen);
            }
            m_metrics.RecordThreatConcurrency(_activeThreatCount());
            _tickProjectiles(dt);
            _clampDebugScenarioActors();
            _clampCombatChamberActors();
        }

        public void TryFire(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = m_world.Player.forward;
            }

            m_shotCooldown = m_projectileTuning.FireCooldown;
            m_metrics.RecordShot();
            m_audio.Play(DeadSignalAudioCue.Fire);
            LastShotBlockedByEnvironment = false;
            var weaponOverclock = m_overclockChoice.SelectedWeapon;
            var shot = m_world.CreateSignalBolt(direction, weaponOverclock, m_overclockChoice.IsWeaponEvolved);
            m_world.PlayPlayerShot(direction, weaponOverclock, m_overclockChoice.IsWeaponEvolved);
            var threatHits = weaponOverclock == SignalWeaponOverclock.PiercingPulse
                ? m_overclockChoice.IsWeaponEvolved
                    ? m_overclockTuning.EvolvedPiercingPulseThreatHits
                    : m_overclockTuning.PiercingPulseThreatHits
                : 1;
            var ricochetBanks = weaponOverclock == SignalWeaponOverclock.ControlledRicochet
                ? m_overclockChoice.IsWeaponEvolved ? m_overclockTuning.EvolvedControlledRicochetBanks : 1
                : 0;
            m_projectiles.Add(new Projectile(
                shot, direction.normalized, m_projectileTuning.Lifetime, weaponOverclock, threatHits, ricochetBanks));
        }

        private void _tickDirector(float dt, bool playerPowered)
        {
            var traceWasCompleted = m_director.IsDeadZoneTraceCompleted;
            var previousPending = m_director.PendingReinforcement;
            var safestEntryIndex = m_world.GetSafestInterceptorEntryIndex(m_world.Player.position);
            var dualGateEntryIndex = m_pendingEntryIndex >= 0 ? m_pendingEntryIndex : safestEntryIndex;
            var dualGateEntryDistance = m_world.GetInterceptorEntryDistance(dualGateEntryIndex, m_world.Player.position);
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
                dualGateEntryDistance,
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.Warden.position),
                DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.Sapper.position),
                dualGateEntryDistance,
                m_model.RelayTowerOnline);
            var pending = m_director.PendingReinforcement;
            if (reinforcement == SecurityReinforcement.None && pending != previousPending)
            {
                m_pendingEntryIndex = pending is SecurityReinforcement.Interceptor or SecurityReinforcement.Suppressor
                    ? safestEntryIndex
                    : -1;
            }

            if (pending != SecurityReinforcement.None)
            {
                var warningProgress = m_tuning.ReinforcementEntryDelay <= 0f
                    ? 1f
                    : 1f - m_director.EntryCountdown / m_tuning.ReinforcementEntryDelay;
                m_world.SetReinforcementEntryWarning(pending, m_pendingEntryIndex, m_director.IsEntryBlocked, warningProgress);
            }
            else
            {
                m_world.SetReinforcementEntryWarning(SecurityReinforcement.None, -1, false, 0f);
            }
            if (!traceWasCompleted && m_director.IsDeadZoneTraceCompleted)
            {
                m_showFeedback("SECURITY TRACE COMPLETE — INTERCEPTOR DISPATCHED");
            }

            if (reinforcement == SecurityReinforcement.Interceptor)
            {
                m_interceptorHealth = m_tuning.InterceptorHealth;
                m_interceptorChargeCountdown = 0f;
                m_interceptorDashRemaining = 0f;
                m_interceptorRecoveryCountdown = 0f;
                m_interceptorHitCooldown = 0f;
                m_world.DeployInterceptorReinforcement(m_pendingEntryIndex);
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
                var isRelayLockdown = !m_extractionPressure;
                m_suppressorHealth = m_tuning.SuppressorHealth;
                m_suppressorFieldCountdown = 0f;
                m_suppressorFieldCooldown = 0f;
                m_suppressorPulseCountdown = 0f;
                m_world.DeploySuppressorReinforcement(m_pendingEntryIndex);
                m_showFeedback("FLANK GATES OPEN — SUPPRESSOR INBOUND");
                var openingCenter = isRelayLockdown
                    ? m_world.Player.position
                    : _calculateExtractionSuppressionCenter();
                if (!isRelayLockdown && m_extractionUplinkMode == ExtractionUplinkMode.Overdrive)
                {
                    openingCenter = m_world.ClampToArena(openingCenter, m_tuning.SuppressorFieldRadius);
                }

                var warning = isRelayLockdown
                    ? "RELAY LOCKDOWN SWEEP — LEAVE THE RING"
                    : _extractionSuppressionWarning();
                _beginSuppressorWarning(openingCenter, warning);
            }

            if (reinforcement != SecurityReinforcement.None)
            {
                m_pendingEntryIndex = -1;
                m_world.SetReinforcementEntryWarning(SecurityReinforcement.None, -1, false, 0f);
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
                        m_directionalDamageFeedback.Play(
                            m_world.Suppressor.position, m_world.Player.position, PlayerDamageFeedbackKind.Security);
                        m_world.PlayPlayerDamage(m_world.Suppressor.position);
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
                    m_world.SetSuppressorFieldAt(true, true, m_tuning.SuppressorFieldRadius, m_suppressorFieldCenter);
                    if (m_debugScenarioActive)
                    {
                        m_debugScenarioAttackMask |= 8;
                    }
                }

                return;
            }

            m_suppressorFieldCooldown = Mathf.Max(0f, m_suppressorFieldCooldown - dt);
            var anchor = m_debugScenarioActive
                ? m_world.Player.position
                : InterceptorTactics.CalculateCutoffPoint(
                    m_world.Player.position,
                    m_world.ExtractionPosition,
                    0.5f);
            var navigationTarget = m_world.GetNavMeshWaypoint(
                m_world.Suppressor,
                anchor,
                SUPPRESSOR_COLLISION_RADIUS,
                m_model.ShortcutOpen);
            var delta = navigationTarget - m_world.Suppressor.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                m_world.Suppressor.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }

            if (DeadSignalWorld.FlatDistance(m_world.Suppressor.position, anchor) > m_tuning.SuppressorAnchorDistance)
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

        private Vector3 _calculateExtractionSuppressionCenter()
        {
            if (CurrentExtractionSuppressionProfile != ExtractionSuppressionProfile.Standard)
            {
                var center = InterceptorTactics.CalculateGreedSuppressionCenter(
                    m_world.Player.position,
                    m_world.ExtractionPosition,
                    CurrentExtractionSuppressionProfile,
                    m_tuning.OverdriveSuppressionLeadDistance);
                return m_world.ClampToArena(center, m_tuning.SuppressorFieldRadius);
            }

            return InterceptorTactics.CalculateOpeningSuppressionCenter(
                m_world.Player.position,
                m_world.ExtractionPosition,
                m_extractionUplinkMode,
                m_tuning.OverdriveSuppressionLeadDistance);
        }

        private string _extractionSuppressionWarning()
        {
            return CurrentExtractionSuppressionProfile switch
            {
                ExtractionSuppressionProfile.PiercingCrossLane =>
                    "QUENCH COUNTERTRACE — CROSS-LANE SWEEP, BREAK ALIGNMENT",
                ExtractionSuppressionProfile.RicochetCoverFlush =>
                    "QUENCH COUNTERTRACE — COVER FLUSH, LEAVE THE RING",
                _ => m_extractionUplinkMode == ExtractionUplinkMode.Overdrive
                    ? "PREDICTIVE SUPPRESSION SWEEP — BREAK COURSE"
                    : "SUPPRESSION SWEEP LOCKED — LEAVE THE RING"
            };
        }

        private void _tickInterceptor(float dt)
        {
            if (!m_model.TowerOnline || m_interceptorHealth <= 0f)
            {
                m_interceptorCuttingSapperFlank = false;
                m_world.SetInterceptorPresentationState(false, false);
                return;
            }

            m_interceptorHitCooldown = Mathf.Max(0f, m_interceptorHitCooldown - dt);
            m_world.SetInterceptorPresentationState(IsInterceptorCharging, m_interceptorDashRemaining > 0f);
            var coreRotationSpeed = IsInterceptorRecovering ? 80f : 320f;
            m_world.InterceptorCore.Rotate(Vector3.up, coreRotationSpeed * dt, Space.Self);
            var wasCuttingSapperFlank = m_interceptorCuttingSapperFlank;
            m_interceptorCuttingSapperFlank = _isSapperFlankCutActive();
            if (!wasCuttingSapperFlank && m_interceptorCuttingSapperFlank)
            {
                m_showFeedback("INTERCEPTOR CUTTING SAPPER FLANK — SWITCH SIDES");
            }

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
                if (m_world.LastMovementBlocked)
                {
                    m_interceptorDashRemaining = 0f;
                    _beginInterceptorRecovery(true);
                }
                else if (m_interceptorDashRemaining <= 0f)
                {
                    _beginInterceptorRecovery(false);
                }

                return;
            }

            if (m_interceptorRecoveryCountdown > 0f)
            {
                m_interceptorRecoveryCountdown = Mathf.Max(0f, m_interceptorRecoveryCountdown - dt);
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
                    if (m_debugScenarioActive)
                    {
                        m_debugScenarioAttackMask |= 4;
                    }
                }

                return;
            }

            m_interceptorCutoffTarget = _calculateInterceptorCutoffTarget();
            var navigationTarget = m_world.GetNavMeshWaypoint(
                m_world.Interceptor,
                m_interceptorCutoffTarget,
                INTERCEPTOR_COLLISION_RADIUS,
                m_model.ShortcutOpen);
            var delta = navigationTarget - m_world.Interceptor.position;
            delta.y = 0f;
            _faceInterceptor(navigationTarget);
            var cutoffDistance = DeadSignalWorld.FlatDistance(m_world.Interceptor.position, m_interceptorCutoffTarget);
            if (cutoffDistance > m_tuning.InterceptorChargeDistance)
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

        private void _beginInterceptorRecovery(bool hitCover)
        {
            m_interceptorRecoveryCountdown = InterceptorTactics.CalculateDashRecoveryDuration(
                hitCover,
                m_tuning.InterceptorDashRecoveryDuration,
                m_tuning.InterceptorCrashRecoveryDuration);
            m_world.PlayInterceptorRecovery(hitCover, m_interceptorRecoveryCountdown);
            m_showFeedback(hitCover
                ? "INTERCEPTOR CRASHED — COUNTERATTACK WINDOW"
                : "INTERCEPTOR RECOVERING — REPOSITION OR FIRE");
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

            if (m_interceptorCuttingSapperFlank)
            {
                return InterceptorTactics.CalculateSapperFlankPoint(
                    m_world.Player.position,
                    m_world.Sapper.position,
                    m_world.Interceptor.position,
                    m_tuning.InterceptorSapperFlankDistance);
            }

            return InterceptorTactics.CalculateCutoffPoint(
                m_world.Player.position,
                m_world.ExtractionPosition,
                m_tuning.InterceptorCutoffFraction);
        }

        private bool _isSapperFlankCutActive()
        {
            var suppressorCoordinationActive = m_suppressorHealth > 0f &&
                                               (m_suppressorWarningCountdown > 0f ||
                                                (m_suppressorFieldCountdown > 0f && IsPlayerSuppressed));
            return !suppressorCoordinationActive &&
                   m_sapperLatched &&
                   m_sapperHealth > 0f &&
                   DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.Sapper.position) >
                   m_tuning.InterceptorSapperFlankBreakDistance;
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
            m_directionalDamageFeedback.Play(
                m_world.Interceptor.position, m_world.Player.position, PlayerDamageFeedbackKind.Security);
            m_world.PlayPlayerDamage(m_world.Interceptor.position);
            m_combatFeedback.PlaySecurityImpact(m_world.Player.position + Vector3.up * 0.58f);
            m_audio.Play(DeadSignalAudioCue.SecurityImpact);
            m_showFeedback($"INTERCEPTOR IMPACT  −{RunModel.SecurityHitCost:0} SIGNAL");
        }

        private void _tickWarden(float dt)
        {
            if (!m_model.TowerOnline || m_wardenHealth <= 0f)
            {
                m_wardenScreeningSapper = false;
                m_world.SetWardenPresentationState(false, m_tuning.WardenAttackDistance);
                return;
            }

            m_wardenAttackCooldown = Mathf.Max(0f, m_wardenAttackCooldown - dt);
            var wasScreening = m_wardenScreeningSapper;
            var playerToSapperDistance = DeadSignalWorld.FlatDistance(m_world.Player.position, m_world.Sapper.position);
            m_wardenScreeningSapper = m_sapperLatched && m_sapperHealth > 0f &&
                                      playerToSapperDistance > m_tuning.WardenSapperScreenBreakDistance;
            m_wardenTacticalTarget = m_wardenScreeningSapper
                ? WardenTactics.CalculateSapperScreenPoint(
                    m_world.Player.position,
                    m_world.Sapper.position,
                    m_tuning.WardenSapperScreenDistance,
                    m_tuning.WardenSapperScreenBreakDistance)
                : m_world.Player.position;
            if (!wasScreening && m_wardenScreeningSapper)
            {
                m_showFeedback("WARDEN SCREENING SAPPER — FLANK OR BREAK ARMOR");
            }
            m_world.SetWardenPresentationState(m_wardenScreeningSapper, m_tuning.WardenAttackDistance);

            var playerDelta = m_world.Player.position - m_world.Warden.position;
            playerDelta.y = 0f;
            if (playerDelta.magnitude <= m_tuning.WardenAttackDistance && m_wardenAttackCooldown <= 0f)
            {
                if (playerDelta.sqrMagnitude > 0.01f)
                {
                    m_world.Warden.rotation = Quaternion.LookRotation(playerDelta.normalized, Vector3.up);
                }

                _applyWardenHit();
                return;
            }

            var navigationTarget = m_world.GetNavMeshWaypoint(
                m_world.Warden,
                m_wardenTacticalTarget,
                WARDEN_COLLISION_RADIUS,
                m_model.ShortcutOpen);
            var delta = navigationTarget - m_world.Warden.position;
            delta.y = 0f;
            var distance = delta.magnitude;
            if (distance > 0.05f)
            {
                m_world.Warden.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }

            var arrivalDistance = m_wardenScreeningSapper ? 0.1f : m_tuning.WardenAttackDistance;
            if (DeadSignalWorld.FlatDistance(m_world.Warden.position, m_wardenTacticalTarget) > arrivalDistance)
            {
                var desired = m_world.Warden.position + delta.normalized * (m_tuning.WardenSpeed * dt);
                m_world.Warden.position = m_world.ResolveMovement(
                    m_world.Warden.position,
                    desired,
                    WARDEN_COLLISION_RADIUS,
                    m_model.ShortcutOpen);
            }
        }

        private void _applyWardenHit()
        {
            m_wardenAttackCooldown = m_tuning.WardenAttackCooldown;
            m_world.PlayWardenStrike();
            if (m_debugScenarioActive)
            {
                m_debugScenarioAttackMask |= 1;
            }
            if (_tryAbsorbThreatDamage("WARDEN IMPACT"))
            {
                return;
            }

            m_model.TakeSecurityHit();
            m_metrics.RecordSecurityHit();
            m_directionalDamageFeedback.Play(
                m_world.Warden.position, m_world.Player.position, PlayerDamageFeedbackKind.Security);
            m_world.PlayPlayerDamage(m_world.Warden.position);
            m_combatFeedback.PlaySecurityImpact(m_world.Player.position + Vector3.up * 0.58f);
            m_audio.Play(DeadSignalAudioCue.SecurityImpact);
            m_showFeedback("SECURITY IMPACT  −18 SIGNAL");
        }

        private void _tickSapper(float dt)
        {
            if (!m_model.TowerOnline || m_sapperHealth <= 0f)
            {
                return;
            }

            m_world.SapperTelegraph.SetThreatState(true, m_sapperLatched, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            m_world.SetSapperPresentationState(m_sapperLatched, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            if (!m_sapperLatched)
            {
                var navigationTarget = m_world.GetNavMeshWaypoint(
                    m_world.Sapper,
                    m_world.TowerPosition,
                    SAPPER_COLLISION_RADIUS,
                    m_model.ShortcutOpen);
                var delta = navigationTarget - m_world.Sapper.position;
                delta.y = 0f;
                var distance = DeadSignalWorld.FlatDistance(m_world.Sapper.position, m_world.TowerPosition);
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
                m_sapperPulseCooldown = m_tuning.SapperFirstPulseDelay +
                                        (m_model.Salvage < 2 ? EARLY_SAPPER_GRACE_SECONDS : 0f);
                m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
                m_world.SetSapperPresentationState(true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
                m_showFeedback("SAPPER LATCHED - PURGE IT");
            }

            m_sapperPulseCooldown = Mathf.Max(0f, m_sapperPulseCooldown - dt);
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            if (m_sapperPulseCooldown > 0f)
            {
                return;
            }

            m_sapperPulseCooldown = m_tuning.SapperPulseInterval;
            if (m_debugScenarioActive)
            {
                m_debugScenarioAttackMask |= 2;
            }
            m_combatFeedback.PlaySapperImpact(m_world.TowerPosition + Vector3.up * 0.65f);
            m_audio.Play(DeadSignalAudioCue.SapperPulse);
            m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            m_world.SapperTelegraph.NotifyPulse();
            m_world.PlaySapperPulse();
            if (!_tryAbsorbThreatDamage("SAPPER PULSE"))
            {
                m_model.TakeSapperPulse();
                m_metrics.RecordSapperPulse();
                m_directionalDamageFeedback.Play(
                    m_world.Sapper.position, m_world.Player.position, PlayerDamageFeedbackKind.Sapper);
                m_world.PlayPlayerDamage(m_world.Sapper.position);
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
                var wardenHitFraction = float.PositiveInfinity;
                var sapperHitFraction = float.PositiveInfinity;
                var interceptorHitFraction = float.PositiveInfinity;
                var suppressorHitFraction = float.PositiveInfinity;
                var swarmerHitFraction = float.PositiveInfinity;
                var swarmerId = -1;
                var hitWarden = !shot.HasHit(ThreatTarget.Warden) && _tryGetThreatHitFraction(
                    start, end, m_world.Warden, m_wardenHealth, s_wardenProjectileHitRadius, out wardenHitFraction);
                var hitSapper = !shot.HasHit(ThreatTarget.Sapper) && _tryGetThreatHitFraction(
                    start, end, m_world.Sapper, m_sapperHealth, s_sapperProjectileHitRadius, out sapperHitFraction);
                var hitInterceptor = !shot.HasHit(ThreatTarget.Interceptor) && _tryGetThreatHitFraction(
                    start,
                    end,
                    m_world.Interceptor,
                    m_interceptorHealth,
                    s_interceptorProjectileHitRadius,
                    out interceptorHitFraction);
                var hitSuppressor = !shot.HasHit(ThreatTarget.Suppressor) && _tryGetThreatHitFraction(
                    start,
                    end,
                    m_world.Suppressor,
                    m_suppressorHealth,
                    s_suppressorProjectileHitRadius,
                    out suppressorHitFraction);
                var hitSwarmer = m_swarmers.TryGetHitFraction(
                    start, end, shot.HitSwarmerIds, out swarmerId, out swarmerHitFraction);
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
                        Mathf.Min(
                            hitSuppressor ? suppressorHitFraction : float.PositiveInfinity,
                            hitSwarmer ? swarmerHitFraction : float.PositiveInfinity)));
                if (hitObstacle && obstacleHitFraction < nearestThreatFraction)
                {
                    LastShotBlockedByEnvironment = true;
                    var impactPosition = Vector3.Lerp(start, end, obstacleHitFraction);
                    shot.Visual.transform.position = impactPosition;
                    m_combatFeedback.PlayEnvironmentImpact(impactPosition + Vector3.up * 0.03f);
                    if (shot.Weapon == SignalWeaponOverclock.ControlledRicochet && shot.CanRicochet &&
                        _tryRedirectRicochet(shot, impactPosition))
                    {
                        ControlledRicochets++;
                        continue;
                    }

                    m_world.PlayWeaponTermination(impactPosition, shot.Direction, shot.Weapon);
                    UnityEngine.Object.Destroy(shot.Visual);
                    m_projectiles.RemoveAt(index);
                    continue;
                }

                var hitTarget = ThreatTarget.None;
                var hitFraction = float.PositiveInfinity;
                var didHitSwarmer = false;
                if (hitWarden && wardenHitFraction <= sapperHitFraction && wardenHitFraction <= interceptorHitFraction &&
                    wardenHitFraction <= suppressorHitFraction && wardenHitFraction <= swarmerHitFraction)
                {
                    var hitPosition = m_world.Warden.position + Vector3.up * 0.55f;
                    _hitWarden();
                    _tryChainArc(ThreatTarget.Warden, hitPosition);
                    hitTarget = ThreatTarget.Warden;
                    hitFraction = wardenHitFraction;
                }
                else if (hitSapper && sapperHitFraction <= interceptorHitFraction && sapperHitFraction <= suppressorHitFraction &&
                         sapperHitFraction <= swarmerHitFraction)
                {
                    var hitPosition = m_world.Sapper.position + Vector3.up * 0.5f;
                    _hitSapper();
                    _tryChainArc(ThreatTarget.Sapper, hitPosition);
                    hitTarget = ThreatTarget.Sapper;
                    hitFraction = sapperHitFraction;
                }
                else if (hitInterceptor && interceptorHitFraction <= suppressorHitFraction &&
                         interceptorHitFraction <= swarmerHitFraction)
                {
                    var hitPosition = m_world.Interceptor.position + Vector3.up * 0.5f;
                    _hitInterceptor();
                    _tryChainArc(ThreatTarget.Interceptor, hitPosition);
                    hitTarget = ThreatTarget.Interceptor;
                    hitFraction = interceptorHitFraction;
                }
                else if (hitSuppressor && suppressorHitFraction <= swarmerHitFraction)
                {
                    var hitPosition = m_world.Suppressor.position + Vector3.up * 0.5f;
                    _hitSuppressor();
                    _tryChainArc(ThreatTarget.Suppressor, hitPosition);
                    hitTarget = ThreatTarget.Suppressor;
                    hitFraction = suppressorHitFraction;
                }
                else if (hitSwarmer)
                {
                    var hitPosition = m_swarmers.Purge(swarmerId, shot.Visual.transform.position) + Vector3.up * 0.3f;
                    m_combatFeedback.PlaySignalImpact(hitPosition, true);
                    m_audio.Play(DeadSignalAudioCue.SignalImpact);
                    shot.MarkSwarmerHit(swarmerId);
                    hitFraction = swarmerHitFraction;
                    didHitSwarmer = true;
                }

                if (hitTarget != ThreatTarget.None || didHitSwarmer)
                {
                    var resolvedHitPosition = Vector3.Lerp(start, end, hitFraction);
                    if (hitTarget != ThreatTarget.None)
                    {
                        shot.MarkHit(hitTarget);
                    }
                    if (shot.RemainingThreatHits > 0 && shot.Life > 0f)
                    {
                        PiercingPulseFollowThroughs++;
                        var continuationPosition = Vector3.Lerp(start, end, hitFraction) + shot.Direction * 0.08f;
                        shot.Visual.transform.position = continuationPosition;
                        m_world.PlayPiercingContinuation(continuationPosition, shot.Direction);
                        continue;
                    }
                    shot.Visual.transform.position = resolvedHitPosition;
                }
                else
                {
                    shot.Visual.transform.position = end;
                }

                if (hitTarget != ThreatTarget.None || didHitSwarmer || shot.Life <= 0f)
                {
                    m_world.PlayWeaponTermination(shot.Visual.transform.position, shot.Direction, shot.Weapon);
                    UnityEngine.Object.Destroy(shot.Visual);
                    m_projectiles.RemoveAt(index);
                }
            }
        }

        private bool _tryRedirectRicochet(Projectile shot, Vector3 impactPosition)
        {
            var target = ThreatTarget.None;
            var nearestDistance = m_overclockTuning.ControlledRicochetTargetRadius;
            _considerRicochetTarget(ThreatTarget.Warden, m_world.Warden, m_wardenHealth, impactPosition, ref target, ref nearestDistance);
            _considerRicochetTarget(ThreatTarget.Sapper, m_world.Sapper, m_sapperHealth, impactPosition, ref target, ref nearestDistance);
            _considerRicochetTarget(
                ThreatTarget.Interceptor, m_world.Interceptor, m_interceptorHealth, impactPosition, ref target, ref nearestDistance);
            _considerRicochetTarget(
                ThreatTarget.Suppressor, m_world.Suppressor, m_suppressorHealth, impactPosition, ref target, ref nearestDistance);
            if (target == ThreatTarget.None)
            {
                return false;
            }

            var targetPosition = _getThreatPosition(target) + Vector3.up * 0.25f;
            var direction = targetPosition - impactPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return false;
            }

            direction.Normalize();
            var redirectedStart = impactPosition + direction * 0.12f;
            if (m_world.TryGetProjectileObstacleHit(
                    redirectedStart,
                    targetPosition,
                    m_projectileTuning.CollisionRadius,
                    m_model.ShortcutOpen,
                    out var obstacleFraction) && obstacleFraction < 0.98f)
            {
                return false;
            }

            var incomingDirection = shot.Direction;
            shot.Redirect(direction, redirectedStart);
            m_world.PlayRicochetRedirect(impactPosition, incomingDirection, direction);
            return true;
        }

        private void _considerRicochetTarget(
            ThreatTarget candidate,
            Transform transform,
            float health,
            Vector3 start,
            ref ThreatTarget target,
            ref float nearestDistance)
        {
            if (health <= 0f || !transform.gameObject.activeSelf)
            {
                return;
            }

            var distance = DeadSignalWorld.FlatDistance(start, transform.position);
            if (distance > nearestDistance)
            {
                return;
            }

            var targetPosition = transform.position + Vector3.up * 0.25f;
            var direction = targetPosition - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            var redirectedStart = start + direction.normalized * 0.12f;
            if (m_world.TryGetProjectileObstacleHit(
                    redirectedStart,
                    targetPosition,
                    m_projectileTuning.CollisionRadius,
                    m_model.ShortcutOpen,
                    out var obstacleFraction) && obstacleFraction < 0.98f)
            {
                return;
            }

            nearestDistance = distance;
            target = candidate;
        }

        private void _tryChainArc(ThreatTarget source, Vector3 start)
        {
            if (m_overclockChoice.Selected != SignalOverclock.ChainArc)
            {
                return;
            }

            var target = ThreatTarget.None;
            var nearestDistance = m_overclockTuning.ChainArcRadius;
            _considerChainTarget(
                ThreatTarget.Warden, source, ThreatTarget.None, m_world.Warden, m_wardenHealth, start, ref target, ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Sapper, source, ThreatTarget.None, m_world.Sapper, m_sapperHealth, start, ref target, ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Interceptor,
                source,
                ThreatTarget.None,
                m_world.Interceptor,
                m_interceptorHealth,
                start,
                ref target,
                ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Suppressor,
                source,
                ThreatTarget.None,
                m_world.Suppressor,
                m_suppressorHealth,
                start,
                ref target,
                ref nearestDistance);
            if (target == ThreatTarget.None)
            {
                return;
            }

            var targetPosition = _getThreatPosition(target) + Vector3.up * 0.5f;
            m_combatFeedback.PlayChainArc(start, targetPosition);
            _hitThreat(target);
            if (!m_overclockChoice.IsChainArcOverloadReady)
            {
                return;
            }

            var overloadTarget = ThreatTarget.None;
            nearestDistance = m_overclockTuning.ChainArcRadius;
            _considerChainTarget(
                ThreatTarget.Warden,
                source,
                target,
                m_world.Warden,
                m_wardenHealth,
                targetPosition,
                ref overloadTarget,
                ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Sapper,
                source,
                target,
                m_world.Sapper,
                m_sapperHealth,
                targetPosition,
                ref overloadTarget,
                ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Interceptor,
                source,
                target,
                m_world.Interceptor,
                m_interceptorHealth,
                targetPosition,
                ref overloadTarget,
                ref nearestDistance);
            _considerChainTarget(
                ThreatTarget.Suppressor,
                source,
                target,
                m_world.Suppressor,
                m_suppressorHealth,
                targetPosition,
                ref overloadTarget,
                ref nearestDistance);
            if (overloadTarget == ThreatTarget.None)
            {
                return;
            }

            m_overclockChoice.TryConsumeChainArcOverload();
            var overloadTargetPosition = _getThreatPosition(overloadTarget) + Vector3.up * 0.5f;
            m_combatFeedback.PlayChainArc(targetPosition, overloadTargetPosition);
            _hitThreat(overloadTarget);
        }

        private void _hitThreat(ThreatTarget target)
        {
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
            ThreatTarget firstExcluded,
            ThreatTarget secondExcluded,
            Transform transform,
            float health,
            Vector3 start,
            ref ThreatTarget target,
            ref float nearestDistance)
        {
            if (candidate == firstExcluded || candidate == secondExcluded || health <= 0f || !transform.gameObject.activeSelf)
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
            m_world.PlayWardenHit(m_world.Player.position);
            if (m_wardenHealth <= 0f)
            {
                m_combatFeedback.PlaySignalImpact(m_world.Warden.position + Vector3.up * 0.65f, true);
            }
            else
            {
                m_combatFeedback.PlayArmorImpact(m_world.Warden.position + Vector3.up * 0.65f);
            }
            if (m_wardenHealth > 0f)
            {
                m_combatFeedback.PlayThreatReaction(m_world.Warden);
            }
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
            var interrupted = m_sapperHealth > 0f && m_sapperLatched;
            m_world.PlaySapperHit(m_world.Player.position, interrupted);
            if (interrupted)
            {
                m_sapperPulseCooldown = Mathf.Max(m_sapperPulseCooldown, SAPPER_HIT_INTERRUPT_SECONDS);
                m_world.SapperTelegraph.SetThreatState(true, true, m_sapperPulseCooldown, m_tuning.SapperPulseInterval);
            }
            if (m_sapperHealth <= 0f)
            {
                m_combatFeedback.PlaySignalImpact(m_world.Sapper.position + Vector3.up * 0.58f, true);
            }
            else
            {
                m_combatFeedback.PlayArmorImpact(m_world.Sapper.position + Vector3.up * 0.58f);
            }
            if (m_sapperHealth > 0f)
            {
                m_combatFeedback.PlayThreatReaction(m_world.Sapper);
            }
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

            m_showFeedback(m_sapperLatched
                ? $"SAPPER INTERRUPTED — DRAIN DELAYED {SAPPER_HIT_INTERRUPT_SECONDS:0.0}s"
                : "SAPPER SHELL HIT");
        }

        private void _hitInterceptor()
        {
            m_interceptorHealth -= 1f;
            m_world.PlayInterceptorHit(m_world.Player.position);
            if (m_interceptorHealth <= 0f)
            {
                m_combatFeedback.PlaySignalImpact(m_world.Interceptor.position + Vector3.up * 0.5f, true);
            }
            else
            {
                m_combatFeedback.PlayArmorImpact(m_world.Interceptor.position + Vector3.up * 0.5f);
            }
            if (m_interceptorHealth > 0f)
            {
                m_combatFeedback.PlayThreatReaction(m_world.Interceptor);
            }
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
            m_world.PlaySuppressorHit(m_world.Player.position);
            if (m_suppressorHealth <= 0f)
            {
                m_combatFeedback.PlaySignalImpact(m_world.Suppressor.position + Vector3.up * 0.5f, true);
            }
            else
            {
                m_combatFeedback.PlayArmorImpact(m_world.Suppressor.position + Vector3.up * 0.5f);
            }
            if (m_suppressorHealth > 0f)
            {
                m_combatFeedback.PlayThreatReaction(m_world.Suppressor);
            }
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
            if (m_debugPlayerInvulnerable)
            {
                m_showFeedback($"DEBUG SHIELD — {threatName} BLOCKED");
                return true;
            }

            if (!m_overclockChoice.TryAbsorbThreatDamage(m_overclockTuning))
            {
                return false;
            }

            m_combatFeedback.PlayShieldImpact(m_world.Player.position + Vector3.up * 0.58f);
            m_audio.Play(DeadSignalAudioCue.SignalImpact);
            var synergyText = m_overclockChoice.Synergy switch
            {
                SignalOverclockSynergy.ReactiveArc => "  //  ARC OVERLOAD PRIMED",
                SignalOverclockSynergy.ShieldSurge =>
                    $"  //  THRUSTER SURGE {m_overclockTuning.OverdriveSynergySurgeDuration:0.#} SEC",
                _ => string.Empty
            };
            m_showFeedback($"FEEDBACK SHIELD — {threatName} NEGATED{synergyText}  //  PURGE TO RECHARGE");
            return true;
        }

        private static void _placeForDebug(Transform threat, Transform anchor, Vector3 target)
        {
            threat.position = anchor.position;
            var forward = target - threat.position;
            forward.y = 0f;
            threat.rotation = forward.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : anchor.rotation;
        }

        private void _clampDebugScenarioActors()
        {
            if (!m_debugScenarioActive || m_debugScenario == null)
            {
                return;
            }

            m_world.Warden.position = m_debugScenario.ClampToSafeArea(m_world.Warden.position);
            m_world.Sapper.position = m_debugScenario.ClampToSafeArea(m_world.Sapper.position);
            m_world.Interceptor.position = m_debugScenario.ClampToSafeArea(m_world.Interceptor.position);
            m_world.Suppressor.position = m_debugScenario.ClampToSafeArea(m_world.Suppressor.position);
            foreach (var swarmer in m_swarmers.ActiveTransforms)
            {
                swarmer.position = m_debugScenario.ClampToSafeArea(swarmer.position);
            }
        }

        private void _clampCombatChamberActors()
        {
            if (!m_combatChamberActive || m_combatChamberScenario == null)
            {
                return;
            }

            if (IsWardenAlive) m_world.Warden.position = m_combatChamberScenario.ClampToSafeArea(m_world.Warden.position);
            if (IsSapperAlive) m_world.Sapper.position = m_combatChamberScenario.ClampToSafeArea(m_world.Sapper.position);
            foreach (var swarmer in m_swarmers.ActiveTransforms)
            {
                swarmer.position = m_combatChamberScenario.ClampToSafeArea(swarmer.position);
            }
        }

        private void _applySwarmerContact(Vector3 position)
        {
            if (m_debugScenarioActive)
            {
                m_debugScenarioAttackMask |= 16;
            }
            m_combatFeedback.PlaySecurityImpact(position + Vector3.up * 0.35f);
            m_audio.Play(DeadSignalAudioCue.SecurityImpact);
            if (_tryAbsorbThreatDamage("SWARMER IMPACT") || m_debugPlayerInvulnerable)
            {
                return;
            }

            m_model.TakeSuppressionPulse(m_swarmerTuning.ContactSignalDrain);
            m_metrics.RecordSwarmerContact();
            m_directionalDamageFeedback.Play(position, m_world.Player.position, PlayerDamageFeedbackKind.Security);
            m_world.PlayPlayerDamage(position);
            m_showFeedback($"SWARMER IMPACT  −{m_swarmerTuning.ContactSignalDrain:0} SIGNAL");
        }

        private int _activeThreatCount()
        {
            var count = m_swarmers.ActiveCount;
            if (IsWardenAlive && m_world.Warden.gameObject.activeSelf) count++;
            if (IsSapperAlive && m_world.Sapper.gameObject.activeSelf) count++;
            if (IsInterceptorAlive && m_world.Interceptor.gameObject.activeSelf) count++;
            if (IsSuppressorAlive && m_world.Suppressor.gameObject.activeSelf) count++;
            return count;
        }

        private void _purgeSwarmer(Vector3 position)
        {
            var restored = m_model.RestoreSignal(m_swarmerTuning.PurgeSignalReward);
            m_metrics.RecordSwarmerPurge(restored);
            m_combatFeedback.PlaySignalRecovery(position + Vector3.up * 0.3f);
            m_showFeedback($"SWARMER PURGED  +{restored:0} SIGNAL{_purgeRewardText()}");
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
            public Projectile(
                GameObject visual,
                Vector3 direction,
                float lifetime,
                SignalWeaponOverclock weapon,
                int remainingThreatHits,
                int ricochetBanks)
            {
                Visual = visual;
                Direction = direction;
                Life = lifetime;
                Weapon = weapon;
                RemainingThreatHits = remainingThreatHits;
                RemainingRicochetBanks = ricochetBanks;
            }

            public GameObject Visual { get; }
            public Vector3 Direction { get; private set; }
            public float Life { get; set; }
            public SignalWeaponOverclock Weapon { get; }
            public int RemainingThreatHits { get; private set; }
            public int RemainingRicochetBanks { get; private set; }
            public bool CanRicochet => RemainingRicochetBanks > 0;
            public HashSet<int> HitSwarmerIds => m_hitSwarmerIds;

            public bool HasHit(ThreatTarget target) => (m_hitMask & (1 << (int)target)) != 0;

            public void MarkHit(ThreatTarget target)
            {
                m_hitMask |= 1 << (int)target;
                RemainingThreatHits--;
            }

            public void MarkSwarmerHit(int swarmerId)
            {
                m_hitSwarmerIds.Add(swarmerId);
                RemainingThreatHits--;
            }

            public void Redirect(Vector3 direction, Vector3 position)
            {
                Direction = direction;
                RemainingRicochetBanks--;
                Visual.transform.position = position;
                Visual.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            private int m_hitMask;
            private readonly HashSet<int> m_hitSwarmerIds = new();
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
