using System.Collections.Generic;
using DeadSignal.Combat;
using DeadSignal.Missions;
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
        private readonly List<WeaponPathEffect> m_weaponPathEffects = new();

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
        private SignalWeaponOverclock m_currentLaunchWeapon;
        private bool m_currentLaunchEvolved;

        public int ActiveDashEchoCount => m_dashEchoes.Count;
        public float RecoilRemaining => m_recoilRemaining;
        public int MuzzleEffectObjectCount => m_muzzleEffectRoot != null ? 1 : 0;
        public bool IsMuzzleLightActive => m_muzzleLight != null && m_muzzleLight.enabled;
        public bool IsLaunchStreakActive => m_launchStreak != null && m_launchStreak.enabled;
        public int WeaponPathEffectPoolSize => m_weaponPathEffects.Count;
        public int ActiveWeaponPathEffectCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < m_weaponPathEffects.Count; index++)
                {
                    if (m_weaponPathEffects[index].IsActive)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
        public SignalWeaponOverclock LastLaunchWeapon { get; private set; }
        public WeaponPathEffectKind LastWeaponPathEffect { get; private set; }
        public int PiercingContinuationEffectCount { get; private set; }
        public int RicochetRedirectEffectCount { get; private set; }
        public int WeaponTerminationEffectCount { get; private set; }

        public enum WeaponPathEffectKind
        {
            None,
            PiercingContinuation,
            PiercingTermination,
            RicochetRedirect,
            RicochetTermination
        }

        private sealed class DashEcho
        {
            public GameObject Root;
            public LineRenderer Renderer;
            public float Age;
        }

        private sealed class WeaponPathEffect
        {
            public GameObject Root;
            public LineRenderer Renderer;
            public float Age;
            public bool IsActive;
            public float StartAlpha;
            public float EndAlpha;
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
            _createWeaponPathEffects();
        }

        public void PlayShot(Vector3 direction, SignalWeaponOverclock weapon, bool evolved)
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
            LastLaunchWeapon = weapon;
            _playMuzzleEffects(m_recoilDirection, weapon, evolved);
        }

        public void ConfigureBolt(GameObject bolt, SignalWeaponOverclock weapon, bool evolved)
        {
            if (bolt == null || m_tuning == null || !bolt.TryGetComponent<TrailRenderer>(out var trail))
            {
                return;
            }

            var widthMultiplier = evolved ? m_tuning.EvolvedTrailMultiplier : 1f;
            switch (weapon)
            {
                case SignalWeaponOverclock.PiercingPulse:
                    trail.startWidth = m_tuning.PiercingTrailWidth * widthMultiplier;
                    trail.startColor = _weaponColor(weapon, evolved, m_tuning.MaximumAlpha);
                    trail.endColor = _weaponColor(weapon, evolved, 0f);
                    _scaleBoltEnergy(bolt, new Vector3(0.78f, 0.78f, evolved ? 1.5f : 1.3f));
                    break;
                case SignalWeaponOverclock.ControlledRicochet:
                    trail.startWidth = m_tuning.RicochetTrailWidth * widthMultiplier;
                    trail.startColor = _weaponColor(weapon, evolved, m_tuning.MaximumAlpha);
                    trail.endColor = _weaponColor(weapon, evolved, 0f);
                    _scaleBoltEnergy(bolt, new Vector3(evolved ? 1.3f : 1.16f, 0.72f, 0.92f));
                    break;
            }
        }

        public void PlayPiercingContinuation(Vector3 position, Vector3 direction) =>
            _playWeaponPathEffect(position, direction, direction, WeaponPathEffectKind.PiercingContinuation);

        public void PlayRicochetRedirect(Vector3 position, Vector3 incomingDirection, Vector3 outgoingDirection) =>
            _playWeaponPathEffect(position, incomingDirection, outgoingDirection, WeaponPathEffectKind.RicochetRedirect);

        public void PlayWeaponTermination(Vector3 position, Vector3 direction, SignalWeaponOverclock weapon)
        {
            var kind = weapon == SignalWeaponOverclock.PiercingPulse
                ? WeaponPathEffectKind.PiercingTermination
                : weapon == SignalWeaponOverclock.ControlledRicochet
                    ? WeaponPathEffectKind.RicochetTermination
                    : WeaponPathEffectKind.None;
            if (kind != WeaponPathEffectKind.None)
            {
                _playWeaponPathEffect(position, direction, -direction, kind);
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
            _updateMuzzleEffects();
            _updateDashEchoes();
            _updateWeaponPathEffects();
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
            for (var index = 0; index < m_weaponPathEffects.Count; index++)
            {
                _deactivateWeaponPathEffect(m_weaponPathEffects[index]);
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
            m_launchStreak.startColor = _weaponColor(m_currentLaunchWeapon, m_currentLaunchEvolved, 0.78f * streakProgress);
            m_launchStreak.endColor = _weaponColor(m_currentLaunchWeapon, m_currentLaunchEvolved, 0.04f * streakProgress);
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

        private void _playMuzzleEffects(Vector3 direction, SignalWeaponOverclock weapon, bool evolved)
        {
            if (m_muzzleEffectRoot == null)
            {
                return;
            }

            var origin = m_muzzle.position + direction * 0.13f;
            m_muzzleEffectRoot.position = origin;
            m_muzzleEffectRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
            m_muzzleEffectRemaining = Mathf.Max(m_tuning.LaunchStreakDuration, m_tuning.MuzzleLightDuration);
            var side = Vector3.Cross(Vector3.up, direction).normalized;
            if (weapon == SignalWeaponOverclock.ControlledRicochet)
            {
                m_launchStreak.positionCount = 3;
                m_launchStreak.SetPosition(0, origin - direction * 0.05f);
                m_launchStreak.SetPosition(1, origin + direction * m_tuning.LaunchStreakLength * 0.48f + side * 0.12f);
                m_launchStreak.SetPosition(2, origin + direction * m_tuning.LaunchStreakLength);
            }
            else
            {
                m_launchStreak.positionCount = 2;
                m_launchStreak.SetPosition(0, origin - direction * 0.05f);
                m_launchStreak.SetPosition(1, origin + direction * m_tuning.LaunchStreakLength *
                    (weapon == SignalWeaponOverclock.PiercingPulse ? 1.35f : 1f));
            }
            var launchColor = _weaponColor(weapon, evolved, 0.78f);
            m_launchStreak.startColor = launchColor;
            m_launchStreak.endColor = _weaponColor(weapon, evolved, 0.04f);
            m_currentLaunchWeapon = weapon;
            m_currentLaunchEvolved = evolved;
            m_launchStreak.enabled = true;
            var reducedFlashes = m_comfortSettings?.ReducedFlashesEnabled ?? false;
            var main = m_muzzleParticles.main;
            main.startColor = _weaponColor(weapon, evolved, 1f);
            m_muzzleParticles.Emit(reducedFlashes ? m_tuning.ReducedFlashesParticleCount : m_tuning.BurstParticleCount);
            var lightColor = _weaponColor(weapon, evolved, 1f);
            lightColor.a = 1f;
            m_muzzleLight.color = lightColor;
            m_muzzleLight.enabled = !reducedFlashes;
            m_muzzleLight.intensity = reducedFlashes ? 0f : m_tuning.MuzzleLightIntensity;
        }

        private void _createWeaponPathEffects()
        {
            if (m_tuning == null || m_weaponPathEffects.Count > 0)
            {
                return;
            }

            for (var index = 0; index < m_tuning.WeaponEventPoolSize; index++)
            {
                var root = new GameObject($"Weapon Path Effect {index + 1:00}");
                root.transform.SetParent(transform, true);
                var line = root.AddComponent<LineRenderer>();
                line.sharedMaterial = m_energyMaterial;
                line.useWorldSpace = true;
                line.numCapVertices = 4;
                line.numCornerVertices = 2;
                line.textureMode = LineTextureMode.Stretch;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                root.SetActive(false);
                m_weaponPathEffects.Add(new WeaponPathEffect { Root = root, Renderer = line });
            }
        }

        private void _playWeaponPathEffect(
            Vector3 position,
            Vector3 incomingDirection,
            Vector3 outgoingDirection,
            WeaponPathEffectKind kind)
        {
            var effect = _acquireWeaponPathEffect();
            if (effect == null)
            {
                return;
            }

            incomingDirection.y = 0f;
            outgoingDirection.y = 0f;
            incomingDirection = incomingDirection.sqrMagnitude > 0.01f ? incomingDirection.normalized : Vector3.forward;
            outgoingDirection = outgoingDirection.sqrMagnitude > 0.01f ? outgoingDirection.normalized : incomingDirection;
            var weapon = kind is WeaponPathEffectKind.PiercingContinuation or WeaponPathEffectKind.PiercingTermination
                ? SignalWeaponOverclock.PiercingPulse
                : SignalWeaponOverclock.ControlledRicochet;
            var halfLength = m_tuning.WeaponEventLength * 0.5f;
            effect.Renderer.positionCount = kind == WeaponPathEffectKind.RicochetRedirect ? 3 : 2;
            effect.Renderer.SetPosition(0, position - incomingDirection * halfLength + Vector3.up * 0.06f);
            if (effect.Renderer.positionCount == 3)
            {
                effect.Renderer.SetPosition(1, position + Vector3.up * 0.06f);
                effect.Renderer.SetPosition(2, position + outgoingDirection * halfLength + Vector3.up * 0.06f);
            }
            else
            {
                effect.Renderer.SetPosition(1, position + outgoingDirection * halfLength + Vector3.up * 0.06f);
            }
            effect.Renderer.startWidth = m_tuning.WeaponEventWidth;
            effect.Renderer.endWidth = kind is WeaponPathEffectKind.PiercingTermination or WeaponPathEffectKind.RicochetTermination
                ? m_tuning.WeaponEventWidth * 1.8f
                : m_tuning.WeaponEventWidth * 0.35f;
            effect.Renderer.startColor = _weaponColor(weapon, true, 0.82f);
            effect.Renderer.endColor = _weaponColor(weapon, true, 0.12f);
            effect.StartAlpha = effect.Renderer.startColor.a;
            effect.EndAlpha = effect.Renderer.endColor.a;
            effect.Age = 0f;
            effect.IsActive = true;
            effect.Root.SetActive(true);
            LastWeaponPathEffect = kind;
            if (kind == WeaponPathEffectKind.PiercingContinuation)
            {
                PiercingContinuationEffectCount++;
            }
            else if (kind == WeaponPathEffectKind.RicochetRedirect)
            {
                RicochetRedirectEffectCount++;
            }
            else
            {
                WeaponTerminationEffectCount++;
            }
        }

        private WeaponPathEffect _acquireWeaponPathEffect()
        {
            for (var index = 0; index < m_weaponPathEffects.Count; index++)
            {
                if (!m_weaponPathEffects[index].IsActive)
                {
                    return m_weaponPathEffects[index];
                }
            }

            return m_weaponPathEffects.Count > 0 ? m_weaponPathEffects[0] : null;
        }

        private void _updateWeaponPathEffects()
        {
            for (var index = 0; index < m_weaponPathEffects.Count; index++)
            {
                var effect = m_weaponPathEffects[index];
                if (!effect.IsActive)
                {
                    continue;
                }

                effect.Age += Time.deltaTime;
                var progress = Mathf.Clamp01(effect.Age / m_tuning.WeaponEventDuration);
                effect.Renderer.widthMultiplier = 1f - progress * 0.72f;
                var start = effect.Renderer.startColor;
                var end = effect.Renderer.endColor;
                start.a = effect.StartAlpha * (1f - progress);
                end.a = effect.EndAlpha * (1f - progress);
                effect.Renderer.startColor = start;
                effect.Renderer.endColor = end;
                if (progress >= 1f)
                {
                    _deactivateWeaponPathEffect(effect);
                }
            }
        }

        private void _deactivateWeaponPathEffect(WeaponPathEffect effect)
        {
            effect.IsActive = false;
            effect.Age = 0f;
            effect.Root.SetActive(false);
        }

        private void _scaleBoltEnergy(GameObject bolt, Vector3 scaleMultiplier)
        {
            var energy = bolt.transform.Find("Bolt Energy");
            if (energy != null)
            {
                energy.localScale = Vector3.Scale(energy.localScale, scaleMultiplier);
            }
        }

        private Color _weaponColor(SignalWeaponOverclock weapon, bool evolved, float alpha)
        {
            var color = weapon == SignalWeaponOverclock.ControlledRicochet
                ? new Color(1f, 0.62f, 0.16f)
                : weapon == SignalWeaponOverclock.PiercingPulse
                    ? new Color(0.35f, 0.92f, 1f)
                    : new Color(0.25f, 0.95f, 1f);
            if (evolved)
            {
                color = Color.Lerp(color, Color.white, 0.2f);
            }
            color.a = _effectAlpha(alpha);
            return color;
        }

        private float _effectAlpha(float alpha) =>
            Mathf.Min(alpha, m_comfortSettings?.ReducedFlashesEnabled ?? false ? 0.3f : 1f);

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
