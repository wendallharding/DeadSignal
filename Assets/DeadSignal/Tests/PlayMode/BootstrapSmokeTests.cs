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
            Assert.That(game.HasPauseInsignia, Is.True, "The generated pause-menu insignia should load from Resources.");
            CombatFeedbackController combatFeedback = Object.FindFirstObjectByType<CombatFeedbackController>();
            Assert.That(combatFeedback, Is.Not.Null, "Reflex composition should provide the combat-feedback controller.");
            Assert.That(combatFeedback.HasImpactTexture, Is.True, "The generated impact texture should load from Resources.");

            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Transform player = game.transform.Find("Maintenance Drone");
            Vector3 startingPosition = player.position;
            try
            {
                float signalBeforePause = game.CurrentSignal;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;

                Assert.That(game.IsPaused, Is.True, "Gamepad Menu should pause a running game.");
                Assert.That(Time.timeScale, Is.Zero);
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(game.CurrentSignal, Is.EqualTo(signalBeforePause), "Signal must not drain while paused.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.That(game.IsPaused, Is.False, "Gamepad Menu should resume a paused game.");
                Assert.That(Time.timeScale, Is.EqualTo(1f));

                combatFeedback.PlaySignalImpact(player.position + Vector3.up * 0.5f, false);
                Assert.That(combatFeedback.ActiveImpactCount, Is.EqualTo(1));
                Transform impactBurst = combatFeedback.transform.Find("Combat Impact Burst");
                Assert.That(impactBurst, Is.Not.Null);
                Assert.That(impactBurst.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
                Vector3 cameraFacingDirection = -game.GetComponentInChildren<Camera>().transform.forward;
                Assert.That(Vector3.Dot(impactBurst.forward, cameraFacingDirection), Is.GreaterThan(0.99f),
                    "The impact sprite should face the overhead camera.");
                Assert.That(combatFeedback.IsHitStopped, Is.True, "A combat impact should begin a brief hit-stop.");
                Assert.That(Time.timeScale, Is.Zero);
                yield return new WaitForSecondsRealtime(0.08f);
                Assert.That(combatFeedback.IsHitStopped, Is.False, "Hit-stop should end using real time.");
                Assert.That(Time.timeScale, Is.EqualTo(1f));
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
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;

                Assert.That(game.transform.Find("Security Warden").gameObject.activeSelf, Is.True,
                    "Gamepad west button should activate the nearby tower and awaken security.");
                Transform sapper = game.transform.Find("Signal Sapper");
                Assert.That(sapper.gameObject.activeSelf, Is.True,
                    "Tower activation should awaken the Signal Sapper.");
                Assert.That(telegraphRoot.gameObject.activeSelf, Is.True,
                    "Tower activation should reveal the Sapper-to-tower telegraph.");
                Assert.That(telegraph.IsVisible, Is.True);
                Assert.That(telegraph.IsLatched, Is.False);
                Assert.That(telegraphRoot.Find("Sapper Target Tether"), Is.Not.Null);

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
                Assert.That(telegraph.PulseFlashVisible, Is.True,
                    "A completed drain should trigger the expanding tower-floor flash.");

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
            }
            finally
            {
                Time.timeScale = 1f;
                InputSystem.RemoveDevice(gamepad);
            }
        }
    }
}
