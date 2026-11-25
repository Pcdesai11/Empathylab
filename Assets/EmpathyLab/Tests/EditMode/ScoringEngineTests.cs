#nullable enable
using EmpathyLab.Core;
using NUnit.Framework;

namespace EmpathyLab.Tests.EditMode
{
    public sealed class ScoringEngineTests
    {
        [Test]
        public void RecordsThreeDimensionsFromAZeroBaseline()
        {
            var scoring = new ScoringEngine(0);

            scoring.Record(new ScoreDelta(10, 20, 30));

            Assert.AreEqual(10, scoring.Empathy);
            Assert.AreEqual(20, scoring.Clarity);
            Assert.AreEqual(30, scoring.Deescalation);
        }

        [Test]
        public void ClampsEachDimensionToZeroAndOneHundred()
        {
            var high = new ScoringEngine(90);
            high.Record(new ScoreDelta(50, 50, 50));
            Assert.AreEqual(ScoringEngine.Max, high.Empathy);
            Assert.AreEqual(ScoringEngine.Max, high.Clarity);
            Assert.AreEqual(ScoringEngine.Max, high.Deescalation);

            var low = new ScoringEngine(5);
            low.Record(new ScoreDelta(-40, -40, -40));
            Assert.AreEqual(ScoringEngine.Min, low.Empathy);
            Assert.AreEqual(ScoringEngine.Min, low.Clarity);
            Assert.AreEqual(ScoringEngine.Min, low.Deescalation);
        }

        [Test]
        public void ZeroDeltaDoesNotChangeTheScore()
        {
            var scoring = new ScoringEngine(40);

            scoring.Record(ScoreDelta.Zero);

            Assert.AreEqual(40, scoring.Empathy);
            Assert.AreEqual(40, scoring.Clarity);
            Assert.AreEqual(40, scoring.Deescalation);
        }

        [Test]
        public void NegativeDeltaDropsANonZeroBaseline()
        {
            var scoring = new ScoringEngine(40);

            scoring.Record(new ScoreDelta(-12, 4, -16));

            Assert.AreEqual(28, scoring.Empathy);
            Assert.AreEqual(44, scoring.Clarity);
            Assert.AreEqual(24, scoring.Deescalation);
        }

        [Test]
        public void BaselineOutsideTheScaleIsClamped()
        {
            var scoring = new ScoringEngine(140);

            Assert.AreEqual(ScoringEngine.Max, scoring.Empathy);
        }
    }
}
