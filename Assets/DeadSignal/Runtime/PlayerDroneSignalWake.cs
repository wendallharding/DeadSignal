using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal
{
    /// <summary>
    /// Owns the maintenance drone's speed-reactive, presentation-only Signal wake.
    /// </summary>
    public sealed class PlayerDroneSignalWake : MonoBehaviour
    {
        private const string TEXTURE_RESOURCE = "VFX/PlayerDroneSignalWake";

        private TrailRenderer[] m_trails;
        private Transform m_emitterRoot;
        private PlayerDroneMovementTuning m_tuning;
        private Material m_material;
        private bool m_paused;

        public bool HasTexture { get; private set; }
        public bool IsEmitting => m_trails != null && m_trails.Length > 0 && m_trails[0].emitting;
        public int TrailCount => m_trails?.Length ?? 0;

        public void Configure(PlayerDroneMovementTuning tuning)
        {
            m_tuning = tuning;
            var texture = Resources.Load<Texture2D>(TEXTURE_RESOURCE);
            HasTexture = texture != null;
            if (!HasTexture)
            {
                Debug.LogError($"Player drone Signal wake texture is missing at Resources/{TEXTURE_RESOURCE}.", this);
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("URP Unlit shader is unavailable for the player drone Signal wake.", this);
                return;
            }

            m_material = new Material(shader)
            {
                name = "Player Drone Signal Wake (Runtime)",
                mainTexture = texture,
                renderQueue = (int)RenderQueue.Transparent
            };
            m_material.SetFloat("_Surface", 1f);
            m_material.SetFloat("_Blend", 1f);
            m_material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m_material.SetFloat("_DstBlend", (float)BlendMode.One);
            m_material.SetFloat("_ZWrite", 0f);
            m_material.SetColor("_BaseColor", Color.white);
            m_material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            var emitterRoot = new GameObject("Signal Wake Emitters");
            m_emitterRoot = emitterRoot.transform;
            m_emitterRoot.SetParent(transform, false);
            m_trails = new[]
            {
                _createTrail("Signal Wake Left", -m_tuning.WakeEmitterSpacing),
                _createTrail("Signal Wake Right", m_tuning.WakeEmitterSpacing)
            };
            Tick(Vector3.zero);
        }

        public void Tick(Vector3 velocity)
        {
            if (m_trails == null)
            {
                return;
            }

            var speed = velocity.magnitude;
            if (speed > 0.01f)
            {
                m_emitterRoot.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }

            var speedRatio = Mathf.InverseLerp(m_tuning.WakeMinimumSpeed, m_tuning.MaximumSpeed, speed);
            var shouldEmit = !m_paused && speedRatio > 0f;
            foreach (var trail in m_trails)
            {
                trail.emitting = shouldEmit;
                trail.startWidth = Mathf.Lerp(m_tuning.WakeMinimumWidth, m_tuning.WakeMaximumWidth, speedRatio);
            }
        }

        public void SetPaused(bool paused)
        {
            m_paused = paused;
            if (m_trails == null)
            {
                return;
            }

            foreach (var trail in m_trails)
            {
                trail.emitting = false;
                if (paused)
                {
                    trail.Clear();
                }
            }
        }

        public void SetCollisionIntensity(float intensity)
        {
            if (m_material == null)
            {
                return;
            }

            var color = Color.Lerp(Color.white, new Color(1f, 0.42f, 0.08f), Mathf.Clamp01(intensity));
            m_material.SetColor("_BaseColor", color);
        }

        private void OnDestroy()
        {
            if (m_material != null)
            {
                Destroy(m_material);
            }
        }

        private TrailRenderer _createTrail(string trailName, float lateralOffset)
        {
            var trailObject = new GameObject(trailName);
            trailObject.transform.SetParent(m_emitterRoot, false);
            trailObject.transform.localPosition = new Vector3(lateralOffset, 0.18f, -0.35f);
            var trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = m_material;
            trail.time = m_tuning.WakeDuration;
            trail.minVertexDistance = 0.08f;
            trail.endWidth = 0f;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.generateLightingData = false;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            trail.colorGradient = _createColorGradient();
            return trail;
        }

        private static Gradient _createColorGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.8f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.1f, 0.75f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.82f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }
    }
}
