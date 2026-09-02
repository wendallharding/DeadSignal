using System;
using System.Collections.Generic;
using DeadSignal.Player;
using DeadSignal.World;
using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal.Presentation
{
    public enum StationAmbientEffectKind
    {
        Dust,
        Sparks,
        Steam,
        CoolantMist,
        HeatShimmer,
        ElectricalDrift,
        Venting,
        PoweredMachine
    }

    public interface IStationAmbientEffects
    {
        bool HasTuning { get; }
        int EmitterCount { get; }
        int ActiveEmitterCount { get; }
        int ParticleSystemCount { get; }
        float MaximumVisibleAlpha { get; }

        void Configure(Transform player);
        void SetPaused(bool paused);
    }

    /// <summary>
    /// Owns fixed ambient particle emitters bound to authored machinery. Effects are presentation-only and
    /// distance-culled; the authored mission components retain all objective, hazard, and interaction authority.
    /// </summary>
    public sealed class StationAmbientEffectsController : MonoBehaviour, IStationAmbientEffects
    {
        private const string TUNING_RESOURCE = "Tuning/StationAmbientEffectsTuning";
        private const string PARTICLE_TEXTURE_RESOURCE = "VFX/SignalDustMote";
        private const string PARTICLE_MATERIAL_RESOURCE = "Materials/RuntimeParticleTemplate";

        private static readonly Color s_dust = new(0.48f, 0.58f, 0.62f, 0.18f);
        private static readonly Color s_sparks = new(1f, 0.55f, 0.08f, 0.72f);
        private static readonly Color s_steam = new(0.72f, 0.78f, 0.8f, 0.24f);
        private static readonly Color s_coolant = new(0.24f, 0.86f, 0.94f, 0.24f);
        private static readonly Color s_heat = new(1f, 0.36f, 0.06f, 0.16f);
        private static readonly Color s_electrical = new(0.12f, 0.92f, 1f, 0.38f);
        private static readonly Color s_venting = new(0.82f, 0.84f, 0.78f, 0.28f);
        private static readonly Color s_powered = new(0.08f, 0.86f, 1f, 0.28f);

        private readonly List<Emitter> m_emitters = new();
        private IComfortSettings m_comfortSettings;
        private StationAmbientEffectsTuning m_tuning;
        private Transform m_player;
        private Material m_material;
        private bool m_isPaused;

        public bool HasTuning => m_tuning != null;
        public int EmitterCount => m_emitters.Count;
        public int ParticleSystemCount => m_emitters.Count;
        public int ActiveEmitterCount { get; private set; }
        public float MaximumVisibleAlpha { get; private set; }

        private sealed class Emitter
        {
            public StationAmbientEffectKind Kind;
            public Transform Root;
            public ParticleSystem Particles;
            public float BaseRate;
            public Color BaseColor;
            public bool IsVisible;
        }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
            m_comfortSettings.ReducedFlashesChanged += _handleReducedFlashesChanged;
        }

        public void Configure(Transform player)
        {
            if (m_emitters.Count > 0)
            {
                m_player = player;
                return;
            }

            m_player = player;
            m_tuning = Resources.Load<StationAmbientEffectsTuning>(TUNING_RESOURCE);
            if (m_tuning == null)
            {
                Debug.LogError("Station ambient-effects tuning is missing from Resources/Tuning.", this);
                return;
            }

            _createSharedMaterial();
            _bind<AuthoredCoolantReclamationObjective>(StationAmbientEffectKind.CoolantMist, owner => owner.SealPosition);
            _bind<AuthoredCoolingGantryHeroFinish>(StationAmbientEffectKind.Steam, owner => owner.transform.position);
            _bind<AuthoredBreakerResetObjective>(StationAmbientEffectKind.Sparks, owner => owner.Position);
            _bind<AuthoredFurnaceForgeObjective>(StationAmbientEffectKind.HeatShimmer, owner => owner.Position);
            _bind<AuthoredQuenchStabilizationObjective>(StationAmbientEffectKind.CoolantMist, owner => owner.Position);
            _bind<AuthoredInductionLatticeObjective>(StationAmbientEffectKind.ElectricalDrift, owner => owner.Position);
            _bind<AuthoredSpineVentingObjective>(StationAmbientEffectKind.Venting, owner => owner.Position);
            _bind<AuthoredCentralTowerReadability>(StationAmbientEffectKind.PoweredMachine, owner => owner.transform.position);
            _bind<AuthoredRelayFoundryHeroFinish>(StationAmbientEffectKind.Dust, owner => owner.transform.position);
            _bind<AuthoredSpineTowerReadability>(StationAmbientEffectKind.PoweredMachine, owner => owner.transform.position);
            _refreshAccessibility();
            _refreshCulling(true);
        }

        public void SetPaused(bool paused)
        {
            m_isPaused = paused;
            foreach (var emitter in m_emitters)
            {
                if (paused)
                {
                    emitter.Particles.Pause(true);
                }
                else if (emitter.IsVisible)
                {
                    emitter.Particles.Play(true);
                }
            }
        }

        private void LateUpdate()
        {
            _refreshCulling(false);
        }

        private void OnDestroy()
        {
            if (m_comfortSettings != null)
            {
                m_comfortSettings.ReducedFlashesChanged -= _handleReducedFlashesChanged;
            }

            if (m_material != null)
            {
                Destroy(m_material);
            }
        }

        private void _bind<TOwner>(StationAmbientEffectKind kind, Func<TOwner, Vector3> position)
            where TOwner : Component
        {
            var owners = FindObjectsByType<TOwner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var owner in owners)
            {
                _createEmitter(kind, owner.transform, position(owner));
            }
        }

        private void _createEmitter(StationAmbientEffectKind kind, Transform owner, Vector3 position)
        {
            var root = new GameObject($"Ambient {kind} Emitter");
            root.transform.SetParent(owner, true);
            root.transform.position = position + _heightOffset(kind);
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = m_tuning.MaximumParticlesPerEmitter;
            main.startLifetime = _lifetime(kind);
            main.startSpeed = _speed(kind);
            main.startSize = _size(kind);
            main.gravityModifier = kind == StationAmbientEffectKind.Sparks ? 0.12f : -0.008f;

            var emission = particles.emission;
            var rate = _emissionRate(kind);
            emission.rateOverTime = rate;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = kind is StationAmbientEffectKind.Sparks or StationAmbientEffectKind.ElectricalDrift
                ? ParticleSystemShapeType.Hemisphere
                : ParticleSystemShapeType.Box;
            shape.scale = _shape(kind);

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = _verticalVelocity(kind);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = kind == StationAmbientEffectKind.Sparks
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.maxParticleSize = 0.06f;
            renderer.sharedMaterial = m_material;

            m_emitters.Add(new Emitter
            {
                Kind = kind,
                Root = root.transform,
                Particles = particles,
                BaseRate = rate,
                BaseColor = _color(kind)
            });
        }

        private void _createSharedMaterial()
        {
            var template = Resources.Load<Material>(PARTICLE_MATERIAL_RESOURCE);
            if (template != null)
            {
                m_material = new Material(template);
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                             Shader.Find("Particles/Standard Unlit");
                if (shader == null)
                {
                    Debug.LogWarning("DEAD SIGNAL could not find a particle shader for station ambience.", this);
                    return;
                }
                m_material = new Material(shader);
            }

            m_material.name = "Station Ambient Runtime Material";
            m_material.mainTexture = Resources.Load<Texture2D>(PARTICLE_TEXTURE_RESOURCE);
            m_material.renderQueue = 3000;
            m_material.SetFloat("_Surface", 1f);
            m_material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private void _refreshCulling(bool force)
        {
            if (m_player == null || m_tuning == null)
            {
                return;
            }

            ActiveEmitterCount = 0;
            var maximumDistanceSquared = m_tuning.CullingDistance * m_tuning.CullingDistance;
            foreach (var emitter in m_emitters)
            {
                var delta = emitter.Root.position - m_player.position;
                delta.y = 0f;
                var visible = delta.sqrMagnitude <= maximumDistanceSquared;
                if (force || visible != emitter.IsVisible)
                {
                    emitter.IsVisible = visible;
                    if (visible && !m_isPaused)
                    {
                        emitter.Particles.Play(true);
                    }
                    else
                    {
                        emitter.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }

                if (visible)
                {
                    ActiveEmitterCount++;
                }
            }
        }

        private void _refreshAccessibility()
        {
            if (m_tuning == null)
            {
                return;
            }

            MaximumVisibleAlpha = 0f;
            var reduced = m_comfortSettings?.ReducedFlashesEnabled == true;
            foreach (var emitter in m_emitters)
            {
                var emission = emitter.Particles.emission;
                emission.rateOverTime = emitter.BaseRate * (reduced ? m_tuning.ReducedFlashesEmissionMultiplier : 1f);
                var color = emitter.BaseColor;
                color.a *= reduced ? m_tuning.ReducedFlashesAlphaMultiplier : 1f;
                var main = emitter.Particles.main;
                main.startColor = color;
                MaximumVisibleAlpha = Mathf.Max(MaximumVisibleAlpha, color.a);
            }
        }

        private void _handleReducedFlashesChanged(bool enabled)
        {
            _refreshAccessibility();
        }

        private float _emissionRate(StationAmbientEffectKind kind)
        {
            return kind switch
            {
                StationAmbientEffectKind.Sparks => m_tuning.SparkEmissionRate,
                StationAmbientEffectKind.Dust or StationAmbientEffectKind.ElectricalDrift => m_tuning.SparseEmissionRate,
                _ => m_tuning.ContinuousEmissionRate
            };
        }

        private static Color _color(StationAmbientEffectKind kind)
        {
            return kind switch
            {
                StationAmbientEffectKind.Dust => s_dust,
                StationAmbientEffectKind.Sparks => s_sparks,
                StationAmbientEffectKind.Steam => s_steam,
                StationAmbientEffectKind.CoolantMist => s_coolant,
                StationAmbientEffectKind.HeatShimmer => s_heat,
                StationAmbientEffectKind.ElectricalDrift => s_electrical,
                StationAmbientEffectKind.Venting => s_venting,
                _ => s_powered
            };
        }

        private static Vector3 _heightOffset(StationAmbientEffectKind kind) => Vector3.up * (kind switch
        {
            StationAmbientEffectKind.HeatShimmer => 0.9f,
            StationAmbientEffectKind.Steam or StationAmbientEffectKind.CoolantMist or StationAmbientEffectKind.Venting => 0.7f,
            StationAmbientEffectKind.Sparks or StationAmbientEffectKind.ElectricalDrift => 0.5f,
            _ => 0.3f
        });

        private static ParticleSystem.MinMaxCurve _lifetime(StationAmbientEffectKind kind) => kind switch
        {
            StationAmbientEffectKind.Sparks => new ParticleSystem.MinMaxCurve(0.12f, 0.28f),
            StationAmbientEffectKind.HeatShimmer => new ParticleSystem.MinMaxCurve(0.5f, 0.9f),
            _ => new ParticleSystem.MinMaxCurve(1.2f, 2.4f)
        };

        private static ParticleSystem.MinMaxCurve _speed(StationAmbientEffectKind kind) => kind switch
        {
            StationAmbientEffectKind.Sparks => new ParticleSystem.MinMaxCurve(0.8f, 1.8f),
            StationAmbientEffectKind.Venting => new ParticleSystem.MinMaxCurve(0.3f, 0.65f),
            _ => new ParticleSystem.MinMaxCurve(0.03f, 0.16f)
        };

        private static ParticleSystem.MinMaxCurve _size(StationAmbientEffectKind kind) => kind switch
        {
            StationAmbientEffectKind.Sparks => new ParticleSystem.MinMaxCurve(0.018f, 0.035f),
            StationAmbientEffectKind.HeatShimmer => new ParticleSystem.MinMaxCurve(0.1f, 0.18f),
            StationAmbientEffectKind.Steam or StationAmbientEffectKind.CoolantMist or StationAmbientEffectKind.Venting =>
                new ParticleSystem.MinMaxCurve(0.12f, 0.24f),
            _ => new ParticleSystem.MinMaxCurve(0.04f, 0.1f)
        };

        private static Vector3 _shape(StationAmbientEffectKind kind) => kind switch
        {
            StationAmbientEffectKind.Venting => new Vector3(0.35f, 0.1f, 0.15f),
            StationAmbientEffectKind.HeatShimmer => new Vector3(0.75f, 0.1f, 0.75f),
            _ => new Vector3(0.45f, 0.12f, 0.45f)
        };

        private static ParticleSystem.MinMaxCurve _verticalVelocity(StationAmbientEffectKind kind) => kind switch
        {
            StationAmbientEffectKind.Sparks => new ParticleSystem.MinMaxCurve(0.2f, 0.75f),
            StationAmbientEffectKind.Venting => new ParticleSystem.MinMaxCurve(0.35f, 0.65f),
            StationAmbientEffectKind.Steam or StationAmbientEffectKind.HeatShimmer =>
                new ParticleSystem.MinMaxCurve(0.16f, 0.3f),
            _ => new ParticleSystem.MinMaxCurve(0.04f, 0.14f)
        };
    }
}
