#nullable enable

namespace EmpathyLab.Core
{
    /// <summary>
    /// Points added to the three debrief dimensions. Negative values are allowed and then clamped.
    /// </summary>
    public readonly struct ScoreDelta
    {
        /// <summary>Creates a score change.</summary>
        /// <param name="empathy">Points added to empathy.</param>
        /// <param name="clarity">Points added to clarity.</param>
        /// <param name="deescalation">Points added to de-escalation.</param>
        public ScoreDelta(int empathy, int clarity, int deescalation)
        {
            Empathy = empathy;
            Clarity = clarity;
            Deescalation = deescalation;
        }

        /// <summary>Points added to empathy.</summary>
        public int Empathy { get; }

        /// <summary>Points added to clarity.</summary>
        public int Clarity { get; }

        /// <summary>Points added to de-escalation.</summary>
        public int Deescalation { get; }

        /// <summary>A change that moves no dimension.</summary>
        public static ScoreDelta Zero => new ScoreDelta(0, 0, 0);
    }

    /// <summary>Accumulates empathy, clarity, and de-escalation for one play-through.</summary>
    public interface IScoringEngine
    {
        /// <summary>Empathy, clamped to 0–100.</summary>
        int Empathy { get; }

        /// <summary>Clarity, clamped to 0–100.</summary>
        int Clarity { get; }

        /// <summary>De-escalation, clamped to 0–100.</summary>
        int Deescalation { get; }

        /// <summary>Adds a change and clamps each dimension to 0–100.</summary>
        /// <param name="delta">Change to record.</param>
        void Record(ScoreDelta delta);
    }

    /// <summary>
    /// Scoring rules for a debrief. The baseline lets a harsh line lower the score instead of only adding points.
    /// </summary>
    public sealed class ScoringEngine : IScoringEngine
    {
        /// <summary>Lowest legal score.</summary>
        public const int Min = 0;

        /// <summary>Highest legal score.</summary>
        public const int Max = 100;

        /// <summary>Creates an engine. The baseline is clamped into 0–100.</summary>
        /// <param name="baseline">Starting value for every dimension.</param>
        public ScoringEngine(int baseline = 0)
        {
            var start = Clamp(baseline);
            Empathy = start;
            Clarity = start;
            Deescalation = start;
        }

        /// <inheritdoc />
        public int Empathy { get; private set; }

        /// <inheritdoc />
        public int Clarity { get; private set; }

        /// <inheritdoc />
        public int Deescalation { get; private set; }

        /// <inheritdoc />
        public void Record(ScoreDelta delta)
        {
            Empathy = Clamp(Empathy + delta.Empathy);
            Clarity = Clamp(Clarity + delta.Clarity);
            Deescalation = Clamp(Deescalation + delta.Deescalation);
        }

        static int Clamp(int value)
        {
            if (value < Min)
                return Min;
            if (value > Max)
                return Max;
            return value;
        }
    }
}
