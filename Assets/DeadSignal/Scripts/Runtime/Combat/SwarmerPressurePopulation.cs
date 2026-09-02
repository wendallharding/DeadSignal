using System;
using System.Collections.Generic;
using DeadSignal.Presentation;
using DeadSignal.World;
using UnityEngine;

namespace DeadSignal.Combat
{
    internal sealed class SwarmerPressurePopulation
    {
        private const string PREFAB_RESOURCE = "Actors/SwarmerAssembly";
        private static readonly float s_projectileHitRadius = Mathf.Sqrt(0.45f);

        private readonly DeadSignalWorld m_world;
        private readonly SwarmerPressureTuning m_tuning;
        private readonly Action<Vector3> m_onContact;
        private readonly Action<Vector3> m_onPurge;
        private readonly List<Agent> m_agents = new();
        private readonly GameObject m_prefab;

        private AuthoredCombatScenario m_scenario;
        private float m_secondWaveCountdown;
        private bool m_secondWaveDeployed;

        public SwarmerPressurePopulation(
            DeadSignalWorld world,
            SwarmerPressureTuning tuning,
            Action<Vector3> onContact,
            Action<Vector3> onPurge)
        {
            m_world = world;
            m_tuning = tuning;
            m_onContact = onContact;
            m_onPurge = onPurge;
            m_prefab = Resources.Load<GameObject>(PREFAB_RESOURCE);
        }

        public bool HasAssets => m_prefab != null && m_prefab.transform.Find("Swarmer Core") != null;
        public int ActiveCount { get; private set; }
        public int PeakActiveCount { get; private set; }
        public int SpawnedCount { get; private set; }
        public int PurgedCount { get; private set; }
        public int ContactCount { get; private set; }
        public IReadOnlyList<Transform> ActiveTransforms
        {
            get
            {
                m_activeTransforms.Clear();
                foreach (var agent in m_agents)
                {
                    if (agent.IsActive)
                    {
                        m_activeTransforms.Add(agent.Visual);
                    }
                }
                return m_activeTransforms;
            }
        }

        public void Deploy(AuthoredCombatScenario scenario)
        {
            Reset();
            if (!HasAssets || scenario == null || !scenario.IsComplete)
            {
                return;
            }

            m_scenario = scenario;
            var waveSize = Mathf.Min(m_tuning.WaveSize, m_tuning.MaximumAlive);
            _createWave(scenario.WardenAnchor.position, waveSize, true);
            var secondWaveSize = m_tuning.MaximumAlive - waveSize;
            _createWave(_safeInactiveWaveCenter(scenario.SapperAnchor.position, secondWaveSize), secondWaveSize, false);
            m_secondWaveCountdown = m_tuning.SecondWaveDelay;
            _activateWave(true);
        }

        public void DeploySingleWave(AuthoredCombatScenario scenario, Transform anchor, int count)
        {
            RetireActivePopulation();
            if (!HasAssets || scenario == null || !scenario.IsComplete || anchor == null)
            {
                return;
            }

            m_scenario = scenario;
            _createWave(anchor.position, Mathf.Clamp(count, 1, m_tuning.MaximumAlive), true);
            m_secondWaveDeployed = true;
            _activateWave(true);
        }

        public void Tick(float dt, bool shortcutOpen)
        {
            if (m_scenario == null)
            {
                return;
            }

            if (!m_secondWaveDeployed)
            {
                m_secondWaveCountdown = Mathf.Max(0f, m_secondWaveCountdown - dt);
                if (m_secondWaveCountdown <= 0f && _canDeploySecondWave())
                {
                    _activateWave(false);
                    m_secondWaveDeployed = true;
                }
            }

            foreach (var agent in m_agents)
            {
                if (!agent.IsActive)
                {
                    continue;
                }

                agent.ContactCooldown = Mathf.Max(0f, agent.ContactCooldown - dt);
                var playerDistance = DeadSignalWorld.FlatDistance(agent.Visual.position, m_world.Player.position);
                agent.Presentation.SetPressure(1f - Mathf.InverseLerp(
                    m_tuning.ContactDistance,
                    m_tuning.ContactDistance * 2.4f,
                    playerDistance));
                var navigationTarget = m_world.GetNavMeshWaypoint(
                    agent.Visual,
                    m_world.Player.position,
                    m_tuning.CollisionRadius,
                    shortcutOpen);
                var delta = navigationTarget - agent.Visual.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.0025f)
                {
                    agent.Visual.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                    var desired = agent.Visual.position + delta.normalized * (m_tuning.Speed * dt);
                    agent.Visual.position = m_scenario.ClampToSafeArea(m_world.ResolveMovement(
                        agent.Visual.position,
                        desired,
                        m_tuning.CollisionRadius,
                        shortcutOpen));
                }

                if (agent.ContactCooldown > 0f || playerDistance > m_tuning.ContactDistance)
                {
                    continue;
                }

                agent.ContactCooldown = m_tuning.ContactCooldown;
                agent.Presentation.PlayContact();
                ContactCount++;
                m_onContact(agent.Visual.position);
            }
        }

