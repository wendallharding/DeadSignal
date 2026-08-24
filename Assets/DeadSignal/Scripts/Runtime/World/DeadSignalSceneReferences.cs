using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Scene-owned composition contract for the fixed DEAD SIGNAL map and its persistent actors.
    /// Runtime systems may animate these objects, but do not create or position them.
    /// </summary>
    public sealed class DeadSignalSceneReferences : MonoBehaviour
    {
        [Header("Spatial Anchors")]
        [SerializeField] private Transform m_extractionAnchor;
        [SerializeField] private Transform m_towerAnchor;
        [SerializeField] private Transform m_shortcutAnchor;
        [SerializeField] private Transform m_relayTowerAnchor;
        [SerializeField] private Transform m_relayShortcutAnchor;
        [SerializeField] private Transform m_spineTowerAnchor;
        [SerializeField] private Vector2 m_arenaHalfExtents = new(36f, 8.8f);

        [Header("Environment")]
        [SerializeField] private GameObject m_maintenanceDeck;
        [SerializeField] private GameObject m_maintenanceRoomShell;
        [SerializeField] private GameObject m_extractionPad;
        [SerializeField] private GameObject m_signalTower;
        [SerializeField] private GameObject m_signalRouting;
        [SerializeField] private GameObject m_shortcutGate;
        [SerializeField] private GameObject m_stationMachines;
        [SerializeField] private GameObject m_relayFoundry;
        [SerializeField] private GameObject m_relayTower;
        [SerializeField] private GameObject m_relaySignalRouting;
        [SerializeField] private GameObject m_relayShortcutGate;
        [SerializeField] private GameObject m_capacitorSpine;
        [SerializeField] private GameObject m_spineInductionGallery;
        [SerializeField] private GameObject m_spineTower;
        [SerializeField] private GameObject m_spineSignalRouting;

        [Header("Presentation")]
        [SerializeField] private Camera m_playerCamera;
        [SerializeField] private Transform m_cameraRig;
        [SerializeField] private Light m_keyLight;

        [Header("Actors")]
        [SerializeField] private Transform m_player;
        [SerializeField] private Transform m_warden;
        [SerializeField] private Transform m_sapper;
        [SerializeField] private Transform m_interceptor;
        [SerializeField] private Transform m_suppressor;

        public Vector3 ExtractionPosition => m_extractionAnchor.position;
        public Vector3 TowerPosition => m_towerAnchor.position;
        public Vector3 ShortcutPosition => m_shortcutAnchor.position;
        public Vector3 RelayTowerPosition => m_relayTowerAnchor.position;
        public Vector3 RelayShortcutPosition => m_relayShortcutAnchor.position;
        public Vector3 SpineTowerPosition => m_spineTowerAnchor.position;
        public Vector2 ArenaHalfExtents => m_arenaHalfExtents;
        public GameObject MaintenanceDeck => m_maintenanceDeck;
        public GameObject MaintenanceRoomShell => m_maintenanceRoomShell;
        public GameObject ExtractionPad => m_extractionPad;
        public GameObject SignalTower => m_signalTower;
        public GameObject SignalRouting => m_signalRouting;
        public GameObject ShortcutGate => m_shortcutGate;
        public GameObject StationMachines => m_stationMachines;
        public GameObject RelayFoundry => m_relayFoundry;
        public GameObject RelayTower => m_relayTower;
        public GameObject RelaySignalRouting => m_relaySignalRouting;
        public GameObject RelayShortcutGate => m_relayShortcutGate;
        public GameObject CapacitorSpine => m_capacitorSpine;
        public GameObject SpineInductionGallery => m_spineInductionGallery;
        public GameObject SpineTower => m_spineTower;
        public GameObject SpineSignalRouting => m_spineSignalRouting;
        public Camera PlayerCamera => m_playerCamera;
        public Transform CameraRig => m_cameraRig;
        public Light KeyLight => m_keyLight;
        public Transform Player => m_player;
        public Transform Warden => m_warden;
        public Transform Sapper => m_sapper;
        public Transform Interceptor => m_interceptor;
        public Transform Suppressor => m_suppressor;

        public bool IsComplete =>
            m_extractionAnchor != null && m_towerAnchor != null && m_shortcutAnchor != null &&
            m_relayTowerAnchor != null && m_relayShortcutAnchor != null &&
            m_spineTowerAnchor != null &&
            m_maintenanceDeck != null && m_maintenanceRoomShell != null && m_extractionPad != null &&
            m_signalTower != null && m_signalRouting != null && m_shortcutGate != null && m_stationMachines != null &&
            m_relayFoundry != null && m_relayTower != null && m_relaySignalRouting != null && m_relayShortcutGate != null &&
            m_capacitorSpine != null && m_spineInductionGallery != null &&
            m_spineTower != null && m_spineSignalRouting != null &&
            m_playerCamera != null && m_cameraRig != null && m_keyLight != null && m_player != null &&
            m_warden != null && m_sapper != null && m_interceptor != null && m_suppressor != null;

        public string MissingReferences => string.Join(", ", new[]
        {
            m_extractionAnchor == null ? nameof(m_extractionAnchor) : null,
            m_towerAnchor == null ? nameof(m_towerAnchor) : null,
            m_shortcutAnchor == null ? nameof(m_shortcutAnchor) : null,
            m_relayTowerAnchor == null ? nameof(m_relayTowerAnchor) : null,
            m_relayShortcutAnchor == null ? nameof(m_relayShortcutAnchor) : null,
            m_spineTowerAnchor == null ? nameof(m_spineTowerAnchor) : null,
            m_maintenanceDeck == null ? nameof(m_maintenanceDeck) : null,
            m_maintenanceRoomShell == null ? nameof(m_maintenanceRoomShell) : null,
            m_extractionPad == null ? nameof(m_extractionPad) : null,
            m_signalTower == null ? nameof(m_signalTower) : null,
            m_signalRouting == null ? nameof(m_signalRouting) : null,
            m_shortcutGate == null ? nameof(m_shortcutGate) : null,
            m_stationMachines == null ? nameof(m_stationMachines) : null,
            m_relayFoundry == null ? nameof(m_relayFoundry) : null,
            m_relayTower == null ? nameof(m_relayTower) : null,
            m_relaySignalRouting == null ? nameof(m_relaySignalRouting) : null,
            m_relayShortcutGate == null ? nameof(m_relayShortcutGate) : null,
            m_capacitorSpine == null ? nameof(m_capacitorSpine) : null,
            m_spineInductionGallery == null ? nameof(m_spineInductionGallery) : null,
            m_spineTower == null ? nameof(m_spineTower) : null,
            m_spineSignalRouting == null ? nameof(m_spineSignalRouting) : null,
            m_playerCamera == null ? nameof(m_playerCamera) : null,
            m_cameraRig == null ? nameof(m_cameraRig) : null,
            m_keyLight == null ? nameof(m_keyLight) : null,
            m_player == null ? nameof(m_player) : null,
            m_warden == null ? nameof(m_warden) : null,
            m_sapper == null ? nameof(m_sapper) : null,
            m_interceptor == null ? nameof(m_interceptor) : null,
            m_suppressor == null ? nameof(m_suppressor) : null
        });

        public void Configure(
            Transform extractionAnchor, Transform towerAnchor, Transform shortcutAnchor,
            GameObject maintenanceDeck, GameObject maintenanceRoomShell, GameObject extractionPad,
            GameObject signalTower, GameObject signalRouting, GameObject shortcutGate, GameObject stationMachines,
            Camera playerCamera, Transform cameraRig, Light keyLight,
            Transform player, Transform warden, Transform sapper, Transform interceptor, Transform suppressor)
        {
            m_extractionAnchor = extractionAnchor;
            m_towerAnchor = towerAnchor;
            m_shortcutAnchor = shortcutAnchor;
            m_maintenanceDeck = maintenanceDeck;
            m_maintenanceRoomShell = maintenanceRoomShell;
            m_extractionPad = extractionPad;
            m_signalTower = signalTower;
            m_signalRouting = signalRouting;
            m_shortcutGate = shortcutGate;
            m_stationMachines = stationMachines;
            m_playerCamera = playerCamera;
            m_cameraRig = cameraRig;
            m_keyLight = keyLight;
            m_player = player;
            m_warden = warden;
            m_sapper = sapper;
            m_interceptor = interceptor;
            m_suppressor = suppressor;
        }

        private void OnValidate()
        {
            m_arenaHalfExtents.x = Mathf.Max(1f, m_arenaHalfExtents.x);
            m_arenaHalfExtents.y = Mathf.Max(1f, m_arenaHalfExtents.y);
        }
    }
}
