using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.Player;
using DeadSignal.Presentation;

namespace DeadSignal.World
{
    /// <summary>
    /// Builds runtime game objects and consumes scene-authored spatial data for the current prototype map.
    /// </summary>
    internal sealed class DeadSignalWorld
    {
        public const float STARTING_POWER_RADIUS = 3.6f;
        public const float TOWER_POWER_RADIUS = 7.2f;
        public const float SPINE_POWER_RADIUS = 6.2f;

        public Vector3 ExtractionPosition => m_scene.ExtractionPosition;
        public Vector3 TowerPosition => m_scene.TowerPosition;
        public Vector3 ShortcutPosition => m_scene.ShortcutPosition;
        public Vector3 RelayTowerPosition => m_scene.RelayTowerPosition;
        public Vector3 SpineTowerPosition => m_scene.SpineTowerPosition;
        public Vector2 ArenaHalfExtents => m_scene.ArenaHalfExtents;

        public Vector3 GetSalvagePosition(int index)
        {
            if (m_salvagePickups.Count == 0)
            {
                return TowerPosition;
            }

            return m_salvagePickups[Mathf.Clamp(index, 0, m_salvagePickups.Count - 1)].transform.position;
        }

        public Camera Camera { get; private set; }
        public Transform Player { get; private set; }
        public Transform PlayerNose { get; private set; }
        public Transform PlayerPresentation { get; private set; }
        public Transform PlayerBody { get; private set; }
        public Transform PlayerTurret { get; private set; }
        public PlayerDroneSignalWake PlayerSignalWake { get; private set; }
        public PlayerCombatPresentation PlayerCombatPresentation { get; private set; }
        public ForegroundOcclusionController ForegroundOcclusion { get; private set; }
        public Transform Warden { get; private set; }
        public WardenThreatTelegraph WardenTelegraph { get; private set; }
        public Transform Sapper { get; private set; }
        public Transform SapperCore { get; private set; }
        public Vector3 SapperCoreBaseScale { get; private set; }
        public Transform Interceptor { get; private set; }
        public Transform InterceptorCore { get; private set; }
        public Transform Suppressor { get; private set; }
        public Transform SuppressorCore { get; private set; }
        public SuppressorFieldTelegraph SuppressorFieldTelegraph { get; private set; }
        public Transform TowerCore { get; private set; }
        public Transform RelayTowerCore { get; private set; }
        public Transform SpineTowerCore { get; private set; }
        public SignalSapperTelegraph SapperTelegraph { get; private set; }
        public IReadOnlyList<GameObject> SalvagePickups => m_salvagePickups;

        public SignalRegion GetPayloadRegion(GameObject pickup) => m_salvageRegions.TryGetValue(pickup, out var region)
            ? region
            : SignalRegion.Central;

        public bool IsOptionalCache(GameObject pickup) => m_optionalSalvagePickups.Contains(pickup);

        public void RetirePayloadAlternatives(SignalRegion securedRegion, GameObject securedPickup)
        {
            foreach (var pickup in m_salvagePickups)
            {
                if (pickup != securedPickup && !IsOptionalCache(pickup) && GetPayloadRegion(pickup) == securedRegion)
                {
                    pickup.SetActive(false);
                }
            }
        }
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
        public ReinforcementEntryTelegraph ReinforcementEntryTelegraph { get; private set; }
        public int AuthoredMapObstacleCount { get; private set; }
        public int AuthoredSalvageSocketCount { get; private set; }
        public bool HasPlayerCameraTuning { get; private set; }
        public PlayerFollowCamera PlayerCamera { get; private set; }
        public bool LastMovementBlocked { get; private set; }
        public bool HasRuntimeNavMesh => m_navMeshPlanner?.IsReady ?? false;
        public string NavMeshStatus => m_navMeshPlanner?.LastStatus ?? "Unavailable";
        public bool IsDepartureSurgeConsumed => m_departureSurgeConsumed;

        public DeadSignalWorld(Transform root, IComfortSettings comfortSettings)
        {
            m_root = root;
            m_scene = _findSceneReferences();
            if (m_scene == null || !m_scene.IsComplete)
            {
                throw new MissingReferenceException(
                    "SampleScene is missing its complete DeadSignalSceneReferences composition. " +
                    $"Missing: {m_scene?.MissingReferences ?? "component"}. " +
                    "Run Tools/DEAD SIGNAL/Migrate Runtime World To Scene.");
            }

            m_palette = new DeadSignalPalette(comfortSettings.HighContrastEnabled);
            m_signalBoltPrefab = Resources.Load<GameObject>(SIGNAL_BOLT_PREFAB_RESOURCE);
            m_signalBoltTuning = Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning");
            HasSignalBoltAssets = m_signalBoltPrefab != null &&
                                  m_signalBoltPrefab.transform.Find("Bolt Shell") != null &&
                                  m_signalBoltPrefab.transform.Find("Bolt Energy") != null;
            _buildPresentation();
            _buildArena();
            _registerAuthoredMapObstacles();
            m_navMeshPlanner = new DeadSignalNavMeshPlanner();
            _rebuildNavMesh();
            _registerAuthoredPoweredTerritories();
            _buildActors(comfortSettings);
            m_palette.RebindHierarchy(m_root);
            _configurePlayerCamera();
            _configurePlayerCombatPresentation(comfortSettings);
            ApplyHighContrast(comfortSettings.HighContrastEnabled);
        }

