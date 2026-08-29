using System.Collections.Generic;
using Reflex.Attributes;
using UnityEngine;
using DeadSignal.Player;

namespace DeadSignal.Combat
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
        void PlayThreatReaction(Transform target);
        void PlayShieldImpact(Vector3 position);
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
        private const float SHAKE_DURATION = 0.16f;
        private const int DEFAULT_IMPACT_PREWARM_COUNT = 12;
        private const int DEFAULT_IMPACT_MAXIMUM_COUNT = 16;
        private const int DEFAULT_SPARK_PREWARM_COUNT = 12;
        private const int DEFAULT_SPARK_MAXIMUM_COUNT = 16;
        private const int DEFAULT_CHAIN_PREWARM_COUNT = 6;
        private const int DEFAULT_CHAIN_MAXIMUM_COUNT = 8;
        private const float DEFAULT_IMPACT_DURATION = 0.22f;
        private const float DEFAULT_SPARK_DURATION = 0.22f;
        private const float DEFAULT_CHAIN_DURATION = 0.18f;
        private const float DEFAULT_REDUCED_FLASH_ALPHA = 0.3f;

        private static readonly Color s_signalTint = Color.white;
        private static readonly Color s_securityTint = new(1f, 0.18f, 0.14f);
        private static readonly Color s_sapperTint = new(1f, 0.14f, 0.72f);

        private readonly List<ImpactVisual> m_impacts = new(DEFAULT_IMPACT_MAXIMUM_COUNT);
        private readonly List<SparkVisual> m_sparks = new(DEFAULT_SPARK_MAXIMUM_COUNT);
        private readonly List<ChainArcVisual> m_chainArcs = new(DEFAULT_CHAIN_MAXIMUM_COUNT);
        private readonly List<ThreatReaction> m_threatReactions = new(DEFAULT_IMPACT_MAXIMUM_COUNT);

        private IComfortSettings m_comfortSettings;
        private CombatFeedbackTuning m_tuning;
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
        public bool IsCameraShakeActive => m_shakeRemaining > 0f;
        public bool ReducedFlashesEnabled => m_comfortSettings?.ReducedFlashesEnabled ?? false;
        public int ActiveImpactCount { get; private set; }
        public int ActiveSparkCount { get; private set; }
        public int ActiveChainArcCount { get; private set; }
        public int ActiveThreatReactionCount { get; private set; }
        public int ImpactPoolSize => m_impacts.Count;
        public int SparkPoolSize => m_sparks.Count;
        public int ChainArcPoolSize => m_chainArcs.Count;
        public int CreatedPooledObjectCount { get; private set; }
        public int ChainArcsPlayed { get; private set; }

        private sealed class ImpactVisual
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Color Tint;
            public float Age;
            public float TargetScale;
            public bool IsActive;
            public bool IsPriority;
        }

        private sealed class SparkVisual
        {
            public GameObject Root;
            public ParticleSystem Particles;
            public float Age;
            public bool IsActive;
            public bool IsPriority;
        }

        private sealed class ChainArcVisual
        {
            public GameObject Root;
            public LineRenderer Renderer;
            public float Age;
            public bool IsActive;
        }

        private sealed class ThreatReaction
        {
            public Transform Target;
            public Vector3 RestScale;
            public float Age;
            public bool IsActive;
        }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
            m_comfortSettings.CameraImpulseChanged += _handleCameraImpulseChanged;
        }

        public void Configure(Camera targetCamera)
        {
            m_tuning = Resources.Load<CombatFeedbackTuning>("Tuning/CombatFeedbackTuning");
            if (m_tuning == null)
            {
                Debug.LogWarning("Combat feedback tuning was not found at Resources/Tuning/CombatFeedbackTuning.", this);
            }

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

            _prewarmPools();
        }

        public void PlaySignalImpact(Vector3 position, bool decisive)
        {
            _playImpact(position, s_signalTint, decisive ? 1.28f : 0.86f, decisive ? 0.2f : 0.11f,
                decisive ? HEAVY_HIT_STOP : LIGHT_HIT_STOP, decisive);
            if (decisive)
            {
                _playImpact(position, new Color(0.2f, 0.95f, 1f), 1.9f, 0f, 0f, true);
            }
        }

        public void PlayThreatReaction(Transform target)
        {
            if (target == null || m_isPaused)
            {
                return;
            }

            var reaction = _acquireThreatReaction();
            reaction.Target = target;
            reaction.RestScale = target.localScale;
            reaction.Age = 0f;
            reaction.IsActive = true;
            ActiveThreatReactionCount++;
        }

        public void PlayShieldImpact(Vector3 position)
        {
            _playImpact(position, new Color(0.18f, 0.72f, 1f), 1.65f, 0.16f, LIGHT_HIT_STOP);
            _playImpact(position, Color.white, 0.72f, 0f, 0f);
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
            _playImpact(position, new Color(1f, 0.58f, 0.16f), 0.82f, 0f, 0f,
                m_environmentImpactSprite, m_environmentImpactMaterial,
                "Bulkhead Signal Impact", false);
            _playImpact(position + Vector3.up * 0.03f, Color.white, 0.38f, 0f, 0f);
        }

        public void PlaySignalRecovery(Vector3 position)
        {
            _playImpact(position, Color.white, 1.45f, 0f, 0f, m_signalRecoverySprite, null, "Signal Recovery Burst", true);
        }

        public void PlaySalvageChain(Vector3 position, int chainCount)
        {
            var tint = chainCount >= 3 ? new Color(1f, 0.72f, 0.12f) : Color.white;
            _playImpact(position, tint, 0.85f + Mathf.Min(chainCount, 3) * 0.22f, 0f, 0f,
                m_salvageChainSprite, null, "Salvage Chain Burst", true);
        }

        public void PlayChainArc(Vector3 start, Vector3 end)
        {
            if (m_isPaused || m_chainArcMaterial == null)
            {
                return;
            }

            var chainArc = _acquireChainArc();
            var line = chainArc.Renderer;
            chainArc.Root.SetActive(true);
            var direction = end - start;
            var side = Vector3.Cross(direction.normalized, Vector3.up) * Mathf.Min(0.45f, direction.magnitude * 0.12f);
            line.SetPosition(0, start);
            line.SetPosition(1, Vector3.Lerp(start, end, 0.34f) + side);
            line.SetPosition(2, Vector3.Lerp(start, end, 0.68f) - side);
            line.SetPosition(3, end);
            line.startColor = _chainArcColor(1f);
            line.endColor = _chainArcColor(0.35f);
            chainArc.Age = 0f;
            if (!chainArc.IsActive)
            {
                chainArc.IsActive = true;
                ActiveChainArcCount++;
            }
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
                _clearTransientVisuals();
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
            _updateSparks(dt);
            _updateChainArcs(dt);
            _updateThreatReactions(dt);
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

        }

        private void _playImpact(
            Vector3 position,
            Color tint,
            float targetScale,
            float shakeIntensity,
            float hitStopDuration,
            bool priority = false)
        {
            _playImpact(position, tint, targetScale, shakeIntensity, hitStopDuration, m_impactSprite, null,
                "Combat Impact Burst", priority);
        }

        private void _playImpact(
            Vector3 position,
            Color tint,
            float targetScale,
            float shakeIntensity,
            float hitStopDuration,
            Sprite sprite,
            Material material,
            string objectName,
            bool priority)
        {
            if (m_isPaused || sprite == null)
            {
                return;
            }

            var impact = _acquireImpact(priority);
            if (impact != null)
            {
                var root = impact.Root;
                root.name = objectName;
                root.SetActive(true);
                root.transform.position = position;
                root.transform.rotation = m_targetCamera != null
                    ? Quaternion.LookRotation(-m_targetCamera.transform.forward, m_targetCamera.transform.up)
                    : Quaternion.Euler(-90f, 0f, 0f);
                root.transform.localScale = Vector3.one * 0.12f;

                var spriteRenderer = impact.Renderer;
                spriteRenderer.sprite = sprite;
                spriteRenderer.sharedMaterial = material;
                spriteRenderer.color = _impactColor(tint, 0f);
                impact.Tint = tint;
                impact.TargetScale = targetScale;
                impact.Age = 0f;
                impact.IsPriority = priority;
                _playDirectionalSparks(position, tint, priority);
            }

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

        private void _playDirectionalSparks(Vector3 position, Color tint, bool priority)
        {
            var spark = _acquireSpark(priority);
            if (spark == null)
            {
                return;
            }

            var root = spark.Root;
            root.SetActive(true);
            root.transform.position = position + Vector3.up * 0.12f;
            var particles = spark.Particles;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.startColor = tint;
            particles.Emit(ReducedFlashesEnabled ? 3 : 6);
            particles.Play();
            spark.Age = 0f;
            spark.IsPriority = priority;
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
            for (var index = 0; index < m_impacts.Count; index++)
            {
                var impact = m_impacts[index];
                if (!impact.IsActive)
                {
                    continue;
                }

                impact.Age += dt;
                float progress = Mathf.Clamp01(impact.Age / _impactDuration());
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                impact.Root.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, impact.TargetScale, easedProgress);
                impact.Renderer.color = _impactColor(impact.Tint, progress);

                if (progress < 1f)
                {
                    continue;
                }

                _deactivateImpact(impact);
            }
        }

        private void _updateSparks(float dt)
        {
            for (var index = 0; index < m_sparks.Count; index++)
            {
                var spark = m_sparks[index];
                if (!spark.IsActive)
                {
                    continue;
                }

                spark.Age += dt;
                if (spark.Age < _sparkDuration())
                {
                    continue;
                }

                _deactivateSpark(spark);
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
            for (var index = 0; index < m_chainArcs.Count; index++)
            {
                var chainArc = m_chainArcs[index];
                if (!chainArc.IsActive)
                {
                    continue;
                }

                chainArc.Age += dt;
                var progress = Mathf.Clamp01(chainArc.Age / _chainDuration());
                chainArc.Renderer.startColor = _chainArcColor(1f - progress);
                chainArc.Renderer.endColor = _chainArcColor((1f - progress) * 0.35f);
                if (progress < 1f)
                {
                    continue;
                }

                chainArc.IsActive = false;
                chainArc.Root.SetActive(false);
                ActiveChainArcCount--;
            }
        }

        private void _updateThreatReactions(float dt)
        {
            for (var index = 0; index < m_threatReactions.Count; index++)
            {
                var reaction = m_threatReactions[index];
                if (!reaction.IsActive)
                {
                    continue;
                }

                if (reaction.Target == null || !reaction.Target.gameObject.activeInHierarchy)
                {
                    _deactivateThreatReaction(reaction, false);
                    continue;
                }

                reaction.Age += dt;
                var progress = Mathf.Clamp01(reaction.Age / 0.16f);
                var punch = Mathf.Sin(progress * Mathf.PI) * 0.12f;
                reaction.Target.localScale = reaction.RestScale * (1f + punch);
                if (progress < 1f)
                {
                    continue;
                }

                reaction.Target.localScale = reaction.RestScale;
                _deactivateThreatReaction(reaction, false);
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
            float maximumAlpha = ReducedFlashesEnabled ? _reducedFlashesMaximumAlpha() : 1f;
            return new Color(tint.r, tint.g, tint.b, maximumAlpha * (1f - progress));
        }

        private Color _chainArcColor(float alpha)
        {
            var maximumAlpha = ReducedFlashesEnabled ? _reducedFlashesMaximumAlpha() : 1f;
            return new Color(0.18f, 0.95f, 1f, alpha * maximumAlpha);
        }

        private void _prewarmPools()
        {
            while (m_impacts.Count < _impactPrewarmCount())
            {
                m_impacts.Add(_createImpactVisual());
            }

            while (m_sparks.Count < _sparkPrewarmCount())
            {
                m_sparks.Add(_createSparkVisual());
            }

            while (m_chainArcs.Count < _chainPrewarmCount())
            {
                m_chainArcs.Add(_createChainArcVisual());
            }

            while (m_threatReactions.Count < DEFAULT_IMPACT_MAXIMUM_COUNT)
            {
                m_threatReactions.Add(new ThreatReaction());
            }
        }

        private ImpactVisual _createImpactVisual()
        {
            var root = new GameObject("Pooled Combat Impact");
            root.transform.SetParent(transform, false);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 30;
            root.SetActive(false);
            CreatedPooledObjectCount++;
            return new ImpactVisual { Root = root, Renderer = renderer };
        }

        private SparkVisual _createSparkVisual()
        {
            var root = new GameObject("Directional Impact Sparks");
            root.transform.SetParent(transform, false);
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.12f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.gravityModifier = 0.35f;
            main.stopAction = ParticleSystemStopAction.None;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.08f;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            root.SetActive(false);
            CreatedPooledObjectCount++;
            return new SparkVisual { Root = root, Particles = particles };
        }

        private ChainArcVisual _createChainArcVisual()
        {
            var root = new GameObject("Chain Arc Link");
            root.transform.SetParent(transform, false);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = m_chainArcMaterial;
            line.useWorldSpace = true;
            line.positionCount = 4;
            line.startWidth = 0.075f;
            line.endWidth = 0.025f;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.sortingOrder = 31;
            root.SetActive(false);
            CreatedPooledObjectCount++;
            return new ChainArcVisual { Root = root, Renderer = line };
        }

        private ImpactVisual _acquireImpact(bool priority)
        {
            for (var index = 0; index < m_impacts.Count; index++)
            {
                if (!m_impacts[index].IsActive)
                {
                    m_impacts[index].IsActive = true;
                    ActiveImpactCount++;
                    return m_impacts[index];
                }
            }

            if (m_impacts.Count < _impactMaximumCount())
            {
                var created = _createImpactVisual();
                created.IsActive = true;
                m_impacts.Add(created);
                ActiveImpactCount++;
                return created;
            }

            ImpactVisual oldest = null;
            for (var index = 0; index < m_impacts.Count; index++)
            {
                var candidate = m_impacts[index];
                if (candidate.IsPriority || (oldest != null && candidate.Age <= oldest.Age))
                {
                    continue;
                }

                oldest = candidate;
            }

            if (oldest == null && priority)
            {
                oldest = m_impacts[0];
                for (var index = 1; index < m_impacts.Count; index++)
                {
                    if (m_impacts[index].Age > oldest.Age)
                    {
                        oldest = m_impacts[index];
                    }
                }
            }

            return oldest;
        }

        private SparkVisual _acquireSpark(bool priority)
        {
            for (var index = 0; index < m_sparks.Count; index++)
            {
                if (!m_sparks[index].IsActive)
                {
                    m_sparks[index].IsActive = true;
                    ActiveSparkCount++;
                    return m_sparks[index];
                }
            }

            if (m_sparks.Count < _sparkMaximumCount())
            {
                var created = _createSparkVisual();
                created.IsActive = true;
                m_sparks.Add(created);
                ActiveSparkCount++;
                return created;
            }

            SparkVisual oldest = null;
            for (var index = 0; index < m_sparks.Count; index++)
            {
                var candidate = m_sparks[index];
                if (candidate.IsPriority || (oldest != null && candidate.Age <= oldest.Age))
                {
                    continue;
                }

                oldest = candidate;
            }

            if (oldest == null && priority)
            {
                oldest = m_sparks[0];
                for (var index = 1; index < m_sparks.Count; index++)
                {
                    if (m_sparks[index].Age > oldest.Age)
                    {
                        oldest = m_sparks[index];
                    }
                }
            }

            return oldest;
        }

        private ChainArcVisual _acquireChainArc()
        {
            for (var index = 0; index < m_chainArcs.Count; index++)
            {
                if (!m_chainArcs[index].IsActive)
                {
                    return m_chainArcs[index];
                }
            }

            if (m_chainArcs.Count < _chainMaximumCount())
            {
                var created = _createChainArcVisual();
                m_chainArcs.Add(created);
                return created;
            }

            var oldest = m_chainArcs[0];
            for (var index = 1; index < m_chainArcs.Count; index++)
            {
                if (m_chainArcs[index].Age > oldest.Age)
                {
                    oldest = m_chainArcs[index];
                }
            }

            return oldest;
        }

        private ThreatReaction _acquireThreatReaction()
        {
            for (var index = 0; index < m_threatReactions.Count; index++)
            {
                if (!m_threatReactions[index].IsActive)
                {
                    return m_threatReactions[index];
                }
            }

            var oldest = m_threatReactions[0];
            for (var index = 1; index < m_threatReactions.Count; index++)
            {
                if (m_threatReactions[index].Age > oldest.Age)
                {
                    oldest = m_threatReactions[index];
                }
            }

            _deactivateThreatReaction(oldest, true);
            return oldest;
        }

        private void _deactivateImpact(ImpactVisual impact)
        {
            impact.IsActive = false;
            impact.IsPriority = false;
            impact.Root.SetActive(false);
            ActiveImpactCount--;
        }

        private void _deactivateSpark(SparkVisual spark)
        {
            spark.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            spark.IsActive = false;
            spark.IsPriority = false;
            spark.Root.SetActive(false);
            ActiveSparkCount--;
        }

        private void _deactivateThreatReaction(ThreatReaction reaction, bool restoreScale)
        {
            if (restoreScale && reaction.Target != null)
            {
                reaction.Target.localScale = reaction.RestScale;
            }

            reaction.Target = null;
            reaction.IsActive = false;
            ActiveThreatReactionCount--;
        }

        private void _clearTransientVisuals()
        {
            for (var index = 0; index < m_impacts.Count; index++)
            {
                if (m_impacts[index].IsActive)
                {
                    _deactivateImpact(m_impacts[index]);
                }
            }

            for (var index = 0; index < m_sparks.Count; index++)
            {
                if (m_sparks[index].IsActive)
                {
                    _deactivateSpark(m_sparks[index]);
                }
            }

            for (var index = 0; index < m_chainArcs.Count; index++)
            {
                if (!m_chainArcs[index].IsActive)
                {
                    continue;
                }

                m_chainArcs[index].IsActive = false;
                m_chainArcs[index].Root.SetActive(false);
                ActiveChainArcCount--;
            }

            for (var index = 0; index < m_threatReactions.Count; index++)
            {
                if (m_threatReactions[index].IsActive)
                {
                    _deactivateThreatReaction(m_threatReactions[index], true);
                }
            }
        }

        private int _impactPrewarmCount() => m_tuning == null ? DEFAULT_IMPACT_PREWARM_COUNT : m_tuning.ImpactPrewarmCount;
        private int _impactMaximumCount() => m_tuning == null ? DEFAULT_IMPACT_MAXIMUM_COUNT : m_tuning.ImpactMaximumCount;
        private float _impactDuration() => m_tuning == null ? DEFAULT_IMPACT_DURATION : m_tuning.ImpactDuration;
        private int _sparkPrewarmCount() => m_tuning == null ? DEFAULT_SPARK_PREWARM_COUNT : m_tuning.SparkPrewarmCount;
        private int _sparkMaximumCount() => m_tuning == null ? DEFAULT_SPARK_MAXIMUM_COUNT : m_tuning.SparkMaximumCount;
        private float _sparkDuration() => m_tuning == null ? DEFAULT_SPARK_DURATION : m_tuning.SparkDuration;
        private int _chainPrewarmCount() => m_tuning == null ? DEFAULT_CHAIN_PREWARM_COUNT : m_tuning.ChainPrewarmCount;
        private int _chainMaximumCount() => m_tuning == null ? DEFAULT_CHAIN_MAXIMUM_COUNT : m_tuning.ChainMaximumCount;
        private float _chainDuration() => m_tuning == null ? DEFAULT_CHAIN_DURATION : m_tuning.ChainDuration;
        private float _reducedFlashesMaximumAlpha() =>
            m_tuning == null ? DEFAULT_REDUCED_FLASH_ALPHA : m_tuning.ReducedFlashesMaximumAlpha;
    }
}
