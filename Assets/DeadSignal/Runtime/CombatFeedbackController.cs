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
        bool HasEnvironmentImpactTexture { get; }
        bool HasSignalRecoveryTexture { get; }
        bool HasSalvageChainTexture { get; }

        void Configure(Camera targetCamera);
        void PlaySignalImpact(Vector3 position, bool decisive);
        void PlaySecurityImpact(Vector3 position);
        void PlaySapperImpact(Vector3 position);
        void PlayEnvironmentImpact(Vector3 position);
        void PlaySignalRecovery(Vector3 position);
        void PlaySalvageChain(Vector3 position, int chainCount);
        void PlayChainArc(Vector3 start, Vector3 end);
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
        private const string ENVIRONMENT_IMPACT_TEXTURE_PATH = "Projectiles/SignalBoltBulkheadImpact";
        private const string ENVIRONMENT_IMPACT_MATERIAL_PATH = "Materials/SignalBoltBulkheadImpact";
        private const string SIGNAL_RECOVERY_TEXTURE_PATH = "VFX/SignalRecoveryBurst";
        private const string SALVAGE_CHAIN_TEXTURE_PATH = "VFX/SalvageChainBurst";
        private const float LIGHT_HIT_STOP = 0.035f;
        private const float HEAVY_HIT_STOP = 0.06f;
        private const float IMPACT_DURATION = 0.22f;
        private const float SHAKE_DURATION = 0.16f;
        private const float REDUCED_FLASH_ALPHA = 0.3f;

        private static readonly Color s_signalTint = Color.white;
        private static readonly Color s_securityTint = new(1f, 0.18f, 0.14f);
        private static readonly Color s_sapperTint = new(1f, 0.14f, 0.72f);

        private readonly List<ImpactVisual> m_impacts = new();
        private readonly List<ChainArcVisual> m_chainArcs = new();

        private IComfortSettings m_comfortSettings;
        private Camera m_targetCamera;
        private Texture2D m_impactTexture;
        private Sprite m_impactSprite;
        private Texture2D m_environmentImpactTexture;
        private Sprite m_environmentImpactSprite;
        private Texture2D m_signalRecoveryTexture;
        private Sprite m_signalRecoverySprite;
        private Texture2D m_salvageChainTexture;
        private Sprite m_salvageChainSprite;
        private Material m_environmentImpactMaterial;
        private Material m_chainArcMaterial;
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
        public bool HasEnvironmentImpactTexture => m_environmentImpactTexture != null && m_environmentImpactMaterial != null;
        public bool HasSignalRecoveryTexture => m_signalRecoveryTexture != null;
        public bool HasSalvageChainTexture => m_salvageChainTexture != null;
        public bool CameraImpulseEnabled => m_comfortSettings?.CameraImpulseEnabled ?? true;
        public bool ReducedFlashesEnabled => m_comfortSettings?.ReducedFlashesEnabled ?? false;
        public int ActiveImpactCount => m_impacts.Count;
        public int ChainArcsPlayed { get; private set; }

        private sealed class ImpactVisual
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Color Tint;
            public float Age;
            public float TargetScale;
        }

        private sealed class ChainArcVisual
        {
            public GameObject Root;
            public LineRenderer Renderer;
            public float Age;
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

            m_environmentImpactTexture = Resources.Load<Texture2D>(ENVIRONMENT_IMPACT_TEXTURE_PATH);
            m_environmentImpactMaterial = Resources.Load<Material>(ENVIRONMENT_IMPACT_MATERIAL_PATH);
            if (m_environmentImpactTexture == null || m_environmentImpactMaterial == null)
            {
                Debug.LogWarning("Signal bolt bulkhead-impact art is missing from Resources.", this);
                return;
            }

            m_chainArcMaterial = Resources.Load<Material>("Materials/SignalBoltEnergy");

            pixelsPerUnit = m_environmentImpactTexture.width / 2.4f;
            m_environmentImpactSprite = Sprite.Create(
                m_environmentImpactTexture,
                new Rect(0f, 0f, m_environmentImpactTexture.width, m_environmentImpactTexture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            m_environmentImpactSprite.name = "Signal Bolt Bulkhead Impact Sprite";

            m_signalRecoveryTexture = Resources.Load<Texture2D>(SIGNAL_RECOVERY_TEXTURE_PATH);
            if (m_signalRecoveryTexture == null)
            {
                Debug.LogWarning($"Signal recovery art was not found at Resources/{SIGNAL_RECOVERY_TEXTURE_PATH}.", this);
                return;
            }

            pixelsPerUnit = m_signalRecoveryTexture.width / 3.4f;
            m_signalRecoverySprite = Sprite.Create(m_signalRecoveryTexture,
                new Rect(0f, 0f, m_signalRecoveryTexture.width, m_signalRecoveryTexture.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
            m_signalRecoverySprite.name = "Signal Recovery Burst Sprite";

            m_salvageChainTexture = Resources.Load<Texture2D>(SALVAGE_CHAIN_TEXTURE_PATH);
            if (m_salvageChainTexture == null)
            {
                Debug.LogWarning($"Salvage chain art was not found at Resources/{SALVAGE_CHAIN_TEXTURE_PATH}.", this);
                return;
            }

            pixelsPerUnit = m_salvageChainTexture.width / 3.4f;
            m_salvageChainSprite = Sprite.Create(m_salvageChainTexture,
                new Rect(0f, 0f, m_salvageChainTexture.width, m_salvageChainTexture.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
            m_salvageChainSprite.name = "Salvage Chain Burst Sprite";
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

        public void PlayEnvironmentImpact(Vector3 position)
        {
            _playImpact(position, Color.white, 0.72f, 0f, 0f, m_environmentImpactSprite, m_environmentImpactMaterial,
                "Bulkhead Signal Impact");
        }

        public void PlaySignalRecovery(Vector3 position)
        {
            _playImpact(position, Color.white, 1.45f, 0f, 0f, m_signalRecoverySprite, null, "Signal Recovery Burst");
        }

        public void PlaySalvageChain(Vector3 position, int chainCount)
        {
            var tint = chainCount >= 3 ? new Color(1f, 0.72f, 0.12f) : Color.white;
            _playImpact(position, tint, 0.85f + Mathf.Min(chainCount, 3) * 0.22f, 0f, 0f,
                m_salvageChainSprite, null, "Salvage Chain Burst");
        }

        public void PlayChainArc(Vector3 start, Vector3 end)
        {
            if (m_isPaused || m_chainArcMaterial == null)
            {
                return;
            }

            var root = new GameObject("Chain Arc Link");
            root.transform.SetParent(transform, true);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = m_chainArcMaterial;
            line.useWorldSpace = true;
            line.positionCount = 4;
            line.startWidth = 0.075f;
            line.endWidth = 0.025f;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            var direction = end - start;
            var side = Vector3.Cross(direction.normalized, Vector3.up) * Mathf.Min(0.45f, direction.magnitude * 0.12f);
            line.SetPositions(new[] { start, Vector3.Lerp(start, end, 0.34f) + side,
                Vector3.Lerp(start, end, 0.68f) - side, end });
            line.startColor = _chainArcColor(1f);
            line.endColor = _chainArcColor(0.35f);
            line.sortingOrder = 31;
            m_chainArcs.Add(new ChainArcVisual { Root = root, Renderer = line });
            ChainArcsPlayed++;
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
            _updateChainArcs(dt);
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

            if (m_environmentImpactSprite != null)
            {
                Destroy(m_environmentImpactSprite);
            }

            if (m_signalRecoverySprite != null)
            {
                Destroy(m_signalRecoverySprite);
            }

            if (m_salvageChainSprite != null)
            {
                Destroy(m_salvageChainSprite);
            }

            foreach (var chainArc in m_chainArcs)
            {
                if (chainArc.Root != null)
                {
                    Destroy(chainArc.Root);
                }
            }
        }

        private void _playImpact(Vector3 position, Color tint, float targetScale, float shakeIntensity, float hitStopDuration)
        {
            _playImpact(position, tint, targetScale, shakeIntensity, hitStopDuration, m_impactSprite, null, "Combat Impact Burst");
        }

        private void _playImpact(
            Vector3 position,
            Color tint,
            float targetScale,
            float shakeIntensity,
            float hitStopDuration,
            Sprite sprite,
            Material material,
            string objectName)
        {
            if (m_isPaused || sprite == null)
            {
                return;
            }

            var root = new GameObject(objectName);
            root.transform.SetParent(transform, true);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            root.transform.localScale = Vector3.one * 0.12f;

            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            if (material != null)
            {
                spriteRenderer.sharedMaterial = material;
            }
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

            if (hitStopDuration > 0f)
            {
                m_hitStopEndsAt = Mathf.Max(m_hitStopEndsAt, Time.realtimeSinceStartup + hitStopDuration);
                Time.timeScale = 0f;
            }
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

        private void _updateChainArcs(float dt)
        {
            for (var index = m_chainArcs.Count - 1; index >= 0; index--)
            {
                var chainArc = m_chainArcs[index];
                chainArc.Age += dt;
                var progress = Mathf.Clamp01(chainArc.Age / 0.18f);
                chainArc.Renderer.startColor = _chainArcColor(1f - progress);
                chainArc.Renderer.endColor = _chainArcColor((1f - progress) * 0.35f);
                if (progress < 1f)
                {
                    continue;
                }

                Destroy(chainArc.Root);
                m_chainArcs.RemoveAt(index);
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

        private Color _chainArcColor(float alpha)
        {
            var maximumAlpha = ReducedFlashesEnabled ? REDUCED_FLASH_ALPHA : 1f;
            return new Color(0.18f, 0.95f, 1f, alpha * maximumAlpha);
        }
    }
}
