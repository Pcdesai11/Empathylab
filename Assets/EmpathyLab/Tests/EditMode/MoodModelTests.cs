#nullable enable
using EmpathyLab.Core;
using NUnit.Framework;

namespace EmpathyLab.Tests.EditMode
{
    public sealed class MoodModelTests
    {
        [Test]
        public void NegativeDeltaClampsAtZeroAndBecomesCalm()
        {
            var mood = new MoodModel(0.2f);

            mood.Apply(new MoodDelta(-1f));

            Assert.AreEqual(0f, mood.Tension, 0.0001f);
            Assert.AreEqual(MoodBand.Calm, mood.Band);
        }

        [Test]
        public void PositiveDeltaClampsAtOneAndBecomesUpset()
        {
            var mood = new MoodModel(0.8f);

            mood.Apply(new MoodDelta(1f));

            Assert.AreEqual(1f, mood.Tension, 0.0001f);
            Assert.AreEqual(MoodBand.Upset, mood.Band);
        }

        [Test]
        public void ZeroDeltaLeavesTensionUnchanged()
        {
            var mood = new MoodModel(0.5f);

            mood.Apply(new MoodDelta(0f));

            Assert.AreEqual(0.5f, mood.Tension, 0.0001f);
            Assert.AreEqual(MoodBand.Neutral, mood.Band);
        }

        [Test]
        public void BandsFollowThePublishedThresholds()
        {
            Assert.AreEqual(MoodBand.Calm, new MoodModel(MoodThresholds.CalmBelow - 0.01f).Band);
            Assert.AreEqual(MoodBand.Neutral, new MoodModel(MoodThresholds.CalmBelow).Band);
            Assert.AreEqual(MoodBand.Neutral, new MoodModel(MoodThresholds.UpsetAt - 0.01f).Band);
            Assert.AreEqual(MoodBand.Upset, new MoodModel(MoodThresholds.UpsetAt).Band);
        }

        [Test]
        public void ConstructorClampsAnOutOfRangeStart()
        {
            Assert.AreEqual(0f, new MoodModel(-0.4f).Tension, 0.0001f);
            Assert.AreEqual(1f, new MoodModel(4f).Tension, 0.0001f);
            Assert.IsFalse(MoodModel.IsInRange(4f));
            Assert.IsTrue(MoodModel.IsInRange(0.72f));
        }
    }
}
