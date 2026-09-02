using DeadSignal.Player;
using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion and shape feedback for one fragile Swarmer without moving its combat root.</summary>
    public sealed class SecuritySwarmerPresentation : MonoBehaviour
    {
        private const float CONTACT_REACTION_DURATION = 0.16f;
        private const float PURGE_DURATION = 0.26f;
        private const float WAKE_DURATION = 0.28f;

        private static readonly Color s_roleRed = new(1f, 0.15f, 0.08f, 1f);
        private static readonly Color s_warningAmber = new(1f, 0.58f, 0.12f, 1f);

        private Transform m_root;
        private Transform m_body;
        private Transform m_core;
        private Transform m_needle;
        private Transform m_tail;
        private Transform[] m_parts;
        private Vector3[] m_restPositions;
        private Quaternion[] m_restRotations;
        private Vector3[] m_restScales;
        private IComfortSettings m_comfortSettings;
        private Material m_effectMaterial;
        private LineRenderer m_stateEffect;
        private LineRenderer m_contactEffect;
        private LineRenderer m_purgeEffect;
        private Vector3 m_previousRootPosition;
        private Vector3 m_hitDirection;
        private float m_phase;
        private float m_wakeRemaining;
        private float m_contactRemaining;
        private float m_purgeRemaining;
        private float m_pressure;
        private float m_pressureTarget;

        public bool IsConfigured => m_root != null && m_body != null && m_core != null && m_needle != null && m_tail != null;
        public bool IsWaking => m_wakeRemaining > 0f;
        public bool IsContactReacting => m_contactRemaining > 0f;
        public bool IsPurgeVisible => m_purgeRemaining > 0f && gameObject.activeSelf;
        public bool IsStateEffectVisible => m_stateEffect != null && m_stateEffect.enabled;
        public bool IsContactEffectVisible => m_contactEffect != null && m_contactEffect.enabled;
        public bool IsPurgeEffectVisible => m_purgeEffect != null && m_purgeEffect.enabled;
        public float Pressure => m_pressure;
        public float MaximumEffectAlpha => Mathf.Max(
            _effectAlpha(m_stateEffect),
            Mathf.Max(_effectAlpha(m_contactEffect), _effectAlpha(m_purgeEffect)));

        internal void Configure(
            Transform root,
            Transform body,
            Transform core,
            Transform needle,
            Transform tail,
            IComfortSettings comfortSettings)
        {
            m_root = root;
            m_body = body;
            m_core = core;
            m_needle = needle;
            m_tail = tail;
            m_comfortSettings = comfortSettings;
            m_parts = new[] { body, core, needle, tail };
            m_restPositions = new Vector3[m_parts.Length];
            m_restRotations = new Quaternion[m_parts.Length];
            m_restScales = new Vector3[m_parts.Length];
            for (var index = 0; index < m_parts.Length; index++)
            {
                m_restPositions[index] = m_parts[index].localPosition;
                m_restRotations[index] = m_parts[index].localRotation;
                m_restScales[index] = m_parts[index].localScale;
            }
            m_phase = Mathf.Abs(GetInstanceID() % 31) * 0.37f;
            _createEffectRenderers();
            ResetPresentation();
        }

        public void PlayWake()
        {
            if (!IsConfigured)
            {
                return;
            }

            ResetPresentation();
            m_previousRootPosition = m_root.position;
            m_wakeRemaining = WAKE_DURATION;
        }

        public void SetPressure(float normalizedPressure)
        {
            m_pressureTarget = Mathf.Clamp01(normalizedPressure);
        }

        public void PlayContact()
        {
            if (!IsConfigured || m_purgeRemaining > 0f)
            {
                return;
            }

            m_contactRemaining = CONTACT_REACTION_DURATION;
        }

        public void PlayHitAndPurge(Vector3 sourcePosition)
        {
            if (!IsConfigured)
            {
                return;
            }

            var worldDirection = m_root.position - sourcePosition;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.01f)
            {
                worldDirection = -m_root.forward;
            }
            m_hitDirection = worldDirection.normalized;
            m_purgeRemaining = PURGE_DURATION;
            m_wakeRemaining = 0f;
            m_contactRemaining = 0f;
            m_pressureTarget = 0f;
            m_stateEffect.enabled = false;
            m_contactEffect.enabled = true;
            m_purgeEffect.enabled = true;
        }

        public void ResetPresentation()
        {
            m_wakeRemaining = 0f;
            m_contactRemaining = 0f;
            m_purgeRemaining = 0f;
            m_pressure = 0f;
            m_pressureTarget = 0f;
            _setEffectVisible(m_stateEffect, false);
            _setEffectVisible(m_contactEffect, false);
            _setEffectVisible(m_purgeEffect, false);
            if (IsConfigured)
            {
                _resetPose();
            }
        }

        private void Update()
        {
            if (!IsConfigured)
            {
                return;
            }

            var dt = Time.deltaTime;
            if (m_purgeRemaining > 0f)
            {
                _tickPurge(dt);
                return;
            }

            m_wakeRemaining = Mathf.Max(0f, m_wakeRemaining - dt);
            m_contactRemaining = Mathf.Max(0f, m_contactRemaining - dt);
            m_pressure = Mathf.MoveTowards(m_pressure, m_pressureTarget, dt * 5.5f);
            var rootDelta = m_root.position - m_previousRootPosition;
            rootDelta.y = 0f;
            m_previousRootPosition = m_root.position;
            var localDelta = m_root.InverseTransformDirection(rootDelta);
            var movement = dt > 0f ? Mathf.Clamp01(rootDelta.magnitude / (dt * 5f)) : 0f;
            var hover = Mathf.Sin(Time.time * 8.5f + m_phase);
            var wake = m_wakeRemaining > 0f ? 1f - m_wakeRemaining / WAKE_DURATION : 1f;
            var contact = m_contactRemaining > 0f ? m_contactRemaining / CONTACT_REACTION_DURATION : 0f;

            _updateLiveEffects(wake, movement, contact);

            m_body.localPosition = m_restPositions[0] + Vector3.up * (hover * 0.025f - (1f - wake) * 0.14f);
            m_body.localRotation = m_restRotations[0] * Quaternion.Euler(
                -movement * 10f - m_pressure * 13f + contact * 12f,
                0f,
                Mathf.Clamp(-localDelta.x * 90f, -12f, 12f));
            m_body.localScale = Vector3.Scale(m_restScales[0], new Vector3(0.76f + wake * 0.24f, wake, 0.82f + wake * 0.18f));

            m_core.localPosition = m_restPositions[1] + Vector3.up * (hover * 0.04f + m_pressure * 0.035f);
            m_core.localRotation = m_restRotations[1] * Quaternion.Euler(0f, Time.time * (45f + movement * 55f), 0f);
            m_core.localScale = m_restScales[1] * (0.9f + wake * 0.1f + m_pressure * 0.12f);

            m_needle.localPosition = m_restPositions[2] + Vector3.forward * (m_pressure * 0.16f - contact * 0.12f);
            m_needle.localRotation = m_restRotations[2] * Quaternion.Euler(-m_pressure * 8f + contact * 16f, 0f, 0f);
            m_tail.localPosition = m_restPositions[3] - Vector3.forward * (movement * 0.08f);
            m_tail.localRotation = m_restRotations[3] * Quaternion.Euler(movement * 10f, 0f, hover * 5f);
        }

        private void OnDisable()
        {
            ResetPresentation();
        }

        private void OnDestroy()
        {
            if (m_effectMaterial != null)
            {
                Destroy(m_effectMaterial);
            }
        }

        private void _createEffectRenderers()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                m_effectMaterial = new Material(shader)
                {
                    name = "Security Swarmer Effect Material",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else
            {
                Debug.LogWarning("Sprites/Default was unavailable; Swarmer effects will use the authored core material.", this);
            }

            var material = m_effectMaterial != null
                ? m_effectMaterial
                : m_core.GetComponent<MeshRenderer>().sharedMaterial;
            m_stateEffect = _createEffectRenderer("Security Swarmer State Effect", material, 7, 0.024f);
            m_contactEffect = _createEffectRenderer("Security Swarmer Contact Effect", material, 5, 0.034f);
            m_purgeEffect = _createEffectRenderer("Security Swarmer Purge Effect", material, 13, 0.03f);
        }

        private LineRenderer _createEffectRenderer(string objectName, Material material, int positionCount, float width)
        {
            var effect = new GameObject(objectName);
            effect.transform.SetParent(transform, false);
            var line = effect.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = positionCount;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.sortingOrder = 24;
            line.enabled = false;
            return line;
        }

        private void _updateLiveEffects(float wakeProgress, float movement, float contact)
        {
            var reducedFlashes = m_comfortSettings != null && m_comfortSettings.ReducedFlashesEnabled;
            var pulse = reducedFlashes ? 1f : 0.88f + Mathf.Sin(Time.time * 15f + m_phase) * 0.12f;
            var wakeWeight = m_wakeRemaining > 0f ? Mathf.Sin(wakeProgress * Mathf.PI) : 0f;
            var stateWeight = Mathf.Max(wakeWeight, Mathf.Max(movement * 0.44f, m_pressure * 0.64f));
            if (stateWeight > 0.01f)
            {
                _setPressureWake(m_stateEffect, wakeWeight, movement, m_pressure);
                _setEffectColor(m_stateEffect, s_roleRed, Mathf.Min(reducedFlashes ? 0.3f : 0.56f, stateWeight * pulse));
                m_stateEffect.enabled = true;
            }
            else
            {
                m_stateEffect.enabled = false;
            }

            if (contact > 0.01f)
            {
                _setContactClamp(m_contactEffect, contact);
                _setEffectColor(m_contactEffect, s_warningAmber, Mathf.Min(reducedFlashes ? 0.3f : 0.68f, contact * pulse));
                m_contactEffect.enabled = true;
            }
            else
            {
                m_contactEffect.enabled = false;
            }
        }

        private void _setPressureWake(LineRenderer line, float wakeWeight, float movement, float pressure)
        {
            var center = m_root.position + Vector3.up * 0.34f;
            var forward = m_root.forward;
            var right = m_root.right;
            var spread = 0.25f + wakeWeight * 0.12f + pressure * 0.06f;
            var rear = 0.3f + movement * 0.28f;
            var front = 0.18f + pressure * 0.2f;
            line.SetPosition(0, center - forward * rear - right * spread);
            line.SetPosition(1, center - forward * rear * 0.35f - right * spread * 0.55f);
            line.SetPosition(2, center + forward * front - right * 0.08f);
            line.SetPosition(3, center + forward * (front + 0.14f));
            line.SetPosition(4, center + forward * front + right * 0.08f);
            line.SetPosition(5, center - forward * rear * 0.35f + right * spread * 0.55f);
            line.SetPosition(6, center - forward * rear + right * spread);
        }

        private void _setContactClamp(LineRenderer line, float contact)
        {
            var center = m_root.position + Vector3.up * 0.34f;
            var forward = m_root.forward;
            var right = m_root.right;
            var opening = Mathf.Lerp(0.36f, 0.12f, contact);
            line.SetPosition(0, center + forward * 0.34f - right * opening);
            line.SetPosition(1, center + forward * 0.12f - right * opening * 0.55f);
            line.SetPosition(2, center + forward * (0.46f + contact * 0.16f));
            line.SetPosition(3, center + forward * 0.12f + right * opening * 0.55f);
            line.SetPosition(4, center + forward * 0.34f + right * opening);
        }

        private void _tickPurge(float dt)
        {
            m_purgeRemaining = Mathf.Max(0f, m_purgeRemaining - dt);
            var normalized = 1f - m_purgeRemaining / PURGE_DURATION;
            var recoil = Mathf.Sin(normalized * Mathf.PI) * 0.12f;
            var localHitDirection = m_root.InverseTransformDirection(m_hitDirection);
            for (var index = 0; index < m_parts.Length; index++)
            {
                m_parts[index].localPosition = m_restPositions[index] + localHitDirection * recoil * (index == 2 ? 1.4f : 1f);
                m_parts[index].localRotation = m_restRotations[index] * Quaternion.Euler(normalized * 75f, normalized * 95f, 0f);
                m_parts[index].localScale = m_restScales[index] * Mathf.Max(0.05f, 1f - normalized);
            }

            _setPurgeEffects(normalized);
            if (m_purgeRemaining <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void _setPurgeEffects(float progress)
        {
            var reducedFlashes = m_comfortSettings != null && m_comfortSettings.ReducedFlashesEnabled;
            var weight = Mathf.Sin(progress * Mathf.PI);
            var center = m_root.position + Vector3.up * 0.34f;
            var right = Vector3.Cross(Vector3.up, m_hitDirection).normalized;
            m_contactEffect.SetPosition(0, center - m_hitDirection * 0.2f - right * 0.18f);
            m_contactEffect.SetPosition(1, center + m_hitDirection * 0.08f - right * 0.06f);
            m_contactEffect.SetPosition(2, center + m_hitDirection * (0.42f + progress * 0.34f));
            m_contactEffect.SetPosition(3, center + m_hitDirection * 0.08f + right * 0.06f);
            m_contactEffect.SetPosition(4, center - m_hitDirection * 0.2f + right * 0.18f);
            _setEffectColor(m_contactEffect, s_warningAmber, Mathf.Min(reducedFlashes ? 0.3f : 0.72f, weight));

            var radius = 0.12f + progress * 0.62f;
            for (var index = 0; index < m_purgeEffect.positionCount; index++)
            {
                var angle = index / (float)(m_purgeEffect.positionCount - 1) * Mathf.PI * 2f;
                var point = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                m_purgeEffect.SetPosition(index, center + point * radius);
            }
            _setEffectColor(m_purgeEffect, s_roleRed, Mathf.Min(reducedFlashes ? 0.3f : 0.58f, weight));
        }

        private static void _setEffectColor(LineRenderer line, Color color, float alpha)
        {
            color.a = alpha;
            line.startColor = color;
            color.a *= 0.45f;
            line.endColor = color;
        }

        private static void _setEffectVisible(LineRenderer line, bool visible)
        {
            if (line != null)
            {
                line.enabled = visible;
            }
        }

        private static float _effectAlpha(LineRenderer line) => line != null && line.enabled ? line.startColor.a : 0f;

        private void _resetPose()
        {
            for (var index = 0; index < m_parts.Length; index++)
            {
                m_parts[index].localPosition = m_restPositions[index];
                m_parts[index].localRotation = m_restRotations[index];
                m_parts[index].localScale = m_restScales[index];
            }
            m_previousRootPosition = m_root.position;
        }
    }
}
