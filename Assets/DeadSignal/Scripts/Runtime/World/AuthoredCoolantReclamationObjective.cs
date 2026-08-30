using DeadSignal.Missions;
using UnityEngine;

namespace DeadSignal.World
{
    public enum CoolantReclamationPresentationState
    {
        Locked,
        FirstBaffle,
        SecondBaffle,
        Release,
        Stable
    }

    /// <summary>
    /// Scene-authored ordered baffle route and presentation for the Coolant Reclamation seal.
    /// </summary>
    public sealed class AuthoredCoolantReclamationObjective : MonoBehaviour
    {
        private const float WAYPOINT_RADIUS = 0.55f;
        private const float RELEASE_RADIUS = 0.65f;
        private const float STATE_TRANSITION_SECONDS = 0.35f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_firstBaffleAnchor;
        [SerializeField] private Transform m_secondBaffleAnchor;
        [SerializeField] private Transform m_sealSocket;
        [SerializeField] private Transform m_releaseAnchor;
        [SerializeField] private GameObject m_firstBaffleMarker;
        [SerializeField] private GameObject m_secondBaffleMarker;
        [SerializeField] private GameObject m_releaseMarker;
        [SerializeField] private GameObject m_stableMarker;
        [SerializeField] private Renderer m_statusRenderer;
        [SerializeField] private Transform m_statusDial;
        [SerializeField] private Renderer m_firstBaffleStatusRenderer;
        [SerializeField] private Renderer m_secondBaffleStatusRenderer;
        [SerializeField] private AuthoredCoolantReclamationHeroFinish m_heroFinish;

        public CoolantSealThreadingPhase Phase => m_threading?.Phase ?? CoolantSealThreadingPhase.AwaitingFirstBaffle;
        public Vector3 FirstBafflePosition => m_firstBaffleAnchor != null ? m_firstBaffleAnchor.position : transform.position;
        public Vector3 SecondBafflePosition => m_secondBaffleAnchor != null ? m_secondBaffleAnchor.position : transform.position;
        public Vector3 SealPosition => m_sealSocket != null ? m_sealSocket.position : transform.position;
        public Vector3 ReleasePosition => m_releaseAnchor != null ? m_releaseAnchor.position : transform.position;
        public Vector3 CurrentTargetPosition => Phase switch
        {
            CoolantSealThreadingPhase.AwaitingFirstBaffle => FirstBafflePosition,
            CoolantSealThreadingPhase.AwaitingSecondBaffle => SecondBafflePosition,
            CoolantSealThreadingPhase.SealAvailable => SealPosition,
            CoolantSealThreadingPhase.Releasing => ReleasePosition,
            _ => ReleasePosition
        };
        public bool IsComplete => Phase == CoolantSealThreadingPhase.Complete;
        public bool IsConfigured => m_firstBaffleAnchor != null && m_secondBaffleAnchor != null &&
                                    m_sealSocket != null && m_releaseAnchor != null &&
                                    m_firstBaffleMarker != null && m_secondBaffleMarker != null &&
                                    m_releaseMarker != null && m_stableMarker != null;
        public bool HasReadabilityAssets => m_statusRenderer != null && m_statusDial != null &&
                                            m_firstBaffleStatusRenderer != null && m_secondBaffleStatusRenderer != null;
        public CoolantReclamationPresentationState PresentationState { get; private set; } =
            CoolantReclamationPresentationState.Locked;

        private void Awake()
        {
            ResetState();
        }

        private void Update()
        {
            if (m_transitionRemaining <= 0f || m_statusDial == null)
            {
                return;
            }

            m_transitionRemaining = Mathf.Max(0f, m_transitionRemaining - Time.unscaledDeltaTime);
            var progress = 1f - m_transitionRemaining / STATE_TRANSITION_SECONDS;
            m_statusDial.localRotation = Quaternion.Slerp(m_transitionStart, m_transitionTarget, progress);
        }

        public void Configure(
            Transform firstBaffleAnchor,
            Transform secondBaffleAnchor,
            Transform sealSocket,
            Transform releaseAnchor,
            GameObject firstBaffleMarker,
            GameObject secondBaffleMarker,
            GameObject releaseMarker,
            GameObject stableMarker)
        {
            m_firstBaffleAnchor = firstBaffleAnchor;
            m_secondBaffleAnchor = secondBaffleAnchor;
            m_sealSocket = sealSocket;
            m_releaseAnchor = releaseAnchor;
            m_firstBaffleMarker = firstBaffleMarker;
            m_secondBaffleMarker = secondBaffleMarker;
            m_releaseMarker = releaseMarker;
            m_stableMarker = stableMarker;
            ResetState();
        }

        public void ConfigureReadability(
            Renderer statusRenderer,
            Transform statusDial,
            Renderer firstBaffleStatusRenderer,
            Renderer secondBaffleStatusRenderer)
        {
            m_statusRenderer = statusRenderer;
            m_statusDial = statusDial;
            m_firstBaffleStatusRenderer = firstBaffleStatusRenderer;
            m_secondBaffleStatusRenderer = secondBaffleStatusRenderer;
            m_hasAppliedPresentation = false;
            m_transitionRemaining = 0f;
            _applyPresentation(CoolantReclamationPresentationState.Locked);
        }

        public void ConfigureHeroFinish(AuthoredCoolantReclamationHeroFinish heroFinish)
        {
            m_heroFinish = heroFinish;
            m_heroFinish?.ApplyState(PresentationState);
        }

