using System.Collections.Generic;
using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal
{
    internal interface ICombatFeedback
    {
        bool IsFrozen { get; }
        bool IsPaused { get; }
        bool HasImpactTexture { get; }

        void Configure(Camera targetCamera);
        void PlaySignalImpact(Vector3 position, bool decisive);
        void PlaySecurityImpact(Vector3 position);
        void PlaySapperImpact(Vector3 position);
        void SetPaused(bool paused);
    }

    /// <summary>
    /// Owns short-lived combat presentation and all temporary time-scale changes.
    /// This keeps hit-stop and pause from competing over global Unity state.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class CombatFeedbackController : MonoBehaviour, ICombatFeedback
    {
        private const string IMPACT_TEXTURE_PATH = "VFX/MaintenanceSignalImpact";
        private const float LIGHT_HIT_STOP = 0.035f;
        private const float HEAVY_HIT_STOP = 0.06f;
        private const float IMPACT_DURATION = 0.22f;
        private const float SHAKE_DURATION = 0.16f;
        private const float REDUCED_FLASH_ALPHA = 0.3f;

        private static readonly Color s_signalTint = Color.white;
        private static readonly Color s_securityTint = new(1f, 0.18f, 0.14f);
        private static readonly Color s_sapperTint = new(1f, 0.14f, 0.72f);

        private readonly List<ImpactVisual> m_impacts = new();

        private IComfortSettings m_comfortSettings;
        private Camera m_targetCamera;
        private Texture2D m_impactTexture;
        private Sprite m_impactSprite;
        private Vector3 m_cameraRestPosition;
        private float m_hitStopEndsAt;
        private float m_shakeRemaining;
        private float m_shakeIntensity;
        private float m_shakePhase;
        private bool m_isPaused;

        public bool IsFrozen => m_isPaused || IsHitStopped;
        public bool IsPaused => m_isPaused;
        public bool IsHitStopped => m_hitStopEndsAt > 0f;
        public bool HasImpactTexture => m_impactTexture != null;
        public bool CameraImpulseEnabled => m_comfortSettings?.CameraImpulseEnabled ?? true;
        public bool ReducedFlashesEnabled => m_comfortSettings?.ReducedFlashesEnabled ?? false;
        public int ActiveImpactCount => m_impacts.Count;

        private sealed class ImpactVisual
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Color Tint;
            public float Age;
            public float TargetScale;
        }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
            m_comfortSettings.CameraImpulseChanged += _handleCameraImpulseChanged;
        }

        public void Configure(Camera targetCamera)
        {
            m_targetCamera = targetCamera;
            if (m_targetCamera != null)
            {
                m_cameraRestPosition = m_targetCamera.transform.localPosition;
            }

            m_impactTexture = Resources.Load<Texture2D>(IMPACT_TEXTURE_PATH);
            if (m_impactTexture == null)
            {
                Debug.LogWarning($"Combat impact texture was not found at Resources/{IMPACT_TEXTURE_PATH}.", this);
                return;
            }

            float pixelsPerUnit = m_impactTexture.width / 3.2f;
            m_impactSprite = Sprite.Create(
                m_impactTexture,
                new Rect(0f, 0f, m_impactTexture.width, m_impactTexture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            m_impactSprite.name = "Maintenance Signal Impact Sprite";
        }

        public void PlaySignalImpact(Vector3 position, bool decisive)
        {
            _playImpact(position, s_signalTint, decisive ? 1.28f : 0.86f, decisive ? 0.2f : 0.11f,
                decisive ? HEAVY_HIT_STOP : LIGHT_HIT_STOP);
        }

        public void PlaySecurityImpact(Vector3 position)
        {
            _playImpact(position, s_securityTint, 1.18f, 0.24f, HEAVY_HIT_STOP);
        }

        public void PlaySapperImpact(Vector3 position)
        {
            _playImpact(position, s_sapperTint, 1.35f, 0.2f, HEAVY_HIT_STOP);
        }

        public void SetPaused(bool paused)
        {
            m_isPaused = paused;
            if (paused)
            {
                m_hitStopEndsAt = 0f;
                Time.timeScale = 0f;
                _resetCamera();
                return;
            }

            Time.timeScale = 1f;
        }

        private void Update()
        {
            _updateHitStop();
            if (IsFrozen)
            {
                return;
            }

            float dt = Time.deltaTime;
            _updateImpacts(dt);
            _updateCameraShake(dt);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            _resetCamera();

            if (m_comfortSettings != null)
            {
                m_comfortSettings.CameraImpulseChanged -= _handleCameraImpulseChanged;
            }

            if (m_impactSprite != null)
            {
                Destroy(m_impactSprite);
            }
        }

        private void _playImpact(Vector3 position, Color tint, float targetScale, float shakeIntensity, float hitStopDuration)
        {
            if (m_isPaused || m_impactSprite == null)
            {
                return;
            }

            var root = new GameObject("Combat Impact Burst");
            root.transform.SetParent(transform, true);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            root.transform.localScale = Vector3.one * 0.12f;

            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = m_impactSprite;
            spriteRenderer.color = _impactColor(tint, 0f);
            spriteRenderer.sortingOrder = 30;

            m_impacts.Add(new ImpactVisual
            {
                Root = root,
                Renderer = spriteRenderer,
                Tint = tint,
                TargetScale = targetScale
            });

            if (CameraImpulseEnabled)
            {
                m_shakeRemaining = Mathf.Max(m_shakeRemaining, SHAKE_DURATION);
                m_shakeIntensity = Mathf.Max(m_shakeIntensity, shakeIntensity);
            }

            m_hitStopEndsAt = Mathf.Max(m_hitStopEndsAt, Time.realtimeSinceStartup + hitStopDuration);
            Time.timeScale = 0f;
        }

        private void _updateHitStop()
        {
            if (!IsHitStopped || Time.realtimeSinceStartup < m_hitStopEndsAt)
            {
                return;
            }

            m_hitStopEndsAt = 0f;
            if (!m_isPaused)
            {
                Time.timeScale = 1f;
            }
        }

        private void _updateImpacts(float dt)
        {
            for (int index = m_impacts.Count - 1; index >= 0; index--)
            {
                ImpactVisual impact = m_impacts[index];
                impact.Age += dt;
                float progress = Mathf.Clamp01(impact.Age / IMPACT_DURATION);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                impact.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, impact.TargetScale, easedProgress);
                impact.Renderer.color = _impactColor(impact.Tint, progress);

                if (progress < 1f)
                {
                    continue;
                }

                Destroy(impact.Root);
                m_impacts.RemoveAt(index);
            }
        }

        private void _updateCameraShake(float dt)
        {
            if (m_targetCamera == null)
            {
                return;
            }

            if (!CameraImpulseEnabled)
            {
                _clearCameraShake();
                return;
            }

            if (m_shakeRemaining <= 0f)
            {
                _resetCamera();
                return;
            }

            m_shakeRemaining = Mathf.Max(0f, m_shakeRemaining - dt);
            m_shakePhase += dt * 92f;
            float remainingRatio = m_shakeRemaining / SHAKE_DURATION;
            float magnitude = m_shakeIntensity * remainingRatio;
            var offset = new Vector3(Mathf.Sin(m_shakePhase), 0f, Mathf.Cos(m_shakePhase * 1.37f)) * magnitude;
            m_targetCamera.transform.localPosition = m_cameraRestPosition + offset;

            if (m_shakeRemaining <= 0f)
            {
                m_shakeIntensity = 0f;
                _resetCamera();
            }
        }

        private void _handleCameraImpulseChanged(bool enabled)
        {
            if (!enabled)
            {
                _clearCameraShake();
            }
        }

        private void _clearCameraShake()
        {
            m_shakeRemaining = 0f;
            m_shakeIntensity = 0f;
            _resetCamera();
        }

        private void _resetCamera()
        {
            if (m_targetCamera != null)
            {
                m_targetCamera.transform.localPosition = m_cameraRestPosition;
            }
        }

        private Color _impactColor(Color tint, float progress)
        {
            float maximumAlpha = ReducedFlashesEnabled ? REDUCED_FLASH_ALPHA : 1f;
            return new Color(tint.r, tint.g, tint.b, maximumAlpha * (1f - progress));
        }
    }
}
