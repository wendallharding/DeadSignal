using System.Collections.Generic;
using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Builds runtime game objects and consumes scene-authored spatial data for the current prototype map.
    /// </summary>
    internal sealed class DeadSignalWorld
    {
        public const float ARENA_HALF_WIDTH = 20f;
        public const float ARENA_HALF_HEIGHT = 8.8f;
        public const float STARTING_POWER_RADIUS = 3.6f;
        public const float TOWER_POWER_RADIUS = 7.2f;

        public Vector3 ExtractionPosition { get; } = new(-9.2f, 0f, -5.6f);
        public Vector3 TowerPosition { get; } = new(-0.6f, 0f, 0.4f);
        public Vector3 ShortcutPosition { get; } = new(4f, 0f, 0.4f);

        public Camera Camera { get; private set; }
        public Transform Player { get; private set; }
        public Transform PlayerNose { get; private set; }
        public Transform PlayerPresentation { get; private set; }
        public PlayerDroneSignalWake PlayerSignalWake { get; private set; }
        public Transform Warden { get; private set; }
        public WardenThreatTelegraph WardenTelegraph { get; private set; }
        public Transform Sapper { get; private set; }
        public Transform SapperCore { get; private set; }
        public Vector3 SapperCoreBaseScale { get; private set; }
        public Transform Interceptor { get; private set; }
        public Transform InterceptorCore { get; private set; }
        public Transform TowerCore { get; private set; }
        public SignalSapperTelegraph SapperTelegraph { get; private set; }
        public IReadOnlyList<GameObject> SalvagePickups => m_salvagePickups;
        public bool HasMaintenanceDeckAssets { get; private set; }
        public int MaintenanceDeckModuleCount { get; private set; }
        public bool HasMaintenanceRoomShellAssets { get; private set; }
        public int RoomShellBulkheadCount { get; private set; }
        public int MachineSocketCount => m_machineSockets.Count;
        public bool HasSignalTowerAssets { get; private set; }
        public int SignalTowerPartCount { get; private set; }
        public bool HasExtractionPadAssets { get; private set; }
        public int ExtractionPadPartCount { get; private set; }
        public bool HasShortcutGateAssets { get; private set; }
        public int ShortcutGatePartCount { get; private set; }
        public bool HasSignalRoutingAssets { get; private set; }
        public int SignalRoutingPartCount { get; private set; }
        public bool HasStationMachineAssets { get; private set; }
        public int StationMachineInstanceCount { get; private set; }
        public int StationMachinePartCount { get; private set; }
        public bool HasSalvageCacheAssets { get; private set; }
        public int SalvageCacheInstanceCount { get; private set; }
        public int SalvageCachePartCount { get; private set; }
        public bool HasPlayerDroneAssets { get; private set; }
        public int PlayerDronePartCount { get; private set; }
        public bool HasSignalBoltAssets { get; }
        public bool LastSignalBoltUsedAuthoredPrefab { get; private set; }
        public bool HasSignalSapperAssets { get; private set; }
        public int SignalSapperPartCount { get; private set; }
        public bool HasSecurityInterceptorAssets { get; private set; }
        public int SecurityInterceptorPartCount { get; private set; }
        public int AuthoredInterceptorEntranceCount { get; private set; }
        public int AuthoredMapObstacleCount { get; private set; }
        public int AuthoredSalvageSocketCount { get; private set; }
        public bool HasPlayerCameraTuning { get; private set; }
        public PlayerFollowCamera PlayerCamera { get; private set; }

        public DeadSignalWorld(Transform root, IComfortSettings comfortSettings)
        {
            m_root = root;
            m_palette = new DeadSignalPalette(comfortSettings.HighContrastEnabled);
            m_signalBoltPrefab = Resources.Load<GameObject>(SIGNAL_BOLT_PREFAB_RESOURCE);
            HasSignalBoltAssets = m_signalBoltPrefab != null &&
                                  m_signalBoltPrefab.transform.Find("Bolt Shell") != null &&
                                  m_signalBoltPrefab.transform.Find("Bolt Energy") != null;
            _buildPresentation();
            _buildArena();
            _registerAuthoredMapObstacles();
            _buildActors(comfortSettings);
            _configurePlayerCamera();
            ApplyHighContrast(comfortSettings.HighContrastEnabled);
        }

        public bool IsPowered(Vector3 position, bool towerOnline)
        {
            if (FlatDistance(position, ExtractionPosition) <= STARTING_POWER_RADIUS)
            {
                return true;
            }

            return towerOnline && FlatDistance(position, TowerPosition) <= TOWER_POWER_RADIUS;
        }

        public Vector3 ClampToArena(Vector3 position, float radius)
        {
            position.x = Mathf.Clamp(position.x, -ARENA_HALF_WIDTH + radius, ARENA_HALF_WIDTH - radius);
            position.z = Mathf.Clamp(position.z, -ARENA_HALF_HEIGHT + radius, ARENA_HALF_HEIGHT - radius);
            return position;
        }

        public Vector3 ResolveMovement(Vector3 current, Vector3 desired, float radius, bool shortcutOpen)
        {
            var position = current;
            var target = desired;
            for (var iteration = 0; iteration < 3; iteration++)
            {
                var nearestFraction = float.PositiveInfinity;
                var nearestNormal = Vector2.zero;
                foreach (var blocker in m_movementBlockers)
                {
                    if ((blocker.IsShortcutGate && shortcutOpen) ||
                        !blocker.TryGetSweepHit(position, target, radius, out var hitFraction, out var hitNormal) ||
                        hitFraction >= nearestFraction)
                    {
                        continue;
                    }

                    nearestFraction = hitFraction;
                    nearestNormal = hitNormal;
                }

                if (float.IsPositiveInfinity(nearestFraction))
                {
                    return target;
                }

                var resolved = OrientedObstacleCollision.ResolveSlide(position, target, nearestFraction, nearestNormal);
                position = Vector3.Lerp(position, target, nearestFraction) +
                           new Vector3(nearestNormal.x, 0f, nearestNormal.y) * 0.001f;
                target = resolved;
                if ((target - position).sqrMagnitude <= Mathf.Epsilon)
                {
                    return position;
                }
            }

            return position;
        }

        public bool TryGetProjectileObstacleHit(
            Vector3 start,
            Vector3 end,
            float radius,
            bool shortcutOpen,
            out float hitFraction)
        {
            hitFraction = 1f;
            var didHit = false;
            foreach (var blocker in m_movementBlockers)
            {
                if (blocker.IsShortcutGate && shortcutOpen)
                {
                    continue;
                }

                if (!blocker.TryGetSegmentHitFraction(start, end, radius, out var candidateFraction) ||
                    candidateFraction > hitFraction)
                {
                    continue;
                }

                hitFraction = candidateFraction;
                didHit = true;
            }

            return didHit;
        }

        public void ActivateTower(float sapperPulseInterval)
        {
            m_towerTerritory.GetComponent<Renderer>().sharedMaterial = m_palette.CyanDim;
            TowerCore.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            m_towerSignalLines.SetActive(true);
            Warden.gameObject.SetActive(true);
            Sapper.gameObject.SetActive(true);
            SapperTelegraph.SetThreatState(true, false, 0f, sapperPulseInterval);
        }

        public void ApplyHighContrast(bool enabled)
        {
            m_palette.ApplyHighContrast(enabled);
            Camera.backgroundColor = enabled ? Color.black : new Color(0.002f, 0.004f, 0.008f);
            RenderSettings.ambientLight = enabled ? new Color(0.055f, 0.065f, 0.08f) : new Color(0.025f, 0.035f, 0.05f);
        }

        public void TickPlayerPresentation(float dt, Vector3 acceleration, PlayerDroneMovementTuning tuning)
        {
            var localAcceleration = Player.InverseTransformDirection(acceleration);
            var bankScale = tuning.MaximumBankDegrees / tuning.Acceleration;
            var targetRotation = Quaternion.Euler(
                Mathf.Clamp(localAcceleration.z * bankScale, -tuning.MaximumBankDegrees, tuning.MaximumBankDegrees),
                0f,
                Mathf.Clamp(-localAcceleration.x * bankScale, -tuning.MaximumBankDegrees, tuning.MaximumBankDegrees));
            var blend = 1f - Mathf.Exp(-tuning.BankSharpness * dt);
            PlayerPresentation.localRotation = Quaternion.Slerp(PlayerPresentation.localRotation, targetRotation, blend);
            PlayerPresentation.localPosition = Vector3.up *
                                               (Mathf.Sin(Time.time * tuning.HoverFrequency * Mathf.PI * 2f) *
                                                tuning.HoverAmplitude);
        }

        public void ConfigurePlayerSignalWake(PlayerDroneMovementTuning tuning)
        {
            PlayerSignalWake = Player.gameObject.AddComponent<PlayerDroneSignalWake>();
            PlayerSignalWake.Configure(tuning);
        }

        public void OpenShortcut()
        {
            m_shortcutGate.SetActive(false);
        }

        public void PurgeWarden()
        {
            Warden.gameObject.SetActive(false);
        }

        public void PurgeSapper()
        {
            Sapper.gameObject.SetActive(false);
            SapperTelegraph.SetThreatState(false, false, 0f, 1f);
        }

        public void PurgeInterceptor()
        {
            Interceptor.gameObject.SetActive(false);
            SetInterceptorTelegraph(false, Interceptor.position);
        }

        public float GetSafestInterceptorEntryDistance(Vector3 playerPosition)
        {
            var first = m_interceptorEntrances[0];
            var second = m_interceptorEntrances[1];
            var index = InterceptorTactics.SelectSafestEntrance(playerPosition, first, second);
            return FlatDistance(playerPosition, index == 0 ? first : second);
        }

        public void DeployInterceptorReinforcement()
        {
            var index = InterceptorTactics.SelectSafestEntrance(
                Player.position,
                m_interceptorEntrances[0],
                m_interceptorEntrances[1]);
            Interceptor.position = m_interceptorEntrances[index];
            Interceptor.gameObject.SetActive(true);
        }

        public void SetInterceptorTelegraph(bool visible, Vector3 target)
        {
            m_interceptorTelegraph.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            m_interceptorTelegraph.SetPosition(0, Interceptor.position + Vector3.up * 0.18f);
            m_interceptorTelegraph.SetPosition(1, target + Vector3.up * 0.18f);
        }

        public void DeployWardenReinforcement()
        {
            Warden.position = s_securityWardenSpawn;
            Warden.gameObject.SetActive(true);
        }

        public void DeploySapperReinforcement(float pulseInterval)
        {
            Sapper.position = s_signalSapperSpawn;
            Sapper.gameObject.SetActive(true);
            SapperTelegraph.SetThreatState(true, false, 0f, pulseInterval);
        }

        public GameObject CreateSignalBolt(Vector3 direction)
        {
            GameObject bolt;
            var spawnPosition = Player.position + direction * 0.9f + Vector3.up * 0.25f;
            if (HasSignalBoltAssets)
            {
                bolt = Object.Instantiate(m_signalBoltPrefab, m_root);
                bolt.name = "Signal Bolt";
                bolt.transform.localPosition = spawnPosition;
                LastSignalBoltUsedAuthoredPrefab = true;
            }
            else
            {
                bolt = _createPrimitive(
                    "Signal Bolt",
                    PrimitiveType.Cube,
                    spawnPosition,
                    new Vector3(0.16f, 0.16f, 0.55f),
                    m_palette.Cyan);
                LastSignalBoltUsedAuthoredPrefab = false;
            }

            bolt.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            return bolt;
        }

        public void TickTower(float dt, bool towerOnline)
        {
            TowerCore.Rotate(Vector3.up, (towerOnline ? 110f : 22f) * dt, Space.World);
            var pulse = 1f + Mathf.Sin(Time.time * (towerOnline ? 5f : 2f)) * 0.08f;
            TowerCore.localScale = new Vector3(1.35f * pulse, 0.22f, 1.35f * pulse);
        }

        public void TickExtraction(float dt, bool canExtract)
        {
            m_extractionBeacon.transform.Rotate(Vector3.up, canExtract ? 150f * dt : 30f * dt, Space.World);
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void _buildPresentation()
        {
            foreach (var existing in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                existing.enabled = false;
            }

            foreach (var existing in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                existing.enabled = false;
            }

            foreach (var existing in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                existing.enabled = false;
            }

            var cameraRigObject = new GameObject("Player Camera Rig");
            cameraRigObject.transform.SetParent(m_root);
            m_cameraRig = cameraRigObject.transform;

            var cameraObject = new GameObject("Dead Signal Camera");
            cameraObject.transform.SetParent(m_cameraRig, false);
            cameraObject.transform.localPosition = new Vector3(0f, 20f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Camera = cameraObject.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.orthographicSize = 10.4f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.002f, 0.004f, 0.008f);
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 40f;
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Cold Overhead Light");
            lightObject.transform.SetParent(m_root);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.38f, 0.52f, 0.65f);
            key.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(0.025f, 0.035f, 0.05f);
        }

        private void _configurePlayerCamera()
        {
            var tuning = Resources.Load<PlayerCameraTuning>(PLAYER_CAMERA_TUNING_RESOURCE);
            HasPlayerCameraTuning = tuning != null;
            if (!HasPlayerCameraTuning)
            {
                Debug.LogWarning($"Player camera tuning was not found at Resources/{PLAYER_CAMERA_TUNING_RESOURCE}.");
                return;
            }

            PlayerCamera = m_cameraRig.gameObject.AddComponent<PlayerFollowCamera>();
            PlayerCamera.Configure(
                Camera,
                Player,
                tuning,
                new Vector2(ARENA_HALF_WIDTH, ARENA_HALF_HEIGHT));
        }

        private void _buildArena()
        {
            _buildMaintenanceDeck();
            _buildMaintenanceRoomShell();

            _createTerritory("Dock Power Territory", ExtractionPosition, STARTING_POWER_RADIUS, m_palette.CyanDim);
            m_towerTerritory = _createTerritory("Tower Power Territory", TowerPosition, TOWER_POWER_RADIUS, m_palette.Dark);

            for (var x = -12; x <= 12; x += 4)
            {
                _createPrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, 8.55f), new Vector3(1.5f, 0.035f, 0.07f), m_palette.RedDim);
                _createPrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, -8.55f), new Vector3(1.5f, 0.035f, 0.07f), m_palette.RedDim);
            }

            _buildExtraction();
            _buildTower();
            _buildStationMachines();
            _buildSignalShortcut();
        }

        private void _buildMaintenanceDeck()
        {
            var modulePrefab = Resources.Load<GameObject>(MAINTENANCE_DECK_MODULE_RESOURCE);
            var deckRoot = new GameObject("Maintenance Deck Modules");
            deckRoot.transform.SetParent(m_root);

            for (var gridX = -3; gridX <= 3; gridX++)
            {
                for (var gridZ = -2; gridZ <= 2; gridZ++)
                {
                    var position = new Vector3(gridX * DECK_MODULE_WIDTH, -0.45f, gridZ * DECK_MODULE_DEPTH);
                    GameObject module;
                    if (modulePrefab != null)
                    {
                        module = Object.Instantiate(modulePrefab, deckRoot.transform);
                        module.name = $"Maintenance Deck Module {gridX},{gridZ}";
                        module.transform.localPosition = position;
                        module.transform.localRotation = Quaternion.identity;
                        module.transform.localScale = new Vector3(DECK_MODULE_WIDTH, 0.6f, DECK_MODULE_DEPTH);
                        module.GetComponent<Renderer>().sharedMaterial = m_palette.Deck;
                    }
                    else
                    {
                        module = _createPrimitive(
                            $"Maintenance Deck Module {gridX},{gridZ}",
                            PrimitiveType.Cube,
                            position,
                            new Vector3(DECK_MODULE_WIDTH, 0.6f, DECK_MODULE_DEPTH),
                            m_palette.Deck,
                            deckRoot.transform);
                    }

                    MaintenanceDeckModuleCount++;
                }
            }

            HasMaintenanceDeckAssets = modulePrefab != null && m_palette.HasDeckTexture;
        }

        private void _buildMaintenanceRoomShell()
        {
            var shellPrefab = Resources.Load<GameObject>(MAINTENANCE_ROOM_SHELL_RESOURCE);
            if (shellPrefab == null)
            {
                var fallbackRoot = new GameObject("Maintenance Room Shell");
                fallbackRoot.transform.SetParent(m_root);
                _createPrimitive("North Bulkhead", PrimitiveType.Cube, new Vector3(0f, 0.25f, 9.1f),
                    new Vector3(27.8f, 0.8f, 0.5f), m_palette.Bulkhead, fallbackRoot.transform);
                _createPrimitive("South Bulkhead", PrimitiveType.Cube, new Vector3(0f, 0.25f, -9.1f),
                    new Vector3(27.8f, 0.8f, 0.5f), m_palette.Bulkhead, fallbackRoot.transform);
                _createPrimitive("East Bulkhead North", PrimitiveType.Cube, new Vector3(13.7f, 0.25f, 5.425f),
                    new Vector3(0.5f, 0.8f, 7.85f), m_palette.Bulkhead, fallbackRoot.transform);
                _createPrimitive("East Bulkhead South", PrimitiveType.Cube, new Vector3(13.7f, 0.25f, -5.425f),
                    new Vector3(0.5f, 0.8f, 7.85f), m_palette.Bulkhead, fallbackRoot.transform);
                m_movementBlockers.Add(new MovementBlocker(new Vector2(13.7f, 5.425f), new Vector2(0.25f, 3.925f), false));
                m_movementBlockers.Add(new MovementBlocker(new Vector2(13.7f, -5.425f), new Vector2(0.25f, 3.925f), false));
                _createPrimitive("West Bulkhead", PrimitiveType.Cube, new Vector3(-13.7f, 0.25f, 0f),
                    new Vector3(0.5f, 0.8f, 18.7f), m_palette.Bulkhead, fallbackRoot.transform);
                m_machineSockets.AddRange(new[]
                {
                    new Vector3(-11.6f, 0f, 6.8f), new Vector3(-8.8f, 0f, 6.9f), new Vector3(10.8f, 0f, 6.8f),
                    new Vector3(11.2f, 0f, -6.7f), new Vector3(4.8f, 0f, -7.1f), new Vector3(-3.8f, 0f, 7.1f)
                });
                RoomShellBulkheadCount = 5;
                return;
            }

            var shell = Object.Instantiate(shellPrefab, m_root);
            shell.name = "Maintenance Room Shell";
            shell.transform.localPosition = Vector3.zero;
            shell.transform.localRotation = Quaternion.identity;
            foreach (var bulkheadRenderer in shell.GetComponentsInChildren<Renderer>())
            {
                bulkheadRenderer.sharedMaterial = m_palette.Bulkhead;
                RoomShellBulkheadCount++;
            }

            var sockets = shell.transform.Find("Machine Sockets");
            if (sockets != null)
            {
                foreach (Transform socket in sockets)
                {
                    m_machineSockets.Add(socket.position);
                }
            }

            HasMaintenanceRoomShellAssets =
                m_palette.HasBulkheadTexture && RoomShellBulkheadCount == 5 && m_machineSockets.Count == 6;
        }

        private void _buildExtraction()
        {
            var extractionPrefab = Resources.Load<GameObject>(EXTRACTION_PAD_PREFAB_RESOURCE);
            var hasValidPrefab = extractionPrefab != null &&
                                 extractionPrefab.transform.Find("Extraction Plinth") != null &&
                                 extractionPrefab.transform.Find("Extraction Ring") != null &&
                                 extractionPrefab.transform.Find("Extraction Center") != null &&
                                 extractionPrefab.transform.Find("Extraction Beacon") != null;
            if (hasValidPrefab)
            {
                var extractionPad = Object.Instantiate(extractionPrefab, m_root);
                extractionPad.name = "Extraction Pad Assembly";
                extractionPad.transform.localPosition = ExtractionPosition;
                extractionPad.transform.localRotation = Quaternion.identity;
                var plinth = extractionPad.transform.Find("Extraction Plinth");
                var ring = extractionPad.transform.Find("Extraction Ring");
                var center = extractionPad.transform.Find("Extraction Center");
                m_extractionBeacon = extractionPad.transform.Find("Extraction Beacon").gameObject;
                plinth.GetComponent<Renderer>().sharedMaterial = m_palette.ExtractionHousing;
                ring.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
                center.GetComponent<Renderer>().sharedMaterial = m_palette.ExtractionHousing;
                m_extractionBeacon.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
                ExtractionPadPartCount = 4;
                HasExtractionPadAssets = m_palette.HasExtractionTexture;
                return;
            }

            var fallbackRoot = new GameObject("Extraction Pad Assembly");
            fallbackRoot.transform.SetParent(m_root, false);
            fallbackRoot.transform.localPosition = ExtractionPosition;
            _createPrimitive("Extraction Plinth", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
                new Vector3(3.2f, 0.08f, 3.2f), m_palette.ExtractionHousing, fallbackRoot.transform);
            _createPrimitive("Extraction Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f),
                new Vector3(2.55f, 0.08f, 2.55f), m_palette.Cyan, fallbackRoot.transform);
            _createPrimitive("Extraction Center", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f),
                new Vector3(2.1f, 0.08f, 2.1f), m_palette.ExtractionHousing, fallbackRoot.transform);
            m_extractionBeacon = _createPrimitive("Extraction Beacon", PrimitiveType.Cube, new Vector3(0f, 0.7f, 1.5f),
                new Vector3(0.22f, 1.4f, 0.22f), m_palette.Cyan, fallbackRoot.transform);
            ExtractionPadPartCount = 4;
        }

        private void _buildTower()
        {
            var towerPrefab = Resources.Load<GameObject>(SIGNAL_TOWER_PREFAB_RESOURCE);
            var hasValidPrefab = towerPrefab != null &&
                                 towerPrefab.transform.Find("Tower Base") != null &&
                                 towerPrefab.transform.Find("Tower Column") != null &&
                                 towerPrefab.transform.Find("Tower Core") != null;
            if (hasValidPrefab)
            {
                var tower = Object.Instantiate(towerPrefab, m_root);
                tower.name = "Signal Tower Assembly";
                tower.transform.localPosition = TowerPosition;
                tower.transform.localRotation = Quaternion.identity;
                var towerBase = tower.transform.Find("Tower Base");
                var towerColumn = tower.transform.Find("Tower Column");
                TowerCore = tower.transform.Find("Tower Core");
                towerBase.GetComponent<Renderer>().sharedMaterial = m_palette.TowerHousing;
                towerColumn.GetComponent<Renderer>().sharedMaterial = m_palette.TowerHousing;
                TowerCore.GetComponent<Renderer>().sharedMaterial = m_palette.RedDim;
                SignalTowerPartCount = 3;
                HasSignalTowerAssets = m_palette.HasTowerTexture;
            }
            else
            {
                var tower = new GameObject("Signal Tower Assembly");
                tower.transform.SetParent(m_root, false);
                tower.transform.localPosition = TowerPosition;
                _createPrimitive("Tower Base", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f),
                    new Vector3(2.2f, 0.25f, 2.2f), m_palette.TowerHousing, tower.transform);
                _createPrimitive("Tower Column", PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f),
                    new Vector3(0.8f, 1.35f, 0.8f), m_palette.TowerHousing, tower.transform);
                TowerCore = _createPrimitive("Tower Core", PrimitiveType.Cylinder, new Vector3(0f, 1.65f, 0f),
                    new Vector3(1.35f, 0.22f, 1.35f), m_palette.RedDim, tower.transform).transform;
                SignalTowerPartCount = 3;
            }

            _buildSignalRouting();
        }

        private void _buildSignalRouting()
        {
            var routingPrefab = Resources.Load<GameObject>(SIGNAL_ROUTING_PREFAB_RESOURCE);
            var hasValidPrefab = routingPrefab != null &&
                                 routingPrefab.transform.Find("Signal Trunk West") != null &&
                                 routingPrefab.transform.Find("Signal Trunk East") != null &&
                                 routingPrefab.transform.Find("Signal Branch") != null;
            if (hasValidPrefab)
            {
                m_towerSignalLines = Object.Instantiate(routingPrefab, m_root);
                m_towerSignalLines.name = "Tower Signal Lines";
                m_towerSignalLines.transform.localPosition = Vector3.zero;
                m_towerSignalLines.transform.localRotation = Quaternion.identity;
                foreach (var routingRenderer in m_towerSignalLines.GetComponentsInChildren<Renderer>())
                {
                    routingRenderer.sharedMaterial = m_palette.SignalRouting;
                    SignalRoutingPartCount++;
                }

                HasSignalRoutingAssets = m_palette.HasSignalRoutingTexture && SignalRoutingPartCount == 3;
                m_towerSignalLines.SetActive(false);
                return;
            }

            m_towerSignalLines = new GameObject("Tower Signal Lines");
            m_towerSignalLines.transform.SetParent(m_root, false);
            var westTrunk = _createPrimitive("Signal Trunk West", PrimitiveType.Cube, new Vector3(-4.7f, -0.03f, 0.4f),
                new Vector3(0.09f, 0.04f, 8.2f), m_palette.SignalRouting, m_towerSignalLines.transform);
            westTrunk.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var eastTrunk = _createPrimitive("Signal Trunk East", PrimitiveType.Cube, new Vector3(4.1f, -0.03f, 0.4f),
                new Vector3(0.09f, 0.04f, 9.4f), m_palette.SignalRouting, m_towerSignalLines.transform);
            eastTrunk.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            _createPrimitive("Signal Branch", PrimitiveType.Cube, new Vector3(-0.6f, -0.025f, -3.5f),
                new Vector3(0.09f, 0.04f, 7.8f), m_palette.SignalRouting, m_towerSignalLines.transform);
            SignalRoutingPartCount = 3;
            m_towerSignalLines.SetActive(false);
        }

        private void _buildStationMachines()
        {
            var machinePrefab = Resources.Load<GameObject>(STATION_MACHINE_PREFAB_RESOURCE);
            var hasValidPrefab = machinePrefab != null &&
                                 machinePrefab.transform.Find("Machine Housing") != null &&
                                 machinePrefab.transform.Find("Machine Status") != null;
            var machinesRoot = new GameObject("Station Machines");
            machinesRoot.transform.SetParent(m_root, false);
            for (var i = 0; i < m_machineSockets.Count; i++)
            {
                var position = m_machineSockets[i];
                if (hasValidPrefab)
                {
                    var machine = Object.Instantiate(machinePrefab, machinesRoot.transform);
                    machine.name = $"Station Machine {i + 1:00}";
                    machine.transform.localPosition = position;
                    machine.transform.localRotation = Quaternion.identity;
                    machine.transform.Find("Machine Housing").GetComponent<Renderer>().sharedMaterial = m_palette.StationMachineHousing;
                    machine.transform.Find("Machine Status").GetComponent<Renderer>().sharedMaterial =
                        i % 2 == 0 ? m_palette.RedDim : m_palette.CyanDim;
                }
                else
                {
                    var machine = new GameObject($"Station Machine {i + 1:00}");
                    machine.transform.SetParent(machinesRoot.transform, false);
                    machine.transform.localPosition = position;
                    _createPrimitive("Machine Housing", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f),
                        new Vector3(1.5f, 0.9f, 1.1f), m_palette.StationMachineHousing, machine.transform);
                    _createPrimitive("Machine Status", PrimitiveType.Cube, new Vector3(0f, 0.92f, -0.15f),
                        new Vector3(0.75f, 0.06f, 0.18f), i % 2 == 0 ? m_palette.RedDim : m_palette.CyanDim, machine.transform);
                }

                StationMachineInstanceCount++;
                StationMachinePartCount += 2;
            }

            HasStationMachineAssets = hasValidPrefab && m_palette.HasStationMachineTexture &&
                                      StationMachineInstanceCount == 6 && StationMachinePartCount == 12;
        }

        private void _buildSignalShortcut()
        {
            // The end passages stay open, so spending Signal for the central route is optional.
            var shortcutPrefab = Resources.Load<GameObject>(SHORTCUT_GATE_PREFAB_RESOURCE);
            var hasValidPrefab = shortcutPrefab != null &&
                                 shortcutPrefab.transform.Find("Shortcut Bulkhead South") != null &&
                                 shortcutPrefab.transform.Find("Shortcut Bulkhead North") != null &&
                                 shortcutPrefab.transform.Find("Shortcut Gate South Post") != null &&
                                 shortcutPrefab.transform.Find("Shortcut Gate North Post") != null &&
                                 shortcutPrefab.transform.Find("Shortcut Gate Signal") != null &&
                                 shortcutPrefab.transform.Find("Signal Shortcut Gate") != null;
            if (hasValidPrefab)
            {
                var shortcut = Object.Instantiate(shortcutPrefab, m_root);
                shortcut.name = "Shortcut Gate Assembly";
                shortcut.transform.localPosition = ShortcutPosition;
                shortcut.transform.localRotation = Quaternion.identity;
                foreach (var childRenderer in shortcut.GetComponentsInChildren<Renderer>())
                {
                    childRenderer.sharedMaterial = childRenderer.name == "Shortcut Gate Signal"
                        ? m_palette.CyanDim
                        : m_palette.ShortcutHousing;
                    ShortcutGatePartCount++;
                }

                m_shortcutGate = shortcut.transform.Find("Signal Shortcut Gate").gameObject;
                m_shortcutGate.GetComponent<Renderer>().sharedMaterial = m_palette.ShortcutLocked;
                HasShortcutGateAssets = m_palette.HasShortcutTexture && ShortcutGatePartCount == 6;
                _addShortcutMovementBlockers();
                return;
            }

            var fallbackRoot = new GameObject("Shortcut Gate Assembly");
            fallbackRoot.transform.SetParent(m_root, false);
            fallbackRoot.transform.localPosition = ShortcutPosition;
            _createPrimitive("Shortcut Bulkhead South", PrimitiveType.Cube, new Vector3(0f, 0.46f, -3.55f),
                new Vector3(0.55f, 1.1f, 4.7f), m_palette.ShortcutHousing, fallbackRoot.transform);
            _createPrimitive("Shortcut Bulkhead North", PrimitiveType.Cube, new Vector3(0f, 0.46f, 3.15f),
                new Vector3(0.55f, 1.1f, 3.9f), m_palette.ShortcutHousing, fallbackRoot.transform);
            _createPrimitive("Shortcut Gate South Post", PrimitiveType.Cube, new Vector3(-0.16f, 0.68f, -1.34f),
                new Vector3(0.85f, 1.45f, 0.25f), m_palette.ShortcutHousing, fallbackRoot.transform);
            _createPrimitive("Shortcut Gate North Post", PrimitiveType.Cube, new Vector3(-0.16f, 0.68f, 1.34f),
                new Vector3(0.85f, 1.45f, 0.25f), m_palette.ShortcutHousing, fallbackRoot.transform);
            _createPrimitive("Shortcut Gate Signal", PrimitiveType.Cube, new Vector3(-0.31f, 1.38f, 0f),
                new Vector3(0.12f, 0.08f, 2.3f), m_palette.CyanDim, fallbackRoot.transform);
            m_shortcutGate = _createPrimitive("Signal Shortcut Gate", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f),
                new Vector3(0.42f, 1.05f, 2.4f), m_palette.ShortcutLocked, fallbackRoot.transform);
            ShortcutGatePartCount = 6;
            _addShortcutMovementBlockers();
        }

        private void _addShortcutMovementBlockers()
        {
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(4f, -3.15f), new Vector2(0.275f, 2.35f), false));
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(4f, 3.55f), new Vector2(0.275f, 1.95f), false));
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(ShortcutPosition.x, ShortcutPosition.z), new Vector2(0.21f, 1.2f), true));
        }

        private void _registerAuthoredMapObstacles()
        {
            var authoredObstacles = Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None);
            foreach (var obstacle in authoredObstacles)
            {
                m_movementBlockers.Add(new MovementBlocker(
                    obstacle.Center,
                    obstacle.ScaledHalfSize,
                    obstacle.RightAxis,
                    obstacle.ForwardAxis,
                    false));
            }

            AuthoredMapObstacleCount = authoredObstacles.Length;
        }

        private void _buildActors(IComfortSettings comfortSettings)
        {
            _buildPlayer();

            _registerInterceptorEntrances();
            _buildInterceptor();

            _buildWarden();

            var wardenTelegraphRoot = new GameObject("Warden Strike Warning");
            wardenTelegraphRoot.transform.SetParent(m_root);
            WardenTelegraph = wardenTelegraphRoot.AddComponent<WardenThreatTelegraph>();
            WardenTelegraph.Configure(Warden, Player, comfortSettings);

            _buildSapper();

            var telegraphRoot = new GameObject("Sapper Drain Telegraph");
            telegraphRoot.transform.SetParent(m_root);
            SapperTelegraph = telegraphRoot.AddComponent<SignalSapperTelegraph>();
            SapperTelegraph.Configure(Sapper, TowerPosition, m_palette.Magenta, m_palette.Magenta, comfortSettings);

            _createSalvage(new Vector3(9.7f, 0f, 6.3f));
            _createSalvage(new Vector3(10.4f, 0f, -6.4f));
            _createSalvage(new Vector3(-5.8f, 0f, 7.2f));
            var authoredSockets = Object.FindObjectsByType<AuthoredSalvageSocket>(FindObjectsSortMode.None);
            foreach (var socket in authoredSockets)
            {
                _createSalvage(socket.Position);
            }

            AuthoredSalvageSocketCount = authoredSockets.Length;
        }

        private void _registerInterceptorEntrances()
        {
            var authoredEntrances = Object.FindObjectsByType<AuthoredInterceptorEntrance>(FindObjectsSortMode.None);
            AuthoredInterceptorEntranceCount = authoredEntrances.Length;
            System.Array.Sort(authoredEntrances, (first, second) => first.Priority.CompareTo(second.Priority));
            foreach (var entrance in authoredEntrances)
            {
                m_interceptorEntrances.Add(entrance.Position);
            }

            if (m_interceptorEntrances.Count < 2)
            {
                m_interceptorEntrances.Clear();
                m_interceptorEntrances.Add(s_interceptorNorthSpawn);
                m_interceptorEntrances.Add(s_interceptorSouthSpawn);
            }
        }

        private void _buildInterceptor()
        {
            var prefab = Resources.Load<GameObject>(SECURITY_INTERCEPTOR_PREFAB_RESOURCE);
            var hasValidPrefab = prefab != null &&
                                 prefab.transform.Find("Interceptor Chassis") != null &&
                                 prefab.transform.Find("Interceptor Blade Left") != null &&
                                 prefab.transform.Find("Interceptor Blade Right") != null &&
                                 prefab.transform.Find("Interceptor Core") != null;
            if (hasValidPrefab)
            {
                var root = Object.Instantiate(prefab, m_root);
                root.name = "Security Interceptor";
                Interceptor = root.transform;
                Interceptor.Find("Interceptor Chassis").GetComponent<Renderer>().sharedMaterial = m_palette.WardenHousing;
                Interceptor.Find("Interceptor Blade Left").GetComponent<Renderer>().sharedMaterial = m_palette.RedDim;
                Interceptor.Find("Interceptor Blade Right").GetComponent<Renderer>().sharedMaterial = m_palette.RedDim;
                InterceptorCore = Interceptor.Find("Interceptor Core");
                InterceptorCore.GetComponent<Renderer>().sharedMaterial = m_palette.Amber;
                HasSecurityInterceptorAssets = true;
                SecurityInterceptorPartCount = 4;
            }
            else
            {
                var root = new GameObject("Security Interceptor");
                root.transform.SetParent(m_root);
                Interceptor = root.transform;
                _createPrimitive("Interceptor Chassis", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f),
                    new Vector3(0.75f, 0.28f, 1.1f), m_palette.WardenHousing, Interceptor);
                _createPrimitive("Interceptor Blade Left", PrimitiveType.Cube, new Vector3(-0.56f, 0.22f, 0f),
                    new Vector3(0.16f, 0.12f, 1.5f), m_palette.RedDim, Interceptor);
                _createPrimitive("Interceptor Blade Right", PrimitiveType.Cube, new Vector3(0.56f, 0.22f, 0f),
                    new Vector3(0.16f, 0.12f, 1.5f), m_palette.RedDim, Interceptor);
                InterceptorCore = _createPrimitive("Interceptor Core", PrimitiveType.Sphere, new Vector3(0f, 0.42f, -0.3f),
                    new Vector3(0.26f, 0.18f, 0.26f), m_palette.Amber, Interceptor).transform;
                HasSecurityInterceptorAssets = false;
                SecurityInterceptorPartCount = 4;
            }

            Interceptor.position = m_interceptorEntrances[0];
            Interceptor.gameObject.SetActive(false);
            var telegraphRoot = new GameObject("Interceptor Charge Telegraph");
            telegraphRoot.transform.SetParent(m_root);
            m_interceptorTelegraph = telegraphRoot.AddComponent<LineRenderer>();
            m_interceptorTelegraph.positionCount = 2;
            m_interceptorTelegraph.startWidth = 0.16f;
            m_interceptorTelegraph.endWidth = 0.05f;
            m_interceptorTelegraph.sharedMaterial = m_palette.Red;
            m_interceptorTelegraph.textureMode = LineTextureMode.Stretch;
            m_interceptorTelegraph.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_interceptorTelegraph.receiveShadows = false;
            telegraphRoot.SetActive(false);
        }

        private void _buildSapper()
        {
            var sapperPrefab = Resources.Load<GameObject>(SIGNAL_SAPPER_PREFAB_RESOURCE);
            var hasValidPrefab = sapperPrefab != null &&
                                 sapperPrefab.transform.Find("Sapper Chassis") != null &&
                                 sapperPrefab.transform.Find("Sapper Fork Left") != null &&
                                 sapperPrefab.transform.Find("Sapper Fork Right") != null &&
                                 sapperPrefab.transform.Find("Sapper Drain Core") != null;
            if (hasValidPrefab)
            {
                var sapperRoot = Object.Instantiate(sapperPrefab, m_root);
                sapperRoot.name = "Signal Sapper";
                sapperRoot.transform.localPosition = s_signalSapperSpawn;
                sapperRoot.transform.localRotation = Quaternion.identity;
                Sapper = sapperRoot.transform;
                Sapper.Find("Sapper Chassis").GetComponent<Renderer>().sharedMaterial = m_palette.SapperHousing;
                Sapper.Find("Sapper Fork Left").GetComponent<Renderer>().sharedMaterial = m_palette.Magenta;
                Sapper.Find("Sapper Fork Right").GetComponent<Renderer>().sharedMaterial = m_palette.Magenta;
                SapperCore = Sapper.Find("Sapper Drain Core");
                SapperCore.GetComponent<Renderer>().sharedMaterial = m_palette.Magenta;
                HasSignalSapperAssets = m_palette.HasSapperTexture;
                SignalSapperPartCount = 4;
            }
            else
            {
                var fallbackRoot = new GameObject("Signal Sapper");
                fallbackRoot.transform.SetParent(m_root);
                fallbackRoot.transform.position = s_signalSapperSpawn;
                Sapper = fallbackRoot.transform;
                _createPrimitive("Sapper Chassis", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f),
                    new Vector3(0.72f, 0.34f, 1.25f), m_palette.SapperHousing, Sapper);
                _createPrimitive("Sapper Fork Left", PrimitiveType.Cube, new Vector3(-0.43f, 0.28f, 0.28f),
                    new Vector3(0.18f, 0.18f, 0.92f), m_palette.Magenta, Sapper);
                _createPrimitive("Sapper Fork Right", PrimitiveType.Cube, new Vector3(0.43f, 0.28f, 0.28f),
                    new Vector3(0.18f, 0.18f, 0.92f), m_palette.Magenta, Sapper);
                SapperCore = _createPrimitive("Sapper Drain Core", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, -0.12f),
                    new Vector3(0.42f, 0.1f, 0.42f), m_palette.Magenta, Sapper).transform;
                HasSignalSapperAssets = false;
                SignalSapperPartCount = 4;
            }

            SapperCoreBaseScale = SapperCore.localScale;
            Sapper.gameObject.SetActive(false);
        }

        private void _buildWarden()
        {
            var wardenPrefab = Resources.Load<GameObject>(SECURITY_WARDEN_PREFAB_RESOURCE);
            var hasValidPrefab = wardenPrefab != null &&
                                 wardenPrefab.transform.Find("Warden Chassis") != null &&
                                 wardenPrefab.transform.Find("Warden Eye") != null &&
                                 wardenPrefab.transform.Find("Warden Crown") != null;
            if (hasValidPrefab)
            {
                var wardenRoot = Object.Instantiate(wardenPrefab, m_root);
                wardenRoot.name = "Security Warden";
                wardenRoot.transform.localPosition = s_securityWardenSpawn;
                wardenRoot.transform.localRotation = Quaternion.identity;
                Warden = wardenRoot.transform;
                Warden.Find("Warden Chassis").GetComponent<Renderer>().sharedMaterial = m_palette.WardenHousing;
                Warden.Find("Warden Eye").GetComponent<Renderer>().sharedMaterial = m_palette.Red;
                Warden.Find("Warden Crown").GetComponent<Renderer>().sharedMaterial = m_palette.RedDim;
            }
            else
            {
                var fallbackRoot = new GameObject("Security Warden");
                fallbackRoot.transform.SetParent(m_root);
                fallbackRoot.transform.position = s_securityWardenSpawn;
                Warden = fallbackRoot.transform;
                _createPrimitive("Warden Chassis", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f),
                    new Vector3(1.15f, 0.55f, 1.15f), m_palette.WardenHousing, Warden);
                _createPrimitive("Warden Eye", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.59f),
                    new Vector3(0.68f, 0.16f, 0.06f), m_palette.Red, Warden);
                _createPrimitive("Warden Crown", PrimitiveType.Cylinder, new Vector3(0f, 0.76f, 0f),
                    new Vector3(0.68f, 0.12f, 0.68f), m_palette.RedDim, Warden);
            }

            Warden.gameObject.SetActive(false);
        }

        private void _buildPlayer()
        {
            var playerPrefab = Resources.Load<GameObject>(PLAYER_DRONE_PREFAB_RESOURCE);
            var hasValidPrefab = playerPrefab != null &&
                                 playerPrefab.transform.Find("Drone Chassis") != null &&
                                 playerPrefab.transform.Find("Drone Signal Ring") != null &&
                                 playerPrefab.transform.Find("Drone Core") != null &&
                                 playerPrefab.transform.Find("Drone Tool") != null;
            if (hasValidPrefab)
            {
                var playerRoot = Object.Instantiate(playerPrefab, m_root);
                playerRoot.name = "Maintenance Drone";
                playerRoot.transform.localPosition = ExtractionPosition;
                playerRoot.transform.localRotation = Quaternion.identity;
                Player = playerRoot.transform;
                Player.Find("Drone Chassis").GetComponent<Renderer>().sharedMaterial = m_palette.PlayerDroneHousing;
                Player.Find("Drone Signal Ring").GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
                Player.Find("Drone Core").GetComponent<Renderer>().sharedMaterial = m_palette.Dark;
                PlayerNose = Player.Find("Drone Tool");
                PlayerNose.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
                _createPlayerPresentationPivot();
                PlayerDronePartCount = 4;
                HasPlayerDroneAssets = m_palette.HasPlayerDroneTexture;
                return;
            }

            var fallbackRoot = new GameObject("Maintenance Drone");
            fallbackRoot.transform.SetParent(m_root);
            fallbackRoot.transform.position = ExtractionPosition;
            Player = fallbackRoot.transform;
            _createPrimitive("Drone Chassis", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f),
                new Vector3(1.05f, 0.22f, 1.05f), m_palette.PlayerDroneHousing, Player);
            _createPrimitive("Drone Signal Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f),
                new Vector3(0.72f, 0.08f, 0.72f), m_palette.Cyan, Player);
            _createPrimitive("Drone Core", PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f),
                new Vector3(0.36f, 0.09f, 0.36f), m_palette.Dark, Player);
            PlayerNose = _createPrimitive("Drone Tool", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.68f),
                new Vector3(0.24f, 0.2f, 0.7f), m_palette.Cyan, Player).transform;
            _createPlayerPresentationPivot();
            PlayerDronePartCount = 4;
        }

        private void _createPlayerPresentationPivot()
        {
            var visualChildren = new List<Transform>();
            foreach (Transform child in Player)
            {
                visualChildren.Add(child);
            }

            var presentation = new GameObject("Drone Presentation");
            PlayerPresentation = presentation.transform;
            PlayerPresentation.SetParent(Player, false);
            foreach (var child in visualChildren)
            {
                child.SetParent(PlayerPresentation, false);
            }
        }

        private void _createSalvage(Vector3 position)
        {
            var salvagePrefab = Resources.Load<GameObject>(SALVAGE_CACHE_PREFAB_RESOURCE);
            var hasValidPrefab = salvagePrefab != null &&
                                 salvagePrefab.transform.Find("Salvage Case") != null &&
                                 salvagePrefab.transform.Find("Salvage Band") != null;
            GameObject root;
            if (hasValidPrefab)
            {
                root = Object.Instantiate(salvagePrefab, m_root);
                root.name = "Salvage Cache";
                root.transform.localPosition = position;
                root.transform.localRotation = Quaternion.identity;
                root.transform.Find("Salvage Case").GetComponent<Renderer>().sharedMaterial = m_palette.SalvageCacheHousing;
                root.transform.Find("Salvage Band").GetComponent<Renderer>().sharedMaterial = m_palette.White;
            }
            else
            {
                root = new GameObject("Salvage Cache");
                root.transform.SetParent(m_root);
                root.transform.position = position;
                _createPrimitive("Salvage Case", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f),
                    new Vector3(0.75f, 0.48f, 0.75f), m_palette.SalvageCacheHousing, root.transform);
                _createPrimitive("Salvage Band", PrimitiveType.Cube, new Vector3(0f, 0.61f, 0f),
                    new Vector3(0.9f, 0.06f, 0.28f), m_palette.White, root.transform);
            }

            m_salvagePickups.Add(root);
            SalvageCacheInstanceCount++;
            SalvageCachePartCount += 2;
            HasSalvageCacheAssets = hasValidPrefab && m_palette.HasSalvageCacheTexture &&
                                    SalvageCachePartCount == SalvageCacheInstanceCount * 2;
        }

        private bool _isBlocked(Vector3 position, float radius, bool shortcutOpen)
        {
            foreach (var blocker in m_movementBlockers)
            {
                if (blocker.IsShortcutGate && shortcutOpen)
                {
                    continue;
                }

                if (blocker.Overlaps(position, radius))
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject _createTerritory(string objectName, Vector3 position, float radius, Material material)
        {
            return _createPrimitive(
                objectName,
                PrimitiveType.Cylinder,
                position + new Vector3(0f, -0.095f, 0f),
                new Vector3(radius * 2f, 0.025f, radius * 2f),
                material);
        }

        private GameObject _createPrimitive(
            string objectName,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent = null)
        {
            var visual = GameObject.CreatePrimitive(type);
            visual.name = objectName;
            visual.transform.SetParent(parent == null ? m_root : parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            var primitiveCollider = visual.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Object.Destroy(primitiveCollider);
            }

            return visual;
        }

        private const string MAINTENANCE_DECK_MODULE_RESOURCE = "Environment/MaintenanceDeckModule";
        private const string MAINTENANCE_ROOM_SHELL_RESOURCE = "Environment/MaintenanceRoomShell";
        private const string SIGNAL_TOWER_PREFAB_RESOURCE = "Environment/SignalTowerAssembly";
        private const string EXTRACTION_PAD_PREFAB_RESOURCE = "Environment/ExtractionPadAssembly";
        private const string SHORTCUT_GATE_PREFAB_RESOURCE = "Environment/ShortcutGateAssembly";
        private const string SIGNAL_ROUTING_PREFAB_RESOURCE = "Environment/SignalRoutingAssembly";
        private const string STATION_MACHINE_PREFAB_RESOURCE = "Environment/StationMachineAssembly";
        private const string SALVAGE_CACHE_PREFAB_RESOURCE = "Environment/SalvageCacheAssembly";
        private const string PLAYER_DRONE_PREFAB_RESOURCE = "Actors/MaintenanceDroneAssembly";
        private const string SECURITY_WARDEN_PREFAB_RESOURCE = "Actors/SecurityWardenAssembly";
        private const string SIGNAL_SAPPER_PREFAB_RESOURCE = "Actors/SignalSapperAssembly";
        private const string SECURITY_INTERCEPTOR_PREFAB_RESOURCE = "Actors/SecurityInterceptorAssembly";
        private const string SIGNAL_BOLT_PREFAB_RESOURCE = "Projectiles/SignalBoltAssembly";
        private const string PLAYER_CAMERA_TUNING_RESOURCE = "Tuning/PlayerCameraTuning";
        private const float DECK_MODULE_WIDTH = 3.9f;
        private const float DECK_MODULE_DEPTH = 3.6f;

        private static readonly Vector3 s_securityWardenSpawn = new(6.8f, 0f, 4.7f);
        private static readonly Vector3 s_signalSapperSpawn = new(-10.8f, 0f, 5.7f);
        private static readonly Vector3 s_interceptorNorthSpawn = new(-16.4f, 0f, 7.1f);
        private static readonly Vector3 s_interceptorSouthSpawn = new(1.5f, 0f, -7.5f);

        private readonly Transform m_root;
        private readonly DeadSignalPalette m_palette;
        private readonly GameObject m_signalBoltPrefab;
        private readonly List<MovementBlocker> m_movementBlockers = new();
        private readonly List<GameObject> m_salvagePickups = new();
        private readonly List<Vector3> m_machineSockets = new();
        private readonly List<Vector3> m_interceptorEntrances = new();

        private GameObject m_towerTerritory;
        private GameObject m_towerSignalLines;
        private GameObject m_extractionBeacon;
        private GameObject m_shortcutGate;
        private Transform m_cameraRig;
        private LineRenderer m_interceptorTelegraph;

        private sealed class MovementBlocker
        {
            public MovementBlocker(Vector2 center, Vector2 halfSize, bool isShortcutGate)
                : this(center, halfSize, Vector2.right, Vector2.up, isShortcutGate)
            {
            }

            public MovementBlocker(
                Vector2 center,
                Vector2 halfSize,
                Vector2 rightAxis,
                Vector2 forwardAxis,
                bool isShortcutGate)
            {
                Center = center;
                HalfSize = halfSize;
                RightAxis = rightAxis;
                ForwardAxis = forwardAxis;
                IsShortcutGate = isShortcutGate;
            }

            public Vector2 Center { get; }
            public Vector2 HalfSize { get; }
            public Vector2 RightAxis { get; }
            public Vector2 ForwardAxis { get; }
            public bool IsShortcutGate { get; }

            public bool Overlaps(Vector3 position, float radius)
            {
                return OrientedObstacleCollision.Overlaps(
                    position,
                    radius,
                    Center,
                    HalfSize,
                    RightAxis,
                    ForwardAxis);
            }

            public bool TryGetSweepHit(
                Vector3 current,
                Vector3 desired,
                float radius,
                out float hitFraction,
                out Vector2 hitNormal)
            {
                return OrientedObstacleCollision.TryGetSweepHit(
                    current,
                    desired,
                    radius,
                    Center,
                    HalfSize,
                    RightAxis,
                    ForwardAxis,
                    out hitFraction,
                    out hitNormal);
            }

            public bool TryGetSegmentHitFraction(Vector3 start, Vector3 end, float radius, out float hitFraction)
            {
                return ProjectileCollision.TryGetOrientedBoxHitFraction(
                    start,
                    end,
                    Center,
                    HalfSize,
                    RightAxis,
                    ForwardAxis,
                    radius,
                    out hitFraction);
            }
        }
    }
}
