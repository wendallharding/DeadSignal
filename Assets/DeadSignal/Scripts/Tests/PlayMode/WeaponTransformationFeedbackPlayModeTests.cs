using System.Collections;
using DeadSignal.Application;
using DeadSignal.Missions;
using DeadSignal.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadSignal.Tests
{
    public sealed class WeaponTransformationFeedbackPlayModeTests
    {
        [UnityTest]
        public IEnumerator ControlledRicochet_UsesItsOwnSelectionAndEvolutionGlyph()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<WeaponTransformationFeedbackController>();

            game.DebugSelectWeapon(SignalWeaponOverclock.ControlledRicochet);
            Assert.That(game.SelectedWeaponOverclock, Is.EqualTo(SignalWeaponOverclock.ControlledRicochet));
            Assert.That(feedback.LastWeapon, Is.EqualTo(SignalWeaponOverclock.ControlledRicochet));
            Assert.That(feedback.LastTexture.name, Is.EqualTo("ControlledRicochetTransformationGlyph"));
            Assert.That(feedback.ActiveCount, Is.EqualTo(1));

            game.DebugActivateSpineTower();
            Assert.That(game.IsWeaponEvolved, Is.True);
            Assert.That(feedback.LastWasEvolution, Is.True);
            Assert.That(feedback.ActiveCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator WeaponSelectionAndEvolution_UseDistinctBoundedAccessibleFeedback()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var feedback = Object.FindFirstObjectByType<WeaponTransformationFeedbackController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.HasTextures, Is.True);
            Assert.That(feedback.PoolSize, Is.EqualTo(2));

            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            try
            {
                if (!game.IsReducedFlashesEnabled)
                {
                    game.ToggleReducedFlashes();
                }

                feedback.Play(game.TowerPosition, SignalWeaponOverclock.ControlledRicochet, false);
                var ricochetTexture = feedback.LastTexture;
                feedback.SetPaused(true);
                feedback.SetPaused(false);

                game.DebugSelectWeapon(SignalWeaponOverclock.PiercingPulse);
                Assert.That(game.SelectedWeaponOverclock, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
                Assert.That(feedback.LastWeapon, Is.EqualTo(SignalWeaponOverclock.PiercingPulse));
                Assert.That(feedback.LastTexture, Is.Not.SameAs(ricochetTexture));
                Assert.That(feedback.LastWasEvolution, Is.False);
                Assert.That(feedback.ActiveCount, Is.EqualTo(1));
                yield return null;
                Assert.That(feedback.CurrentAlpha, Is.InRange(0.01f, 0.28f));

                game.DebugActivateSpineTower();
                Assert.That(game.IsWeaponEvolved, Is.True);
                Assert.That(feedback.LastWasEvolution, Is.True);
                Assert.That(feedback.ActiveCount, Is.EqualTo(2));
                Assert.That(feedback.PlayCount, Is.EqualTo(3));

                feedback.SetPaused(true);
                Assert.That(feedback.ActiveCount, Is.Zero);
                Assert.That(feedback.CurrentAlpha, Is.Zero);
            }
            finally
            {
                feedback.SetPaused(false);
                if (game.IsReducedFlashesEnabled != initialReducedFlashes)
                {
                    game.ToggleReducedFlashes();
                }
            }
        }
    }
}
