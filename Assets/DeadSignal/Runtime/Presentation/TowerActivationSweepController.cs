using Reflex.Attributes;
using UnityEngine;
using DeadSignal.Player;

namespace DeadSignal.Presentation
{
    internal interface ITowerActivationSweep
    {
        bool HasTexture { get; }
        bool IsPlaying { get; }
        float CurrentAlpha { get; }
        float CurrentDiameter { get; }
        float MaximumDiameter { get; }

        void Configure(Vector3 origin, float poweredRadius);
        void Play();
    }

    /// <summary>
    /// Presents the tower's one-shot network expansion without owning any gameplay state.
    /// </summary>
    public sealed class TowerActivationSweepController : MonoBehaviour, ITowerActivationSweep
    {
        private const string SWEEP_TEXTURE_PATH = "VFX/TowerNetworkActivationSweep";
        private const string SWEEP_OBJECT_NAME = "Tower Network Activation Sweep";
        private const float SWEEP_DURATION = 1.2f;
        private const float STARTING_DIAMETER = 1.1f;
        private const float DIAMETER_PADDING = 2.55f;
        private const float STANDARD_MAXIMUM_ALPHA = 0.78f;
        private const float REDUCED_FLASHES_MAXIMUM_ALPHA = 0.28f;

        private IComfortSettings m_comfortSettings;
        private GameObject m_sweepRoot;
        private SpriteRenderer m_sweepRenderer;
        private Texture2D m_sweepTexture;
        private Sprite m_sweepSprite;
        private float m_elapsed;

        public bool HasTexture => m_sweepTexture != null;
        public bool IsPlaying { get; private set; }
        public float CurrentAlpha { get; private set; }
        public float CurrentDiameter { get; private set; }
        public float MaximumDiameter { get; private set; }

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
        }

        public void Configure(Vector3 origin, float poweredRadius)
        {
            MaximumDiameter = poweredRadius * DIAMETER_PADDING;
            m_sweepTexture = Resources.Load<Texture2D>(SWEEP_TEXTURE_PATH);
            if (m_sweepTexture == null)
            {
                Debug.LogWarning($"Tower activation sweep texture was not found at Resources/{SWEEP_TEXTURE_PATH}.", this);
                return;
            }

            m_sweepSprite = Sprite.Create(
                m_sweepTexture,
                new Rect(0f, 0f, m_sweepTexture.width, m_sweepTexture.height),
                new Vector2(0.5f, 0.5f),
                m_sweepTexture.width);
            m_sweepSprite.name = "Tower Network Activation Sweep Sprite";

            m_sweepRoot = new GameObject(SWEEP_OBJECT_NAME);
            m_sweepRoot.transform.SetParent(transform, false);
            m_sweepRoot.transform.position = origin + Vector3.up * 0.16f;
            m_sweepRoot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            m_sweepRenderer = m_sweepRoot.AddComponent<SpriteRenderer>();
            m_sweepRenderer.sprite = m_sweepSprite;
            m_sweepRenderer.sortingOrder = 20;
            m_sweepRoot.SetActive(false);
        }

        public void Play()
        {
            if (m_sweepRoot == null)
            {
                return;
            }

            m_elapsed = 0f;
            IsPlaying = true;
            m_sweepRoot.SetActive(true);
            _applyProgress(0f);
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            m_elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(m_elapsed / SWEEP_DURATION);
            _applyProgress(progress);
            if (progress < 1f)
            {
                return;
            }

            IsPlaying = false;
            CurrentAlpha = 0f;
            m_sweepRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_sweepSprite != null)
            {
                Destroy(m_sweepSprite);
            }
        }

        private void _applyProgress(float progress)
        {
            var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            CurrentDiameter = Mathf.Lerp(STARTING_DIAMETER, MaximumDiameter, easedProgress);
            m_sweepRoot.transform.localScale = Vector3.one * CurrentDiameter;

            var fadeIn = Mathf.Clamp01(progress / 0.12f);
            var fadeOut = 1f - progress;
            var maximumAlpha = m_comfortSettings.ReducedFlashesEnabled
                ? REDUCED_FLASHES_MAXIMUM_ALPHA
                : STANDARD_MAXIMUM_ALPHA;
            CurrentAlpha = fadeIn * fadeOut * maximumAlpha;
            m_sweepRenderer.color = new Color(1f, 1f, 1f, CurrentAlpha);
        }
    }
}
