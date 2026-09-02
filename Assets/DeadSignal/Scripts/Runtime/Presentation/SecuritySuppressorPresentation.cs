using DeadSignal.Player;
using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>Owns presentation-only motion and effects for the authored Security Suppressor.</summary>
    public sealed class SecuritySuppressorPresentation : MonoBehaviour
    {
        private const float ACTIVATION_DURATION = 0.28f;
        private const float EXIT_DURATION = 0.34f;
        private const float HIT_DURATION = 0.2f;
        private const float PURGE_DURATION = 0.38f;
        private const float SHUTDOWN_DURATION = 0.3f;
        private const float WAKE_DURATION = 0.45f;

        private static readonly Color s_roleMagenta = new(1f, 0.1f, 0.72f, 1f);
        private static readonly Color s_forecastAmber = new(1f, 0.58f, 0.12f, 1f);
        private static readonly Color s_exitCyan = new(0.16f, 0.92f, 1f, 1f);

        private Transform m_suppressor;
        private Transform m_player;
        private Transform m_chassis;
        private Transform m_leftEmitter;
        private Transform m_rightEmitter;
        private Transform m_core;
        private Transform m_purgeEcho;
        private IComfortSettings m_comfortSettings;
        private Material m_effectMaterial;
        private LineRenderer m_stateEffect;
        private LineRenderer m_fieldEffect;
        private LineRenderer m_responseEffect;
        private LineRenderer m_purgeEffect;
        private Vector3[] m_restPositions;
        private Quaternion[] m_restRotations;
        private Vector3[] m_restScales;
        private Vector3 m_previousRootPosition;
        private Vector3 m_hitDirection;
        private Vector3 m_fieldCenter;
        private Vector3 m_exitDirection;
        private Vector3 m_purgePosition;
        private Quaternion m_purgeRotation;
        private Vector3 m_purgeScale;
        private float m_fieldRadius = 1f;
        private float m_wakeRemaining;
        private float m_hitRemaining;
        private float m_purgeRemaining;
        private float m_shutdownRemaining;
        private float m_activationRemaining;
        private float m_exitRemaining;
        private bool m_fieldVisible;
        private bool m_fieldActive;
        private bool m_playerCaught;

        public bool IsConfigured => m_suppressor != null && m_player != null && m_chassis != null &&
                                    m_leftEmitter != null && m_rightEmitter != null && m_core != null && m_purgeEcho != null;
        public bool IsWaking => m_wakeRemaining > 0f;
        public bool IsWarning => m_fieldVisible && !m_fieldActive;
        public bool IsProjecting => m_fieldVisible && m_fieldActive;
        public bool IsPlayerCaught => m_playerCaught;
        public bool IsShuttingDown => m_shutdownRemaining > 0f;
        public bool IsHitReacting => m_hitRemaining > 0f;
        public bool IsPurgeVisible => m_purgeRemaining > 0f && m_purgeEcho != null && m_purgeEcho.gameObject.activeSelf;
        public bool IsStateEffectVisible => m_stateEffect != null && m_stateEffect.enabled;
        public bool IsFieldEffectVisible => m_fieldEffect != null && m_fieldEffect.enabled;
        public bool IsResponseEffectVisible => m_responseEffect != null && m_responseEffect.enabled;
        public bool IsPurgeEffectVisible => m_purgeEffect != null && m_purgeEffect.enabled;
        public float MaximumEffectAlpha => Mathf.Max(_visibleAlpha(m_stateEffect), Mathf.Max(_visibleAlpha(m_fieldEffect),
            Mathf.Max(_visibleAlpha(m_responseEffect), _visibleAlpha(m_purgeEffect))));

        internal void Configure(Transform suppressor, Transform player, Transform chassis, Transform leftEmitter,
            Transform rightEmitter, Transform core, IComfortSettings comfortSettings)
        {
            m_suppressor = suppressor;
            m_player = player;
            m_chassis = chassis;
            m_leftEmitter = leftEmitter;
            m_rightEmitter = rightEmitter;
            m_core = core;
            m_comfortSettings = comfortSettings;
            var parts = _parts();
            m_restPositions = new Vector3[parts.Length];
            m_restRotations = new Quaternion[parts.Length];
            m_restScales = new Vector3[parts.Length];
            for (var index = 0; index < parts.Length; index++)
            {
                m_restPositions[index] = parts[index].localPosition;
                m_restRotations[index] = parts[index].localRotation;
                m_restScales[index] = parts[index].localScale;
            }
            m_purgeEcho = _createPurgeEcho();
            _createEffects();
            ResetPresentation();
        }

        public void PlayWake()
        {
            if (!IsConfigured) return;
            ResetPresentation();
            m_previousRootPosition = m_suppressor.position;
            m_wakeRemaining = WAKE_DURATION;
        }

        public void SetFieldState(bool visible, bool active, float radius, Vector3 center)
        {
            if (!IsConfigured) return;
            if (m_fieldActive && (!visible || !active)) m_shutdownRemaining = SHUTDOWN_DURATION;
            if (!m_fieldActive && visible && active) m_activationRemaining = ACTIVATION_DURATION;
            m_fieldVisible = visible;
            m_fieldActive = visible && active;
            m_fieldRadius = Mathf.Max(0.1f, radius);
            m_fieldCenter = center;
            m_fieldCenter.y = 0f;
            if (!m_fieldActive) _setPlayerCaught(false);
        }

        public void PlayHit(Vector3 sourcePosition)
        {
            if (!IsConfigured) return;
            m_hitDirection = m_suppressor.position - sourcePosition;
            m_hitDirection.y = 0f;
            if (m_hitDirection.sqrMagnitude < 0.01f) m_hitDirection = -m_suppressor.forward;
            m_hitDirection.Normalize();
            m_hitRemaining = HIT_DURATION;
        }

        public void PlayPurge()
        {
            if (!IsConfigured) return;
            m_purgeEcho.SetPositionAndRotation(m_suppressor.position, m_suppressor.rotation);
            m_purgeEcho.localScale = m_suppressor.lossyScale;
            m_purgePosition = m_purgeEcho.localPosition;
            m_purgeRotation = m_purgeEcho.localRotation;
            m_purgeScale = m_purgeEcho.localScale;
            var parts = _parts();
            for (var index = 0; index < parts.Length; index++) _copyPose(parts[index], m_purgeEcho.GetChild(index));
            m_purgeEcho.gameObject.SetActive(true);
            m_purgeRemaining = PURGE_DURATION;
            m_purgeEffect.enabled = true;
            m_fieldVisible = false;
            m_fieldActive = false;
            m_playerCaught = false;
            m_stateEffect.enabled = false;
            m_fieldEffect.enabled = false;
            m_responseEffect.enabled = false;
            _resetLivePose();
        }

        public void ResetPresentation()
        {
            m_wakeRemaining = 0f;
            m_hitRemaining = 0f;
            m_purgeRemaining = 0f;
            m_shutdownRemaining = 0f;
            m_activationRemaining = 0f;
            m_exitRemaining = 0f;
            m_fieldVisible = false;
            m_fieldActive = false;
            m_playerCaught = false;
            if (m_purgeEcho != null) m_purgeEcho.gameObject.SetActive(false);
            _show(m_stateEffect, false);
            _show(m_fieldEffect, false);
            _show(m_responseEffect, false);
            _show(m_purgeEffect, false);
            _resetLivePose();
        }

        private void Update()
        {
            if (!IsConfigured) return;
            var dt = Time.deltaTime;
            _tickPurge(dt);
            if (!m_suppressor.gameObject.activeInHierarchy) return;
            m_wakeRemaining = Mathf.Max(0f, m_wakeRemaining - dt);
            m_hitRemaining = Mathf.Max(0f, m_hitRemaining - dt);
            m_shutdownRemaining = Mathf.Max(0f, m_shutdownRemaining - dt);
            m_activationRemaining = Mathf.Max(0f, m_activationRemaining - dt);
            m_exitRemaining = Mathf.Max(0f, m_exitRemaining - dt);
            _setPlayerCaught(IsProjecting && _flatDistance(m_player.position, m_fieldCenter) < m_fieldRadius);

            var rootDelta = m_suppressor.position - m_previousRootPosition;
            rootDelta.y = 0f;
            m_previousRootPosition = m_suppressor.position;
            var movement = dt > 0f ? Mathf.Clamp01(rootDelta.magnitude / (dt * 3f)) : 0f;
            var wakeProgress = m_wakeRemaining > 0f ? 1f - m_wakeRemaining / WAKE_DURATION : 1f;
            var wakeEase = 1f - Mathf.Pow(1f - wakeProgress, 3f);
            var warningPulse = IsWarning ? 0.5f + Mathf.Sin(Time.time * 12f) * 0.5f : 0f;
            var sustainPulse = IsProjecting ? 0.5f + Mathf.Sin(Time.time * 18f) * 0.5f : 0f;
            var shutdownProgress = IsShuttingDown ? 1f - m_shutdownRemaining / SHUTDOWN_DURATION : 1f;
            var shutdownArc = IsShuttingDown ? Mathf.Sin(shutdownProgress * Mathf.PI) : 0f;
            var hit = IsHitReacting ? Mathf.Sin(m_hitRemaining / HIT_DURATION * Mathf.PI) : 0f;
            var localHit = Quaternion.Inverse(m_suppressor.rotation) * m_hitDirection;
            var bank = Mathf.Clamp(rootDelta.x * 10f, -7f, 7f);
            m_chassis.localPosition = m_restPositions[0] + new Vector3(localHit.x * hit * 0.06f,
                Mathf.Lerp(-0.13f, 0f, wakeEase) + movement * 0.025f - shutdownArc * 0.035f,
                localHit.z * hit * 0.06f);
            m_chassis.localRotation = m_restRotations[0] * Quaternion.Euler(movement * -3f + shutdownArc * 8f, 0f,
                -bank - localHit.x * hit * 10f);
            m_chassis.localScale = Vector3.Scale(m_restScales[0], new Vector3(1f, Mathf.Lerp(0.55f, 1f, wakeEase), 1f));
            _poseEmitter(m_leftEmitter, 1, -1f, wakeEase, warningPulse, sustainPulse, shutdownArc);
            _poseEmitter(m_rightEmitter, 2, 1f, wakeEase, warningPulse, sustainPulse, shutdownArc);
            m_core.localPosition = m_restPositions[3] + Vector3.up * (warningPulse * 0.03f + sustainPulse * 0.055f);
            m_core.localRotation = m_restRotations[3] * Quaternion.Euler(0f,
                Time.time * (IsProjecting ? 300f : IsWarning ? 150f : 55f), 0f);
            m_core.localScale = m_restScales[3] * Mathf.Lerp(0.5f, 1f + warningPulse * 0.12f + sustainPulse * 0.2f, wakeEase);
            _updateEffects(wakeProgress, movement, hit);
        }

        private void OnDisable() => ResetPresentation();

        private void OnDestroy()
        {
            if (m_effectMaterial != null) Destroy(m_effectMaterial);
        }

        private void _createEffects()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                m_effectMaterial = new Material(shader)
                {
                    name = "Security Suppressor Effect Material",
                    color = Color.white,
                    mainTexture = Texture2D.whiteTexture,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else Debug.LogWarning("Sprites/Default was unavailable; Suppressor effects will use the authored core material.", this);
            var material = m_effectMaterial != null ? m_effectMaterial : m_core.GetComponent<MeshRenderer>().sharedMaterial;
            m_stateEffect = _createLine("Security Suppressor State Effect", material, 9, 0.045f, false);
            m_fieldEffect = _createLine("Security Suppressor Field Effect", material, 32, 0.045f, true);
            m_responseEffect = _createLine("Security Suppressor Response Effect", material, 9, 0.06f, false);
            m_purgeEffect = _createLine("Security Suppressor Purge Effect", material, 17, 0.055f, false);
        }

        private LineRenderer _createLine(string objectName, Material material, int positions, float width, bool loop)
        {
            var effect = new GameObject(objectName);
            effect.transform.SetParent(transform, false);
            var line = effect.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = positions;
            line.widthMultiplier = width;
            line.loop = loop;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.sortingOrder = 27;
            line.enabled = false;
            return line;
        }

        private void _updateEffects(float wakeProgress, float movement, float hit)
        {
            var reduced = m_comfortSettings != null && m_comfortSettings.ReducedFlashesEnabled;
            var pulse = reduced ? 1f : 0.88f + Mathf.Sin(Time.time * 14f) * 0.12f;
            var wake = m_wakeRemaining > 0f ? Mathf.Sin(wakeProgress * Mathf.PI) : 0f;
            var shutdown = IsShuttingDown ? Mathf.Sin((1f - m_shutdownRemaining / SHUTDOWN_DURATION) * Mathf.PI) : 0f;
            var state = Mathf.Max(wake, Mathf.Max(movement * 0.35f,
                Mathf.Max(IsWarning ? 1f : 0f, Mathf.Max(IsProjecting ? 0.72f : 0f, shutdown))));
            if (state > 0.01f)
            {
                _setBrace(m_stateEffect, m_suppressor.position, m_suppressor.forward, IsWarning, IsProjecting, IsShuttingDown);
                _color(m_stateEffect, IsWarning ? s_forecastAmber : s_roleMagenta,
                    Mathf.Min(reduced ? 0.3f : 0.76f, state * pulse));
                m_stateEffect.enabled = true;
            }
            else m_stateEffect.enabled = false;

            var activation = m_activationRemaining > 0f ? m_activationRemaining / ACTIVATION_DURATION : 0f;
            var field = IsWarning ? 0.72f : IsProjecting ? Mathf.Max(0.48f, activation) : shutdown;
            if (field > 0.01f)
            {
                _setFieldEdge(m_fieldEffect, m_fieldCenter, m_fieldRadius, IsWarning, activation);
                _color(m_fieldEffect, IsWarning ? s_forecastAmber : s_roleMagenta,
                    Mathf.Min(reduced ? 0.3f : 0.68f, field * pulse));
                m_fieldEffect.enabled = true;
            }
            else m_fieldEffect.enabled = false;

            var caught = m_playerCaught ? 1f : 0f;
            var exit = m_exitRemaining > 0f ? m_exitRemaining / EXIT_DURATION : 0f;
            var response = Mathf.Max(hit, Mathf.Max(caught, exit));
            if (response > 0.01f)
            {
                var useHit = hit >= caught && hit >= exit;
                var useExit = !useHit && exit > caught;
                var origin = useHit ? m_suppressor.position + Vector3.up * 0.5f : m_player.position + Vector3.up * 0.22f;
                var direction = useHit ? m_hitDirection : useExit ? m_exitDirection : m_player.position - m_fieldCenter;
                _setResponse(m_responseEffect, origin, direction, useExit);
                _color(m_responseEffect, useExit ? s_exitCyan : s_roleMagenta,
                    Mathf.Min(reduced ? 0.3f : 0.82f, response * pulse));
                m_responseEffect.enabled = true;
            }
            else m_responseEffect.enabled = false;
        }

        private void _setPlayerCaught(bool caught)
        {
            if (caught == m_playerCaught) return;
            if (!caught && m_playerCaught)
            {
                m_exitDirection = m_player.position - m_fieldCenter;
                m_exitDirection.y = 0f;
                if (m_exitDirection.sqrMagnitude < 0.01f) m_exitDirection = Vector3.forward;
                m_exitDirection.Normalize();
                m_exitRemaining = EXIT_DURATION;
            }
            m_playerCaught = caught;
        }

        private static void _setBrace(LineRenderer line, Vector3 center, Vector3 forward, bool warning, bool active, bool shutdown)
        {
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var origin = center + Vector3.up * 0.48f;
            var width = active ? 0.92f : warning ? 0.78f : 0.68f;
            var depth = shutdown ? 0.24f : active ? 0.5f : 0.38f;
            var opening = active ? 0.5f : 0.28f;
            line.SetPosition(0, origin - right * width - forward * depth);
            line.SetPosition(1, origin - right * width + forward * depth);
            line.SetPosition(2, origin - right * opening + forward * depth * 0.45f);
            line.SetPosition(3, origin - right * opening - forward * depth * 0.3f);
            line.SetPosition(4, origin - forward * (shutdown ? 0.55f : 0.18f));
            line.SetPosition(5, origin + right * opening - forward * depth * 0.3f);
            line.SetPosition(6, origin + right * opening + forward * depth * 0.45f);
            line.SetPosition(7, origin + right * width + forward * depth);
            line.SetPosition(8, origin + right * width - forward * depth);
        }

        private static void _setFieldEdge(LineRenderer line, Vector3 center, float radius, bool warning, float activation)
        {
            var edgeRadius = radius * (warning ? 0.94f : Mathf.Lerp(1f, 1.08f, activation));
            center.y = 0.1f;
            for (var index = 0; index < 32; index++)
            {
                var angle = index / 32f * Mathf.PI * 2f;
                var notch = index % 8 == 0 ? 0.9f : 1f;
                line.SetPosition(index, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * edgeRadius * notch);
            }
        }

        private static void _setResponse(LineRenderer line, Vector3 origin, Vector3 direction, bool exiting)
        {
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var radius = 0.54f;
            var lead = exiting ? 0.58f : 0.28f;
            line.SetPosition(0, origin - right * radius - direction * 0.22f);
            line.SetPosition(1, origin - right * radius + direction * 0.22f);
            line.SetPosition(2, origin - right * 0.18f + direction * lead);
            line.SetPosition(3, origin + direction * (lead + 0.24f));
            line.SetPosition(4, origin + right * 0.18f + direction * lead);
            line.SetPosition(5, origin + right * radius + direction * 0.22f);
            line.SetPosition(6, origin + right * radius - direction * 0.22f);
            line.SetPosition(7, origin - direction * (exiting ? 0.12f : 0.32f));
            line.SetPosition(8, origin - right * radius - direction * 0.22f);
        }

        private static void _color(LineRenderer line, Color color, float alpha)
        {
            color.a = alpha;
            line.startColor = color;
            color.a *= 0.45f;
            line.endColor = color;
        }

        private void _poseEmitter(Transform emitter, int index, float side, float wakeEase, float warningPulse,
            float sustainPulse, float shutdownArc)
        {
            var deployment = IsProjecting ? 0.16f : IsWarning ? 0.08f + warningPulse * 0.035f : 0f;
            emitter.localPosition = m_restPositions[index] + new Vector3(side * deployment, Mathf.Lerp(-0.1f, 0f, wakeEase), 0f);
            emitter.localRotation = m_restRotations[index] * Quaternion.Euler(sustainPulse * -5f + shutdownArc * 12f,
                side * (IsProjecting ? 12f : IsWarning ? 5f : 0f), side * deployment * 25f);
            emitter.localScale = Vector3.Scale(m_restScales[index], new Vector3(
                Mathf.Lerp(0.5f, 1f + sustainPulse * 0.08f, wakeEase), Mathf.Lerp(0.5f, 1f, wakeEase), 1f));
        }

        private Transform[] _parts() => new[] { m_chassis, m_leftEmitter, m_rightEmitter, m_core };

        private Transform _createPurgeEcho()
        {
            var echo = new GameObject("Security Suppressor Purge Echo").transform;
            echo.SetParent(transform, false);
            foreach (var source in _parts())
            {
                var part = new GameObject($"{source.name} Echo");
                part.transform.SetParent(echo, false);
                part.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
                renderer.shadowCastingMode = source.GetComponent<MeshRenderer>().shadowCastingMode;
                renderer.receiveShadows = source.GetComponent<MeshRenderer>().receiveShadows;
            }
            echo.gameObject.SetActive(false);
            return echo;
        }

        private void _tickPurge(float dt)
        {
            if (m_purgeRemaining <= 0f || m_purgeEcho == null) return;
            m_purgeRemaining = Mathf.Max(0f, m_purgeRemaining - dt);
            var progress = 1f - m_purgeRemaining / PURGE_DURATION;
            m_purgeEcho.localPosition = m_purgePosition + Vector3.up * progress * 0.16f;
            m_purgeEcho.localRotation = m_purgeRotation * Quaternion.Euler(progress * -28f, progress * 240f, 0f);
            m_purgeEcho.localScale = Vector3.Scale(m_purgeScale,
                new Vector3(1f + progress * 0.55f, 1f - progress * 0.9f, 1f + progress * 0.55f));
            var radius = Mathf.Lerp(0.36f, 1.5f, progress);
            var center = m_purgeEcho.position + Vector3.up * 0.12f;
            for (var index = 0; index < 17; index++)
            {
                var angle = index / 16f * Mathf.PI * 2f;
                m_purgeEffect.SetPosition(index, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
            var reduced = m_comfortSettings != null && m_comfortSettings.ReducedFlashesEnabled;
            _color(m_purgeEffect, s_roleMagenta, Mathf.Min(reduced ? 0.3f : 0.72f, 1f - progress));
            m_purgeEffect.enabled = m_purgeRemaining > 0f;
            if (m_purgeRemaining <= 0f)
            {
                m_purgeEcho.gameObject.SetActive(false);
                m_purgeEffect.enabled = false;
            }
        }

        private static void _copyPose(Transform source, Transform destination)
        {
            destination.localPosition = source.localPosition;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        private void _resetLivePose()
        {
            if (m_chassis == null || m_restPositions == null) return;
            var parts = _parts();
            for (var index = 0; index < parts.Length; index++)
            {
                parts[index].localPosition = m_restPositions[index];
                parts[index].localRotation = m_restRotations[index];
                parts[index].localScale = m_restScales[index];
            }
        }

        private static float _flatDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private static void _show(LineRenderer line, bool visible)
        {
            if (line != null) line.enabled = visible;
        }

        private static float _visibleAlpha(LineRenderer line) => line != null && line.enabled ? line.startColor.a : 0f;
    }
}
