using System.Collections.Generic;
using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Builds and owns the runtime scene graph plus spatial queries for the fixed prototype arena.
    /// </summary>
    internal sealed class DeadSignalWorld
    {
        public const float ARENA_HALF_WIDTH = 13.2f;
        public const float ARENA_HALF_HEIGHT = 8.8f;
        public const float STARTING_POWER_RADIUS = 3.6f;
        public const float TOWER_POWER_RADIUS = 7.2f;

        private readonly Transform m_root;
        private readonly DeadSignalPalette m_palette = new();
        private readonly List<MovementBlocker> m_movementBlockers = new();
        private readonly List<GameObject> m_salvagePickups = new();

        private GameObject m_towerTerritory;
        private GameObject m_towerSignalLines;
        private GameObject m_extractionBeacon;
        private GameObject m_shortcutGate;

        public DeadSignalWorld(Transform root)
        {
            m_root = root;
            _buildPresentation();
            _buildArena();
            _buildActors();
        }

        public Vector3 ExtractionPosition { get; } = new(-9.2f, 0f, -5.6f);
        public Vector3 TowerPosition { get; } = new(-0.6f, 0f, 0.4f);
        public Vector3 ShortcutPosition { get; } = new(4f, 0f, 0.4f);

        public Camera Camera { get; private set; }
        public Transform Player { get; private set; }
        public Transform PlayerNose { get; private set; }
        public Transform Warden { get; private set; }
        public Transform Sapper { get; private set; }
        public Transform SapperCore { get; private set; }
        public Transform TowerCore { get; private set; }
        public SignalSapperTelegraph SapperTelegraph { get; private set; }
        public IReadOnlyList<GameObject> SalvagePickups => m_salvagePickups;

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
            if (!_isBlocked(desired, radius, shortcutOpen))
            {
                return desired;
            }

            var xOnly = new Vector3(desired.x, current.y, current.z);
            var zOnly = new Vector3(current.x, current.y, desired.z);
            bool canMoveX = !_isBlocked(xOnly, radius, shortcutOpen);
            bool canMoveZ = !_isBlocked(zOnly, radius, shortcutOpen);
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

        public void ActivateTower(float sapperPulseInterval)
        {
            m_towerTerritory.GetComponent<Renderer>().sharedMaterial = m_palette.CyanDim;
            TowerCore.GetComponent<Renderer>().sharedMaterial = m_palette.Cyan;
            m_towerSignalLines.SetActive(true);
            Warden.gameObject.SetActive(true);
            Sapper.gameObject.SetActive(true);
            SapperTelegraph.SetThreatState(true, false, 0f, sapperPulseInterval);
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
            SapperTelegraph.SetThreatState(false, false, 0f, DeadSignalThreatController.SAPPER_PULSE_INTERVAL);
        }

        public GameObject CreateSignalBolt(Vector3 direction)
        {
            var bolt = _createPrimitive(
                "Signal Bolt",
                PrimitiveType.Cube,
                Player.position + direction * 0.9f + Vector3.up * 0.25f,
                new Vector3(0.16f, 0.16f, 0.55f),
                m_palette.Cyan);
            bolt.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            return bolt;
        }

        public void TickTower(float dt, bool towerOnline)
        {
            TowerCore.Rotate(Vector3.up, (towerOnline ? 110f : 22f) * dt, Space.World);
            float pulse = 1f + Mathf.Sin(Time.time * (towerOnline ? 5f : 2f)) * 0.08f;
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

            foreach (var existing in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                existing.enabled = false;
            }

            var cameraObject = new GameObject("Dead Signal Camera");
            cameraObject.transform.SetParent(m_root);
            cameraObject.transform.position = new Vector3(0f, 20f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Camera = cameraObject.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.orthographicSize = 10.4f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.002f, 0.004f, 0.008f);
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 40f;

            var lightObject = new GameObject("Cold Overhead Light");
            lightObject.transform.SetParent(m_root);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.38f, 0.52f, 0.65f);
            key.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(0.025f, 0.035f, 0.05f);
        }

        private void _buildArena()
        {
            _createPrimitive("Station Deck", PrimitiveType.Cube, new Vector3(0f, -0.45f, 0f), new Vector3(27.5f, 0.6f, 18.5f), m_palette.Dark);

            for (int x = -12; x <= 12; x += 2)
            {
                _createPrimitive("Deck Seam", PrimitiveType.Cube, new Vector3(x, -0.12f, 0f), new Vector3(0.025f, 0.015f, 17.6f), m_palette.Steel);
            }

            for (int z = -8; z <= 8; z += 2)
            {
                _createPrimitive("Deck Seam", PrimitiveType.Cube, new Vector3(0f, -0.115f, z), new Vector3(26.4f, 0.015f, 0.025f), m_palette.Steel);
            }

            _createPrimitive("North Bulkhead", PrimitiveType.Cube, new Vector3(0f, 0.25f, 9.1f), new Vector3(27.8f, 0.8f, 0.5f), m_palette.Steel);
            _createPrimitive("South Bulkhead", PrimitiveType.Cube, new Vector3(0f, 0.25f, -9.1f), new Vector3(27.8f, 0.8f, 0.5f), m_palette.Steel);
            _createPrimitive("East Bulkhead", PrimitiveType.Cube, new Vector3(13.7f, 0.25f, 0f), new Vector3(0.5f, 0.8f, 18.7f), m_palette.Steel);
            _createPrimitive("West Bulkhead", PrimitiveType.Cube, new Vector3(-13.7f, 0.25f, 0f), new Vector3(0.5f, 0.8f, 18.7f), m_palette.Steel);

            _createTerritory("Dock Power Territory", ExtractionPosition, STARTING_POWER_RADIUS, m_palette.CyanDim);
            m_towerTerritory = _createTerritory("Tower Power Territory", TowerPosition, TOWER_POWER_RADIUS, m_palette.Dark);

            for (int x = -12; x <= 12; x += 4)
            {
                _createPrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, 8.55f), new Vector3(1.5f, 0.035f, 0.07f), m_palette.RedDim);
                _createPrimitive("Security Edge Marker", PrimitiveType.Cube, new Vector3(x, -0.06f, -8.55f), new Vector3(1.5f, 0.035f, 0.07f), m_palette.RedDim);
            }

            _buildExtraction();
            _buildTower();
            _buildStationMachines();
            _buildSignalShortcut();
        }

        private void _buildExtraction()
        {
            _createPrimitive("Extraction Plinth", PrimitiveType.Cylinder, ExtractionPosition + new Vector3(0f, 0.02f, 0f), new Vector3(3.2f, 0.08f, 3.2f), m_palette.CyanDim);
            _createPrimitive("Extraction Ring", PrimitiveType.Cylinder, ExtractionPosition + new Vector3(0f, 0.08f, 0f), new Vector3(2.55f, 0.08f, 2.55f), m_palette.Cyan);
            _createPrimitive("Extraction Center", PrimitiveType.Cylinder, ExtractionPosition + new Vector3(0f, 0.14f, 0f), new Vector3(2.1f, 0.08f, 2.1f), m_palette.Dark);
            m_extractionBeacon = _createPrimitive("Extraction Beacon", PrimitiveType.Cube, ExtractionPosition + new Vector3(0f, 0.7f, 1.5f), new Vector3(0.22f, 1.4f, 0.22f), m_palette.Cyan);
        }

        private void _buildTower()
        {
            _createPrimitive("Tower Base", PrimitiveType.Cylinder, TowerPosition + new Vector3(0f, 0.15f, 0f), new Vector3(2.2f, 0.25f, 2.2f), m_palette.Steel);
            _createPrimitive("Tower Column", PrimitiveType.Cylinder, TowerPosition + new Vector3(0f, 0.85f, 0f), new Vector3(0.8f, 1.35f, 0.8f), m_palette.Steel);
            TowerCore = _createPrimitive("Tower Core", PrimitiveType.Cylinder, TowerPosition + new Vector3(0f, 1.65f, 0f), new Vector3(1.35f, 0.22f, 1.35f), m_palette.RedDim).transform;
            m_towerSignalLines = new GameObject("Tower Signal Lines");
            m_towerSignalLines.transform.SetParent(m_root);
            _createPrimitive("Signal Trunk West", PrimitiveType.Cube, new Vector3(-4.7f, -0.03f, 0.4f), new Vector3(8.2f, 0.04f, 0.09f), m_palette.Cyan, m_towerSignalLines.transform);
            _createPrimitive("Signal Trunk East", PrimitiveType.Cube, new Vector3(4.1f, -0.03f, 0.4f), new Vector3(9.4f, 0.04f, 0.09f), m_palette.Cyan, m_towerSignalLines.transform);
            _createPrimitive("Signal Branch", PrimitiveType.Cube, new Vector3(-0.6f, -0.025f, -3.5f), new Vector3(0.09f, 0.04f, 7.8f), m_palette.Cyan, m_towerSignalLines.transform);
            m_towerSignalLines.SetActive(false);
        }

        private void _buildStationMachines()
        {
            Vector3[] locations =
            {
                new(-11.6f, 0f, 6.8f), new(-8.8f, 0f, 6.9f), new(10.8f, 0f, 6.8f),
                new(11.2f, 0f, -6.7f), new(4.8f, 0f, -7.1f), new(-3.8f, 0f, 7.1f)
            };

            for (int i = 0; i < locations.Length; i++)
            {
                var position = locations[i];
                _createPrimitive("Machine Block", PrimitiveType.Cube, position + new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 0.9f, 1.1f), m_palette.Steel);
                _createPrimitive("Machine Status", PrimitiveType.Cube, position + new Vector3(0f, 0.92f, -0.15f), new Vector3(0.75f, 0.06f, 0.18f),
                    i % 2 == 0 ? m_palette.RedDim : m_palette.CyanDim);
            }
        }

        private void _buildSignalShortcut()
        {
            // The end passages stay open, so spending Signal for the central route is optional.
            _createBarrierSegment("Shortcut Bulkhead South", new Vector3(4f, 0.46f, -3.15f), new Vector3(0.55f, 1.1f, 4.7f));
            _createBarrierSegment("Shortcut Bulkhead North", new Vector3(4f, 0.46f, 3.55f), new Vector3(0.55f, 1.1f, 3.9f));

            _createPrimitive("Shortcut Gate South Post", PrimitiveType.Cube, ShortcutPosition + new Vector3(-0.16f, 0.68f, -1.34f), new Vector3(0.85f, 1.45f, 0.25f), m_palette.Steel);
            _createPrimitive("Shortcut Gate North Post", PrimitiveType.Cube, ShortcutPosition + new Vector3(-0.16f, 0.68f, 1.34f), new Vector3(0.85f, 1.45f, 0.25f), m_palette.Steel);
            _createPrimitive("Shortcut Gate Signal", PrimitiveType.Cube, ShortcutPosition + new Vector3(-0.31f, 1.38f, 0f), new Vector3(0.12f, 0.08f, 2.3f), m_palette.CyanDim);
            m_shortcutGate = _createPrimitive("Signal Shortcut Gate", PrimitiveType.Cube, ShortcutPosition + new Vector3(0f, 0.55f, 0f), new Vector3(0.42f, 1.05f, 2.4f), m_palette.RedDim);
            m_movementBlockers.Add(new MovementBlocker(new Vector2(ShortcutPosition.x, ShortcutPosition.z), new Vector2(0.21f, 1.2f), true));
        }

        private void _createBarrierSegment(string objectName, Vector3 position, Vector3 scale)
        {
            _createPrimitive(objectName, PrimitiveType.Cube, position, scale, m_palette.Steel);
            m_movementBlockers.Add(new MovementBlocker(
                new Vector2(position.x, position.z),
                new Vector2(scale.x * 0.5f, scale.z * 0.5f),
                false));
        }

        private void _buildActors()
        {
            var playerRoot = new GameObject("Maintenance Drone");
            playerRoot.transform.SetParent(m_root);
            playerRoot.transform.position = ExtractionPosition;
            Player = playerRoot.transform;
            _createPrimitive("Drone Chassis", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(1.05f, 0.22f, 1.05f), m_palette.White, Player);
            _createPrimitive("Drone Signal Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.08f, 0.72f), m_palette.Cyan, Player);
            _createPrimitive("Drone Core", PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.36f, 0.09f, 0.36f), m_palette.Dark, Player);
            PlayerNose = _createPrimitive("Drone Tool", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.68f), new Vector3(0.24f, 0.2f, 0.7f), m_palette.Cyan, Player).transform;

            var enemyRoot = new GameObject("Security Warden");
            enemyRoot.transform.SetParent(m_root);
            enemyRoot.transform.position = new Vector3(6.8f, 0f, 4.7f);
            Warden = enemyRoot.transform;
            _createPrimitive("Warden Chassis", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f), new Vector3(1.15f, 0.55f, 1.15f), m_palette.Steel, Warden);
            _createPrimitive("Warden Eye", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.59f), new Vector3(0.68f, 0.16f, 0.06f), m_palette.Red, Warden);
            _createPrimitive("Warden Crown", PrimitiveType.Cylinder, new Vector3(0f, 0.76f, 0f), new Vector3(0.68f, 0.12f, 0.68f), m_palette.RedDim, Warden);
            Warden.gameObject.SetActive(false);

            var sapperRoot = new GameObject("Signal Sapper");
            sapperRoot.transform.SetParent(m_root);
            sapperRoot.transform.position = new Vector3(-10.8f, 0f, 5.7f);
            Sapper = sapperRoot.transform;
            _createPrimitive("Sapper Chassis", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.72f, 0.34f, 1.25f), m_palette.Steel, Sapper);
            _createPrimitive("Sapper Fork Left", PrimitiveType.Cube, new Vector3(-0.43f, 0.28f, 0.28f), new Vector3(0.18f, 0.18f, 0.92f), m_palette.Magenta, Sapper);
            _createPrimitive("Sapper Fork Right", PrimitiveType.Cube, new Vector3(0.43f, 0.28f, 0.28f), new Vector3(0.18f, 0.18f, 0.92f), m_palette.Magenta, Sapper);
            SapperCore = _createPrimitive("Sapper Drain Core", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, -0.12f), new Vector3(0.42f, 0.1f, 0.42f), m_palette.Magenta, Sapper).transform;
            Sapper.gameObject.SetActive(false);

            var telegraphRoot = new GameObject("Sapper Drain Telegraph");
            telegraphRoot.transform.SetParent(m_root);
            SapperTelegraph = telegraphRoot.AddComponent<SignalSapperTelegraph>();
            SapperTelegraph.Configure(Sapper, TowerPosition, m_palette.Magenta, m_palette.Magenta);

            _createSalvage(new Vector3(9.7f, 0f, 6.3f));
            _createSalvage(new Vector3(10.4f, 0f, -6.4f));
            _createSalvage(new Vector3(-5.8f, 0f, 7.2f));
        }

        private void _createSalvage(Vector3 position)
        {
            var root = new GameObject("Salvage Cache");
            root.transform.SetParent(m_root);
            root.transform.position = position;
            _createPrimitive("Salvage Case", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(0.75f, 0.48f, 0.75f), m_palette.Amber, root.transform);
            _createPrimitive("Salvage Band", PrimitiveType.Cube, new Vector3(0f, 0.61f, 0f), new Vector3(0.9f, 0.06f, 0.28f), m_palette.White, root.transform);
            m_salvagePickups.Add(root);
        }

        private bool _isBlocked(Vector3 position, float radius, bool shortcutOpen)
        {
            foreach (var blocker in m_movementBlockers)
            {
                if (blocker.IsShortcutGate && shortcutOpen)
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

        private sealed class MovementBlocker
        {
            public MovementBlocker(Vector2 center, Vector2 halfSize, bool isShortcutGate)
            {
                Center = center;
                HalfSize = halfSize;
                IsShortcutGate = isShortcutGate;
            }

            public Vector2 Center { get; }
            public Vector2 HalfSize { get; }
            public bool IsShortcutGate { get; }
        }
    }
}
