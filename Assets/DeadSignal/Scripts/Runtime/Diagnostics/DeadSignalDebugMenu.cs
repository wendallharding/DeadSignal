using OOI.AutoUI;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Missions;

namespace DeadSignal.Diagnostics
{
    public enum DebugLocation
    {
        Extraction,
        CentralTower,
        Shortcut,
        RelayTower,
        CacheOne,
        CacheTwo,
        CacheThree,
        CacheFour
    }

    public enum DebugScenario
    {
        FreshRun,
        TowerActivation,
        FirstOverclock,
        ActiveSalvageChain,
        SapperPulse,
        InterceptorCharge,
        SuppressorExtraction,
        CriticalRecovery,
        OptionalCache,
        StableExtraction,
        OverdriveExtraction,
        Victory,
        Failure
    }

    /// <summary>
    /// Development-only AutoUI feature laboratory. It delegates state changes to DeadSignalGame so normal invariants remain intact.
    /// </summary>
    public sealed class DeadSignalDebugMenu : MonoBehaviour
    {
        private DeadSignalGame m_game;
        private AutoUIProvider m_provider;
        private bool m_isOpen;
        private bool m_runWhileOpen;

        public bool IsOpen => m_isOpen;
        public static bool IsAvailable => IsAllowed(UnityEngine.Application.isEditor, Debug.isDebugBuild);

        public static bool IsAllowed(bool isEditor, bool isDebugBuild) => isEditor || isDebugBuild;

        [BeginLayoutGroup("Overview", width: 520, backgroundColor: 0x071820F2)]
        [Label, HideLabel]
        public string LiveRunState => m_game != null ? m_game.DebugOverview : "Waiting for DEAD SIGNAL runtime...";
        [EndLayoutGroup]

        [BeginLayoutGroup("Scenarios", width: 520, layoutGroupType: LayoutGroupType.Grid, numGridColumns: 2, gridCellHeight: 54,
            backgroundColor: 0x102B35F2)]
        [Button] public void FreshRun() => _scenario(DebugScenario.FreshRun);
        [Button] public void TowerActivation() => _scenario(DebugScenario.TowerActivation);
        [Button] public void FirstOverclock() => _scenario(DebugScenario.FirstOverclock);
        [Button] public void ActiveSalvageChain() => _scenario(DebugScenario.ActiveSalvageChain);
        [Button] public void SapperPulse() => _scenario(DebugScenario.SapperPulse);
        [Button] public void InterceptorCharge() => _scenario(DebugScenario.InterceptorCharge);
        [Button] public void SuppressorExtraction() => _scenario(DebugScenario.SuppressorExtraction);
        [Button] public void CriticalRecovery() => _scenario(DebugScenario.CriticalRecovery);
        [Button] public void OptionalCache() => _scenario(DebugScenario.OptionalCache);
        [Button] public void StableExtraction() => _scenario(DebugScenario.StableExtraction);
        [Button] public void OverdriveExtraction() => _scenario(DebugScenario.OverdriveExtraction);
        [Button] public void Victory() => _scenario(DebugScenario.Victory);
        [Button] public void Failure() => _scenario(DebugScenario.Failure);
        [EndLayoutGroup]

        [BeginLayoutGroup("Run and Player", width: 500, backgroundColor: 0x17242CF2)]
        [EditBox(onChanged: nameof(_setSignal))] public float Signal { get; set; } = RunModel.StartingSignal;
        [Dropdown] public DebugLocation TeleportLocation { get; set; }
        [Button] public void Teleport() => m_game?.DebugTeleport(TeleportLocation);
        [Button] public void ResetDashCooldown() => m_game?.DebugResetDashCooldown();
        [Button] public void ActivateCentralTower() => m_game?.DebugActivateTower();
        [Button] public void ActivateRelayTower() => m_game?.DebugActivateRelayTower();
        [Button] public void OpenShortcut() => m_game?.DebugOpenShortcut();
        [EndLayoutGroup]

        [BeginLayoutGroup("Threats", width: 500, layoutGroupType: LayoutGroupType.Grid, numGridColumns: 2, gridCellHeight: 52,
            backgroundColor: 0x32151AF2)]
        [Button] public void SpawnWarden() => _spawn(SecurityReinforcement.Warden);
        [Button] public void PurgeWarden() => _purge(SecurityReinforcement.Warden);
        [Button] public void SpawnSapper() => _spawn(SecurityReinforcement.Sapper);
        [Button] public void PurgeSapper() => _purge(SecurityReinforcement.Sapper);
        [Button] public void SpawnInterceptor() => _spawn(SecurityReinforcement.Interceptor);
        [Button] public void PurgeInterceptor() => _purge(SecurityReinforcement.Interceptor);
        [Button] public void SpawnSuppressor() => _spawn(SecurityReinforcement.Suppressor);
        [Button] public void PurgeSuppressor() => _purge(SecurityReinforcement.Suppressor);
        [EndLayoutGroup]

        [BeginLayoutGroup("Salvage and Upgrades", width: 500, backgroundColor: 0x33280EF2)]
        [Button] public void CollectNextCache() => m_game?.DebugCollectNextCache();
        [Button] public void SelectChainArc() => m_game?.DebugSelectOverclock(SignalOverclock.ChainArc);
        [Button] public void SelectOverdriveThrusters() => m_game?.DebugSelectOverclock(SignalOverclock.OverdriveThrusters);
        [Button] public void SelectEmergencyCapacitor() => m_game?.DebugSelectAuxiliary(SignalAuxiliaryOverclock.EmergencyCapacitor);
        [Button] public void SelectFeedbackShield() => m_game?.DebugSelectAuxiliary(SignalAuxiliaryOverclock.FeedbackShield);
        [Button] public void SelectPiercingPulse() => m_game?.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
        [Button] public void SelectControlledRicochet() => m_game?.DebugSelectWeapon(SignalWeaponOverclock.ControlledRicochet);
        [EndLayoutGroup]

