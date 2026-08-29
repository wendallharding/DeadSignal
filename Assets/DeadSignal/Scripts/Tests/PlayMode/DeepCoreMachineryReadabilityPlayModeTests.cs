using System.Collections;
using DeadSignal.Application;
using DeadSignal.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class DeepCoreMachineryReadabilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator InductionAndFlux_PresentDistinctFourStateLifecyclesAndReset()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            var induction = Object.FindFirstObjectByType<AuthoredInductionLatticeObjective>(FindObjectsInactive.Include);
            var flux = Object.FindFirstObjectByType<AuthoredFluxShuntObjective>(FindObjectsInactive.Include);
            Assert.That(game, Is.Not.Null);
            Assert.That(induction.HasReadabilityAssets, Is.True);
            Assert.That(flux.HasReadabilityAssets, Is.True);
            Assert.That(induction.PresentationState, Is.EqualTo(InductionLatticePresentationState.PrerequisiteLocked));
            Assert.That(flux.PresentationState, Is.EqualTo(FluxShuntPresentationState.PrerequisiteLocked));

            var inductionGlyph = induction.transform.Find("Induction Lattice Objective/Induction Charge Status");
            var fluxGlyph = flux.transform.Find("Flux Shunt Route Status");
            Assert.That(inductionGlyph, Is.Not.Null);
            Assert.That(fluxGlyph, Is.Not.Null);
            Assert.That(inductionGlyph.GetComponent<Collider>(), Is.Null);
            Assert.That(fluxGlyph.GetComponent<Collider>(), Is.Null);
            Assert.That(Resources.Load<Texture2D>("Environment/DeepCoreMachineryStatusPanel"), Is.Not.Null);
            Assert.That(Resources.Load<Material>("Materials/DeepCoreReadability/DeepCoreMachineryStatus"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/InductionChargeGlyphReadability"), Is.Not.Null);
            Assert.That(Resources.Load<Mesh>("Environment/FluxShuntGlyphReadability"), Is.Not.Null);

            game.DebugActivateSpineTower();
            yield return null;
            Assert.That(induction.PresentationState, Is.EqualTo(InductionLatticePresentationState.ChargeAvailable));
            Assert.That(flux.PresentationState, Is.EqualTo(FluxShuntPresentationState.PrerequisiteLocked));

            game.DebugChargeInductionLattice();
            Assert.That(induction.PresentationState, Is.EqualTo(InductionLatticePresentationState.Charging));
            yield return null;
            Assert.That(flux.PresentationState, Is.EqualTo(FluxShuntPresentationState.RoutingAvailable));
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(induction.PresentationState, Is.EqualTo(InductionLatticePresentationState.Charged));
            Assert.That(Quaternion.Angle(Quaternion.identity, inductionGlyph.localRotation), Is.GreaterThan(100f),
                "Charging should resolve through a radial turn rather than a shunt throw.");

            game.DebugRouteFluxShunt();
            Assert.That(flux.PresentationState, Is.EqualTo(FluxShuntPresentationState.Routing));
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(flux.PresentationState, Is.EqualTo(FluxShuntPresentationState.Routed));
            Assert.That(Quaternion.Angle(Quaternion.identity, fluxGlyph.localRotation), Is.InRange(60f, 75f),
                "Routing should resolve through one bounded lever throw rather than radial charging.");

            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            induction = Object.FindFirstObjectByType<AuthoredInductionLatticeObjective>(FindObjectsInactive.Include);
            flux = Object.FindFirstObjectByType<AuthoredFluxShuntObjective>(FindObjectsInactive.Include);
            Assert.That(induction.PresentationState, Is.EqualTo(InductionLatticePresentationState.PrerequisiteLocked));
            Assert.That(flux.PresentationState, Is.EqualTo(FluxShuntPresentationState.PrerequisiteLocked));
            Assert.That(induction.transform.Find("Induction Lattice Objective/Induction Charge Status").localRotation,
                Is.EqualTo(Quaternion.identity));
            Assert.That(flux.transform.Find("Flux Shunt Route Status").localRotation, Is.EqualTo(Quaternion.identity));
        }
    }
}
