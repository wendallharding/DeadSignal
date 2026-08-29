using DeadSignal.Missions;
using UnityEngine;

namespace DeadSignal.World
{
    public enum CargoAnnexPresentationState
    {
        Locked,
        Available,
        Committed,
        Secured
    }

    /// <summary>
    /// Scene-authored spatial contract and presentation for the Cargo Annex coupling retrieval.
    /// </summary>
    public sealed class AuthoredCargoAnnexObjective : MonoBehaviour
    {
        private const float STATE_TRANSITION_SECONDS = 0.35f;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform m_commitmentAnchor;
        [SerializeField] private Transform m_couplingSocket;
        [SerializeField] private Transform m_withdrawalAnchor;
        [SerializeField] private GameObject m_commitmentMarker;
        [SerializeField] private GameObject m_withdrawalMarker;
        [SerializeField] private GameObject m_securedMarker;
        [SerializeField] private Renderer m_couplingStatusRenderer;
        [SerializeField] private Transform m_couplingRotor;

        public CargoCouplingRetrievalPhase Phase => m_retrieval?.Phase ?? CargoCouplingRetrievalPhase.AwaitingCommit;
        public Vector3 CommitmentPosition => m_commitmentAnchor != null ? m_commitmentAnchor.position : transform.position;
        public Vector3 CouplingPosition => m_couplingSocket != null ? m_couplingSocket.position : transform.position;
        public Vector3 WithdrawalPosition => m_withdrawalAnchor != null ? m_withdrawalAnchor.position : transform.position;
        public Vector3 CurrentTargetPosition => Phase switch
        {
            CargoCouplingRetrievalPhase.AwaitingCommit => CommitmentPosition,
            CargoCouplingRetrievalPhase.CouplingAvailable => CouplingPosition,
            CargoCouplingRetrievalPhase.Withdrawing => WithdrawalPosition,
            _ => WithdrawalPosition
        };
        public bool IsComplete => Phase == CargoCouplingRetrievalPhase.Complete;
        public bool IsConfigured => m_commitmentAnchor != null && m_couplingSocket != null && m_withdrawalAnchor != null &&
                                    m_commitmentMarker != null && m_withdrawalMarker != null && m_securedMarker != null;
        public bool HasReadabilityAssets => m_couplingStatusRenderer != null && m_couplingRotor != null;
        public CargoAnnexPresentationState PresentationState { get; private set; } = CargoAnnexPresentationState.Locked;

        private void Awake()
        {
            ResetState();
        }

        private void Update()
        {
            if (m_transitionRemaining <= 0f || m_couplingRotor == null)
            {
                return;
            }

            m_transitionRemaining = Mathf.Max(0f, m_transitionRemaining - Time.unscaledDeltaTime);
            var progress = 1f - m_transitionRemaining / STATE_TRANSITION_SECONDS;
            m_couplingRotor.localRotation = Quaternion.Slerp(m_transitionStart, m_transitionTarget, progress);
        }

        public void Configure(
            Transform commitmentAnchor,
            Transform couplingSocket,
            Transform withdrawalAnchor,
            GameObject commitmentMarker,
            GameObject withdrawalMarker,
            GameObject securedMarker)
        {
            m_commitmentAnchor = commitmentAnchor;
            m_couplingSocket = couplingSocket;
            m_withdrawalAnchor = withdrawalAnchor;
            m_commitmentMarker = commitmentMarker;
            m_withdrawalMarker = withdrawalMarker;
            m_securedMarker = securedMarker;
            ResetState();
        }

        public void ConfigureReadability(Renderer couplingStatusRenderer, Transform couplingRotor)
        {
            m_couplingStatusRenderer = couplingStatusRenderer;
            m_couplingRotor = couplingRotor;
            m_hasAppliedPresentation = false;
            m_transitionRemaining = 0f;
            _applyPresentation(CargoAnnexPresentationState.Locked);
        }

        public void ResetState()
        {
            if (!IsConfigured)
            {
                m_retrieval = null;
                _updatePresentation(false);
                return;
            }

            m_localAxis = _localFlatDirection(m_withdrawalAnchor.localPosition, m_commitmentAnchor.localPosition);
            m_retrieval = new CargoCouplingRetrieval(
                _progress(m_commitmentAnchor.localPosition),
                _progress(m_withdrawalAnchor.localPosition));
            _updatePresentation(false);
        }

