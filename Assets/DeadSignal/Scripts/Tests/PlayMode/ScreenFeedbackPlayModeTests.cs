using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DeadSignal.Application;
using DeadSignal.Combat;
using DeadSignal.Presentation;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class ScreenFeedbackPlayModeTests
    {
        [UnityTest]
        public IEnumerator DirectionalDamageAndCriticalSignal_StayBoundedAndClearOnPause()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var directional = Object.FindFirstObjectByType<DirectionalDamageFeedbackController>(FindObjectsInactive.Include);
            var combatFeedback = Object.FindFirstObjectByType<CombatFeedbackController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(directional, Is.Not.Null);
            Assert.That(combatFeedback, Is.Not.Null);
            Assert.That(game.HasDirectionalDamageIndicator, Is.True);
            Assert.That(directional.IsVisible, Is.False);

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                var player = game.transform.position;
                directional.Play(player + Vector3.right * 4f, player, PlayerDamageFeedbackKind.Security);
                Assert.That(directional.IsVisible, Is.True);
                Assert.That(directional.CurrentDirection.x, Is.GreaterThan(0.5f));
                Assert.That(directional.CurrentAlpha, Is.InRange(0.01f, 0.5f));

                if (!game.IsReducedFlashesEnabled)
                {
                    game.ToggleReducedFlashes();
                }

                directional.Play(player + Vector3.forward * 4f, player, PlayerDamageFeedbackKind.Sapper);
                Assert.That(directional.CurrentAlpha, Is.LessThanOrEqualTo(0.24f));
                combatFeedback.SetPaused(true);
                directional.Tick(0.1f);
                Assert.That(directional.IsVisible, Is.False);
                Assert.That(directional.CurrentAlpha, Is.Zero);
                combatFeedback.SetPaused(false);

                game.DebugSetSignal(20f);
                yield return null;
                Assert.That(game.CurrentSignalReserveState, Is.EqualTo(SignalReserveState.Critical));
                Assert.That(game.LowSignalWarningIntensity, Is.InRange(0.01f, 0.1f),
                    "Reduced Flashes should retain a steady critical edge without exceeding its alpha cap.");
            }
            finally
            {
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.ToggleReducedFlashes();
                }

                combatFeedback.SetPaused(false);
            }
        }
    }
}
