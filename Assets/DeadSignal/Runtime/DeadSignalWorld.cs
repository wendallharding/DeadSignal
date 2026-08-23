using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        public Transform PlayerBody { get; private set; }
        public Transform PlayerTurret { get; private set; }
        public PlayerDroneSignalWake PlayerSignalWake { get; private set; }
        public Transform Warden { get; private set; }
        public WardenThreatTelegraph WardenTelegraph { get; private set; }
        public Transform Sapper { get; private set; }
        public Transform SapperCore { get; private set; }
        public Vector3 SapperCoreBaseScale { get; private set; }
        public Transform Interceptor { get; private set; }
        public Transform InterceptorCore { get; private set; }
        public Transform Suppressor { get; private set; }
        public Transform SuppressorCore { get; private set; }
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
        public bool HasSecuritySuppressorAssets { get; private set; }
        public int SecuritySuppressorPartCount { get; private set; }
        public int AuthoredInterceptorEntranceCount { get; private set; }
        public int AuthoredMapObstacleCount { get; private set; }
        public int AuthoredSalvageSocketCount { get; private set; }
        public bool HasPlayerCameraTuning { get; private set; }
        public PlayerFollowCamera PlayerCamera { get; private set; }
        public bool LastMovementBlocked { get; private set; }

        public DeadSignalWorld(Transform root, IComfortSettings comfortSettings)
        {
            m_root = root;
            m_palette = new DeadSignalPalette(comfortSettings.HighContrastEnabled);
            _createTerritoryMaterials();
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
            LastMovementBlocked = false;
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

                LastMovementBlocked = true;

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

        public Vector3 GetNavigationWaypoint(Vector3 current, Vector3 destination, float radius, bool shortcutOpen)
        {
            if (!_tryGetNavigationBlocker(current, destination, radius, shortcutOpen, out var blockingObstacle))
            {
                return destination;
            }

            var bestWaypoint = destination;
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < MovementBlocker.DETOUR_WAYPOINT_COUNT; index++)
            {
                var candidate = blockingObstacle.GetDetourWaypoint(index, radius + NAVIGATION_CLEARANCE);
                var waypoint = new Vector3(candidate.x, current.y, candidate.y);
                if (!_hasNavigationLine(current, waypoint, radius, shortcutOpen))
                {
                    continue;
                }

                var score = FlatDistance(current, waypoint) + FlatDistance(waypoint, destination);
                if (!_hasNavigationLine(waypoint, destination, radius, shortcutOpen))
                {
                    score += NAVIGATION_BLOCKED_ROUTE_PENALTY;
                }

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestWaypoint = waypoint;
            }

            return bestWaypoint;
        }

        public Vector3 GetObjectiveTarget(RunModel model)
        {
            return _currentObjectiveTarget(model);
        }

        public Vector3 GetObjectiveGuidanceWaypoint(RunModel model, float radius)
        {
            return GetNavigationWaypoint(Player.position, _currentObjectiveTarget(model), radius, model.ShortcutOpen);
        }

        public Vector3 GetNearestPoweredTarget(Vector3 position, bool towerOnline)
        {
            if (!towerOnline)
            {
                return FlatDistance(position, TowerPosition) <= FlatDistance(position, ExtractionPosition)
                    ? TowerPosition
                    : ExtractionPosition;
            }

            return FlatDistance(position, TowerPosition) <= FlatDistance(position, ExtractionPosition)
                ? TowerPosition
                : ExtractionPosition;
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
            m_towerTerritory.GetComponent<Renderer>().sharedMaterial = m_poweredTerritoryMaterial;
            foreach (var marker in m_towerTerritoryMarkers)
            {
                marker.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            }
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
            RenderSettings.ambientLight = enabled ? new Color(0.075f, 0.085f, 0.1f) : new Color(0.045f, 0.055f, 0.07f);
        }

        public void TickPlayerPresentation(
            float dt,
            Vector3 acceleration,
            Vector3 velocity,
            Vector3 aimDirection,
            PlayerDroneMovementTuning tuning)
        {
            PlayerCamera?.SetAimDirection(aimDirection);
            var bodyForward = velocity.sqrMagnitude > 0.01f ? velocity.normalized : PlayerBody.forward;
            bodyForward.y = 0f;
            var bodyYaw = Quaternion.LookRotation(bodyForward, Vector3.up);
            var localAcceleration = Quaternion.Inverse(bodyYaw) * acceleration;
            var bankScale = tuning.MaximumBankDegrees / tuning.Acceleration;
            var targetBank = Quaternion.Euler(
                Mathf.Clamp(localAcceleration.z * bankScale, -tuning.MaximumBankDegrees, tuning.MaximumBankDegrees),
                0f,
                Mathf.Clamp(-localAcceleration.x * bankScale, -tuning.MaximumBankDegrees, tuning.MaximumBankDegrees));
            var bodyTurnBlend = 1f - Mathf.Exp(-tuning.BodyTurnSharpness * dt);
            var bankBlend = 1f - Mathf.Exp(-tuning.BankSharpness * dt);
            var currentYaw = Quaternion.Euler(0f, PlayerBody.localEulerAngles.y, 0f);
            var smoothedYaw = Quaternion.Slerp(currentYaw, bodyYaw, bodyTurnBlend);
            var currentBank = Quaternion.Inverse(currentYaw) * PlayerBody.localRotation;
            var smoothedBank = Quaternion.Slerp(currentBank, targetBank, bankBlend);
            PlayerBody.localRotation = smoothedYaw * smoothedBank;

            if (aimDirection.sqrMagnitude > 0.01f)
            {
                var turretTarget = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
                var turretBlend = 1f - Mathf.Exp(-tuning.TurretTurnSharpness * dt);
                PlayerTurret.rotation = Quaternion.Slerp(PlayerTurret.rotation, turretTarget, turretBlend);
            }

            PlayerTurret.localPosition = Vector3.up * tuning.TurretMountHeight;

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

        public void PurgeSuppressor()
        {
            Suppressor.gameObject.SetActive(false);
            SetSuppressorField(false, false, 1f);
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

        public void DeploySuppressorReinforcement()
        {
            var index = InterceptorTactics.SelectSafestEntrance(
                Player.position,
                m_interceptorEntrances[0],
                m_interceptorEntrances[1]);
            Suppressor.position = m_interceptorEntrances[index];
            Suppressor.gameObject.SetActive(true);
            SetSuppressorField(false, false, 1f);
        }

        public void SetSuppressorField(bool visible, bool active, float radius)
        {
            SetSuppressorFieldAt(visible, active, radius, Suppressor.position);
        }

        public void SetSuppressorFieldAt(bool visible, bool active, float radius, Vector3 center)
        {
            m_suppressorField.SetActive(visible);
            m_suppressorField.transform.position = center + Vector3.up * 0.035f;
            m_suppressorField.transform.localScale = new Vector3(radius * 2f, 0.025f, radius * 2f);
            m_suppressorField.GetComponent<Renderer>().sharedMaterial = active ? m_palette.Magenta : m_palette.Amber;
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

        public void TickEnvironmentPresentation(float dt, bool towerOnline, bool powered)
        {
            m_environmentTime += dt;
            m_boundaryPulse = Mathf.MoveTowards(m_boundaryPulse, 0f, dt * 1.5f);
            if (m_poweredTerritoryMaterial != null)
            {
                m_poweredTerritoryMaterial.SetFloat("_Pulse", m_boundaryPulse);
            }

            for (var index = 0; index < m_environmentAnimators.Count; index++)
            {
                var animator = m_environmentAnimators[index];
                if (animator != null)
                {
                    animator.Rotate(Vector3.up, (index % 2 == 0 ? 12f : -9f) * dt, Space.Self);
                }
            }

            foreach (var cache in m_salvagePickups)
            {
                if (!cache.activeSelf)
                {
                    continue;
                }

                var beacon = cache.transform.Find("Salvage Beacon");
                if (beacon != null)
                {
                    var pulse = 0.75f + Mathf.Sin(m_environmentTime * 4f + cache.transform.position.x) * 0.25f;
                    beacon.localScale = new Vector3(0.1f, 1.1f + pulse * 0.35f, 0.1f);
                }
            }

            for (var index = 0; index < m_landmarkLights.Count; index++)
            {
                var light = m_landmarkLights[index];
                var baseIntensity = index == 0 && !towerOnline ? 0.35f : 1f;
                light.intensity = baseIntensity * (0.88f + Mathf.Sin(m_environmentTime * 2.2f + index) * 0.12f);
            }

            if (m_deadZoneVignette != null)
            {
                m_deadZoneVignette.intensity.value = Mathf.MoveTowards(
                    m_deadZoneVignette.intensity.value, powered ? 0.14f : 0.22f, dt * 0.15f);
            }
        }

        public void TickGameplayAssists(
            float dt,
            RunModel model,
            DeadSignalThreatController threats,
            Vector3 aimDirection,
            float guidanceStrength)
        {
            var target = GetObjectiveGuidanceWaypoint(model, 0.48f);
            _updateGuideLine(m_routeGuide, Player.position, target, m_palette.Amber, 0.045f + guidanceStrength * 0.055f);
            m_routeGuide.enabled = guidanceStrength > 0.01f;

            var aimEnd = Player.position + aimDirection.normalized * 4.2f;
            _updateGuideLine(m_aimGuide, Player.position + Vector3.up * 0.18f, aimEnd + Vector3.up * 0.18f,
                m_palette.White, 0.025f);

            _updateThreatHealth(Warden, threats.WardenHealth, threats.WardenMaximumHealth);
            _updateThreatHealth(Sapper, threats.SapperHealth, threats.SapperMaximumHealth);
            _updateThreatHealth(Interceptor, threats.InterceptorHealth, threats.InterceptorMaximumHealth);
            _updateThreatHealth(Suppressor, threats.SuppressorHealth, threats.SuppressorMaximumHealth);

            foreach (var cache in m_salvagePickups)
            {
                if (!cache.activeSelf)
                {
                    continue;
                }

                var locator = cache.transform.Find("Salvage Locator");
                if (locator != null)
                {
                    var distance = FlatDistance(Player.position, cache.transform.position);
                    var proximity = 1f - Mathf.Clamp01((distance - 0.85f) / 3f);
                    locator.localScale = Vector3.one * Mathf.Lerp(1.35f, 1.65f, proximity);
                    locator.Rotate(Vector3.up, dt * Mathf.Lerp(45f, 130f, proximity), Space.Self);
                }

                var beacon = cache.transform.Find("Salvage Beacon");
                if (beacon != null)
                {
                    var pulse = 1f + Mathf.Sin(Time.time * 5f + cache.transform.position.x) * 0.18f;
                    beacon.localScale = new Vector3(0.1f * pulse, 1.3f, 0.1f * pulse);
                }
            }

            if (model.Signal / RunModel.MaximumSignal <= 0.25f)
            {
                var poweredTarget = GetNearestPoweredTarget(Player.position, model.TowerOnline);
                poweredTarget = GetNavigationWaypoint(Player.position, poweredTarget, 0.48f, model.ShortcutOpen);
                _updateGuideLine(m_emergencyGuide, Player.position, poweredTarget, m_palette.Cyan, 0.1f);
                m_emergencyGuide.enabled = true;
            }
            else
            {
                m_emergencyGuide.enabled = false;
            }

            if (LastMovementBlocked)
            {
                m_collisionPulse = 1f;
            }
            m_collisionPulse = Mathf.MoveTowards(m_collisionPulse, 0f, dt * 4f);
            PlayerSignalWake.SetCollisionIntensity(m_collisionPulse);
        }

        public void PlayBoundaryTransition()
        {
            m_boundaryPulse = 1f;
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
            cameraObject.transform.localPosition = new Vector3(0f, 12f, -7.4f);
            cameraObject.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            Camera = cameraObject.AddComponent<Camera>();
            Camera.orthographic = false;
            Camera.fieldOfView = 38f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.002f, 0.004f, 0.008f);
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 80f;
            cameraObject.AddComponent<AudioListener>();
            _buildPostProcessing(cameraObject);

            var lightObject = new GameObject("Cold Overhead Light");
            lightObject.transform.SetParent(m_root);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.38f, 0.52f, 0.65f);
            key.intensity = 1.35f;
            key.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.045f, 0.055f, 0.07f);
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

            _createTerritory("Dock Power Territory", ExtractionPosition, STARTING_POWER_RADIUS, m_poweredTerritoryMaterial);
            _createTerritoryMarkers("Dock Power Boundary", ExtractionPosition, STARTING_POWER_RADIUS, m_palette.Cyan, null);
            m_towerTerritory = _createTerritory("Tower Power Territory", TowerPosition, TOWER_POWER_RADIUS, m_palette.Dark);
            _createTerritoryMarkers("Tower Power Boundary", TowerPosition, TOWER_POWER_RADIUS, m_palette.Dark,
                m_towerTerritoryMarkers);

            for (var x = -12; x <= 12; x += 4)
            {
                _createPrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, 8.55f), new Vector3(1.5f, 0.035f, 0.07f), m_palette.RedDim);
                _createPrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, -8.55f), new Vector3(1.5f, 0.035f, 0.07f), m_palette.RedDim);
            }

            _buildExtraction();
            _buildTower();
            _buildStationMachines();
            _buildSignalShortcut();
            _buildRouteDetails();
            _buildLocalizedLighting();
            _buildEnvironmentalDressing();
            _buildGameplayAssists();
            _buildExtractionApproach();
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
                m_environmentAnimators.Add(m_extractionBeacon.transform);
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
            m_environmentAnimators.Add(m_extractionBeacon.transform);
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
                m_environmentAnimators.Add(TowerCore);
                SignalTowerPartCount = 3;
                m_environmentAnimators.Add(TowerCore);
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

            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(TowerPosition.x, TowerPosition.z), Vector2.one * TOWER_BLOCKER_HALF_SIZE, false));
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
                _createObstacleTrim(obstacle);
            }

            AuthoredMapObstacleCount = authoredObstacles.Length;
        }

        private void _buildActors(IComfortSettings comfortSettings)
        {
            _buildPlayer();

            _registerInterceptorEntrances();
            _buildInterceptor();
            _buildSuppressor();

            _buildWarden();

            var wardenTelegraphRoot = new GameObject("Warden Strike Warning");
            wardenTelegraphRoot.transform.SetParent(m_root);
            WardenTelegraph = wardenTelegraphRoot.AddComponent<WardenThreatTelegraph>();
            WardenTelegraph.Configure(Warden, Player, comfortSettings);

            _buildSapper();
            _addThreatSilhouette(Interceptor, m_palette.Red, 0.9f, true);
            _addThreatSilhouette(Suppressor, m_palette.Amber, 1.05f, false);
            _addThreatSilhouette(Warden, m_palette.Red, 1.25f, false);
            _addThreatSilhouette(Sapper, m_palette.Magenta, 0.95f, true);

            var telegraphRoot = new GameObject("Sapper Drain Telegraph");
            telegraphRoot.transform.SetParent(m_root);
            SapperTelegraph = telegraphRoot.AddComponent<SignalSapperTelegraph>();
            SapperTelegraph.Configure(Sapper, TowerPosition, m_palette.Magenta, m_palette.Magenta, comfortSettings);

            var routeVariant = PlayerPrefs.GetInt("DeadSignal.RouteVariant", 0) % 3;
            var northCache = routeVariant == 1 ? new Vector3(8.7f, 0f, 6.5f) : new Vector3(9.7f, 0f, 6.3f);
            var southCache = routeVariant == 2 ? new Vector3(9.2f, 0f, -6.5f) : new Vector3(10.4f, 0f, -6.4f);
            var relayCache = routeVariant == 0 ? new Vector3(-5.8f, 0f, 7.2f) : new Vector3(-7f, 0f, 6.9f);
            _createSalvage(northCache);
            _createSalvage(southCache);
            _createSalvage(relayCache);
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

        private void _buildSuppressor()
        {
            var prefab = Resources.Load<GameObject>(SECURITY_SUPPRESSOR_PREFAB_RESOURCE);
            var hasValidPrefab = prefab != null &&
                                 prefab.transform.Find("Suppressor Chassis") != null &&
                                 prefab.transform.Find("Suppressor Emitter Left") != null &&
                                 prefab.transform.Find("Suppressor Emitter Right") != null &&
                                 prefab.transform.Find("Suppressor Core") != null;
            var root = hasValidPrefab ? Object.Instantiate(prefab, m_root) : new GameObject("Security Suppressor");
            root.name = "Security Suppressor";
            if (!hasValidPrefab)
            {
                root.transform.SetParent(m_root);
                _createPrimitive("Suppressor Chassis", PrimitiveType.Cylinder, new Vector3(0f, 0.34f, 0f),
                    new Vector3(0.9f, 0.22f, 0.9f), m_palette.WardenHousing, root.transform);
                _createPrimitive("Suppressor Emitter Left", PrimitiveType.Cube, new Vector3(-0.58f, 0.38f, 0f),
                    new Vector3(0.18f, 0.18f, 0.92f), m_palette.Magenta, root.transform);
                _createPrimitive("Suppressor Emitter Right", PrimitiveType.Cube, new Vector3(0.58f, 0.38f, 0f),
                    new Vector3(0.18f, 0.18f, 0.92f), m_palette.Magenta, root.transform);
                _createPrimitive("Suppressor Core", PrimitiveType.Sphere, new Vector3(0f, 0.58f, 0f),
                    new Vector3(0.3f, 0.22f, 0.3f), m_palette.Magenta, root.transform);
            }

            Suppressor = root.transform;
            Suppressor.Find("Suppressor Chassis").GetComponent<Renderer>().sharedMaterial = m_palette.WardenHousing;
            Suppressor.Find("Suppressor Emitter Left").GetComponent<Renderer>().sharedMaterial = m_palette.Magenta;
            Suppressor.Find("Suppressor Emitter Right").GetComponent<Renderer>().sharedMaterial = m_palette.Magenta;
            SuppressorCore = Suppressor.Find("Suppressor Core");
            SuppressorCore.GetComponent<Renderer>().sharedMaterial = m_palette.Amber;
            HasSecuritySuppressorAssets = hasValidPrefab;
            SecuritySuppressorPartCount = 4;
            Suppressor.position = m_interceptorEntrances[0];
            Suppressor.gameObject.SetActive(false);

            m_suppressorField = _createPrimitive("Suppressor Field Warning", PrimitiveType.Cylinder, Vector3.zero,
                Vector3.one, m_palette.Amber, m_root);
            Object.Destroy(m_suppressorField.GetComponent<Collider>());
            m_suppressorField.SetActive(false);
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

            var body = new GameObject("Drone Body Facing");
            PlayerBody = body.transform;
            PlayerBody.SetParent(PlayerPresentation, false);

            var turret = new GameObject("Drone Turret Facing");
            PlayerTurret = turret.transform;
            PlayerTurret.SetParent(PlayerPresentation, false);
            foreach (var child in visualChildren)
            {
                var facingRoot = child == PlayerNose || child.name == "Drone Core" ? PlayerTurret : PlayerBody;
                child.SetParent(facingRoot, true);
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

            _createPrimitive("Salvage Locator", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f),
                new Vector3(1.35f, 0.025f, 1.35f), m_palette.Amber, root.transform);
            _createPrimitive("Salvage Beacon", PrimitiveType.Cube, new Vector3(0f, 1.35f, 0f),
                new Vector3(0.1f, 1.3f, 0.1f), m_palette.Amber, root.transform);

            m_salvagePickups.Add(root);
            SalvageCacheInstanceCount++;
            SalvageCachePartCount += 4;
            HasSalvageCacheAssets = hasValidPrefab && m_palette.HasSalvageCacheTexture &&
                                    SalvageCachePartCount == SalvageCacheInstanceCount * 4;
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

        private bool _hasNavigationLine(Vector3 start, Vector3 end, float radius, bool shortcutOpen)
        {
            return !_tryGetNavigationBlocker(start, end, radius, shortcutOpen, out _);
        }

        private bool _tryGetNavigationBlocker(
            Vector3 start,
            Vector3 end,
            float radius,
            bool shortcutOpen,
            out MovementBlocker blockingObstacle)
        {
            blockingObstacle = null;
            var nearestFraction = float.PositiveInfinity;
            foreach (var blocker in m_movementBlockers)
            {
                if ((blocker.IsShortcutGate && shortcutOpen) ||
                    blocker.Overlaps(end, 0f) ||
                    !blocker.TryGetSweepHit(start, end, radius, out var hitFraction, out _) ||
                    hitFraction >= nearestFraction)
                {
                    continue;
                }

                nearestFraction = hitFraction;
                blockingObstacle = blocker;
            }

            return blockingObstacle != null;
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

        private void _createTerritoryMarkers(
            string objectName,
            Vector3 position,
            float radius,
            Material material,
            List<GameObject> markers)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(m_root, false);
            for (var index = 0; index < 16; index++)
            {
                var angle = index * Mathf.PI * 2f / 16f;
                var markerPosition = position + new Vector3(Mathf.Cos(angle) * radius, -0.045f, Mathf.Sin(angle) * radius);
                var marker = _createPrimitive($"Boundary Marker {index + 1:00}", PrimitiveType.Cube, markerPosition,
                    new Vector3(0.08f, 0.035f, 0.7f), material, root.transform);
                marker.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                markers?.Add(marker);
            }
        }

        private void _createObstacleTrim(AuthoredMapObstacle obstacle)
        {
            var halfSize = obstacle.ScaledHalfSize;
            var center = new Vector3(obstacle.Center.x, 0.035f, obstacle.Center.y);
            var angle = -Mathf.Atan2(obstacle.RightAxis.y, obstacle.RightAxis.x) * Mathf.Rad2Deg;
            var trim = _createPrimitive("Collision Readability Trim", PrimitiveType.Cube, center,
                new Vector3(halfSize.x * 2f + 0.18f, 0.035f, halfSize.y * 2f + 0.18f), m_palette.Steel);
            trim.transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        private void _buildRouteDetails()
        {
            var details = new GameObject("Route Identity Markings");
            details.transform.SetParent(m_root, false);
            var routePoints = new[]
            {
                new Vector3(-8.8f, -0.035f, -3.5f), new Vector3(-5.8f, -0.035f, -1.6f),
                new Vector3(2.8f, -0.035f, 2.8f), new Vector3(7.2f, -0.035f, 5.5f),
                new Vector3(7.8f, -0.035f, -5.6f)
            };
            for (var index = 0; index < routePoints.Length; index++)
            {
                var material = index < 2 ? m_palette.CyanDim : index < 4 ? m_palette.Amber : m_palette.RedDim;
                _createPrimitive($"Route Stripe {index + 1:00}", PrimitiveType.Cube, routePoints[index],
                    new Vector3(2.2f, 0.025f, 0.12f), material, details.transform);
            }
        }

        private void _createTerritoryMaterials()
        {
            var shader = Shader.Find("Dead Signal/Powered Territory");
            if (shader == null)
            {
                Debug.LogWarning("Powered territory shader was not found; the deck will use the clarity fallback.");
                m_poweredTerritoryMaterial = m_palette.CyanDim;
                return;
            }

            m_poweredTerritoryMaterial = new Material(shader) { name = "Powered Territory Runtime" };
            m_poweredTerritoryMaterial.SetColor("_BaseColor", new Color(0.015f, 0.42f, 0.5f, 0.32f));
            m_poweredTerritoryMaterial.SetColor("_EdgeColor", new Color(0.05f, 0.95f, 1f, 0.92f));
        }

        private void _buildLocalizedLighting()
        {
            _createLandmarkLight("Tower Signal Pool", TowerPosition + Vector3.up * 3.2f, new Color(0.05f, 0.75f, 1f), 7f, 1.2f);
            _createLandmarkLight("Extraction Guidance Pool", ExtractionPosition + Vector3.up * 3f,
                new Color(0.08f, 0.9f, 1f), 6f, 1.05f);
            _createLandmarkLight("Salvage Annex Worklight", new Vector3(8.8f, 3f, 5.8f), new Color(1f, 0.48f, 0.08f), 5f, 0.75f);
            _createLandmarkLight("Security Bay Alarm", new Vector3(8.5f, 3f, -5.5f), new Color(1f, 0.08f, 0.12f), 5f, 0.65f);
        }

        private void _createLandmarkLight(string objectName, Vector3 position, Color color, float range, float intensity)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(m_root, false);
            root.transform.position = position;
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            m_landmarkLights.Add(light);
        }

        private void _buildEnvironmentalDressing()
        {
            var root = new GameObject("Station Surface Storytelling");
            root.transform.SetParent(m_root, false);
            var accents = new[]
            {
                (new Vector3(-10.2f, -0.03f, -5.8f), new Vector3(2.8f, 0.02f, 0.08f), m_palette.Cyan),
                (new Vector3(-1.0f, -0.03f, 3.2f), new Vector3(0.08f, 0.02f, 3.2f), m_palette.CyanDim),
                (new Vector3(9.4f, -0.03f, 5.1f), new Vector3(2.4f, 0.02f, 0.12f), m_palette.Amber),
                (new Vector3(9.1f, -0.03f, -5.2f), new Vector3(2.4f, 0.02f, 0.12f), m_palette.RedDim),
                (new Vector3(-5.2f, -0.025f, 6.9f), new Vector3(1.4f, 0.018f, 0.5f), m_palette.Steel)
            };
            for (var index = 0; index < accents.Length; index++)
            {
                _createPrimitive($"Floor Story Accent {index + 1:00}", PrimitiveType.Cube, accents[index].Item1,
                    accents[index].Item2, accents[index].Item3, root.transform);
            }
        }

        private void _buildPostProcessing(GameObject cameraObject)
        {
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var volumeObject = new GameObject("Dead Signal Global Grade");
            volumeObject.transform.SetParent(m_root, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = volume.profile.Add<Bloom>();
            bloom.intensity.Override(0.28f);
            bloom.threshold.Override(1.15f);
            bloom.scatter.Override(0.5f);
            m_deadZoneVignette = volume.profile.Add<Vignette>();
            m_deadZoneVignette.intensity.Override(0.14f);
            m_deadZoneVignette.smoothness.Override(0.48f);
            var color = volume.profile.Add<ColorAdjustments>();
            color.postExposure.Override(0.08f);
            color.contrast.Override(8f);
            color.saturation.Override(-5f);
        }

        private void _buildGameplayAssists()
        {
            m_routeGuide = _createGuideLine("Objective Route Pulse", 18);
            m_aimGuide = _createGuideLine("Projected Aim Guide", 2);
            m_emergencyGuide = _createGuideLine("Critical Signal Route", 12);
        }

        private LineRenderer _createGuideLine(string objectName, int positionCount)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(m_root, false);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = m_palette.SignalRouting;
            line.useWorldSpace = true;
            line.positionCount = positionCount;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.enabled = false;
            return line;
        }

        private void _updateGuideLine(LineRenderer line, Vector3 start, Vector3 end, Material material, float width)
        {
            line.sharedMaterial = material;
            line.startWidth = width;
            line.endWidth = width * 0.35f;
            var count = line.positionCount;
            var flatDirection = end - start;
            flatDirection.y = 0f;
            var side = Vector3.Cross(Vector3.up, flatDirection.normalized);
            var bend = Mathf.Min(1.35f, flatDirection.magnitude * 0.16f);
            var control = Vector3.Lerp(start, end, 0.5f) + side * bend;
            for (var index = 0; index < count; index++)
            {
                var progress = count <= 1 ? 1f : index / (count - 1f);
                var inverse = 1f - progress;
                var position = inverse * inverse * start + 2f * inverse * progress * control + progress * progress * end;
                position.y = 0.12f + Mathf.Sin(progress * Mathf.PI * 8f - m_environmentTime * 4f) * 0.025f;
                line.SetPosition(index, position);
            }
            line.enabled = true;
        }

        private Vector3 _currentObjectiveTarget(RunModel model)
        {
            if (!model.TowerOnline)
            {
                return TowerPosition;
            }
            if (model.CanExtract)
            {
                return ExtractionPosition;
            }
            var nearest = TowerPosition;
            var distance = float.PositiveInfinity;
            foreach (var cache in m_salvagePickups)
            {
                if (cache.activeSelf && FlatDistance(Player.position, cache.transform.position) < distance)
                {
                    nearest = cache.transform.position;
                    distance = FlatDistance(Player.position, nearest);
                }
            }
            return nearest;
        }

        private void _updateThreatHealth(Transform threat, float health, float maximum)
        {
            var bar = threat?.Find("World Health Remaining");
            if (bar == null)
            {
                return;
            }
            bar.gameObject.SetActive(threat.gameObject.activeSelf && health > 0f);
            bar.localScale = new Vector3(Mathf.Clamp01(health / maximum) * 1.15f, 0.06f, 0.08f);
        }

        private void _buildExtractionApproach()
        {
            var root = new GameObject("Extraction Approach Lane");
            root.transform.SetParent(m_root, false);
            for (var index = 0; index < 7; index++)
            {
                var progress = index / 6f;
                var center = Vector3.Lerp(new Vector3(-3.5f, -0.02f, -3.8f), ExtractionPosition, progress);
                _createPrimitive($"Approach Light L {index + 1:00}", PrimitiveType.Cube,
                    center + new Vector3(-0.65f, 0f, 0f), new Vector3(0.32f, 0.025f, 0.07f), m_palette.Cyan, root.transform);
                _createPrimitive($"Approach Light R {index + 1:00}", PrimitiveType.Cube,
                    center + new Vector3(0.65f, 0f, 0f), new Vector3(0.32f, 0.025f, 0.07f), m_palette.Cyan, root.transform);
            }
        }

        private void _addThreatSilhouette(Transform threat, Material material, float width, bool swept)
        {
            if (threat == null)
            {
                return;
            }

            var left = _createPrimitive("Threat Silhouette Left", PrimitiveType.Cube,
                new Vector3(-width, 0.28f, swept ? -0.3f : 0f), new Vector3(0.55f, 0.09f, 0.16f), material, threat);
            var right = _createPrimitive("Threat Silhouette Right", PrimitiveType.Cube,
                new Vector3(width, 0.28f, swept ? 0.3f : 0f), new Vector3(0.55f, 0.09f, 0.16f), material, threat);
            left.transform.localRotation = Quaternion.Euler(0f, swept ? -28f : 0f, 0f);
            right.transform.localRotation = Quaternion.Euler(0f, swept ? 28f : 0f, 0f);
            _createPrimitive("World Health Backing", PrimitiveType.Cube, new Vector3(-0.58f, 1.28f, 0f),
                new Vector3(1.25f, 0.08f, 0.1f), m_palette.Dark, threat);
            _createPrimitive("World Health Remaining", PrimitiveType.Cube, new Vector3(-0.58f, 1.3f, 0f),
                new Vector3(1.15f, 0.06f, 0.08f), material, threat);
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
        private const string SECURITY_SUPPRESSOR_PREFAB_RESOURCE = "Actors/SecuritySuppressorAssembly";
        private const string SIGNAL_BOLT_PREFAB_RESOURCE = "Projectiles/SignalBoltAssembly";
        private const string PLAYER_CAMERA_TUNING_RESOURCE = "Tuning/PlayerCameraTuning";
        private const float DECK_MODULE_WIDTH = 3.9f;
        private const float DECK_MODULE_DEPTH = 3.6f;
        private const float TOWER_BLOCKER_HALF_SIZE = 0.62f;
        private const float NAVIGATION_CLEARANCE = 0.08f;
        private const float NAVIGATION_BLOCKED_ROUTE_PENALTY = 20f;

        private static readonly Vector3 s_securityWardenSpawn = new(6.8f, 0f, 4.7f);
        private static readonly Vector3 s_signalSapperSpawn = new(-10.8f, 0f, 5.7f);
        private static readonly Vector3 s_interceptorNorthSpawn = new(-16.4f, 0f, 7.1f);
        private static readonly Vector3 s_interceptorSouthSpawn = new(1.5f, 0f, -7.5f);

        private readonly Transform m_root;
        private readonly DeadSignalPalette m_palette;
        private readonly GameObject m_signalBoltPrefab;
        private readonly List<MovementBlocker> m_movementBlockers = new();
        private readonly List<GameObject> m_salvagePickups = new();
        private readonly List<GameObject> m_towerTerritoryMarkers = new();
        private readonly List<Transform> m_environmentAnimators = new();
        private readonly List<Light> m_landmarkLights = new();
        private readonly List<Vector3> m_machineSockets = new();
        private readonly List<Vector3> m_interceptorEntrances = new();
        private GameObject m_suppressorField;
        private Material m_poweredTerritoryMaterial;
        private Vignette m_deadZoneVignette;
        private float m_environmentTime;
        private float m_boundaryPulse;
        private float m_collisionPulse;
        private LineRenderer m_routeGuide;
        private LineRenderer m_aimGuide;
        private LineRenderer m_emergencyGuide;

        private GameObject m_towerTerritory;
        private GameObject m_towerSignalLines;
        private GameObject m_extractionBeacon;
        private GameObject m_shortcutGate;
        private Transform m_cameraRig;
        private LineRenderer m_interceptorTelegraph;

        private sealed class MovementBlocker
        {
            public const int DETOUR_WAYPOINT_COUNT = 4;

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

            public Vector2 GetDetourWaypoint(int index, float clearance)
            {
                var expandedRight = RightAxis * (HalfSize.x + clearance);
                var expandedForward = ForwardAxis * (HalfSize.y + clearance);
                return index switch
                {
                    0 => Center + expandedRight + expandedForward,
                    1 => Center + expandedRight - expandedForward,
                    2 => Center - expandedRight + expandedForward,
                    _ => Center - expandedRight - expandedForward
                };
            }
        }
    }
}
