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
            Assert.That(Enum.IsDefined(typeof(DebugScenario), DebugScenario.OpeningTacticalWindow), Is.True);
            Assert.That(Enum.IsDefined(typeof(DebugScenario), DebugScenario.SpineReturnTacticalWindow), Is.True);
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

        [Test]
        public void TacticalWindowCommandLinePreset_SelectsAuthoredComparisonLocation()
        {
            Assert.That(DeadSignalGame.TryParseTacticalWindowScenario(
                new[] { "player.exe", "-deadSignalTacticalWindow=Opening" }, out var opening), Is.True);
            Assert.That(opening, Is.EqualTo(DebugScenario.OpeningTacticalWindow));

            Assert.That(DeadSignalGame.TryParseTacticalWindowScenario(
                new[] { "player.exe", "-DEADSIGNALTACTICALWINDOW=spinereturn" }, out var spineReturn), Is.True);
            Assert.That(spineReturn, Is.EqualTo(DebugScenario.SpineReturnTacticalWindow));

            Assert.That(DeadSignalGame.TryParseTacticalWindowScenario(
                new[] { "player.exe", "-deadSignalTacticalWindow=unknown" }, out _), Is.False);
        }

        [Test]
        public void TacticalWindowCaptureArgument_IsExplicitAndCaseInsensitive()
        {
            Assert.That(DeadSignalGame.HasTacticalWindowCaptureArgument(
                new[] { "player.exe", "-deadSignalTacticalWindowCapture" }), Is.True);
            Assert.That(DeadSignalGame.HasTacticalWindowCaptureArgument(
                new[] { "player.exe", "-DEADSIGNALTACTICALWINDOWCAPTURE" }), Is.True);
            Assert.That(DeadSignalGame.HasTacticalWindowCaptureArgument(
                new[] { "player.exe", "-deadSignalTacticalWindow=Opening" }), Is.False);
            Assert.That(DeadSignalGame.HasTacticalWindowCaptureArgument(null), Is.False);
        }

        [Test]
        public void TacticalWindowSweepArgument_IsExplicitAndCaseInsensitive()
        {
            Assert.That(DeadSignalGame.HasTacticalWindowSweepArgument(
                new[] { "player.exe", "-deadSignalTacticalWindowSweep" }), Is.True);
            Assert.That(DeadSignalGame.HasTacticalWindowSweepArgument(
                new[] { "player.exe", "-DEADSIGNALTACTICALWINDOWSWEEP" }), Is.True);
            Assert.That(DeadSignalGame.HasTacticalWindowSweepArgument(
                new[] { "player.exe", "-deadSignalTacticalWindowCapture" }), Is.False);
            Assert.That(DeadSignalGame.HasTacticalWindowSweepArgument(null), Is.False);
        }
    }
}
