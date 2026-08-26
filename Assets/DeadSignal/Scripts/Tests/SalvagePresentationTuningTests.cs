using NUnit.Framework;
using UnityEngine;
using DeadSignal.Missions;
using DeadSignal.Salvage;
using DeadSignal.World;

namespace DeadSignal.Tests
{
    public sealed class SalvagePresentationTuningTests
    {
        [Test]
        public void AuthoredTuning_HasSafeCollectionAndPresentationValues()
        {
            var tuning = Resources.Load<SalvagePresentationTuning>("Tuning/SalvagePresentationTuning");

            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.RotationSpeed, Is.GreaterThanOrEqualTo(0f));
            Assert.That(tuning.HoverHeight, Is.GreaterThanOrEqualTo(tuning.HoverAmplitude));
            Assert.That(tuning.HoverFrequency, Is.GreaterThanOrEqualTo(0f));
            Assert.That(tuning.CollectionRadius, Is.InRange(0.5f, 1.25f));
            Assert.That(tuning.RequiredCacheSignalReward, Is.GreaterThanOrEqualTo(10f));
            Assert.That(tuning.RecoveryFieldDuration, Is.GreaterThanOrEqualTo(2f));
            Assert.That(tuning.RecoveryFieldRadius, Is.GreaterThanOrEqualTo(2f));
            Assert.That(tuning.OptionalCacheSignalReward, Is.EqualTo(18f),
                "The optional route should provide meaningful survival recovery without refilling the full reserve.");
        }

        [Test]
        public void AuthoredSalvageSocket_ReportsScenePosition()
        {
            var socketObject = new GameObject("Salvage socket test");
            try
            {
                socketObject.transform.position = new Vector3(18.7f, 0f, 0f);
                var socket = socketObject.AddComponent<AuthoredSalvageSocket>();
                socket.Configure(SignalRegion.Relay, false);

                Assert.That(socket.Position, Is.EqualTo(socketObject.transform.position));
                Assert.That(socket.Region, Is.EqualTo(SignalRegion.Relay));
                Assert.That(socket.IsOptional, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(socketObject);
            }
        }
    }
}