        public bool IsPowered(Vector3 position, bool towerOnline, bool relayTowerOnline = false, bool spineTowerOnline = false)
        {
            if (FlatDistance(position, ExtractionPosition) <= STARTING_POWER_RADIUS)
            {
                return true;
            }

            if (towerOnline && FlatDistance(position, TowerPosition) <= TOWER_POWER_RADIUS ||
                   relayTowerOnline && FlatDistance(position, RelayTowerPosition) <= TOWER_POWER_RADIUS ||
                   spineTowerOnline && FlatDistance(position, SpineTowerPosition) <= SPINE_POWER_RADIUS)
            {
                return true;
            }

            foreach (var territory in m_authoredPoweredTerritories)
            {
                if (_isTerritorySourceOnline(territory.Source, towerOnline, relayTowerOnline, spineTowerOnline) &&
                    territory.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 ClampToArena(Vector3 position, float radius)
        {
            position.x = Mathf.Clamp(position.x, -ArenaHalfExtents.x + radius, ArenaHalfExtents.x - radius);
            position.z = Mathf.Clamp(position.z, -ArenaHalfExtents.y + radius, ArenaHalfExtents.y - radius);
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
                    if (!blocker.IsActive || (blocker.IsShortcutGate && shortcutOpen) ||
                        (blocker.IsRelayShortcutGate && m_relayShortcutOpen) ||
                        (blocker.IsSpineReturnGate && m_spineReturnOpen) ||
                        (blocker.IsQuenchReturnGate && m_quenchReturnOpen) ||
                        (blocker.IsDepartureReturnGate && m_departureReturnOpen) ||
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

        public void RebindRuntimeMaterials(Transform root)
        {
            if (root != null)
            {
                m_palette.RebindHierarchy(root);
            }
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

        public Vector3 GetNavMeshWaypoint(Transform actor, Vector3 destination, float radius, bool shortcutOpen)
        {
            if (m_navMeshPlanner?.IsReady == true)
            {
                var waypoint = m_navMeshPlanner.GetWaypoint(actor, destination);
                if (DeadSignalWorld.FlatDistance(actor.position, waypoint) > 0.05f)
                {
                    return waypoint;
                }
            }
            return GetNavigationWaypoint(actor.position, destination, radius, shortcutOpen);
        }

        public int GetRemainingNavMeshCorners(Transform actor) => m_navMeshPlanner?.GetRemainingCornerCount(actor) ?? 0;

        public void InvalidateNavMeshRoute(Transform actor) => m_navMeshPlanner?.Invalidate(actor);

        public Vector3 GetObjectiveTarget(RunModel model)
        {
            return _currentObjectiveTarget(model);
        }

        public Vector3 GetObjectiveGuidanceWaypoint(RunModel model, float radius)
        {
            return GetNavigationWaypoint(Player.position, _currentObjectiveTarget(model), radius, model.ShortcutOpen);
        }

        public Vector3 GetNearestPoweredTarget(
            Vector3 position,
            bool towerOnline,
            bool relayTowerOnline = false,
            bool spineTowerOnline = false)
        {
            if (!towerOnline)
            {
                return FlatDistance(position, TowerPosition) <= FlatDistance(position, ExtractionPosition)
                    ? TowerPosition
                    : ExtractionPosition;
            }

            var nearest = FlatDistance(position, TowerPosition) <= FlatDistance(position, ExtractionPosition)
                ? TowerPosition : ExtractionPosition;
            if (relayTowerOnline && FlatDistance(position, RelayTowerPosition) < FlatDistance(position, nearest))
            {
                nearest = RelayTowerPosition;
            }

            return spineTowerOnline && FlatDistance(position, SpineTowerPosition) < FlatDistance(position, nearest)
                ? SpineTowerPosition : nearest;
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
                if (!blocker.IsActive || (blocker.IsShortcutGate && shortcutOpen) ||
                    (blocker.IsRelayShortcutGate && m_relayShortcutOpen) ||
                    (blocker.IsSpineReturnGate && m_spineReturnOpen) ||
                    (blocker.IsQuenchReturnGate && m_quenchReturnOpen) ||
                    (blocker.IsDepartureReturnGate && m_departureReturnOpen))
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
            m_towerTerritory.GetComponent<Renderer>().sharedMaterial = m_palette.PoweredTerritory;
            foreach (var marker in m_towerTerritoryMarkers)
            {
                marker.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            }
            TowerCore.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            m_towerSignalLines.SetActive(true);
            Warden.gameObject.SetActive(true);
            Sapper.gameObject.SetActive(true);
            SapperTelegraph.SetThreatState(true, false, 0f, sapperPulseInterval);
            _activateAuthoredTerritories(PoweredTerritorySource.CentralTower);
        }

        public void ActivateRelayTower()
        {
            m_relayTerritory.GetComponent<Renderer>().sharedMaterial = m_palette.PoweredTerritory;
            foreach (var marker in m_relayTerritoryMarkers)
            {
                marker.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            }
            RelayTowerCore.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            m_relaySignalLines.SetActive(true);
            m_relayShortcutGate.SetActive(false);
            m_relayShortcutOpen = true;
            _rebuildNavMesh();
            _activateAuthoredTerritories(PoweredTerritorySource.RelayTower);
        }

        public void ActivateSpineTower()
        {
            m_spineTerritory.GetComponent<Renderer>().sharedMaterial = m_palette.PoweredTerritory;
            foreach (var marker in m_spineTerritoryMarkers)
            {
                marker.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            }
            SpineTowerCore.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            m_spineSignalLines.SetActive(true);
            m_spineReturnGate.SetActive(false);
            m_spineReturnOpen = true;
            _rebuildNavMesh();
            _activateAuthoredTerritories(PoweredTerritorySource.SpineTower);
        }

        public void OpenQuenchReturn()
        {
            if (m_quenchReturnOpen || m_quenchReturnGate == null)
            {
                return;
            }

            m_quenchReturnGate.SetActive(false);
            if (m_quenchReturnSignal != null)
            {
                m_quenchReturnSignal.SetActive(true);
            }
            m_quenchReturnOpen = true;
            _rebuildNavMesh();
        }

        public void OpenDepartureReturn()
        {
            if (m_departureReturnOpen || m_departureReturnGate == null)
            {
                return;
            }

            m_departureReturnGate.SetActive(false);
            if (m_departureReturnSignal != null)
            {
                m_departureReturnSignal.SetActive(true);
            }
            if (m_departureSurgeSignal != null)
            {
                m_departureSurgeSignal.SetActive(true);
            }
            m_departurePreviousLocalX = m_departureChannel == null
                ? 0f
                : m_departureChannel.InverseTransformPoint(Player.position).x;
            m_departureReturnOpen = true;
            _rebuildNavMesh();
        }

        public bool TryConsumeDepartureSurge(Vector3 playerPosition)
        {
            if (!m_departureReturnOpen || m_departureSurgeConsumed || m_departureChannel == null)
            {
                return false;
            }

            var localPosition = m_departureChannel.InverseTransformPoint(playerPosition);
            var crossedDirectLane = m_departurePreviousLocalX < 0f && localPosition.x >= 0f &&
                                    Mathf.Abs(localPosition.z) <= 0.9f;
            m_departurePreviousLocalX = localPosition.x;
            if (!crossedDirectLane)
            {
                return false;
            }

            m_departureSurgeConsumed = true;
            if (m_departureSurgeSignal != null)
            {
                m_departureSurgeSignal.SetActive(false);
            }
            return true;
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
            m_shortcutOpen = true;
            _rebuildNavMesh();
        }

        public void RefreshNavigation()
        {
            _rebuildNavMesh();
        }

        public void Dispose()
        {
            m_navMeshPlanner?.Dispose();
            m_palette.Dispose();
        }

        private void _rebuildNavMesh()
        {
            if (m_navMeshPlanner == null)
            {
                return;
            }
            var obstacles = new List<NavMeshObstacleBounds>(m_movementBlockers.Count);
            foreach (var blocker in m_movementBlockers)
            {
                if (!blocker.IsActive || blocker.IsShortcutGate && m_shortcutOpen ||
                    blocker.IsRelayShortcutGate && m_relayShortcutOpen ||
                    blocker.IsSpineReturnGate && m_spineReturnOpen ||
                    blocker.IsQuenchReturnGate && m_quenchReturnOpen ||
                    blocker.IsDepartureReturnGate && m_departureReturnOpen)
                {
                    continue;
                }
                obstacles.Add(new NavMeshObstacleBounds(blocker.Center, blocker.HalfSize, blocker.ForwardAxis));
            }
            m_navMeshPlanner.Build(ArenaHalfExtents, obstacles, 0.48f);
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
            return GetInterceptorEntryDistance(GetSafestInterceptorEntryIndex(playerPosition), playerPosition);
        }

        public int GetSafestInterceptorEntryIndex(Vector3 playerPosition)
        {
            if (m_deepRouteEntranceIndex >= 0 && playerPosition.z > 12.5f)
            {
                return m_deepRouteEntranceIndex;
            }

            var pairStart = m_interceptorEntrances.Count >= 4 && playerPosition.x > m_scene.RelayShortcutPosition.x
                ? 2
                : 0;
            return pairStart + InterceptorTactics.SelectSafestEntrance(
                playerPosition,
                m_interceptorEntrances[pairStart],
                m_interceptorEntrances[pairStart + 1]);
        }

        public float GetInterceptorEntryDistance(int index, Vector3 playerPosition)
        {
            return FlatDistance(playerPosition, m_interceptorEntrances[Mathf.Clamp(index, 0, m_interceptorEntrances.Count - 1)]);
        }

        public void DeployInterceptorReinforcement(int entranceIndex = -1)
        {
            var index = entranceIndex >= 0 ? entranceIndex : GetSafestInterceptorEntryIndex(Player.position);
            Interceptor.position = m_interceptorEntrances[index];
            Interceptor.gameObject.SetActive(true);
        }

        public void DeploySuppressorReinforcement(int entranceIndex = -1)
        {
            var index = entranceIndex >= 0 ? entranceIndex : GetSafestInterceptorEntryIndex(Player.position);
            Suppressor.position = m_interceptorEntrances[index];
            Suppressor.gameObject.SetActive(true);
            SetSuppressorField(false, false, 1f);
        }

        public Vector3 GetReinforcementEntryPosition(SecurityReinforcement reinforcement, int entranceIndex)
        {
            return reinforcement switch
            {
                SecurityReinforcement.Warden => s_securityWardenSpawn,
                SecurityReinforcement.Sapper => s_signalSapperSpawn,
                _ => m_interceptorEntrances[Mathf.Clamp(entranceIndex, 0, m_interceptorEntrances.Count - 1)]
            };
        }

        public void SetReinforcementEntryWarning(
            SecurityReinforcement reinforcement,
            int entranceIndex,
            bool blocked,
            float warningProgress)
        {
            var visible = reinforcement != SecurityReinforcement.None;
            var position = visible ? GetReinforcementEntryPosition(reinforcement, entranceIndex) : Vector3.zero;
            ReinforcementEntryTelegraph.SetState(visible, position, blocked, warningProgress);
        }

        public void SetSuppressorField(bool visible, bool active, float radius)
        {
            SetSuppressorFieldAt(visible, active, radius, Suppressor.position);
        }

        public void SetSuppressorFieldAt(bool visible, bool active, float radius, Vector3 center)
        {
            SuppressorFieldTelegraph.SetState(visible, active, radius, center);
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
            var trail = bolt.GetComponent<TrailRenderer>();
            if (trail != null && m_signalBoltTuning != null)
            {
                trail.time = m_signalBoltTuning.TrailDuration;
                trail.startWidth = m_signalBoltTuning.StartingWidth;
                trail.endWidth = m_signalBoltTuning.EndingWidth;
                trail.minVertexDistance = m_signalBoltTuning.MinimumVertexDistance;
                trail.startColor = new Color(0.72f, 1f, 1f, m_signalBoltTuning.MaximumAlpha);
                trail.endColor = new Color(0.05f, 0.75f, 1f, 0f);
            }

            var energy = bolt.transform.Find("Bolt Energy");
            if (energy != null)
            {
                energy.localScale *= 1.22f;
            }
            return bolt;
        }

        public void PlayPlayerShot(Vector3 direction)
        {
            PlayerCombatPresentation?.PlayShot(direction);
        }

        public void PlayPlayerDash(Vector3 start, Vector3 end)
        {
            PlayerCombatPresentation?.PlayDash(start, end);
        }

        public void TickTower(float dt, bool towerOnline)
        {
            TowerCore.Rotate(Vector3.up, (towerOnline ? 110f : 22f) * dt, Space.World);
            RelayTowerCore.Rotate(Vector3.up, (m_relayShortcutOpen ? 110f : 22f) * dt, Space.World);
            SpineTowerCore.Rotate(Vector3.up, 26f * dt, Space.World);
            var pulse = 1f + Mathf.Sin(Time.time * (towerOnline ? 5f : 2f)) * 0.08f;
            TowerCore.localScale = new Vector3(1.35f * pulse, 0.22f, 1.35f * pulse);
            RelayTowerCore.localScale = new Vector3(1.35f * pulse, 0.22f, 1.35f * pulse);
        }

        public void TickEnvironmentPresentation(float dt, bool towerOnline, bool powered)
        {
            m_environmentTime += dt;
            m_boundaryPulse = Mathf.MoveTowards(m_boundaryPulse, 0f, dt * 1.5f);
            if (m_palette.PoweredTerritory != null)
            {
                m_palette.PoweredTerritory.SetFloat("_Pulse", m_boundaryPulse);
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
                var poweredTarget = GetNearestPoweredTarget(
                    Player.position, model.TowerOnline, model.RelayTowerOnline, model.SpineTowerOnline);
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

        private static DeadSignalSceneReferences _findSceneReferences()
        {
            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                foreach (var sceneRoot in scene.GetRootGameObjects())
                {
                    var references = sceneRoot.GetComponentInChildren<DeadSignalSceneReferences>(true);
                    if (references != null)
                    {
                        return references;
                    }
                }
            }

            return null;
        }

        private void _buildPresentation()
        {
            Camera = m_scene.PlayerCamera;
            m_cameraRig = m_scene.CameraRig;
            m_cameraRig.SetParent(m_root, true);
            Camera.gameObject.SetActive(true);
            Camera.enabled = true;
            m_scene.KeyLight.gameObject.SetActive(true);
            m_scene.KeyLight.enabled = true;
            _buildPostProcessing(Camera.gameObject);
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
                ArenaHalfExtents);
        }

        private void _buildArena()
        {
            _bindAuthoredEnvironment();

            _createTerritory("Dock Power Territory", ExtractionPosition, STARTING_POWER_RADIUS, m_palette.PoweredTerritory);
            _createTerritoryMarkers("Dock Power Boundary", ExtractionPosition, STARTING_POWER_RADIUS, m_palette.Cyan, null);
            m_towerTerritory = _createTerritory("Tower Power Territory", TowerPosition, TOWER_POWER_RADIUS, m_palette.Dark);
            _createTerritoryMarkers("Tower Power Boundary", TowerPosition, TOWER_POWER_RADIUS, m_palette.Dark,
                m_towerTerritoryMarkers);
            m_relayTerritory = _createTerritory("Relay Power Territory", RelayTowerPosition, TOWER_POWER_RADIUS, m_palette.Dark);
            _createTerritoryMarkers("Relay Power Boundary", RelayTowerPosition, TOWER_POWER_RADIUS, m_palette.Dark,
                m_relayTerritoryMarkers);
            m_spineTerritory = _createTerritory("Spine Power Territory", SpineTowerPosition, SPINE_POWER_RADIUS, m_palette.Dark);
            _createTerritoryMarkers("Spine Power Boundary", SpineTowerPosition, SPINE_POWER_RADIUS, m_palette.Dark,
                m_spineTerritoryMarkers);

            _buildRouteDetails();
            _buildLocalizedLighting();
            _buildEnvironmentalDressing();
            _buildGameplayAssists();
            _buildExtractionApproach();
        }

        private void _bindAuthoredEnvironment()
        {
            m_scene.MaintenanceDeck.transform.SetParent(m_root, true);
            m_scene.MaintenanceRoomShell.transform.SetParent(m_root, true);
            m_scene.ExtractionPad.transform.SetParent(m_root, true);
            m_scene.SignalTower.transform.SetParent(m_root, true);
            m_scene.SignalRouting.transform.SetParent(m_root, true);
            m_scene.ShortcutGate.transform.SetParent(m_root, true);
            m_scene.StationMachines.transform.SetParent(m_root, true);
            m_scene.RelayFoundry.transform.SetParent(m_root, true);
            m_scene.CapacitorSpine.transform.SetParent(m_root, true);
            m_scene.SpineInductionGallery.transform.SetParent(m_root, true);
            foreach (var renderer in m_scene.MaintenanceDeck.GetComponentsInChildren<Renderer>())
            {
                MaintenanceDeckModuleCount++;
            }

            HasMaintenanceDeckAssets = MaintenanceDeckModuleCount == 35 && m_palette.HasDeckTexture;
            foreach (var renderer in m_scene.MaintenanceRoomShell.GetComponentsInChildren<Renderer>())
            {
                RoomShellBulkheadCount++;
            }

            var sockets = m_scene.MaintenanceRoomShell.transform.Find("Machine Sockets");
            if (sockets != null)
            {
                foreach (Transform socket in sockets)
                {
                    m_machineSockets.Add(socket.position);
                }
            }

            HasMaintenanceRoomShellAssets = RoomShellBulkheadCount == 5 && m_machineSockets.Count == 6 &&
                                            m_palette.HasBulkheadTexture;
            var extraction = m_scene.ExtractionPad.transform;
            m_extractionBeacon = extraction.Find("Extraction Beacon").gameObject;
            m_environmentAnimators.Add(m_extractionBeacon.transform);
            ExtractionPadPartCount = 4;
            HasExtractionPadAssets = m_palette.HasExtractionTexture;

            var tower = m_scene.SignalTower.transform;
            TowerCore = tower.Find("Tower Core");
            m_environmentAnimators.Add(TowerCore);
            SignalTowerPartCount = 3;
            HasSignalTowerAssets = m_palette.HasTowerTexture;

            m_towerSignalLines = m_scene.SignalRouting;
            foreach (var renderer in m_towerSignalLines.GetComponentsInChildren<Renderer>())
            {
                SignalRoutingPartCount++;
            }
            HasSignalRoutingAssets = SignalRoutingPartCount == 3 && m_palette.HasSignalRoutingTexture;
            m_towerSignalLines.SetActive(false);

            var relayTower = m_scene.RelayTower.transform;
            RelayTowerCore = relayTower.Find("Tower Core");
            m_environmentAnimators.Add(RelayTowerCore);
            m_relaySignalLines = m_scene.RelaySignalRouting;
            m_relaySignalLines.SetActive(false);
            m_relayShortcutGate = m_scene.RelayShortcutGate;
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(m_scene.RelayShortcutPosition.x, m_scene.RelayShortcutPosition.z),
                new Vector2(0.22f, 0.75f), false, true));
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(RelayTowerPosition.x, RelayTowerPosition.z), Vector2.one * TOWER_BLOCKER_HALF_SIZE, false));

            var spineTower = m_scene.SpineTower.transform;
            SpineTowerCore = spineTower.Find("Tower Core");
            m_environmentAnimators.Add(SpineTowerCore);
            m_spineSignalLines = m_scene.SpineSignalRouting;
            m_spineSignalLines.SetActive(false);
            m_spineReturnGate = m_scene.CapacitorSpine.transform.Find("Capacitor Transfer Bank").gameObject;
            var quenchLoop = m_root.Find(
                "Spine Induction Gallery Region/Convergence Chamber Region/Arc Furnace Region/Quench Loop Region");
            m_quenchReturnGate = quenchLoop?.Find("Quench Pressure Shutter")?.gameObject;
            m_quenchReturnSignal = quenchLoop?.Find("Quench Cache Return Signal")?.gameObject;
            if (m_quenchReturnSignal != null)
            {
                m_quenchReturnSignal.SetActive(false);
            }
            m_departureChannel = GameObject.Find("Extraction Departure Channel")?.transform;
            m_departureReturnGate = m_departureChannel?.Find("Departure Cargo Shutter")?.gameObject;
            m_departureReturnSignal = m_departureChannel?.Find("Departure Cargo Return Signal")?.gameObject;
            m_departureSurgeSignal = m_departureChannel?.Find("Departure Capacitor Surge Signal")?.gameObject;
            if (m_departureReturnSignal != null)
            {
                m_departureReturnSignal.SetActive(false);
            }
            if (m_departureSurgeSignal != null)
            {
                m_departureSurgeSignal.SetActive(false);
            }
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(SpineTowerPosition.x, SpineTowerPosition.z), Vector2.one * TOWER_BLOCKER_HALF_SIZE, false));

            foreach (Transform machine in m_scene.StationMachines.transform)
            {
                StationMachineInstanceCount++;
                StationMachinePartCount += 2;
            }
            HasStationMachineAssets = StationMachineInstanceCount == 6 && m_palette.HasStationMachineTexture;

            var shortcut = m_scene.ShortcutGate.transform;
            foreach (var renderer in shortcut.GetComponentsInChildren<Renderer>())
            {
                ShortcutGatePartCount++;
            }
            m_shortcutGate = shortcut.Find("Signal Shortcut Gate").gameObject;
            HasShortcutGateAssets = ShortcutGatePartCount == 6 && m_palette.HasShortcutTexture;
            _addShortcutMovementBlockers();
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(TowerPosition.x, TowerPosition.z), Vector2.one * TOWER_BLOCKER_HALF_SIZE, false));
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
                m_authoredMapObstacles.Add(obstacle);
                var opensWithSpineTower = obstacle.transform == m_spineReturnGate.transform ||
                                          obstacle.transform.IsChildOf(m_spineReturnGate.transform);
                var opensWithOptionalCache = m_quenchReturnGate != null &&
                                             (obstacle.transform == m_quenchReturnGate.transform ||
                                              obstacle.transform.IsChildOf(m_quenchReturnGate.transform));
                var opensWithExtractionReadiness = m_departureReturnGate != null &&
                                                   (obstacle.transform == m_departureReturnGate.transform ||
                                                    obstacle.transform.IsChildOf(m_departureReturnGate.transform));
                m_movementBlockers.Add(new MovementBlocker(
                    obstacle.Center,
                    obstacle.ScaledHalfSize,
                    obstacle.RightAxis,
                    obstacle.ForwardAxis,
                    false,
                    false,
                    opensWithSpineTower,
                    opensWithOptionalCache,
                    opensWithExtractionReadiness,
                    obstacle.gameObject));
                _createObstacleTrim(obstacle);
            }

            AuthoredMapObstacleCount = authoredObstacles.Length;
        }

        private void _registerAuthoredPoweredTerritories()
        {
            foreach (var territory in Object.FindObjectsByType<AuthoredPoweredTerritory>(FindObjectsSortMode.None))
            {
                territory.SetPowered(false);
                m_authoredPoweredTerritories.Add(territory);
            }
        }

        private void _activateAuthoredTerritories(PoweredTerritorySource source)
        {
            foreach (var territory in m_authoredPoweredTerritories)
            {
                if (territory.Source == source)
                {
                    territory.SetPowered(true);
                }
            }
        }

        private static bool _isTerritorySourceOnline(
            PoweredTerritorySource source,
            bool towerOnline,
            bool relayTowerOnline,
            bool spineTowerOnline)
        {
            return source switch
            {
                PoweredTerritorySource.CentralTower => towerOnline,
                PoweredTerritorySource.RelayTower => relayTowerOnline,
                PoweredTerritorySource.SpineTower => spineTowerOnline,
                _ => false
            };
        }

        private void _configurePlayerCombatPresentation(IComfortSettings comfortSettings)
        {
            PlayerCombatPresentation = Player.gameObject.AddComponent<PlayerCombatPresentation>();
            PlayerCombatPresentation.Configure(
                PlayerTurret,
                PlayerNose,
                Resources.Load<Material>("Materials/SignalBoltTrail"),
                comfortSettings);
        }

        private void _buildActors(IComfortSettings comfortSettings)
        {
            _bindAuthoredActors();
            _registerInterceptorEntrances();
            var entryTelegraphRoot = new GameObject("Reinforcement Entry Telegraph");
            entryTelegraphRoot.transform.SetParent(m_root);
            ReinforcementEntryTelegraph = entryTelegraphRoot.AddComponent<ReinforcementEntryTelegraph>();
            ReinforcementEntryTelegraph.Configure(m_palette.Amber, m_palette.Red);
            Interceptor.position = m_interceptorEntrances[0];
            Suppressor.position = m_interceptorEntrances[0];

            var wardenTelegraphRoot = new GameObject("Warden Strike Warning");
            wardenTelegraphRoot.transform.SetParent(m_root);
            WardenTelegraph = wardenTelegraphRoot.AddComponent<WardenThreatTelegraph>();
            WardenTelegraph.Configure(Warden, Player, comfortSettings);

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
            var spineNorthCache = new Vector3(39f, 0f, 3f);
            var spineSouthCache = new Vector3(39f, 0f, -3f);
            _createSalvage(northCache, SignalRegion.Central);
            _createSalvage(southCache, SignalRegion.Central);
            var authoredSockets = Object.FindObjectsByType<AuthoredSalvageSocket>(FindObjectsSortMode.None);
            System.Array.Sort(authoredSockets, (first, second) =>
            {
                var regionOrder = first.Region.CompareTo(second.Region);
                return regionOrder != 0 ? regionOrder : second.Position.z.CompareTo(first.Position.z);
            });
            foreach (var socket in authoredSockets)
            {
                if (!socket.IsOptional)
                {
                    _createSalvage(socket.Position, socket.Region);
                }
            }

            _createSalvage(spineNorthCache, SignalRegion.Spine);
            _createSalvage(spineSouthCache, SignalRegion.Spine);
            foreach (var socket in authoredSockets)
            {
                if (socket.IsOptional)
                {
                    _createSalvage(socket.Position, socket.Region, true);
                }
            }

            AuthoredSalvageSocketCount = authoredSockets.Length;
        }

        private void _bindAuthoredActors()
        {
            Player = m_scene.Player;
            Player.SetParent(m_root, true);
            Player.position = ExtractionPosition;
            PlayerNose = Player.Find("Drone Tool");
            _createPlayerPresentationPivot();
            PlayerDronePartCount = 4;
            HasPlayerDroneAssets = m_palette.HasPlayerDroneTexture;

            Warden = m_scene.Warden;
            Warden.SetParent(m_root, true);

            Sapper = m_scene.Sapper;
            Sapper.SetParent(m_root, true);
            SapperCore = Sapper.Find("Sapper Drain Core");
            SapperCoreBaseScale = SapperCore.localScale;
            HasSignalSapperAssets = m_palette.HasSapperTexture;
            SignalSapperPartCount = 4;

            Interceptor = m_scene.Interceptor;
            Interceptor.SetParent(m_root, true);
            InterceptorCore = Interceptor.Find("Interceptor Core");
            HasSecurityInterceptorAssets = true;
            SecurityInterceptorPartCount = 4;

            Suppressor = m_scene.Suppressor;
            Suppressor.SetParent(m_root, true);
            SuppressorCore = Suppressor.Find("Suppressor Core");
            HasSecuritySuppressorAssets = true;
            SecuritySuppressorPartCount = 4;

            Warden.gameObject.SetActive(false);
            Sapper.gameObject.SetActive(false);
            Interceptor.gameObject.SetActive(false);
            Suppressor.gameObject.SetActive(false);

            var interceptorTelegraphRoot = new GameObject("Interceptor Charge Telegraph");
            interceptorTelegraphRoot.transform.SetParent(m_root);
            m_interceptorTelegraph = interceptorTelegraphRoot.AddComponent<LineRenderer>();
            m_interceptorTelegraph.positionCount = 2;
            m_interceptorTelegraph.startWidth = 0.16f;
            m_interceptorTelegraph.endWidth = 0.05f;
            m_interceptorTelegraph.sharedMaterial = m_palette.Red;
            m_interceptorTelegraph.textureMode = LineTextureMode.Stretch;
            m_interceptorTelegraph.shadowCastingMode = ShadowCastingMode.Off;
            m_interceptorTelegraph.receiveShadows = false;
            interceptorTelegraphRoot.SetActive(false);

            var suppressorFieldRoot = new GameObject("Suppressor Field Warning");
            suppressorFieldRoot.transform.SetParent(m_root, false);
            SuppressorFieldTelegraph = suppressorFieldRoot.AddComponent<SuppressorFieldTelegraph>();
            SuppressorFieldTelegraph.Configure(m_palette.Amber);
        }

        private void _registerInterceptorEntrances()
        {
            var authoredEntrances = Object.FindObjectsByType<AuthoredInterceptorEntrance>(FindObjectsSortMode.None);
            AuthoredInterceptorEntranceCount = authoredEntrances.Length;
            System.Array.Sort(authoredEntrances, (first, second) => first.Priority.CompareTo(second.Priority));
            foreach (var entrance in authoredEntrances)
            {
                m_interceptorEntrances.Add(entrance.Position);
                if (entrance.Position.z > 12.5f)
                {
                    m_deepRouteEntranceIndex = m_interceptorEntrances.Count - 1;
                }
            }

            if (m_interceptorEntrances.Count < 2)
            {
                m_interceptorEntrances.Clear();
                m_interceptorEntrances.Add(s_interceptorNorthSpawn);
                m_interceptorEntrances.Add(s_interceptorSouthSpawn);
            }
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

        private void _createSalvage(Vector3 position, SignalRegion region, bool isOptional = false)
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
            m_salvageRegions.Add(root, region);
            if (isOptional)
            {
                m_optionalSalvagePickups.Add(root);
            }
            SalvageCacheInstanceCount++;
            SalvageCachePartCount += 4;
            HasSalvageCacheAssets = hasValidPrefab && m_palette.HasSalvageCacheTexture &&
                                    SalvageCachePartCount == SalvageCacheInstanceCount * 4;
        }

        private bool _isBlocked(Vector3 position, float radius, bool shortcutOpen)
        {
            foreach (var blocker in m_movementBlockers)
            {
                if ((blocker.IsShortcutGate && shortcutOpen) || (blocker.IsRelayShortcutGate && m_relayShortcutOpen) ||
                    (blocker.IsSpineReturnGate && m_spineReturnOpen) ||
                    (blocker.IsQuenchReturnGate && m_quenchReturnOpen) ||
                    (blocker.IsDepartureReturnGate && m_departureReturnOpen))
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
                if ((blocker.IsShortcutGate && shortcutOpen) || (blocker.IsRelayShortcutGate && m_relayShortcutOpen) ||
                    (blocker.IsSpineReturnGate && m_spineReturnOpen) ||
                    (blocker.IsQuenchReturnGate && m_quenchReturnOpen) ||
                    (blocker.IsDepartureReturnGate && m_departureReturnOpen) ||
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
            switch (model.CurrentMissionStage)
            {
                case MissionStage.CentralTower:
                    return TowerPosition;
                case MissionStage.RelayTower:
                    return RelayTowerPosition;
                case MissionStage.SpineTower:
                    return SpineTowerPosition;
                case MissionStage.Extraction:
                    return ExtractionPosition;
                case MissionStage.CentralPayload:
                    return _nearestPayloadTarget(SignalRegion.Central);
                case MissionStage.RelayPayload:
                    return _nearestPayloadTarget(SignalRegion.Relay);
                case MissionStage.SpinePayload:
                    return _nearestPayloadTarget(SignalRegion.Spine);
                default:
                    return TowerPosition;
            }
        }

        private Vector3 _nearestPayloadTarget(SignalRegion region)
        {
            var nearest = region switch
            {
                SignalRegion.Relay => RelayTowerPosition,
                SignalRegion.Spine => SpineTowerPosition,
                _ => TowerPosition
            };
            var distance = float.PositiveInfinity;
            foreach (var cache in m_salvagePickups)
            {
                if (!cache.activeSelf || IsOptionalCache(cache) || GetPayloadRegion(cache) != region ||
                    FlatDistance(Player.position, cache.transform.position) >= distance)
                {
                    continue;
                }

                nearest = cache.transform.position;
                distance = FlatDistance(Player.position, nearest);
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

        private const string SALVAGE_CACHE_PREFAB_RESOURCE = "Environment/SalvageCacheAssembly";
        private const string SIGNAL_BOLT_PREFAB_RESOURCE = "Projectiles/SignalBoltAssembly";
        private const string PLAYER_CAMERA_TUNING_RESOURCE = "Tuning/PlayerCameraTuning";
        private const float TOWER_BLOCKER_HALF_SIZE = 0.62f;
        private const float NAVIGATION_CLEARANCE = 0.08f;
        private const float NAVIGATION_BLOCKED_ROUTE_PENALTY = 20f;

        private static readonly Vector3 s_securityWardenSpawn = new(6.8f, 0f, 4.7f);
        private static readonly Vector3 s_signalSapperSpawn = new(-10.8f, 0f, 5.7f);
        private static readonly Vector3 s_interceptorNorthSpawn = new(-16.4f, 0f, 7.1f);
        private static readonly Vector3 s_interceptorSouthSpawn = new(1.5f, 0f, -7.5f);

        private readonly Transform m_root;
        private readonly DeadSignalSceneReferences m_scene;
        private readonly DeadSignalPalette m_palette;
        private readonly GameObject m_signalBoltPrefab;
        private readonly SignalBoltPresentationTuning m_signalBoltTuning;
        private readonly List<AuthoredMapObstacle> m_authoredMapObstacles = new();
        private readonly List<MovementBlocker> m_movementBlockers = new();
        private readonly List<GameObject> m_salvagePickups = new();
        private readonly Dictionary<GameObject, SignalRegion> m_salvageRegions = new();
        private readonly HashSet<GameObject> m_optionalSalvagePickups = new();
        private readonly List<AuthoredPoweredTerritory> m_authoredPoweredTerritories = new();
        private readonly List<GameObject> m_towerTerritoryMarkers = new();
        private readonly List<GameObject> m_relayTerritoryMarkers = new();
        private readonly List<GameObject> m_spineTerritoryMarkers = new();
        private readonly List<Transform> m_environmentAnimators = new();
        private readonly List<Light> m_landmarkLights = new();
        private readonly List<Vector3> m_machineSockets = new();
        private readonly List<Vector3> m_interceptorEntrances = new();
        private int m_deepRouteEntranceIndex = -1;
        private Vignette m_deadZoneVignette;
        private float m_environmentTime;
        private float m_boundaryPulse;
        private float m_collisionPulse;
        private LineRenderer m_routeGuide;
        private LineRenderer m_aimGuide;
        private LineRenderer m_emergencyGuide;

        private GameObject m_towerTerritory;
        private GameObject m_towerSignalLines;
        private GameObject m_relayTerritory;
        private GameObject m_spineTerritory;
        private GameObject m_relaySignalLines;
        private GameObject m_spineSignalLines;
        private GameObject m_spineReturnGate;
        private bool m_spineReturnOpen;
        private GameObject m_quenchReturnGate;
        private GameObject m_quenchReturnSignal;
        private bool m_quenchReturnOpen;
        private GameObject m_departureReturnGate;
        private GameObject m_departureReturnSignal;
        private Transform m_departureChannel;
        private GameObject m_departureSurgeSignal;
        private bool m_departureReturnOpen;
        private bool m_departureSurgeConsumed;
        private float m_departurePreviousLocalX;
        private GameObject m_relayShortcutGate;
        private bool m_relayShortcutOpen;
        private GameObject m_extractionBeacon;
        private GameObject m_shortcutGate;
        private bool m_shortcutOpen;
        private DeadSignalNavMeshPlanner m_navMeshPlanner;
        private Transform m_cameraRig;
        private LineRenderer m_interceptorTelegraph;

        private sealed class MovementBlocker
        {
            public const int DETOUR_WAYPOINT_COUNT = 4;

            public MovementBlocker(
                Vector2 center,
                Vector2 halfSize,
                bool isShortcutGate,
                bool isRelayShortcutGate = false,
                bool isSpineReturnGate = false,
                bool isQuenchReturnGate = false,
                bool isDepartureReturnGate = false,
                GameObject source = null)
                : this(center, halfSize, Vector2.right, Vector2.up, isShortcutGate, isRelayShortcutGate,
                    isSpineReturnGate, isQuenchReturnGate, isDepartureReturnGate, source)
            {
            }

            public MovementBlocker(
                Vector2 center,
                Vector2 halfSize,
                Vector2 rightAxis,
                Vector2 forwardAxis,
                bool isShortcutGate,
                bool isRelayShortcutGate = false,
                bool isSpineReturnGate = false,
                bool isQuenchReturnGate = false,
                bool isDepartureReturnGate = false,
                GameObject source = null)
            {
                Center = center;
                HalfSize = halfSize;
                RightAxis = rightAxis;
                ForwardAxis = forwardAxis;
                IsShortcutGate = isShortcutGate;
                IsRelayShortcutGate = isRelayShortcutGate;
                IsSpineReturnGate = isSpineReturnGate;
                IsQuenchReturnGate = isQuenchReturnGate;
                IsDepartureReturnGate = isDepartureReturnGate;
                Source = source;
            }

            public Vector2 Center { get; }
            public Vector2 HalfSize { get; }
            public Vector2 RightAxis { get; }
            public Vector2 ForwardAxis { get; }
            public bool IsShortcutGate { get; }
            public bool IsRelayShortcutGate { get; }
            public bool IsSpineReturnGate { get; }
            public bool IsQuenchReturnGate { get; }
            public bool IsDepartureReturnGate { get; }
            public GameObject Source { get; }
            public bool IsActive => Source == null || Source.activeInHierarchy;

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
