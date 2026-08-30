using System.Collections.Generic;
using DeadSignal.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadSignal.Presentation
{
    /// <summary>Owns short-lived firing, recoil, and dash effects attached to the player drone.</summary>
    public sealed class PlayerCombatPresentation : MonoBehaviour
    {
        private const float RECOIL_DURATION = 0.11f;
        private const float RECOIL_DISTANCE = 0.16f;
        private const float MUZZLE_LIGHT_DURATION = 0.075f;
        private const float DASH_ECHO_DURATION = 0.24f;

        private readonly List<DashEcho> m_dashEchoes = new();

        private Transform m_turret;
        private Transform m_muzzle;
        private Material m_energyMaterial;
        private IComfortSettings m_comfortSettings;
        private PlayerDronePresentation m_dronePresentation;
        private float m_recoilRemaining;
        private Vector3 m_recoilDirection;
        private Vector3 m_turretRestPosition;

        public int ActiveDashEchoCount => m_dashEchoes.Count;
        public float RecoilRemaining => m_recoilRemaining;

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
            IComfortSettings comfortSettings,
            PlayerDronePresentation dronePresentation)
        {
            m_turret = turret;
            m_muzzle = muzzle;
            m_energyMaterial = energyMaterial;
            m_comfortSettings = comfortSettings;
            m_dronePresentation = dronePresentation;
            m_turretRestPosition = turret.localPosition;
        }

        public void PlayShot(Vector3 direction, bool evolved)
        {
            if (m_turret == null || m_muzzle == null)
            {
                return;
            }

            direction.y = 0f;
            m_turretRestPosition = m_turret.localPosition;
            m_recoilDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : m_turret.forward;
            m_recoilRemaining = RECOIL_DURATION;
            m_dronePresentation?.PlayFire(evolved);
            _playMuzzleParticles(m_recoilDirection);
            if (!(m_comfortSettings?.ReducedFlashesEnabled ?? false))
            {
                _playMuzzleLight();
            }
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
            _updateDashEchoes();
        }

        private void OnDisable()
        {
            if (m_turret != null)
            {
                m_turret.localPosition = m_turretRestPosition;
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
            var progress = 1f - m_recoilRemaining / RECOIL_DURATION;
            var recoil = Mathf.Sin(progress * Mathf.PI) * RECOIL_DISTANCE;
            m_turret.localPosition = m_turretRestPosition - m_turret.InverseTransformDirection(m_recoilDirection) * recoil;
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

        private void _playMuzzleParticles(Vector3 direction)
        {
            var root = new GameObject("Signal Muzzle Burst");
            root.transform.position = m_muzzle.position + direction * 0.16f;
            root.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.08f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
            main.startColor = _effectColor(1f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.025f;
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = m_energyMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.11f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Emit(m_comfortSettings?.ReducedFlashesEnabled ?? false ? 3 : 7);
            particles.Play();
        }

        private void _playMuzzleLight()
        {
            var root = new GameObject("Signal Muzzle Light");
            root.transform.position = m_muzzle.position;
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 0.95f, 1f);
            light.range = 2.8f;
            light.intensity = 3.2f;
            light.shadows = LightShadows.None;
            Destroy(root, MUZZLE_LIGHT_DURATION);
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
