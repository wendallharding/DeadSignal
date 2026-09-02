using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class EvolvedWeaponPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator EvolvedPiercingPulse_ShowsLaunchFlightContinuationAndTerminationWithoutRuleChanges()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var presentation = player.GetComponent<PlayerCombatPresentation>();
            var tuning = Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning");
            var warden = game.transform.Find("Security Warden");
            var sapper = game.transform.Find("Signal Sapper");
            var interceptor = game.transform.Find("Security Interceptor");

            game.DebugActivateRelayTower();
            game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
            game.DebugActivateSpineTower();
            game.DebugSpawnThreat(SecurityReinforcement.Interceptor);
            player.position = new Vector3(22f, 0f, 5f);
            warden.position = new Vector3(24f, 0f, 5f);
            sapper.position = new Vector3(26f, 0f, 5f);
            interceptor.position = new Vector3(28f, 0f, 5f);
            game.DebugFireAt(player.position + Vector3.right * 20f);
            Assert.That(presentation.LastLaunchWeapon, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
            var bolt = game.transform.Cast<Transform>().First(child => child.name == "Signal Bolt");
            var trail = bolt.GetComponent<TrailRenderer>();
            Assert.That(trail.startWidth, Is.EqualTo(tuning.PiercingTrailWidth * tuning.EvolvedTrailMultiplier).Within(0.001f));

            presentation.PlayPiercingContinuation(player.position + Vector3.right * 2f, Vector3.right);
            Assert.That(presentation.PiercingContinuationEffectCount, Is.EqualTo(1));
            Assert.That(presentation.ActiveWeaponPathEffectCount, Is.GreaterThan(0));
            _captureWhenRequested("P43-Piercing-Pulse-1600x900.png", 1600, 900);

            yield return new WaitForSeconds(0.5f);
            Assert.That(presentation.WeaponTerminationEffectCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(presentation.WeaponPathEffectPoolSize, Is.EqualTo(tuning.WeaponEventPoolSize));
            Assert.That(tuning.Speed, Is.EqualTo(13.5f));
            Assert.That(tuning.Lifetime, Is.EqualTo(1.5f));
            Assert.That(tuning.FireCooldown, Is.EqualTo(0.16f));
            Assert.That(tuning.CollisionRadius, Is.EqualTo(0.08f));
        }

        [UnityTest]
        public IEnumerator EvolvedControlledRicochet_ShowsRedirectAndAccessibleTerminationWithoutRuleChanges()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var player = game.transform.Find("Maintenance Drone");
            var presentation = player.GetComponent<PlayerCombatPresentation>();
            var tuning = Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning");
            var warden = game.transform.Find("Security Warden");
            var sapper = game.transform.Find("Signal Sapper");
            var initialReducedFlashes = game.IsReducedFlashesEnabled;

            try
            {
                game.DebugActivateRelayTower();
                game.DebugSelectWeapon(SignalWeaponOverclock.ControlledRicochet);
                game.DebugActivateSpineTower();
                player.position = new Vector3(27.5f, 0f, 5f);
                warden.position = new Vector3(31.5f, 0f, 5f);
                sapper.position = new Vector3(35f, 0f, -5f);

                game.DebugFireAt(player.position + Vector3.forward * 20f);
                Assert.That(presentation.LastLaunchWeapon, Is.EqualTo(SignalWeaponOverclock.ControlledRicochet));
                var bolt = game.transform.Cast<Transform>().First(child => child.name == "Signal Bolt");
                Assert.That(bolt.GetComponent<TrailRenderer>().startWidth,
                    Is.EqualTo(tuning.RicochetTrailWidth * tuning.EvolvedTrailMultiplier).Within(0.001f));

                if (!game.IsReducedFlashesEnabled)
                {
                    game.DebugToggleReducedFlashes();
                }
                presentation.PlayRicochetRedirect(player.position + Vector3.forward, Vector3.forward, Vector3.right);
                Assert.That(presentation.RicochetRedirectEffectCount, Is.EqualTo(1));
                var activeEffect = player.GetComponentsInChildren<LineRenderer>()
                    .First(renderer => renderer.gameObject.name.StartsWith("Weapon Path Effect", StringComparison.Ordinal));
                Assert.That(activeEffect.startColor.a, Is.LessThanOrEqualTo(0.31f));
                Assert.That(activeEffect.endColor.a, Is.LessThanOrEqualTo(0.31f));
                _captureWhenRequested("P43-Controlled-Ricochet-Reduced-Flashes-1280x720.png", 1280, 720);

                yield return new WaitForSeconds(tuning.Lifetime + 0.2f);
                Assert.That(game.ActiveSignalBoltCount, Is.Zero);
                Assert.That(presentation.WeaponTerminationEffectCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(presentation.WeaponPathEffectPoolSize, Is.EqualTo(tuning.WeaponEventPoolSize));
            }
            finally
            {
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.DebugToggleReducedFlashes();
                }
            }
        }

        private static void _captureWhenRequested(string fileName, int width, int height)
        {
            const string ARGUMENT_PREFIX = "-deadSignalEvolvedWeaponCaptureDir=";
            var argument = Environment.GetCommandLineArgs()
                .FirstOrDefault(value => value.StartsWith(ARGUMENT_PREFIX, StringComparison.OrdinalIgnoreCase));
            if (argument == null)
            {
                return;
            }

            var directory = argument.Substring(ARGUMENT_PREFIX.Length).Trim('"');
            Directory.CreateDirectory(directory);
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(candidate => candidate.enabled);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = camera.aspect;
            try
            {
                camera.aspect = (float)width / height;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                var capture = new Texture2D(width, height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply();
                File.WriteAllBytes(Path.Combine(directory, fileName), capture.EncodeToPNG());
                Object.DestroyImmediate(capture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
