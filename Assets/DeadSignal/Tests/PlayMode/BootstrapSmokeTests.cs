using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class BootstrapSmokeTests
    {
        [UnityTest]
        public IEnumerator SceneLoad_BootstrapsCompleteRuntimeWithoutErrors()
        {
            yield return null;

            DeadSignalGame game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null, "Runtime bootstrap did not create the game controller.");
            Assert.That(Object.FindFirstObjectByType<DeadSignalHud>(), Is.Not.Null,
                "Runtime bootstrap should compose a dedicated HUD presenter.");
            Assert.That(Object.FindFirstObjectByType<ObjectiveBeaconHud>(), Is.Not.Null,
                "Runtime bootstrap should compose a dedicated objective beacon presenter.");
            Assert.That(game.transform.Find("Maintenance Drone"), Is.Not.Null);
            Assert.That(game.transform.Find("Security Warden"), Is.Not.Null, "Dormant security should exist before tower activation.");
            Assert.That(game.transform.Find("Signal Sapper"), Is.Not.Null, "Dormant sapper should exist before tower activation.");
            Assert.That(game.transform.Find("Signal Sapper").gameObject.activeSelf, Is.False);
            Transform telegraphRoot = game.transform.Find("Sapper Drain Telegraph");
            Assert.That(telegraphRoot, Is.Not.Null, "The Sapper telegraph should be constructed with the runtime arena.");
            SignalSapperTelegraph telegraph = telegraphRoot.GetComponent<SignalSapperTelegraph>();
            Assert.That(telegraphRoot.gameObject.activeSelf, Is.False, "The Sapper telegraph should remain hidden while dormant.");
            Assert.That(telegraph.IsVisible, Is.False);
            Assert.That(game.transform.Find("Tower Power Territory"), Is.Not.Null);
            Assert.That(game.transform.Find("Extraction Beacon"), Is.Not.Null);
            Assert.That(game.transform.Find("Signal Shortcut Gate"), Is.Not.Null);
            Assert.That(Camera.main != null || Object.FindFirstObjectByType<Camera>() != null, Is.True);
            Assert.That(Object.FindFirstObjectByType<AudioListener>(), Is.Not.Null,
                "The runtime camera should provide the listener required by the synthesized soundscape.");
            Assert.That(game.HasPauseInsignia, Is.True, "The generated pause-menu insignia should load from Resources.");
            Assert.That(game.HasCameraComfortIcon, Is.True, "The generated Steady Camera icon should load from Resources.");
            Assert.That(game.HasReducedFlashesIcon, Is.True, "The generated Reduced Flashes icon should load from Resources.");
            Assert.That(game.HasHighContrastIcon, Is.True, "The generated High Contrast icon should load from Resources.");
            Assert.That(game.HasObjectiveBeaconIcon, Is.True, "The generated objective beacon icon should load from Resources.");
            Assert.That(game.HasInputLinkIcon, Is.True, "The generated input-link icon should load from Resources.");
            Assert.That(game.HasAudioLinkIcon, Is.True, "The generated audio-link icon should load from Resources.");
            Assert.That(game.HasGeneratedAudio, Is.True, "The runtime audio service should synthesize ambience and cue clips.");
            Assert.That(game.ActiveInputPromptDevice, Is.EqualTo(InputPromptDevice.KeyboardMouse),
                "A fresh run should begin with keyboard-and-mouse guidance until controller input is received.");
            Assert.That(game.CurrentObjectiveBeaconPhase, Is.EqualTo(ObjectiveBeaconPhase.Tower));
            Assert.That(game.CurrentObjectiveBeaconTarget, Is.EqualTo(new Vector3(-0.6f, 0f, 0.4f)));
            CombatFeedbackController combatFeedback = Object.FindFirstObjectByType<CombatFeedbackController>();
            Assert.That(combatFeedback, Is.Not.Null, "Reflex composition should provide the combat-feedback controller.");
            Assert.That(combatFeedback.HasImpactTexture, Is.True, "The generated impact texture should load from Resources.");
            DeadSignalAudio audio = Object.FindFirstObjectByType<DeadSignalAudio>();
            Assert.That(audio, Is.Not.Null, "Reflex composition should provide the adaptive audio controller.");
            Assert.That(audio.HasGeneratedClips, Is.True);

            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Transform player = game.transform.Find("Maintenance Drone");
            Vector3 startingPosition = player.position;
            bool hadCameraImpulsePreference = PlayerPrefs.HasKey("DeadSignal.CameraImpulseEnabled");
            bool initialCameraImpulse = game.IsCameraImpulseEnabled;
            bool hadReducedFlashesPreference = PlayerPrefs.HasKey("DeadSignal.ReducedFlashesEnabled");
            bool initialReducedFlashes = game.IsReducedFlashesEnabled;
            bool hadHighContrastPreference = PlayerPrefs.HasKey("DeadSignal.HighContrastEnabled");
            bool initialHighContrast = game.IsHighContrastEnabled;
            bool hadAudioPreference = PlayerPrefs.HasKey("DeadSignal.AudioEnabled");
            bool initialAudio = game.IsAudioEnabled;
            try
            {
                float signalBeforePause = game.CurrentSignal;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;

                Assert.That(game.IsPaused, Is.True, "Gamepad Menu should pause a running game.");
                Assert.That(game.ActiveInputPromptDevice, Is.EqualTo(InputPromptDevice.Gamepad),
                    "A meaningful controller action should immediately switch every adaptive prompt to gamepad guidance.");
                Assert.That(Time.timeScale, Is.Zero);
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(game.CurrentSignal, Is.EqualTo(signalBeforePause), "Signal must not drain while paused.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadLeft));
                yield return null;
                Assert.That(game.IsAudioEnabled, Is.EqualTo(!initialAudio),
                    "Gamepad d-pad left should toggle Signal Audio while paused.");
                Assert.That(audio.AudioEnabled, Is.EqualTo(game.IsAudioEnabled),
                    "The Reflex-composed audio service should share the persisted presentation setting.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.AudioEnabled", -1), Is.EqualTo(game.IsAudioEnabled ? 1 : 0),
                    "The audio choice should persist for future runs.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleAudio();
                Assert.That(game.IsAudioEnabled, Is.EqualTo(initialAudio),
                    "The pause option should restore its prior audio state.");
                if (!game.IsAudioEnabled)
                {
                    game.ToggleAudio();
                }

                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
                yield return null;
                Assert.That(game.IsCameraImpulseEnabled, Is.EqualTo(!initialCameraImpulse),
                    "Gamepad north should toggle the Steady Camera option while paused.");
                Assert.That(combatFeedback.CameraImpulseEnabled, Is.EqualTo(game.IsCameraImpulseEnabled),
                    "Reflex-composed combat feedback should share the comfort setting.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.CameraImpulseEnabled", -1),
                    Is.EqualTo(game.IsCameraImpulseEnabled ? 1 : 0), "The comfort choice should persist for future runs.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleCameraImpulse();
                Assert.That(game.IsCameraImpulseEnabled, Is.EqualTo(initialCameraImpulse),
                    "The pause option should restore its prior camera-impulse state.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return null;
                Assert.That(game.IsReducedFlashesEnabled, Is.EqualTo(!initialReducedFlashes),
                    "Gamepad d-pad down should toggle Reduced Flashes while paused.");
                Assert.That(combatFeedback.ReducedFlashesEnabled, Is.EqualTo(game.IsReducedFlashesEnabled),
                    "Reflex-composed combat feedback should share the reduced-flashes setting.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.ReducedFlashesEnabled", -1),
                    Is.EqualTo(game.IsReducedFlashesEnabled ? 1 : 0), "The reduced-flashes choice should persist for future runs.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleReducedFlashes();
                Assert.That(game.IsReducedFlashesEnabled, Is.EqualTo(initialReducedFlashes),
                    "The pause option should restore its prior reduced-flashes state.");

                Transform salvageCase = game.transform.Find("Salvage Cache/Salvage Case");
                Assert.That(salvageCase, Is.Not.Null);
                Color initialSalvageColor = salvageCase.GetComponent<Renderer>().sharedMaterial.color;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadUp));
                yield return null;
                Assert.That(game.IsHighContrastEnabled, Is.EqualTo(!initialHighContrast),
                    "Gamepad d-pad up should toggle High Contrast while paused.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.HighContrastEnabled", -1),
                    Is.EqualTo(game.IsHighContrastEnabled ? 1 : 0), "The high-contrast choice should persist for future runs.");
                Assert.That(salvageCase.GetComponent<Renderer>().sharedMaterial.color, Is.Not.EqualTo(initialSalvageColor),
                    "High Contrast should immediately remap shared world materials while paused.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleHighContrast();
                Assert.That(game.IsHighContrastEnabled, Is.EqualTo(initialHighContrast),
                    "The pause option should restore its prior high-contrast state.");

                if (!game.IsHighContrastEnabled)
                {
                    game.ToggleHighContrast();
                }

                Assert.That(game.IsHighContrastEnabled, Is.True,
                    "High Contrast should remain active for restart-persistence validation.");

                if (!game.IsReducedFlashesEnabled)
                {
                    game.ToggleReducedFlashes();
                }

                Assert.That(game.IsReducedFlashesEnabled, Is.True,
                    "Reduced Flashes should be active for presentation validation.");

                if (game.IsCameraImpulseEnabled)
                {
                    game.ToggleCameraImpulse();
                }

                Assert.That(game.IsCameraImpulseEnabled, Is.False,
                    "The camera-impulse suppression path should be active for feedback validation.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.That(game.IsPaused, Is.False, "Gamepad Menu should resume a paused game.");
                Assert.That(Time.timeScale, Is.EqualTo(1f));

                Camera gameCamera = game.GetComponentInChildren<Camera>();
                Vector3 cameraRestPosition = gameCamera.transform.position;
                combatFeedback.PlaySignalImpact(player.position + Vector3.up * 0.5f, false);
                Assert.That(combatFeedback.ActiveImpactCount, Is.EqualTo(1));
                Transform impactBurst = combatFeedback.transform.Find("Combat Impact Burst");
                Assert.That(impactBurst, Is.Not.Null);
                Assert.That(impactBurst.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
                Assert.That(impactBurst.GetComponent<SpriteRenderer>().color.a, Is.LessThanOrEqualTo(0.3f),
                    "Reduced Flashes should cap combat-burst opacity without removing the hit confirmation.");
                Vector3 cameraFacingDirection = -game.GetComponentInChildren<Camera>().transform.forward;
                Assert.That(Vector3.Dot(impactBurst.forward, cameraFacingDirection), Is.GreaterThan(0.99f),
                    "The impact sprite should face the overhead camera.");
                Assert.That(combatFeedback.IsHitStopped, Is.True, "A combat impact should begin a brief hit-stop.");
                Assert.That(Time.timeScale, Is.Zero);
                yield return new WaitForSecondsRealtime(0.08f);
                Assert.That(combatFeedback.IsHitStopped, Is.False, "Hit-stop should end using real time.");
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(gameCamera.transform.position, Is.EqualTo(cameraRestPosition),
                    "Steady Camera should suppress camera impulse without removing hit-stop or impact art.");
                Assert.That(combatFeedback.ActiveImpactCount, Is.EqualTo(1),
                    "The burst should remain long enough to read after hit-stop ends.");
                yield return new WaitForSeconds(0.3f);
                Assert.That(combatFeedback.ActiveImpactCount, Is.Zero, "Finished impact bursts should clean themselves up.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState
                {
                    leftStick = Vector2.right,
                    rightStick = Vector2.up
                });
                yield return null;

                Assert.That(player.position.x, Is.GreaterThan(startingPosition.x), "Left stick should move the drone.");
                Assert.That(Vector3.Dot(player.forward, Vector3.forward), Is.GreaterThan(0.9f), "Right stick should aim the drone.");

                player.position = new Vector3(-0.6f, 0f, 0.4f);
                int cuesBeforeTower = audio.PlayedCueCount;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;

                Assert.That(game.transform.Find("Security Warden").gameObject.activeSelf, Is.True,
                    "Gamepad west button should activate the nearby tower and awaken security.");
                Assert.That(audio.PlayedCueCount, Is.GreaterThan(cuesBeforeTower),
                    "Tower activation should produce an audible state-change cue when audio is enabled.");
                Assert.That(audio.PoweredVolume, Is.GreaterThan(0f),
                    "The powered network layer should remain active after the tower comes online.");
                Transform sapper = game.transform.Find("Signal Sapper");
                Assert.That(sapper.gameObject.activeSelf, Is.True,
                    "Tower activation should awaken the Signal Sapper.");
                Assert.That(telegraphRoot.gameObject.activeSelf, Is.True,
                    "Tower activation should reveal the Sapper-to-tower telegraph.");
                Assert.That(telegraph.IsVisible, Is.True);
                Assert.That(telegraph.IsLatched, Is.False);
                Assert.That(telegraphRoot.Find("Sapper Target Tether"), Is.Not.Null);
                yield return null;

                Assert.That(game.CurrentObjectiveBeaconPhase, Is.EqualTo(ObjectiveBeaconPhase.Salvage),
                    "Tower activation should advance guidance to the nearest live salvage cache.");
                var salvageTarget = game.CurrentObjectiveBeaconTarget;
                Assert.That(new Vector2(salvageTarget.x, salvageTarget.z), Is.EqualTo(new Vector2(-5.8f, 7.2f)),
                    "The beacon should select the closest remaining cache from the tower.");

                float signalBeforePulse = game.CurrentSignal;
                sapper.position = new Vector3(-0.6f, 0f, 0.4f);
                yield return null;

                Assert.That(game.IsSapperLatched, Is.True, "The sapper should latch onto the powered tower.");
                Assert.That(telegraph.IsLatched, Is.True, "The telegraph should switch to its countdown presentation when latched.");
                float initialCountdown = telegraph.DisplayedCountdown;
                Assert.That(initialCountdown, Is.GreaterThan(1f));
                yield return new WaitForSeconds(0.2f);
                Assert.That(telegraph.DisplayedCountdown, Is.LessThan(initialCountdown),
                    "The displayed drain countdown should decrease with the gameplay pulse timer.");

                float pulseTimeout = 2f;
                while (game.CurrentSignal > signalBeforePulse - RunModel.SapperPulseCost && pulseTimeout > 0f)
                {
                    pulseTimeout -= Time.deltaTime;
                    yield return null;
                }

                Assert.That(game.CurrentSignal, Is.LessThanOrEqualTo(signalBeforePulse - RunModel.SapperPulseCost),
                    "A latched sapper should pulse-drain Signal until destroyed.");
                Assert.That(telegraph.PulseFlashVisible, Is.False,
                    "Reduced Flashes should suppress the expanding floor flash while preserving the countdown.");

                sapper.position = player.position + Vector3.forward * 2f;
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.up });
                yield return new WaitForSeconds(0.22f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.up });
                yield return new WaitForSeconds(0.22f);

                Assert.That(sapper.gameObject.activeSelf, Is.False,
                    "Two Signal bolts should purge the two-health Sapper.");
                Assert.That(telegraphRoot.gameObject.activeSelf, Is.False,
                    "Purging the Sapper should immediately hide every telegraph element.");
                Assert.That(telegraph.IsVisible, Is.False);

                foreach (Transform child in game.transform)
                {
                    if (child.name != "Salvage Cache" || !child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    player.position = child.position;
                    yield return null;
                }

                yield return null;
                Assert.That(game.CurrentObjectiveBeaconPhase, Is.EqualTo(ObjectiveBeaconPhase.Extraction),
                    "Securing every cache should advance guidance to extraction.");
                Assert.That(game.CurrentObjectiveBeaconTarget, Is.EqualTo(new Vector3(-9.2f, 0f, -5.6f)));

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                player.position = new Vector3(3f, 0f, 0.4f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
                for (int frame = 0; frame < 15; frame++)
                {
                    yield return null;
                }

                Assert.That(player.position.x, Is.LessThan(3.35f),
                    "The closed shortcut gate should block the drone at the central bulkhead.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;

                Assert.That(game.transform.Find("Signal Shortcut Gate").gameObject.activeSelf, Is.False,
                    "Gamepad west button should spend Signal and retract the powered shortcut gate.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
                for (int frame = 0; frame < 30; frame++)
                {
                    yield return null;
                }

                Assert.That(player.position.x, Is.GreaterThan(4.35f),
                    "The retracted shortcut gate should allow the drone through the bulkhead.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                player.position = new Vector3(-9.2f, 0f, -5.6f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;

                int completedRunInstanceId = game.GetInstanceID();
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;

                DeadSignalGame restartedGame = Object.FindFirstObjectByType<DeadSignalGame>();
                Assert.That(restartedGame, Is.Not.Null,
                    "Restarting a completed run should bootstrap a fresh playable runtime after the scene reloads.");
                Assert.That(restartedGame.GetInstanceID(), Is.Not.EqualTo(completedRunInstanceId));
                Assert.That(restartedGame.transform.Find("Maintenance Drone"), Is.Not.Null);
                Assert.That(restartedGame.IsHighContrastEnabled, Is.True,
                    "A restarted run should apply the persisted high-contrast palette during construction.");
                Assert.That(restartedGame.IsAudioEnabled, Is.True,
                    "A restarted run should apply the persisted Signal Audio choice during construction.");
            }
            finally
            {
                Time.timeScale = 1f;
                if (hadCameraImpulsePreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.CameraImpulseEnabled", initialCameraImpulse ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.CameraImpulseEnabled");
                }

                if (hadReducedFlashesPreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.ReducedFlashesEnabled", initialReducedFlashes ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.ReducedFlashesEnabled");
                }

                if (hadHighContrastPreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.HighContrastEnabled", initialHighContrast ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.HighContrastEnabled");
                }

                if (hadAudioPreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.AudioEnabled", initialAudio ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.AudioEnabled");
                }

                PlayerPrefs.Save();
                InputSystem.RemoveDevice(gamepad);
            }
        }
    }
}