        public void ResetState()
        {
            if (!IsConfigured)
            {
                m_threading = null;
                _updatePresentation(false);
                return;
            }

            var first = m_firstBaffleAnchor.localPosition;
            var second = m_secondBaffleAnchor.localPosition;
            m_threading = new CoolantSealThreading(first.x, first.z, second.x, second.z, WAYPOINT_RADIUS);
            _updatePresentation(false);
        }

        public void ObservePlayer(Vector3 playerPosition, bool objectiveAvailable)
        {
            if (m_threading == null)
            {
                return;
            }

            var localPosition = transform.InverseTransformPoint(playerPosition);
            m_threading.Observe(localPosition.x, localPosition.z, objectiveAvailable);
            _updatePresentation(objectiveAvailable);
        }

        public bool TryReleaseSeal(bool objectiveAvailable)
        {
            if (m_threading == null || !m_threading.TryReleaseSeal(objectiveAvailable))
            {
                return false;
            }

            _updatePresentation(objectiveAvailable);
            return true;
        }

        public bool CanCompleteRelease(Vector3 playerPosition, bool objectiveAvailable)
        {
            return m_threading != null && m_threading.CanCompleteRelease(
                DeadSignalWorld.FlatDistance(playerPosition, ReleasePosition) <= RELEASE_RADIUS,
                objectiveAvailable);
        }

        public void CompleteRelease()
        {
            m_threading?.CompleteRelease();
            _updatePresentation(false);
        }

        private void _updatePresentation(bool objectiveAvailable)
        {
            if (m_firstBaffleMarker != null)
            {
                m_firstBaffleMarker.SetActive(objectiveAvailable && Phase == CoolantSealThreadingPhase.AwaitingFirstBaffle);
            }
            if (m_secondBaffleMarker != null)
            {
                m_secondBaffleMarker.SetActive(objectiveAvailable && Phase == CoolantSealThreadingPhase.AwaitingSecondBaffle);
            }
            if (m_releaseMarker != null)
            {
                m_releaseMarker.SetActive(objectiveAvailable && Phase == CoolantSealThreadingPhase.Releasing);
            }
            if (m_stableMarker != null)
            {
                m_stableMarker.SetActive(Phase == CoolantSealThreadingPhase.Complete);
            }

            var state = Phase switch
            {
                CoolantSealThreadingPhase.AwaitingSecondBaffle => CoolantReclamationPresentationState.SecondBaffle,
                CoolantSealThreadingPhase.SealAvailable => CoolantReclamationPresentationState.Release,
                CoolantSealThreadingPhase.Releasing => CoolantReclamationPresentationState.Release,
                CoolantSealThreadingPhase.Complete => CoolantReclamationPresentationState.Stable,
                _ when objectiveAvailable => CoolantReclamationPresentationState.FirstBaffle,
                _ => CoolantReclamationPresentationState.Locked
            };
            _applyPresentation(state);
        }

        private void _applyPresentation(CoolantReclamationPresentationState state)
        {
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            m_heroFinish?.ApplyState(state);
            var locked = new Color(0.18f, 0.07f, 0.06f);
            var dormant = new Color(0.12f, 0.18f, 0.2f);
            var amber = new Color(1f, 0.48f, 0.06f);
            var cyan = new Color(0.02f, 0.92f, 1f);
            var statusColor = state switch
            {
                CoolantReclamationPresentationState.Locked => locked,
                CoolantReclamationPresentationState.FirstBaffle => amber,
                CoolantReclamationPresentationState.SecondBaffle => new Color(0.82f, 0.84f, 0.72f),
                CoolantReclamationPresentationState.Release => amber,
                _ => cyan
            };
            _setRendererColor(m_statusRenderer, statusColor,
                state == CoolantReclamationPresentationState.Locked ? 0.06f : 1.15f);

            var firstColor = state switch
            {
                CoolantReclamationPresentationState.Locked => locked,
                CoolantReclamationPresentationState.FirstBaffle => amber,
                _ => cyan
            };
            var secondColor = state switch
            {
                CoolantReclamationPresentationState.Locked => locked,
                CoolantReclamationPresentationState.FirstBaffle => dormant,
                CoolantReclamationPresentationState.SecondBaffle => amber,
                _ => cyan
            };
            _setRendererColor(m_firstBaffleStatusRenderer, firstColor,
                state == CoolantReclamationPresentationState.Locked ? 0.06f : 1.2f);
            _setRendererColor(m_secondBaffleStatusRenderer, secondColor,
                state == CoolantReclamationPresentationState.Locked ? 0.06f : 1.2f);

            if (m_statusDial == null)
            {
                return;
            }

            m_transitionStart = m_statusDial.localRotation;
            m_transitionTarget = Quaternion.Euler(0f, state switch
            {
                CoolantReclamationPresentationState.Locked => 0f,
                CoolantReclamationPresentationState.FirstBaffle => 36f,
                CoolantReclamationPresentationState.SecondBaffle => 108f,
                CoolantReclamationPresentationState.Release => 180f,
                _ => 270f
            }, 0f);
            m_transitionRemaining = STATE_TRANSITION_SECONDS;
        }

        private void _setRendererColor(Renderer target, Color color, float emissionMultiplier)
        {
            if (target == null)
            {
                return;
            }

            m_statusProperties ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(m_statusProperties);
            m_statusProperties.SetColor(s_baseColor, color);
            m_statusProperties.SetColor(s_emissionColor, color * emissionMultiplier);
            target.SetPropertyBlock(m_statusProperties);
        }

        private CoolantSealThreading m_threading;
        private MaterialPropertyBlock m_statusProperties;
        private Quaternion m_transitionStart;
        private Quaternion m_transitionTarget;
        private bool m_hasAppliedPresentation;
        private float m_transitionRemaining;
    }
}
