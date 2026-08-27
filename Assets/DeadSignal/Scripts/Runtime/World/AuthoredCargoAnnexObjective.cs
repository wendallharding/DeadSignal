using DeadSignal.Missions;
using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored spatial contract and presentation for the Cargo Annex coupling retrieval.
    /// </summary>
    public sealed class AuthoredCargoAnnexObjective : MonoBehaviour
    {
        [SerializeField] private Transform m_commitmentAnchor;
        [SerializeField] private Transform m_couplingSocket;
        [SerializeField] private Transform m_withdrawalAnchor;
        [SerializeField] private GameObject m_commitmentMarker;
        [SerializeField] private GameObject m_withdrawalMarker;
        [SerializeField] private GameObject m_securedMarker;

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

        private void Awake()
        {
            ResetState();
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
        }

        private CargoCouplingRetrieval m_retrieval;
        private Vector2 m_localAxis = Vector2.right;
    }
}