        public bool TryGetHitFraction(
            Vector3 start,
            Vector3 end,
            HashSet<int> excludedIds,
            out int agentId,
            out float hitFraction)
        {
            agentId = -1;
            hitFraction = float.PositiveInfinity;
            foreach (var agent in m_agents)
            {
                if (!agent.IsActive || excludedIds.Contains(agent.Id))
                {
                    continue;
                }

                var center = agent.Visual.position + Vector3.up * 0.3f;
                if (!ProjectileCollision.TryGetCircleHitFraction(start, end, center, s_projectileHitRadius, out var candidate) ||
                    candidate >= hitFraction)
                {
                    continue;
                }

                agentId = agent.Id;
                hitFraction = candidate;
            }
            return agentId >= 0;
        }

        public Vector3 Purge(int agentId, Vector3 sourcePosition)
        {
            var agent = m_agents.Find(candidate => candidate.Id == agentId);
            if (agent == null || !agent.IsActive)
            {
                return Vector3.zero;
            }

            var position = agent.Visual.position;
            agent.IsActive = false;
            agent.Presentation.PlayHitAndPurge(sourcePosition);
            ActiveCount--;
            PurgedCount++;
            m_onPurge(position);
            return position;
        }

        public void PurgeAllForDebug()
        {
            for (var index = m_agents.Count - 1; index >= 0; index--)
            {
                var agent = m_agents[index];
                if (agent.IsActive)
                {
                    Purge(agent.Id, agent.Visual.position - agent.Visual.forward);
                }
            }
        }

        public void Reset()
        {
            RetireActivePopulation();
            PeakActiveCount = 0;
            SpawnedCount = 0;
            PurgedCount = 0;
            ContactCount = 0;
        }

        public void RetireActivePopulation()
        {
            foreach (var agent in m_agents)
            {
                if (agent.Visual != null)
                {
                    agent.Visual.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(agent.Visual.gameObject);
                }
            }

            m_agents.Clear();
            m_activeTransforms.Clear();
            m_scenario = null;
            m_secondWaveCountdown = 0f;
            m_secondWaveDeployed = false;
            ActiveCount = 0;
        }

        private bool _canDeploySecondWave()
        {
            foreach (var agent in m_agents)
            {
                if (agent.IsFirstWave || agent.IsActive ||
                    DeadSignalWorld.FlatDistance(agent.Visual.position, m_world.Player.position) >= m_tuning.SafeSpawnDistance)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private void _createWave(Vector3 center, int count, bool isFirstWave)
        {
            var inward = m_world.Player.position - center;
            inward.y = 0f;
            if (inward.sqrMagnitude > 0.01f)
            {
                center += inward.normalized * 0.6f;
            }

            for (var index = 0; index < count; index++)
            {
                var offset = (index - (count - 1) * 0.5f) * m_tuning.SpawnSpacing;
                var visual = UnityEngine.Object.Instantiate(m_prefab, center + Vector3.forward * offset, Quaternion.identity);
                visual.name = $"Security Swarmer {m_agents.Count + 1}";
                visual.transform.SetParent(m_world.Player.parent, true);
                visual.SetActive(false);
                m_world.RebindRuntimeMaterials(visual.transform);
                var presentation = visual.GetComponent<SecuritySwarmerPresentation>();
                presentation.Configure(
                    visual.transform,
                    visual.transform.Find("Swarmer Body"),
                    visual.transform.Find("Swarmer Core"),
                    visual.transform.Find("Swarmer Needle"),
                    visual.transform.Find("Swarmer Tail"),
                    m_world.ComfortSettings);
                m_agents.Add(new Agent(m_agents.Count, visual.transform, presentation, isFirstWave));
            }
        }

        private Vector3 _safeInactiveWaveCenter(Vector3 center, int count)
        {
            var awayFromPlayer = center - m_world.Player.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude < 0.01f)
            {
                awayFromPlayer = Vector3.right;
            }

            var maximumSpawnOffset = Mathf.Max(0f, (count - 1) * 0.5f * m_tuning.SpawnSpacing);
            // _createWave nudges the formation 0.6m inward, so retain enough clearance after that adjustment.
            var minimumCenterDistance = m_tuning.SafeSpawnDistance + maximumSpawnOffset + 0.75f;
            if (awayFromPlayer.magnitude >= minimumCenterDistance)
            {
                return center;
            }

            return m_world.Player.position + awayFromPlayer.normalized * minimumCenterDistance;
        }

        private void _activateWave(bool firstWave)
        {
            foreach (var agent in m_agents)
            {
                if (agent.IsFirstWave != firstWave || agent.IsActive)
                {
                    continue;
                }

                agent.IsActive = true;
                agent.Visual.gameObject.SetActive(true);
                agent.Presentation.PlayWake();
                ActiveCount++;
                SpawnedCount++;
            }
            PeakActiveCount = Mathf.Max(PeakActiveCount, ActiveCount);
        }

        private readonly List<Transform> m_activeTransforms = new();

        private sealed class Agent
        {
            public Agent(int id, Transform visual, SecuritySwarmerPresentation presentation, bool isFirstWave)
            {
                Id = id;
                Visual = visual;
                Presentation = presentation;
                IsFirstWave = isFirstWave;
            }

            public int Id { get; }
            public Transform Visual { get; }
            public SecuritySwarmerPresentation Presentation { get; }
            public bool IsFirstWave { get; }
            public bool IsActive { get; set; }
            public float ContactCooldown { get; set; }
        }
    }
}