        public void ObservePlayer(Vector3 playerPosition, bool objectiveAvailable)
        {
            if (m_retrieval == null)
            {
                return;
            }

            m_retrieval.Observe(_playerProgress(playerPosition), objectiveAvailable);
            _updatePresentation(objectiveAvailable);
        }

        public bool TryTakeCoupling(Vector3 playerPosition, bool objectiveAvailable)
        {
            if (m_retrieval == null ||
                !m_retrieval.TryTakeCoupling(_playerProgress(playerPosition), objectiveAvailable))
            {
                return false;
            }

            _updatePresentation(objectiveAvailable);
            return true;
        }

        public bool CanCompleteWithdrawal(Vector3 playerPosition, bool objectiveAvailable)
        {
            return m_retrieval != null &&
                   m_retrieval.CanCompleteWithdrawal(_playerProgress(playerPosition), objectiveAvailable);
        }

        public void CompleteWithdrawal()
        {
            m_retrieval?.CompleteWithdrawal();
            _updatePresentation(false);
        }

        private static Vector2 _localFlatDirection(Vector3 from, Vector3 to)
        {
            var direction = new Vector2(to.x - from.x, to.z - from.z);
            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        }

        private float _playerProgress(Vector3 playerPosition)
        {
            return _progress(transform.InverseTransformPoint(playerPosition));
        }

        private float _progress(Vector3 localPosition)
        {
            return Vector2.Dot(new Vector2(localPosition.x, localPosition.z), m_localAxis);
        }

        private void _updatePresentation(bool objectiveAvailable)
        {
            if (m_commitmentMarker != null)
            {
                m_commitmentMarker.SetActive(objectiveAvailable && Phase == CargoCouplingRetrievalPhase.AwaitingCommit);
            }
            if (m_withdrawalMarker != null)
            {
                m_withdrawalMarker.SetActive(objectiveAvailable && Phase == CargoCouplingRetrievalPhase.Withdrawing);
            }
            if (m_securedMarker != null)
            {
                m_securedMarker.SetActive(Phase == CargoCouplingRetrievalPhase.Complete);
            }

            var state = Phase switch
            {
                CargoCouplingRetrievalPhase.Withdrawing => CargoAnnexPresentationState.Committed,
                CargoCouplingRetrievalPhase.Complete => CargoAnnexPresentationState.Secured,
                _ when objectiveAvailable => CargoAnnexPresentationState.Available,
                _ => CargoAnnexPresentationState.Locked
            };
            _applyPresentation(state);
        }

        private void _applyPresentation(CargoAnnexPresentationState state)
        {
            if (m_hasAppliedPresentation && PresentationState == state)
            {
                return;
            }

            m_hasAppliedPresentation = true;
            PresentationState = state;
            var color = state switch
            {
                CargoAnnexPresentationState.Locked => new Color(0.18f, 0.07f, 0.06f),
                CargoAnnexPresentationState.Available => new Color(1f, 0.48f, 0.06f),
                CargoAnnexPresentationState.Committed => new Color(0.82f, 0.84f, 0.72f),
                _ => new Color(0.02f, 0.92f, 1f)
            };
            if (m_couplingStatusRenderer != null)
            {
                m_statusProperties ??= new MaterialPropertyBlock();
                m_couplingStatusRenderer.GetPropertyBlock(m_statusProperties);
                m_statusProperties.SetColor(s_baseColor, color);
                m_statusProperties.SetColor(s_emissionColor,
                    color * (state == CargoAnnexPresentationState.Locked ? 0.06f : 1.2f));
                m_couplingStatusRenderer.SetPropertyBlock(m_statusProperties);
            }

            if (m_couplingRotor == null)
            {
                return;
            }

            m_transitionStart = m_couplingRotor.localRotation;
            m_transitionTarget = Quaternion.Euler(0f, state switch
            {
                CargoAnnexPresentationState.Locked => 0f,
                CargoAnnexPresentationState.Available => 45f,
                CargoAnnexPresentationState.Committed => 90f,
                _ => 135f
            }, 0f);
            m_transitionRemaining = STATE_TRANSITION_SECONDS;
        }

        private CargoCouplingRetrieval m_retrieval;
        private MaterialPropertyBlock m_statusProperties;
        private Vector2 m_localAxis = Vector2.right;
        private Quaternion m_transitionStart;
        private Quaternion m_transitionTarget;
        private bool m_hasAppliedPresentation;
        private float m_transitionRemaining;
    }
}