        [BeginLayoutGroup("Extraction and Presentation", width: 520, backgroundColor: 0x082A2AF2)]
        [Button] public void MakeExtractionReady() => m_game?.DebugMakeExtractionReady();
        [Button] public void BeginStableUplink() => m_game?.DebugBeginExtraction(ExtractionUplinkMode.Stable);
        [Button] public void BeginOverdriveUplink() => m_game?.DebugBeginExtraction(ExtractionUplinkMode.Overdrive);
        [Button] public void CompleteUplink() => m_game?.DebugCompleteExtraction();
        [Button] public void PlayTowerSweep() => m_game?.DebugPlayTowerSweep();
        [Button] public void PlaySignalImpact() => m_game?.DebugPlaySignalImpact();
        [Button] public void PlaySignalRecovery() => m_game?.DebugPlaySignalRecovery();
        [Button] public void PlaySalvageChain() => m_game?.DebugPlaySalvageChain();
        [EndLayoutGroup]

        [BeginLayoutGroup("Accessibility", width: 500, backgroundColor: 0x202020F2)]
        [Toggle(onChanged: nameof(_toggleRunWhileOpen))] public bool RunWhileMenuOpen { get => m_runWhileOpen; set => m_runWhileOpen = value; }
        [Button] public void ToggleSteadyCamera() => m_game?.DebugToggleCameraImpulse();
        [Button] public void ToggleReducedFlashes() => m_game?.DebugToggleReducedFlashes();
        [Button] public void ToggleHighContrast() => m_game?.DebugToggleHighContrast();
        [Button] public void ToggleAudio() => m_game?.DebugToggleAudio();
        [EndLayoutGroup]

        [BeginLayoutGroup("Diagnostics", width: 540, backgroundColor: 0x101010F2)]
        [Label, HideLabel] public string Composition => m_game != null ? m_game.DebugComposition : "Runtime unavailable";
        [Button] public void CopyDiagnosticsToClipboard()
        {
            GUIUtility.systemCopyBuffer = $"{LiveRunState}\n{Composition}";
        }
        [EndLayoutGroup]

        public void Configure(DeadSignalGame game)
        {
            m_game = game;
            Signal = game.CurrentSignal;
            if (!IsAvailable)
            {
                enabled = false;
                return;
            }

            var generatorPrefab = Resources.Load<AutoUIGenerator>("UI/Debug/AutoUI");
            var canvasPrefab = Resources.Load<Canvas>("UI/Debug/Canvas_DebugMenu");
            if (generatorPrefab == null || canvasPrefab == null)
            {
                Debug.LogError("DEAD SIGNAL debug menu could not load the AutoUI runtime prefabs.", this);
                enabled = false;
                return;
            }

            m_provider = gameObject.AddComponent<AutoUIProvider>();
            _configureProvider(generatorPrefab, canvasPrefab);
            m_provider.Generate();
            m_provider.CanvasRef.gameObject.name = "DEAD SIGNAL — Debug Menu";
            m_provider.CanvasRef.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsAvailable || Keyboard.current == null || !Keyboard.current.f5Key.wasPressedThisFrame)
            {
                return;
            }

            _setOpen(!m_isOpen);
        }

        private void OnDisable()
        {
            if (m_isOpen)
            {
                _setOpen(false);
            }
        }

        private void _setOpen(bool open)
        {
            m_isOpen = open;
            m_provider.CanvasRef.gameObject.SetActive(open);
            m_game.SetDebugMenuState(open, m_runWhileOpen);
            Cursor.visible = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void _toggleRunWhileOpen()
        {
            if (m_isOpen)
            {
                m_game.SetDebugMenuState(true, m_runWhileOpen);
            }
        }

        private void _setSignal()
        {
            m_game?.DebugSetSignal(Signal);
        }

        private void _configureProvider(AutoUIGenerator generatorPrefab, Canvas canvasPrefab)
        {
            // AutoUI 1.0 exposes generation but not runtime configuration. Keep its serialized field bridge isolated here so the
            // imported package remains untouched and can be updated independently.
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            var providerType = typeof(AutoUIProvider);
            providerType.GetField("m_autoUIPrefab", FLAGS)?.SetValue(m_provider, generatorPrefab);
            providerType.GetField("m_canvasTemplate", FLAGS)?.SetValue(m_provider, canvasPrefab);
            providerType.GetField("m_autoUIObjects", FLAGS)?.SetValue(m_provider, new List<GameObject> { gameObject });
            providerType.GetField("m_additionalPagePrefabs", FLAGS)?.SetValue(m_provider, new List<GameObject>());
            providerType.GetField("m_pageProviders", FLAGS)?.SetValue(m_provider, new List<MenuScreenProviderBase>());
        }

        private void _scenario(DebugScenario scenario) => m_game?.DebugApplyScenario(scenario);
        private void _spawn(SecurityReinforcement reinforcement) => m_game?.DebugSpawnThreat(reinforcement);
        private void _purge(SecurityReinforcement reinforcement) => m_game?.DebugPurgeThreat(reinforcement);
    }
}
