using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal
{
    public interface ISignalDust
    {
        bool HasTexture { get; }
        bool IsPowered { get; }
        int MaximumParticles { get; }
        float EmissionRate { get; }

        void Configure();
        void SetPaused(bool paused);
        void Tick(bool isPowered, bool towerOnline, float signalRatio);
    }

    /// <summary>
    /// Owns a bounded ambient particle field that reinforces powered and dead-zone state without affecting gameplay.
    /// </summary>
    public sealed class SignalDustController : MonoBehaviour, ISignalDust
    {
        private const string RUNTIME_PARTICLE_MATERIAL_RESOURCE = "Materials/RuntimeParticleTemplate";
        private const int MAXIMUM_PARTICLES = 56;
        private const float DEAD_EMISSION_RATE = 2.5f;
        private const float POWERED_EMISSION_RATE = 9f;

        private static readonly Color s_deadColor = new(0.18f, 0.3f, 0.42f, 0.22f);
        private static readonly Color s_poweredColor = new(0.12f, 0.9f, 1f, 0.58f);
        private static readonly Color s_highContrastPoweredColor = new(0.55f, 1f, 1f, 0.78f);

        private IComfortSettings m_comfortSettings;
        private ParticleSystem m_particles;
        private Material m_material;
        private bool m_isConfigured;
        private float m_signalRatio = 1f;

        public bool HasTexture { get; private set; }
        public bool IsPowered { get; private set; }
        public int MaximumParticles => MAXIMUM_PARTICLES;
        public float EmissionRate { get; private set; }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
            m_comfortSettings.HighContrastChanged += _handleHighContrastChanged;
        }

        public void Configure()
        {
            if (m_isConfigured)
            {
                return;
            }

            var texture = Resources.Load<Texture2D>("VFX/SignalDustMote");
            HasTexture = texture != null;

            var field = new GameObject("Adaptive Signal Dust Field");
            field.transform.SetParent(transform, false);
            field.transform.position = new Vector3(0f, 0.7f, 0f);
            m_particles = field.AddComponent<ParticleSystem>();

            var main = m_particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = MAXIMUM_PARTICLES;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 5.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.gravityModifier = -0.006f;

            var emission = m_particles.emission;
            emission.rateOverTime = DEAD_EMISSION_RATE;

            var shape = m_particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(25.6f, 0.2f, 16.8f);

            var velocity = m_particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

            var renderer = field.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.maxParticleSize = 0.08f;
            _createMaterial(renderer, texture);

            m_isConfigured = true;
            _applyPresentation();
            m_particles.Play();
        }

        public void Tick(bool isPowered, bool towerOnline, float signalRatio)
        {
            IsPowered = isPowered;
            m_signalRatio = Mathf.Clamp01(signalRatio);
            if (!m_isConfigured)
            {
                return;
            }

            float targetRate = isPowered ? POWERED_EMISSION_RATE : DEAD_EMISSION_RATE;
            if (towerOnline && isPowered)
            {
                targetRate += 3f;
            }

            EmissionRate = targetRate;
            var emission = m_particles.emission;
            emission.rateOverTime = targetRate;
            _applyPresentation();
        }

        public void SetPaused(bool paused)
        {
            if (m_particles == null)
            {
                return;
            }

            if (paused)
            {
                m_particles.Pause(true);
            }
            else
            {
                m_particles.Play(true);
            }
        }

        private void OnDestroy()
        {
            if (m_comfortSettings != null)
            {
                m_comfortSettings.HighContrastChanged -= _handleHighContrastChanged;
            }

            if (m_material != null)
            {
                Destroy(m_material);
            }
        }

        private void _createMaterial(ParticleSystemRenderer particleRenderer, Texture2D texture)
        {
            var template = Resources.Load<Material>(RUNTIME_PARTICLE_MATERIAL_RESOURCE);
            if (template != null)
            {
                m_material = new Material(template);
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                if (shader == null)
                {
                    Debug.LogWarning("DEAD SIGNAL could not find a particle shader; Signal dust will use Unity's default material.");
                    return;
                }

                m_material = new Material(shader);
            }

            m_material.name = "Signal Dust Runtime Material";
            m_material.mainTexture = texture;
            m_material.renderQueue = 3000;
            m_material.SetFloat("_Surface", 1f);
            m_material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particleRenderer.sharedMaterial = m_material;
        }

        private void _applyPresentation()
        {
            if (m_particles == null)
            {
                return;
            }

            var main = m_particles.main;
            if (!IsPowered)
            {
                main.startColor = s_deadColor;
                return;
            }

            var poweredColor = m_comfortSettings.HighContrastEnabled ? s_highContrastPoweredColor : s_poweredColor;
            poweredColor.a *= Mathf.Lerp(0.65f, 1f, m_signalRatio);
            main.startColor = poweredColor;
        }

        private void _handleHighContrastChanged(bool enabled)
        {
            _applyPresentation();
        }
    }
}
