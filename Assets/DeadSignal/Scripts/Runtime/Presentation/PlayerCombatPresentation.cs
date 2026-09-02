using System.Collections.Generic;
using DeadSignal.Combat;
using DeadSignal.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Presentation
{
    /// <summary>Owns short-lived firing, recoil, and dash effects attached to the player drone.</summary>
    public sealed class PlayerCombatPresentation : MonoBehaviour
    {
        private const float DASH_ECHO_DURATION = 0.24f;

        private readonly List<DashEcho> m_dashEchoes = new();

        private Transform m_turret;
        private Transform m_muzzle;
        private Material m_energyMaterial;
        private SignalBoltPresentationTuning m_tuning;
        private IComfortSettings m_comfortSettings;
        private PlayerDronePresentation m_dronePresentation;
        private Transform m_muzzleEffectRoot;
        private ParticleSystem m_muzzleParticles;
        private Light m_muzzleLight;
        private LineRenderer m_launchStreak;
        private float m_recoilRemaining;
        private float m_muzzleEffectRemaining;
        private Vector3 m_recoilDirection;
        private Vector3 m_turretRestPosition;

        public int ActiveDashEchoCount => m_dashEchoes.Count;
        public float RecoilRemaining => m_recoilRemaining;
        public int MuzzleEffectObjectCount => m_muzzleEffectRoot != null ? 1 : 0;
        public bool IsMuzzleLightActive => m_muzzleLight != null && m_muzzleLight.enabled;
        public bool IsLaunchStreakActive => m_launchStreak != null && m_launchStreak.enabled;

        private sealed class DashEcho
        {
            public GameObject Root;
            public LineRenderer Renderer;
            public float Age;
        }

        internal void Configure(
            Transform turret,
            Transform muzzle,
            Material energyMaterial,
            SignalBoltPresentationTuning tuning,
            IComfortSettings comfortSettings,
            PlayerDronePresentation dronePresentation)
        {
            m_turret = turret;
            m_muzzle = muzzle;
            m_energyMaterial = energyMaterial;
            m_tuning = tuning;
            m_comfortSettings = comfortSettings;
            m_dronePresentation = dronePresentation;
            m_turretRestPosition = turret.localPosition;
            _createMuzzleEffects();
        }

        public void PlayShot(Vector3 direction, bool evolved)
        {
            if (m_turret == null || m_muzzle == null || m_tuning == null)
            {
                return;
            }

            direction.y = 0f;
            m_turretRestPosition = m_turret.localPosition;
            m_recoilDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : m_turret.forward;
            m_recoilRemaining = m_tuning.RecoilDuration;
            m_dronePresentation?.PlayFire(evolved);
            _playMuzzleEffects(m_recoilDirection);
        }

        public void PlayDash(Vector3 start, Vector3 end)
        {
            var delta = end - start;
            if (delta.sqrMagnitude < 0.1f || m_energyMaterial == null)
            {
                return;
            }

            m_dronePresentation?.PlayDash();

            var root = new GameObject("Player Dash Afterimage");
            root.transform.SetParent(transform, true);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = m_energyMaterial;
            line.useWorldSpace = true;
            line.positionCount = 4;
            line.SetPositions(new[] { start, Vector3.Lerp(start, end, 0.32f), Vector3.Lerp(start, end, 0.68f), end });
            line.startWidth = 0.52f;
            line.endWidth = 0.04f;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = _effectColor(0.7f);
            line.endColor = _effectColor(0.04f);
            m_dashEchoes.Add(new DashEcho { Root = root, Renderer = line });
            _playDashParticles(start, end);
        }

        private void LateUpdate()
        {
            _updateRecoil();
            _updateMuzzleEffects();
            _updateDashEchoes();
        }

        private void OnDisable()
        {
            if (m_turret != null)
            {
                m_turret.localPosition = m_turretRestPosition;
            }

            m_recoilRemaining = 0f;
            m_muzzleEffectRemaining = 0f;
            if (m_muzzleParticles != null)
            {
                m_muzzleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (m_muzzleLight != null)
            {
                m_muzzleLight.enabled = false;
            }
            if (m_launchStreak != null)
            {
                m_launchStreak.enabled = false;
            }
        }

        private void _updateRecoil()
        {
            if (m_turret == null)
            {
                return;
            }

            if (m_recoilRemaining <= 0f)
            {
                m_turret.localPosition = m_turretRestPosition;
                return;
            }

            m_recoilRemaining = Mathf.Max(0f, m_recoilRemaining - Time.deltaTime);
            var progress = 1f - m_recoilRemaining / m_tuning.RecoilDuration;
            var recoil = Mathf.Sin(progress * Mathf.PI) * m_tuning.RecoilDistance;
            m_turret.localPosition = m_turretRestPosition - m_turret.InverseTransformDirection(m_recoilDirection) * recoil;
        }

        private void _updateMuzzleEffects()
        {
            if (m_tuning == null || m_muzzleEffectRemaining <= 0f)
            {
                if (m_muzzleLight != null)
                {
                    m_muzzleLight.enabled = false;
                }
                if (m_launchStreak != null)
                {
                    m_launchStreak.enabled = false;
                }
                return;
            }

            m_muzzleEffectRemaining = Mathf.Max(0f, m_muzzleEffectRemaining - Time.deltaTime);
            var reducedFlashes = m_comfortSettings?.ReducedFlashesEnabled ?? false;
            var lightProgress = Mathf.Clamp01(m_muzzleEffectRemaining / m_tuning.MuzzleLightDuration);
            m_muzzleLight.enabled = !reducedFlashes && lightProgress > 0f;
            m_muzzleLight.intensity = m_tuning.MuzzleLightIntensity * lightProgress;

            var streakProgress = Mathf.Clamp01(m_muzzleEffectRemaining / m_tuning.LaunchStreakDuration);
            m_launchStreak.enabled = streakProgress > 0f;
            m_launchStreak.startWidth = m_tuning.LaunchStreakWidth * streakProgress;
            m_launchStreak.endWidth = m_tuning.LaunchStreakWidth * 0.14f * streakProgress;
            m_launchStreak.startColor = _effectColor(0.78f * streakProgress);
            m_launchStreak.endColor = _effectColor(0.04f * streakProgress);
        }

        private void _updateDashEchoes()
        {
            for (var index = m_dashEchoes.Count - 1; index >= 0; index--)
            {
                var echo = m_dashEchoes[index];
                echo.Age += Time.deltaTime;
                var progress = Mathf.Clamp01(echo.Age / DASH_ECHO_DURATION);
                echo.Renderer.startColor = _effectColor((1f - progress) * 0.7f);
                echo.Renderer.endColor = _effectColor((1f - progress) * 0.04f);
                echo.Renderer.widthMultiplier = 1f - progress * 0.65f;
                if (progress < 1f)
                {
                    continue;
                }

                Destroy(echo.Root);
                m_dashEchoes.RemoveAt(index);
            }
        }

        private void _createMuzzleEffects()
        {
            if (m_muzzleEffectRoot != null || m_muzzle == null || m_tuning == null)
            {
                return;
            }

            var root = new GameObject("Basic Fire Presentation");
            root.transform.SetParent(transform, true);
            m_muzzleEffectRoot = root.transform;
            m_muzzleParticles = root.AddComponent<ParticleSystem>();
            m_muzzleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = m_muzzleParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = m_tuning.BurstDuration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
            main.startColor = _effectColor(1f);
            main.stopAction = ParticleSystemStopAction.None;
            var shape = m_muzzleParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.025f;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = m_energyMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.11f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            m_muzzleLight = root.AddComponent<Light>();
            m_muzzleLight.type = LightType.Point;
            m_muzzleLight.color = new Color(0.2f, 0.95f, 1f);
            m_muzzleLight.range = m_tuning.MuzzleLightRange;
            m_muzzleLight.intensity = 0f;
            m_muzzleLight.shadows = LightShadows.None;
            m_muzzleLight.enabled = false;

            m_launchStreak = root.AddComponent<LineRenderer>();
            m_launchStreak.sharedMaterial = m_energyMaterial;
            m_launchStreak.useWorldSpace = true;
            m_launchStreak.positionCount = 2;
            m_launchStreak.numCapVertices = 4;
            m_launchStreak.textureMode = LineTextureMode.Stretch;
            m_launchStreak.shadowCastingMode = ShadowCastingMode.Off;
            m_launchStreak.receiveShadows = false;
            m_launchStreak.enabled = false;
        }

        private void _playMuzzleEffects(Vector3 direction)
        {
            if (m_muzzleEffectRoot == null)
            {
                return;
            }

            var origin = m_muzzle.position + direction * 0.13f;
            m_muzzleEffectRoot.position = origin;
            m_muzzleEffectRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
            m_muzzleEffectRemaining = Mathf.Max(m_tuning.LaunchStreakDuration, m_tuning.MuzzleLightDuration);
            m_launchStreak.SetPosition(0, origin - direction * 0.05f);
            m_launchStreak.SetPosition(1, origin + direction * m_tuning.LaunchStreakLength);
            m_launchStreak.enabled = true;
            var reducedFlashes = m_comfortSettings?.ReducedFlashesEnabled ?? false;
            var main = m_muzzleParticles.main;
            main.startColor = _effectColor(1f);
            m_muzzleParticles.Emit(reducedFlashes ? m_tuning.ReducedFlashesParticleCount : m_tuning.BurstParticleCount);
            m_muzzleLight.enabled = !reducedFlashes;
            m_muzzleLight.intensity = reducedFlashes ? 0f : m_tuning.MuzzleLightIntensity;
        }

        private void _playDashParticles(Vector3 start, Vector3 end)
        {
            var root = new GameObject("Dash Wake Particles");
            root.transform.position = Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.14f;
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.12f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.1f);
            main.startColor = _effectColor(0.7f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Mathf.Abs(end.x - start.x) + 0.4f, 0.08f, Mathf.Abs(end.z - start.z) + 0.4f);
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = m_energyMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Emit(m_comfortSettings?.ReducedFlashesEnabled ?? false ? 8 : 16);
            particles.Play();
        }

        private Color _effectColor(float alpha)
        {
            var maximumAlpha = m_comfortSettings?.ReducedFlashesEnabled ?? false ? 0.3f : 1f;
            return new Color(0.25f, 0.95f, 1f, alpha * maximumAlpha);
        }
    }
}
