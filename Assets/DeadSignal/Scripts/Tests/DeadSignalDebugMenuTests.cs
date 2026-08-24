using NUnit.Framework;
using DeadSignal.Diagnostics;

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
    }
}
