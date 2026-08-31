using System;
using System.Collections;
using System.IO;
using System.Linq;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using DeadSignal.Presentation;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class ActOneBranchLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator ActOneBranchLighting_DistinguishesVerbsRelightsCompletionAndStaysWithinBudget()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var tuning = Resources.Load<EnvironmentLightingTuning>("Tuning/EnvironmentLightingTuning");
            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var scene = Object.FindFirstObjectByType<DeadSignalSceneReferences>();
            var cargo = Object.FindFirstObjectByType<AuthoredCargoAnnexObjective>(FindObjectsInactive.Include);
            var coolant = Object.FindFirstObjectByType<AuthoredCoolantReclamationObjective>(FindObjectsInactive.Include);
            var relay = Object.FindFirstObjectByType<AuthoredRelayForkObjective>(FindObjectsInactive.Include);
            var transfer = Object.FindFirstObjectByType<AuthoredTransferVaultObjective>(FindObjectsInactive.Include);

            Assert.That(tuning, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(scene, Is.Not.Null);
            Assert.That(cargo, Is.Not.Null);
            Assert.That(coolant, Is.Not.Null);
            Assert.That(relay, Is.Not.Null);
            Assert.That(transfer, Is.Not.Null);

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            var initialHighContrast = game.IsHighContrastEnabled;
            if (initialReducedFlashes)
            {
                game.DebugToggleReducedFlashes();
            }
            if (initialHighContrast)
            {
                game.DebugToggleHighContrast();
            }

            var branchProfiles = tuning.LandmarkLights.Where(profile =>
                profile.PowerSource is EnvironmentLightPowerSource.CargoCoupling or
                    EnvironmentLightPowerSource.CoolantSeal or
                    EnvironmentLightPowerSource.RelayFeeds or
                    EnvironmentLightPowerSource.TransferAssembly).ToArray();
            var branchLights = branchProfiles.ToDictionary(
                profile => profile.PowerSource,
                profile => GameObject.Find(profile.Name).GetComponent<Light>());
            Assert.That(branchLights.Values, Has.All.Not.Null);
            Assert.That(branchLights[EnvironmentLightPowerSource.CargoCoupling].type, Is.EqualTo(LightType.Spot));
            Assert.That(branchLights[EnvironmentLightPowerSource.CoolantSeal].type, Is.EqualTo(LightType.Point));
            Assert.That(branchLights[EnvironmentLightPowerSource.RelayFeeds].type, Is.EqualTo(LightType.Spot));
            Assert.That(branchLights[EnvironmentLightPowerSource.TransferAssembly].type, Is.EqualTo(LightType.Point));
            Assert.That(branchProfiles.Select(profile => profile.Color.maxColorComponent), Is.Unique,
                "Act I rooms must remain value-distinct without relying on hue alone.");

            game.DebugActivateTower();
            scene.Player.position = cargo.CommitmentPosition;
            yield return null;
            var cargoProfile = branchProfiles.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.CargoCoupling);
            var cargoLight = branchLights[EnvironmentLightPowerSource.CargoCoupling];
            Assert.That(cargoLight.enabled, Is.True);
            Assert.That(cargoLight.color, Is.EqualTo(cargoProfile.GetColor(false)));
            yield return new WaitForSecondsRealtime(0.6f);
            _captureIfRequested(scene.PlayerCamera, "P17-Cargo-Available-1600x900.png", 1600, 900);

            scene.Player.position = cargo.CouplingPosition;
            yield return null;
            scene.Player.position = cargo.WithdrawalPosition;
            yield return null;
            Assert.That(cargo.PresentationState, Is.EqualTo(CargoAnnexPresentationState.Secured));
            yield return null;
            Assert.That(cargoLight.color, Is.EqualTo(cargoProfile.GetColor(true)));
            Assert.That(cargoLight.intensity, Is.GreaterThan(cargoProfile.GetIntensity(false)));
            yield return new WaitForSecondsRealtime(0.6f);
            _captureIfRequested(scene.PlayerCamera, "P17-Cargo-Secured-1280x720.png", 1280, 720);
            _captureIfRequested(scene.PlayerCamera, "P17-Cargo-Secured-1600x900.png", 1600, 900);

            scene.Player.position = coolant.FirstBafflePosition;
            yield return null;
            scene.Player.position = coolant.SecondBafflePosition;
            yield return null;
            scene.Player.position = coolant.SealPosition;
            yield return null;
            scene.Player.position = coolant.ReleasePosition;
            yield return null;
            var coolantProfile = branchProfiles.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.CoolantSeal);
            var coolantLight = branchLights[EnvironmentLightPowerSource.CoolantSeal];
            Assert.That(coolant.PresentationState, Is.EqualTo(CoolantReclamationPresentationState.Stable));
            yield return null;
            Assert.That(coolantLight.enabled, Is.True);
            Assert.That(coolantLight.color, Is.EqualTo(coolantProfile.GetColor(true)));
            yield return new WaitForSecondsRealtime(0.6f);
            _captureIfRequested(scene.PlayerCamera, "P17-Coolant-Stable-1600x900.png", 1600, 900);

            game.DebugTeleport(DebugLocation.RelayFork);
            game.DebugRouteCentralComponents();
            yield return null;
            var relayProfile = branchProfiles.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.RelayFeeds);
            var relayLight = branchLights[EnvironmentLightPowerSource.RelayFeeds];
            Assert.That(relay.PresentationState,
                Is.EqualTo(RelayForkPresentationState.Routing).Or.EqualTo(RelayForkPresentationState.Routed));
            yield return null;
            Assert.That(relayLight.enabled, Is.True);
            Assert.That(relayLight.color, Is.EqualTo(relayProfile.GetColor(true)));
            yield return new WaitForSecondsRealtime(0.6f);
            _captureIfRequested(scene.PlayerCamera, "P17-Relay-Routed-1600x900.png", 1600, 900);

            game.DebugTeleport(DebugLocation.TransferVault);
            game.DebugAssembleCentralPayload();
            yield return null;
            var transferProfile = branchProfiles.Single(profile =>
                profile.PowerSource == EnvironmentLightPowerSource.TransferAssembly);
            var transferLight = branchLights[EnvironmentLightPowerSource.TransferAssembly];
            Assert.That(transfer.PresentationState,
                Is.EqualTo(TransferVaultPresentationState.Processing).Or.EqualTo(TransferVaultPresentationState.Assembled));
            yield return null;
            Assert.That(transferLight.enabled, Is.True);
            Assert.That(transferLight.color, Is.EqualTo(transferProfile.GetColor(true)));

            if (!game.IsReducedFlashesEnabled)
            {
                game.DebugToggleReducedFlashes();
            }
            if (!game.IsHighContrastEnabled)
            {
                game.DebugToggleHighContrast();
            }
            yield return new WaitForSecondsRealtime(0.6f);
            _captureIfRequested(scene.PlayerCamera, "P17-Transfer-Assembled-Accessible-1600x900.png", 1600, 900);
            if (!initialHighContrast)
            {
                game.DebugToggleHighContrast();
            }
            if (!initialReducedFlashes)
            {
                game.DebugToggleReducedFlashes();
            }

            var enabledLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(light => light.enabled).ToArray();
            Assert.That(enabledLights, Has.Length.LessThanOrEqualTo(tuning.MaximumVisibleRealtimeLights));
            Assert.That(enabledLights.Count(light => light.shadows != LightShadows.None),
                Is.LessThanOrEqualTo(tuning.MaximumShadowedRealtimeLights));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(138));
        }

        private static void _captureIfRequested(Camera camera, string fileName, int width, int height)
        {
            var captureDirectory = Environment.GetEnvironmentVariable("DEAD_SIGNAL_P17_CAPTURE_DIR");
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                return;
            }

            Directory.CreateDirectory(captureDirectory);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, fileName), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
