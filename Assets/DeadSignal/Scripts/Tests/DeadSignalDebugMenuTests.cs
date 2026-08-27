using NUnit.Framework;
using DeadSignal.Application;
using DeadSignal.Diagnostics;
using System;

namespace DeadSignal.Tests
{
    public sealed class DeadSignalDebugMenuTests
    {
        [Test]
        public void Availability_AllowsEditorAndDevelopmentBuilds()
        {
            Assert.That(DeadSignalDebugMenu.IsAllowed(true, false), Is.True);
            Assert.That(DeadSignalDebugMenu.IsAllowed(false, true), Is.True);
        }

        [Test]
        public void Availability_RejectsNonDevelopmentPlayerBuild()
        {
            Assert.That(DeadSignalDebugMenu.IsAllowed(false, false), Is.False);
        }

        [Test]
        public void Harness_ExposesExpandedLocationsScenariosAndTimeControls()
        {
            Assert.That(Enum.IsDefined(typeof(DebugLocation), DebugLocation.FarEast), Is.True);
            Assert.That(Enum.IsDefined(typeof(DebugLocation), DebugLocation.SpineTower), Is.True);
            Assert.That(Enum.IsDefined(typeof(DebugScenario), DebugScenario.EasternRoomCombat), Is.True);
            Assert.That(Enum.IsDefined(typeof(DebugScenario), DebugScenario.EasternRoomCombatNoSwarmers), Is.True);
            Assert.That(Enum.IsDefined(typeof(DebugScenario), DebugScenario.AllEffects), Is.True);
            Assert.That(Enum.GetValues(typeof(DebugTimeScale)).Length, Is.EqualTo(5));
        }

        [Test]
        public void CombatLabCommandLinePreset_SelectsMatchedSwarmerPopulation()
        {
            Assert.That(DeadSignalGame.TryParseCombatLabScenario(
                new[] { "player.exe", "-deadSignalCombatLab=SwarmersOn" }, out var swarmersOn), Is.True);
            Assert.That(swarmersOn, Is.EqualTo(DebugScenario.EasternRoomCombat));

            Assert.That(DeadSignalGame.TryParseCombatLabScenario(
                new[] { "player.exe", "-DEADSIGNALCOMBATLAB=swarmersoff" }, out var swarmersOff), Is.True);
            Assert.That(swarmersOff, Is.EqualTo(DebugScenario.EasternRoomCombatNoSwarmers));

            Assert.That(DeadSignalGame.TryParseCombatLabScenario(
                new[] { "player.exe", "-deadSignalCombatLab=unknown" }, out _), Is.False);
        }
    }
}
