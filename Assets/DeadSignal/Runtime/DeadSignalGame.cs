using System.Collections.Generic;
using Reflex.Attributes;
using Reflex.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DeadSignal
{
    public sealed class DeadSignalGame : MonoBehaviour
    {
        private const float ArenaHalfWidth = 13.2f;
        private const float ArenaHalfHeight = 8.8f;
        private const float StartingPowerRadius = 3.6f;
        private const float TowerPowerRadius = 7.2f;
        private const float PlayerSpeed = 6.4f;
        private const float GamepadStickDeadzone = 0.18f;
        private const float PlayerCollisionRadius = 0.48f;
        private const float EnemyCollisionRadius = 0.54f;
        private const float SapperCollisionRadius = 0.42f;
        private const float SapperLatchDistance = 1.25f;
        private const float SapperFirstPulseDelay = 1.6f;
        private const float SapperPulseInterval = 1.35f;

        private readonly Vector3 extractionPosition = new(-9.2f, 0f, -5.6f);
        private readonly Vector3 towerPosition = new(-0.6f, 0f, 0.4f);
        private readonly Vector3 shortcutPosition = new(4f, 0f, 0.4f);
        private readonly List<Projectile> projectiles = new();
        private readonly List<SalvagePickup> pickups = new();
        private readonly List<MovementBlocker> movementBlockers = new();

        private RunModel model;
        private RunMetrics metrics;
        private Transform player;
        private Transform playerNose;
        private Transform enemy;
        private Transform sapper;
        private Transform sapperCore;
        private SignalSapperTelegraph sapperTelegraph;
        private Transform towerCore;
        private GameObject towerTerritory;
        private GameObject towerSignalLines;
        private GameObject extractionBeacon;
        private GameObject shortcutGate;
        private Camera gameCamera;
        private Material cyan;
        private Material cyanDim;
        private Material amber;
        private Material red;
        private Material redDim;
        private Material magenta;
        private Material dark;
        private Material steel;
        private Material white;
        private float enemyHealth = 3f;
        private float sapperHealth = 2f;
        private float enemyAttackCooldown;
        private float sapperPulseCooldown;
        private float shotCooldown;
        private float feedbackTimer;
        private string feedback = string.Empty;
        private bool lastPoweredState;
        private bool sapperLatched;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle centerStyle;
        private GUIStyle giantStyle;
        private GUIStyle reportStyle;
        private Texture2D m_pauseInsignia;
        private ICombatFeedback m_combatFeedback;
        private Container m_container;
        private bool m_fireBuffered;

        public float CurrentSignal => model?.Signal ?? 0f;
        public bool IsSapperLatched => sapperLatched;
        public bool IsPaused => m_combatFeedback?.IsPaused ?? false;
        public bool HasPauseInsignia => m_pauseInsignia != null;

        private sealed class Projectile
        {
            public GameObject Visual;
            public Vector3 Direction;
            public float Life;
        }

        private sealed class SalvagePickup
        {
            public GameObject Visual;
            public bool Collected;
        }

        private sealed class MovementBlocker
        {
            public Vector2 Center;
            public Vector2 HalfSize;
            public bool IsShortcutGate;
        }

        [Inject]
        private void _construct(ICombatFeedback combatFeedback, Container container)
        {
            m_combatFeedback = combatFeedback;
            m_container = container;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            model = new RunModel();
            metrics = new RunMetrics();
            m_pauseInsignia = Resources.Load<Texture2D>("UI/MaintenanceNetworkInsignia");
            Application.targetFrameRate = 120;
            BuildMaterials();
            BuildPresentation();
            m_combatFeedback.Configure(gameCamera);
            BuildArena();
            BuildActors();
            lastPoweredState = IsPowered(player.position);
        }

        private void Update()
        {
            if (model.Outcome == RunOutcome.Running && _pressedPause())
            {
                _setPaused(!IsPaused);
            }

            if (m_combatFeedback.IsFrozen)
            {
                if (!IsPaused && PressedFire())
                {
                    m_fireBuffered = true;
                }

                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            feedbackTimer = Mathf.Max(0f, feedbackTimer - dt);
            shotCooldown = Mathf.Max(0f, shotCooldown - dt);

            if (model.Outcome != RunOutcome.Running)
            {
                if (PressedRestart())
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                return;
            }

            Vector2 moveInput = ReadMovement();
            Vector3 movement = new(moveInput.x, 0f, moveInput.y);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            Vector3 next = player.position + movement * (PlayerSpeed * dt);
            next.x = Mathf.Clamp(next.x, -ArenaHalfWidth + 0.6f, ArenaHalfWidth - 0.6f);
            next.z = Mathf.Clamp(next.z, -ArenaHalfHeight + 0.6f, ArenaHalfHeight - 0.6f);
            player.position = ResolveMovement(player.position, next, PlayerCollisionRadius);

            Vector3 aimDirection = ReadAimDirection();
            if (aimDirection.sqrMagnitude > 0.01f)
            {
                player.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            }

            bool powered = IsPowered(player.position);
            model.Advance(dt, movement.sqrMagnitude > 0.01f, powered);
            metrics.Advance(dt, powered);
            if (powered != lastPoweredState)
            {
                ShowFeedback(powered ? "NETWORK LINK RESTORED" : "DEAD ZONE — SIGNAL BLEED");
                lastPoweredState = powered;
            }

            if ((m_fireBuffered || PressedFire()) && shotCooldown <= 0f)
            {
                m_fireBuffered = false;
                FireProjectile(aimDirection);
            }

            if (PressedInteract())
            {
                HandleInteraction();
            }

            UpdateTower(dt);
            UpdateEnemy(dt);
            UpdateSapper(dt);
            UpdateProjectiles(dt);
            UpdatePickups(dt);
            UpdateExtraction(dt);
        }

        private void BuildMaterials()
        {
            cyan = MakeMaterial("Signal Cyan", new Color(0.02f, 0.92f, 1f), new Color(0f, 1.8f, 2.2f));
            cyanDim = MakeMaterial("Powered Deck", new Color(0.015f, 0.18f, 0.2f), new Color(0f, 0.11f, 0.13f));
            amber = MakeMaterial("Salvage Amber", new Color(1f, 0.48f, 0.06f), new Color(2.4f, 0.65f, 0.02f));
            red = MakeMaterial("Security Red", new Color(1f, 0.035f, 0.045f), new Color(2.2f, 0.01f, 0.01f));
            redDim = MakeMaterial("Dead Zone Red", new Color(0.2f, 0.018f, 0.025f), new Color(0.14f, 0.005f, 0.005f));
            magenta = MakeMaterial("Sapper Magenta", new Color(0.92f, 0.025f, 0.62f), new Color(2.2f, 0.01f, 1.15f));
            dark = MakeMaterial("Station Black", new Color(0.012f, 0.018f, 0.026f), Color.black);
            steel = MakeMaterial("Station Steel", new Color(0.085f, 0.11f, 0.14f), new Color(0.01f, 0.018f, 0.02f));
            white = MakeMaterial("Drone White", new Color(0.62f, 0.72f, 0.75f), new Color(0.03f, 0.06f, 0.07f));
        }

        private void BuildPresentation()
        {
            foreach (Camera existing in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                existing.enabled = false;
            }

            foreach (Light existing in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                existing.enabled = false;
            }

            GameObject cameraObject = new("Dead Signal Camera");
            cameraObject.transform.SetParent(transform);
            cameraObject.transform.position = new Vector3(0f, 20f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 10.4f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.002f, 0.004f, 0.008f);
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 40f;

            GameObject lightObject = new("Cold Overhead Light");
            lightObject.transform.SetParent(transform);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            Light key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.38f, 0.52f, 0.65f);
            key.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(0.025f, 0.035f, 0.05f);
        }

        private void BuildArena()
        {
            CreatePrimitive("Station Deck", PrimitiveType.Cube, new Vector3(0f, -0.45f, 0f), new Vector3(27.5f, 0.6f, 18.5f), dark);

            for (int x = -12; x <= 12; x += 2)
            {
                CreatePrimitive("Deck Seam", PrimitiveType.Cube, new Vector3(x, -0.12f, 0f), new Vector3(0.025f, 0.015f, 17.6f), steel);
            }

            for (int z = -8; z <= 8; z += 2)
            {
                CreatePrimitive("Deck Seam", PrimitiveType.Cube, new Vector3(0f, -0.115f, z), new Vector3(26.4f, 0.015f, 0.025f), steel);
            }

            CreatePrimitive("North Bulkhead", PrimitiveType.Cube, new Vector3(0f, 0.25f, 9.1f), new Vector3(27.8f, 0.8f, 0.5f), steel);
            CreatePrimitive("South Bulkhead", PrimitiveType.Cube, new Vector3(0f, 0.25f, -9.1f), new Vector3(27.8f, 0.8f, 0.5f), steel);
            CreatePrimitive("East Bulkhead", PrimitiveType.Cube, new Vector3(13.7f, 0.25f, 0f), new Vector3(0.5f, 0.8f, 18.7f), steel);
            CreatePrimitive("West Bulkhead", PrimitiveType.Cube, new Vector3(-13.7f, 0.25f, 0f), new Vector3(0.5f, 0.8f, 18.7f), steel);

            CreateTerritory("Dock Power Territory", extractionPosition, StartingPowerRadius, cyanDim);
            towerTerritory = CreateTerritory("Tower Power Territory", towerPosition, TowerPowerRadius, dark);

            for (int x = -12; x <= 12; x += 4)
            {
                CreatePrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, 8.55f), new Vector3(1.5f, 0.035f, 0.07f), redDim);
                CreatePrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, -8.55f), new Vector3(1.5f, 0.035f, 0.07f), redDim);
            }

            BuildExtraction();
            BuildTower();
            BuildStationMachines();
            BuildSignalShortcut();
        }

        private void BuildExtraction()
        {
            CreatePrimitive("Extraction Plinth", PrimitiveType.Cylinder, extractionPosition + new Vector3(0f, 0.02f, 0f), new Vector3(3.2f, 0.08f, 3.2f), cyanDim);
            CreatePrimitive("Extraction Ring", PrimitiveType.Cylinder, extractionPosition + new Vector3(0f, 0.08f, 0f), new Vector3(2.55f, 0.08f, 2.55f), cyan);
            CreatePrimitive("Extraction Center", PrimitiveType.Cylinder, extractionPosition + new Vector3(0f, 0.14f, 0f), new Vector3(2.1f, 0.08f, 2.1f), dark);
            extractionBeacon = CreatePrimitive("Extraction Beacon", PrimitiveType.Cube, extractionPosition + new Vector3(0f, 0.7f, 1.5f), new Vector3(0.22f, 1.4f, 0.22f), cyan);
        }

        private void BuildTower()
        {
            CreatePrimitive("Tower Base", PrimitiveType.Cylinder, towerPosition + new Vector3(0f, 0.15f, 0f), new Vector3(2.2f, 0.25f, 2.2f), steel);
            CreatePrimitive("Tower Column", PrimitiveType.Cylinder, towerPosition + new Vector3(0f, 0.85f, 0f), new Vector3(0.8f, 1.35f, 0.8f), steel);
            towerCore = CreatePrimitive("Tower Core", PrimitiveType.Cylinder, towerPosition + new Vector3(0f, 1.65f, 0f), new Vector3(1.35f, 0.22f, 1.35f), redDim).transform;
            towerSignalLines = new GameObject("Tower Signal Lines");
            towerSignalLines.transform.SetParent(transform);
            CreatePrimitive("Signal Trunk West", PrimitiveType.Cube, new Vector3(-4.7f, -0.03f, 0.4f), new Vector3(8.2f, 0.04f, 0.09f), cyan, towerSignalLines.transform);
            CreatePrimitive("Signal Trunk East", PrimitiveType.Cube, new Vector3(4.1f, -0.03f, 0.4f), new Vector3(9.4f, 0.04f, 0.09f), cyan, towerSignalLines.transform);
            CreatePrimitive("Signal Branch", PrimitiveType.Cube, new Vector3(-0.6f, -0.025f, -3.5f), new Vector3(0.09f, 0.04f, 7.8f), cyan, towerSignalLines.transform);
            towerSignalLines.SetActive(false);
        }

        private void BuildStationMachines()
        {
            Vector3[] locations =
            {
                new(-11.6f, 0f, 6.8f), new(-8.8f, 0f, 6.9f), new(10.8f, 0f, 6.8f),
                new(11.2f, 0f, -6.7f), new(4.8f, 0f, -7.1f), new(-3.8f, 0f, 7.1f)
            };

            for (int i = 0; i < locations.Length; i++)
            {
                Vector3 p = locations[i];
                CreatePrimitive("Machine Block", PrimitiveType.Cube, p + new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 0.9f, 1.1f), steel);
                CreatePrimitive("Machine Status", PrimitiveType.Cube, p + new Vector3(0f, 0.92f, -0.15f), new Vector3(0.75f, 0.06f, 0.18f), i % 2 == 0 ? redDim : cyanDim);
            }
        }

        private void BuildSignalShortcut()
        {
            // The end passages stay open, so spending Signal for the central route is optional.
            CreateBarrierSegment("Shortcut Bulkhead South", new Vector3(4f, 0.46f, -3.15f), new Vector3(0.55f, 1.1f, 4.7f));
            CreateBarrierSegment("Shortcut Bulkhead North", new Vector3(4f, 0.46f, 3.55f), new Vector3(0.55f, 1.1f, 3.9f));

            CreatePrimitive("Shortcut Gate South Post", PrimitiveType.Cube, shortcutPosition + new Vector3(-0.16f, 0.68f, -1.34f), new Vector3(0.85f, 1.45f, 0.25f), steel);
            CreatePrimitive("Shortcut Gate North Post", PrimitiveType.Cube, shortcutPosition + new Vector3(-0.16f, 0.68f, 1.34f), new Vector3(0.85f, 1.45f, 0.25f), steel);
            CreatePrimitive("Shortcut Gate Signal", PrimitiveType.Cube, shortcutPosition + new Vector3(-0.31f, 1.38f, 0f), new Vector3(0.12f, 0.08f, 2.3f), cyanDim);
            shortcutGate = CreatePrimitive("Signal Shortcut Gate", PrimitiveType.Cube, shortcutPosition + new Vector3(0f, 0.55f, 0f), new Vector3(0.42f, 1.05f, 2.4f), redDim);
            movementBlockers.Add(new MovementBlocker
            {
                Center = new Vector2(shortcutPosition.x, shortcutPosition.z),
                HalfSize = new Vector2(0.21f, 1.2f),
                IsShortcutGate = true
            });
        }

        private void CreateBarrierSegment(string objectName, Vector3 position, Vector3 scale)
        {
            CreatePrimitive(objectName, PrimitiveType.Cube, position, scale, steel);
            movementBlockers.Add(new MovementBlocker
            {
                Center = new Vector2(position.x, position.z),
                HalfSize = new Vector2(scale.x * 0.5f, scale.z * 0.5f)
            });
        }

        private void BuildActors()
        {
            GameObject playerRoot = new("Maintenance Drone");
            playerRoot.transform.SetParent(transform);
            playerRoot.transform.position = extractionPosition;
            player = playerRoot.transform;
            CreatePrimitive("Drone Chassis", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(1.05f, 0.22f, 1.05f), white, player);
            CreatePrimitive("Drone Signal Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.08f, 0.72f), cyan, player);
            CreatePrimitive("Drone Core", PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.36f, 0.09f, 0.36f), dark, player);
            playerNose = CreatePrimitive("Drone Tool", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.68f), new Vector3(0.24f, 0.2f, 0.7f), cyan, player).transform;

            GameObject enemyRoot = new("Security Warden");
            enemyRoot.transform.SetParent(transform);
            enemyRoot.transform.position = new Vector3(6.8f, 0f, 4.7f);
            enemy = enemyRoot.transform;
            CreatePrimitive("Warden Chassis", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f), new Vector3(1.15f, 0.55f, 1.15f), steel, enemy);
            CreatePrimitive("Warden Eye", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.59f), new Vector3(0.68f, 0.16f, 0.06f), red, enemy);
            CreatePrimitive("Warden Crown", PrimitiveType.Cylinder, new Vector3(0f, 0.76f, 0f), new Vector3(0.68f, 0.12f, 0.68f), redDim, enemy);
            enemy.gameObject.SetActive(false);

            GameObject sapperRoot = new("Signal Sapper");
            sapperRoot.transform.SetParent(transform);
            sapperRoot.transform.position = new Vector3(-10.8f, 0f, 5.7f);
            sapper = sapperRoot.transform;
            CreatePrimitive("Sapper Chassis", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.72f, 0.34f, 1.25f), steel, sapper);
            CreatePrimitive("Sapper Fork Left", PrimitiveType.Cube, new Vector3(-0.43f, 0.28f, 0.28f), new Vector3(0.18f, 0.18f, 0.92f), magenta, sapper);
            CreatePrimitive("Sapper Fork Right", PrimitiveType.Cube, new Vector3(0.43f, 0.28f, 0.28f), new Vector3(0.18f, 0.18f, 0.92f), magenta, sapper);
            sapperCore = CreatePrimitive("Sapper Drain Core", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, -0.12f), new Vector3(0.42f, 0.1f, 0.42f), magenta, sapper).transform;
            sapper.gameObject.SetActive(false);

            GameObject telegraphRoot = new("Sapper Drain Telegraph");
            telegraphRoot.transform.SetParent(transform);
            sapperTelegraph = telegraphRoot.AddComponent<SignalSapperTelegraph>();
            sapperTelegraph.Configure(sapper, towerPosition, magenta, magenta);

            CreateSalvage(new Vector3(9.7f, 0f, 6.3f));
            CreateSalvage(new Vector3(10.4f, 0f, -6.4f));
            CreateSalvage(new Vector3(-5.8f, 0f, 7.2f));
        }

        private void CreateSalvage(Vector3 position)
        {
            GameObject root = new("Salvage Cache");
            root.transform.SetParent(transform);
            root.transform.position = position;
            CreatePrimitive("Salvage Case", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(0.75f, 0.48f, 0.75f), amber, root.transform);
            CreatePrimitive("Salvage Band", PrimitiveType.Cube, new Vector3(0f, 0.61f, 0f), new Vector3(0.9f, 0.06f, 0.28f), white, root.transform);
            pickups.Add(new SalvagePickup { Visual = root });
        }

        private void UpdateTower(float dt)
        {
            towerCore.Rotate(Vector3.up, (model.TowerOnline ? 110f : 22f) * dt, Space.World);
            float pulse = 1f + Mathf.Sin(Time.time * (model.TowerOnline ? 5f : 2f)) * 0.08f;
            towerCore.localScale = new Vector3(1.35f * pulse, 0.22f, 1.35f * pulse);
        }

        private void UpdateEnemy(float dt)
        {
            if (!model.TowerOnline || enemyHealth <= 0f)
            {
                return;
            }

            enemyAttackCooldown = Mathf.Max(0f, enemyAttackCooldown - dt);
            Vector3 delta = player.position - enemy.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance > 0.05f)
            {
                enemy.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }

            if (distance > 1.05f)
            {
                Vector3 desired = enemy.position + delta.normalized * (2.15f * dt);
                enemy.position = ResolveMovement(enemy.position, desired, EnemyCollisionRadius);
            }
            else if (enemyAttackCooldown <= 0f)
            {
                enemyAttackCooldown = 0.9f;
                model.TakeSecurityHit();
                metrics.RecordSecurityHit();
                m_combatFeedback.PlaySecurityImpact(player.position + Vector3.up * 0.58f);
                ShowFeedback("SECURITY IMPACT  −18 SIGNAL");
            }
        }

        private void UpdateSapper(float dt)
        {
            if (!model.TowerOnline || sapperHealth <= 0f)
            {
                return;
            }

            sapperTelegraph.SetThreatState(true, sapperLatched, sapperPulseCooldown, SapperPulseInterval);
            sapperCore.Rotate(Vector3.up, (sapperLatched ? 260f : 120f) * dt, Space.Self);
            if (!sapperLatched)
            {
                Vector3 delta = towerPosition - sapper.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance > 0.05f)
                {
                    sapper.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                }

                if (distance > SapperLatchDistance)
                {
                    Vector3 desired = sapper.position + delta.normalized * (1.8f * dt);
                    sapper.position = ResolveMovement(sapper.position, desired, SapperCollisionRadius);
                    return;
                }

                sapperLatched = true;
                sapperPulseCooldown = SapperFirstPulseDelay;
                sapperTelegraph.SetThreatState(true, true, sapperPulseCooldown, SapperPulseInterval);
                ShowFeedback("SAPPER LATCHED - PURGE IT");
            }

            sapperPulseCooldown = Mathf.Max(0f, sapperPulseCooldown - dt);
            sapperTelegraph.SetThreatState(true, true, sapperPulseCooldown, SapperPulseInterval);
            float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.18f;
            sapperCore.localScale = new Vector3(0.42f * pulse, 0.1f, 0.42f * pulse);
            if (sapperPulseCooldown > 0f)
            {
                return;
            }

            sapperPulseCooldown = SapperPulseInterval;
            model.TakeSapperPulse();
            metrics.RecordSapperPulse();
            m_combatFeedback.PlaySapperImpact(towerPosition + Vector3.up * 0.65f);
            sapperTelegraph.SetThreatState(true, true, sapperPulseCooldown, SapperPulseInterval);
            sapperTelegraph.NotifyPulse();
            ShowFeedback($"SAPPER DRAIN  -{RunModel.SapperPulseCost:0} SIGNAL");
        }

        private void FireProjectile(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = player.forward;
            }

            if (!model.TrySpend(RunModel.ShotCost))
            {
                ShowFeedback("INSUFFICIENT SIGNAL");
                return;
            }

            shotCooldown = 0.16f;
            metrics.RecordShot();
            GameObject shot = CreatePrimitive("Signal Bolt", PrimitiveType.Cube, player.position + direction * 0.9f + Vector3.up * 0.25f, new Vector3(0.16f, 0.16f, 0.55f), cyan);
            shot.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            projectiles.Add(new Projectile { Visual = shot, Direction = direction.normalized, Life = 1.5f });
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile shot = projectiles[i];
                shot.Life -= dt;
                shot.Visual.transform.position += shot.Direction * (13.5f * dt);

                bool hitEnemy = enemy.gameObject.activeSelf && enemyHealth > 0f &&
                                Vector3.SqrMagnitude(shot.Visual.transform.position - (enemy.position + Vector3.up * 0.3f)) < 0.9f;
                bool hitSapper = sapper.gameObject.activeSelf && sapperHealth > 0f &&
                                 Vector3.SqrMagnitude(shot.Visual.transform.position - (sapper.position + Vector3.up * 0.3f)) < 0.75f;
                if (hitEnemy)
                {
                    enemyHealth -= 1f;
                    m_combatFeedback.PlaySignalImpact(enemy.position + Vector3.up * 0.65f, enemyHealth <= 0f);
                    if (enemyHealth <= 0f)
                    {
                        enemy.gameObject.SetActive(false);
                        ShowFeedback("SECURITY NODE PURGED");
                    }
                    else
                    {
                        ShowFeedback("SECURITY ARMOR HIT");
                    }
                }

                if (hitSapper)
                {
                    sapperHealth -= 1f;
                    m_combatFeedback.PlaySignalImpact(sapper.position + Vector3.up * 0.58f, sapperHealth <= 0f);
                    if (sapperHealth <= 0f)
                    {
                        sapper.gameObject.SetActive(false);
                        sapperTelegraph.SetThreatState(false, false, 0f, SapperPulseInterval);
                        ShowFeedback("SIGNAL SAPPER PURGED");
                    }
                    else
                    {
                        ShowFeedback("SAPPER SHELL HIT");
                    }
                }

                if (hitEnemy || hitSapper || shot.Life <= 0f)
                {
                    Destroy(shot.Visual);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void UpdatePickups(float dt)
        {
            foreach (SalvagePickup pickup in pickups)
            {
                if (pickup.Collected)
                {
                    continue;
                }

                pickup.Visual.transform.Rotate(Vector3.up, 70f * dt, Space.World);
                float hover = 0.06f + Mathf.Sin(Time.time * 3f + pickup.Visual.transform.position.x) * 0.04f;
                Vector3 position = pickup.Visual.transform.position;
                position.y = hover;
                pickup.Visual.transform.position = position;

                if (FlatDistance(player.position, pickup.Visual.transform.position) < 0.85f)
                {
                    pickup.Collected = true;
                    pickup.Visual.SetActive(false);
                    model.CollectSalvage();
                    ShowFeedback($"SALVAGE SECURED  {model.Salvage}/{RunModel.SalvageRequired}");
                }
            }
        }

        private void UpdateExtraction(float dt)
        {
            float speed = model.CanExtract ? 150f : 30f;
            extractionBeacon.transform.Rotate(Vector3.up, speed * dt, Space.World);
        }

        private void HandleInteraction()
        {
            if (!model.TowerOnline && FlatDistance(player.position, towerPosition) < 1.8f)
            {
                if (model.TryActivateTower())
                {
                    towerTerritory.GetComponent<Renderer>().sharedMaterial = cyanDim;
                    towerCore.GetComponent<Renderer>().sharedMaterial = cyan;
                    towerSignalLines.SetActive(true);
                    enemy.gameObject.SetActive(true);
                    sapper.gameObject.SetActive(true);
                    sapperTelegraph.SetThreatState(true, false, 0f, SapperPulseInterval);
                    ShowFeedback("TOWER ONLINE - TWO THREATS AWAKENED");
                }
                else
                {
                    ShowFeedback("TOWER REQUIRES 10 SIGNAL");
                }

                return;
            }

            if (!model.ShortcutOpen && FlatDistance(player.position, shortcutPosition) < 1.9f)
            {
                if (model.TryOpenShortcut())
                {
                    shortcutGate.SetActive(false);
                    ShowFeedback($"SHORTCUT OPEN  -{RunModel.ShortcutCost:0} SIGNAL");
                }
                else if (!model.TowerOnline)
                {
                    ShowFeedback("SHORTCUT OFFLINE - ACTIVATE TOWER");
                }
                else
                {
                    ShowFeedback($"KEEP 1 SIGNAL AFTER {RunModel.ShortcutCost:0} COST");
                }

                return;
            }

            if (FlatDistance(player.position, extractionPosition) < 1.65f)
            {
                if (model.TryExtract())
                {
                    ShowFeedback("EXTRACTION COMPLETE");
                }
                else
                {
                    ShowFeedback($"EXTRACTION LOCKED — {RunModel.SalvageRequired - model.Salvage} SALVAGE MISSING");
                }
            }
        }

        private bool IsPowered(Vector3 position)
        {
            if (FlatDistance(position, extractionPosition) <= StartingPowerRadius)
            {
                return true;
            }

            return model.TowerOnline && FlatDistance(position, towerPosition) <= TowerPowerRadius;
        }

        private Vector2 ReadMovement()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 keyboardMovement = Vector2.zero;
            if (keyboard != null)
            {
                keyboardMovement.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                                     (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                keyboardMovement.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                                     (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            Vector2 gamepadMovement = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            if (gamepadMovement.sqrMagnitude < GamepadStickDeadzone * GamepadStickDeadzone)
            {
                gamepadMovement = Vector2.zero;
            }

            return Vector2.ClampMagnitude(keyboardMovement + gamepadMovement, 1f);
        }

        private Vector3 ReadAimDirection()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude >= GamepadStickDeadzone * GamepadStickDeadzone)
                {
                    return new Vector3(stick.x, 0f, stick.y).normalized;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || gameCamera == null)
            {
                return player != null ? player.forward : Vector3.forward;
            }

            Ray ray = gameCamera.ScreenPointToRay(mouse.position.ReadValue());
            Plane deck = new(Vector3.up, Vector3.zero);
            if (deck.Raycast(ray, out float distance))
            {
                Vector3 direction = ray.GetPoint(distance) - player.position;
                direction.y = 0f;
                return direction.normalized;
            }

            return player.forward;
        }

        private static bool PressedFire()
        {
            return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                   (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && (Gamepad.current.rightTrigger.wasPressedThisFrame ||
                                                Gamepad.current.rightShoulder.wasPressedThisFrame));
        }

        private static bool PressedInteract()
        {
            return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        }

        private static bool PressedRestart()
        {
            Keyboard keyboard = Keyboard.current;
            return (keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) ||
                   (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        private static bool _pressedPause()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        }

        private void _setPaused(bool paused)
        {
            m_combatFeedback.SetPaused(paused);
        }

        private void OnDestroy()
        {
            if (m_container != null)
            {
                m_container.Dispose();
                m_container = null;
            }
        }

        private void ShowFeedback(string message)
        {
            feedback = message;
            feedbackTimer = 2.2f;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private Vector3 ResolveMovement(Vector3 current, Vector3 desired, float radius)
        {
            if (!IsBlocked(desired, radius))
            {
                return desired;
            }

            Vector3 xOnly = new(desired.x, current.y, current.z);
            Vector3 zOnly = new(current.x, current.y, desired.z);
            bool canMoveX = !IsBlocked(xOnly, radius);
            bool canMoveZ = !IsBlocked(zOnly, radius);
            if (canMoveX && canMoveZ)
            {
                return Mathf.Abs(desired.x - current.x) >= Mathf.Abs(desired.z - current.z) ? xOnly : zOnly;
            }

            if (canMoveX)
            {
                return xOnly;
            }

            return canMoveZ ? zOnly : current;
        }

        private bool IsBlocked(Vector3 position, float radius)
        {
            foreach (MovementBlocker blocker in movementBlockers)
            {
                if (blocker.IsShortcutGate && model.ShortcutOpen)
                {
                    continue;
                }

                if (Mathf.Abs(position.x - blocker.Center.x) < blocker.HalfSize.x + radius &&
                    Mathf.Abs(position.z - blocker.Center.y) < blocker.HalfSize.y + radius)
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject CreateTerritory(string objectName, Vector3 position, float radius, Material material)
        {
            return CreatePrimitive(objectName, PrimitiveType.Cylinder, position + new Vector3(0f, -0.095f, 0f), new Vector3(radius * 2f, 0.025f, radius * 2f), material);
        }

        private GameObject CreatePrimitive(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = objectName;
            visual.transform.SetParent(parent == null ? transform : parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Collider primitiveCollider = visual.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Destroy(primitiveCollider);
            }

            return visual;
        }

        private static Material MakeMaterial(string materialName, Color baseColor, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = materialName, color = baseColor };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            material.enableInstancing = true;
            return material;
        }

        private void EnsureGuiStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.15f, 0.95f, 1f);
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            smallStyle.normal.textColor = new Color(0.72f, 0.82f, 0.86f);
            centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 17 };
            giantStyle = new GUIStyle(centerStyle) { fontSize = 38, fontStyle = FontStyle.Bold };
            reportStyle = new GUIStyle(centerStyle) { fontSize = 15, fontStyle = FontStyle.Normal };
        }

        private void OnGUI()
        {
            EnsureGuiStyles();
            float signalRatio = Mathf.Clamp01(model.Signal / RunModel.MaximumSignal);
            Rect panel = new(18f, 18f, 350f, 154f);
            GUI.color = new Color(0.015f, 0.025f, 0.035f, 0.94f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(34f, 28f, 300f, 30f), "DEAD SIGNAL", titleStyle);
            GUI.Label(new Rect(34f, 61f, 280f, 24f), $"SIGNAL  {Mathf.CeilToInt(model.Signal):000}", labelStyle);
            GUI.color = new Color(0.05f, 0.09f, 0.11f, 1f);
            GUI.DrawTexture(new Rect(34f, 88f, 300f, 14f), Texture2D.whiteTexture);
            GUI.color = signalRatio > 0.25f ? new Color(0.02f, 0.9f, 1f) : new Color(1f, 0.06f, 0.05f);
            GUI.DrawTexture(new Rect(34f, 88f, 300f * signalRatio, 14f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(34f, 108f, 300f, 23f), $"SALVAGE  {model.Salvage}/{RunModel.SalvageRequired}", labelStyle);
            string zone = IsPowered(player.position) ? "● POWERED TERRITORY" : "▲ DEAD ZONE — ACTIVE DRAIN";
            smallStyle.normal.textColor = IsPowered(player.position) ? new Color(0.05f, 0.95f, 1f) : new Color(1f, 0.22f, 0.18f);
            GUI.Label(new Rect(34f, 134f, 300f, 24f), zone, smallStyle);

            GUI.color = new Color(0.015f, 0.025f, 0.035f, 0.86f);
            GUI.Box(new Rect(Screen.width - 374f, 18f, 356f, 176f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width - 358f, 28f, 330f, 22f), CurrentObjective(), labelStyle);
            smallStyle.normal.textColor = model.TowerOnline && sapperHealth > 0f ? new Color(1f, 0.18f, 0.72f) : new Color(0.5f, 0.68f, 0.7f);
            GUI.Label(new Rect(Screen.width - 358f, 54f, 330f, 22f), SapperStatus(), smallStyle);
            smallStyle.normal.textColor = new Color(0.72f, 0.82f, 0.86f);
            GUI.Label(new Rect(Screen.width - 358f, 78f, 330f, 94f),
                "KEYS  WASD Move | Mouse Aim | LMB Fire | E Use\n" +
                "PAD  LS Move | RS Aim | RT/RB Fire | X Use\n" +
                "PAUSE  Esc / Menu\nRESTART  R / Enter / A", smallStyle);

            string prompt = ContextPrompt();
            if (!string.IsNullOrEmpty(prompt))
            {
                GUI.color = new Color(0.015f, 0.025f, 0.035f, 0.93f);
                GUI.Box(new Rect(Screen.width * 0.5f - 220f, Screen.height - 86f, 440f, 44f), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(Screen.width * 0.5f - 210f, Screen.height - 80f, 420f, 32f), prompt, centerStyle);
            }

            if (feedbackTimer > 0f)
            {
                GUI.color = feedback.Contains("DEAD") || feedback.Contains("SECURITY") ? new Color(1f, 0.25f, 0.2f) : new Color(0.1f, 0.95f, 1f);
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, 28f, 600f, 40f), feedback, centerStyle);
            }

            if (model.Outcome != RunOutcome.Running)
            {
                GUI.color = new Color(0.002f, 0.005f, 0.008f, 0.93f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = model.Outcome == RunOutcome.Victory ? new Color(0.08f, 0.96f, 1f) : new Color(1f, 0.08f, 0.06f);
                string result = model.Outcome == RunOutcome.Victory ? "SIGNAL RECOVERED" : "DRONE OFFLINE";
                GUI.Label(new Rect(0f, Screen.height * 0.5f - 80f, Screen.width, 60f), result, giantStyle);
                GUI.color = Color.white;
                string detail = model.Outcome == RunOutcome.Victory
                    ? "Salvage extracted. The station lives a little longer."
                    : "Signal depleted in the dark.";
                GUI.Label(new Rect(0f, Screen.height * 0.5f - 10f, Screen.width, 36f), detail, centerStyle);
                GUI.color = new Color(0.72f, 0.84f, 0.88f);
                GUI.Label(new Rect(0f, Screen.height * 0.5f + 28f, Screen.width, 54f), RunReport(), reportStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(0f, Screen.height * 0.5f + 88f, Screen.width, 36f), "PRESS R / ENTER / GAMEPAD A TO RESTART", centerStyle);
            }

            if (IsPaused)
            {
                GUI.color = new Color(0.002f, 0.005f, 0.008f, 0.94f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (m_pauseInsignia != null)
                {
                    GUI.DrawTexture(new Rect(Screen.width * 0.5f - 92f, Screen.height * 0.5f - 205f, 184f, 184f),
                        m_pauseInsignia, ScaleMode.ScaleToFit, true);
                }

                GUI.color = new Color(0.08f, 0.96f, 1f);
                GUI.Label(new Rect(0f, Screen.height * 0.5f - 30f, Screen.width, 54f), "SIGNAL LINK SUSPENDED", giantStyle);
                GUI.color = new Color(0.76f, 0.86f, 0.9f);
                GUI.Label(new Rect(0f, Screen.height * 0.5f + 34f, Screen.width, 32f),
                    "Signal drain, threats, projectiles, and run time are frozen.", reportStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(0f, Screen.height * 0.5f + 78f, Screen.width, 36f),
                    "PRESS ESC / GAMEPAD MENU TO RESUME", centerStyle);
            }

            GUI.color = Color.white;
        }

        private string CurrentObjective()
        {
            if (!model.TowerOnline)
            {
                return "OBJECTIVE  Bring the tower online";
            }

            if (!model.CanExtract)
            {
                return $"OBJECTIVE  Recover salvage ({model.Salvage}/{RunModel.SalvageRequired})";
            }

            return "OBJECTIVE  Return to cyan extraction pad";
        }

        private string RunReport()
        {
            int totalSeconds = Mathf.FloorToInt(metrics.ElapsedSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"RUN REPORT   {minutes:00}:{seconds:00}   |   DEAD ZONE {metrics.DeadZoneSeconds:0.0}s   |   " +
                   $"SHOTS {metrics.ShotsFired}   |   HITS {metrics.SecurityHits}   |   DRAINS {metrics.SapperPulses}   |   SIGNAL {Mathf.CeilToInt(model.Signal)}";
        }

        private string SapperStatus()
        {
            if (!model.TowerOnline)
            {
                return "THREAT  SIGNAL SAPPER DORMANT";
            }

            if (sapperHealth <= 0f)
            {
                return "THREAT  SIGNAL SAPPER PURGED";
            }

            return sapperLatched
                ? $"THREAT  SAPPER DRAIN IN {sapperPulseCooldown:0.0}s (-{RunModel.SapperPulseCost:0})"
                : "THREAT  SIGNAL SAPPER APPROACHING TOWER";
        }

        private string ContextPrompt()
        {
            if (!model.ShortcutOpen && FlatDistance(player.position, shortcutPosition) < 1.9f)
            {
                return model.TowerOnline
                    ? $"[E / GAMEPAD X]  BURN {RunModel.ShortcutCost:0} SIGNAL FOR SHORTCUT"
                    : "SHORTCUT OFFLINE - ACTIVATE TOWER FIRST";
            }

            if (!model.TowerOnline && FlatDistance(player.position, towerPosition) < 1.8f)
            {
                return "[E / GAMEPAD X]  ACTIVATE SIGNAL TOWER  —  COST 10";
            }

            if (FlatDistance(player.position, extractionPosition) < 1.65f)
            {
                return model.CanExtract ? "[E / GAMEPAD X]  EXTRACT SALVAGE" : $"EXTRACTION LOCKED  —  {RunModel.SalvageRequired - model.Salvage} SALVAGE MISSING";
            }

            return string.Empty;
        }
    }
}
