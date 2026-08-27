using DeadSignal.Missions;
using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-authored ordered baffle route and presentation for the Coolant Reclamation seal.
    /// </summary>
    public sealed class AuthoredCoolantReclamationObjective : MonoBehaviour
    {
        private const float WAYPOINT_RADIUS = 0.55f;
        private const float RELEASE_RADIUS = 0.65f;

        [SerializeField] private Transform m_firstBaffleAnchor;
        [SerializeField] private Transform m_secondBaffleAnchor;
        [SerializeField] private Transform m_sealSocket;
        [SerializeField] private Transform m_releaseAnchor;
        [SerializeField] private GameObject m_firstBaffleMarker;
        [SerializeField] private GameObject m_secondBaffleMarker;
        [SerializeField] private GameObject m_releaseMarker;
        [SerializeField] private GameObject m_stableMarker;

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

        private void Awake()
        {
            ResetState();
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
        }

        private CoolantSealThreading m_threading;
    }
}
