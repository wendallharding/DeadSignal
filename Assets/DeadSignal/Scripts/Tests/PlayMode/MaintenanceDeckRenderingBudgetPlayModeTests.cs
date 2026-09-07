using System.Collections;
using System.Linq;
using DeadSignal.Application;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests.PlayMode
{
    public sealed class MaintenanceDeckRenderingBudgetPlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthoredDeckModules_ReceiveLightingWithoutCastingRedundantShadows()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null);
            var maintenanceDeck = game.transform.Find("Maintenance Deck Modules");
            Assert.That(maintenanceDeck, Is.Not.Null);
            var renderers = maintenanceDeck.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Has.Length.EqualTo(35));
            Assert.That(renderers.All(renderer => renderer.shadowCastingMode == ShadowCastingMode.Off), Is.True);
            Assert.That(renderers.All(renderer => renderer.receiveShadows), Is.True,
                "The deck should retain authored light and shadow response after dropping its caster pass.");
        }
    }
}
