#nullable enable
using System;

namespace EmpathyLab.Core
{
    /// <summary>How tense the other person feels, from settled to upset.</summary>
    public enum MoodBand
    {
        /// <summary>Tension is below <see cref="MoodThresholds.CalmBelow"/>.</summary>
        Calm = 0,

        /// <summary>Tension is between the calm and upset cutoffs.</summary>
        Neutral = 1,

        /// <summary>Tension is at or above <see cref="MoodThresholds.UpsetAt"/>.</summary>
        Upset = 2
    }

    /// <summary>
    /// Band cutoffs shared by the model, the UI, and the tests.
    /// </summary>
    public static class MoodThresholds
    {
        /// <summary>Tension below this value is <see cref="MoodBand.Calm"/>.</summary>
        public const float CalmBelow = 0.34f;

        /// <summary>Tension at or above this value is <see cref="MoodBand.Upset"/>.</summary>
        public const float UpsetAt = 0.67f;
    }

    /// <summary>A change added to tension. Positive values make the person more upset.</summary>
    public readonly struct MoodDelta
    {
        /// <summary>Creates a tension change.</summary>
        /// <param name="tension">Amount added before the model clamps to 0–1.</param>
        public MoodDelta(float tension) => Tension = tension;

        /// <summary>Amount added to tension.</summary>
        public float Tension { get; }
    }

    /// <summary>
    /// Tracks how upset the other person is. Presentation code reads this and does not own the rules.
    /// </summary>
    public interface IMoodModel
    {
        /// <summary>Tension from 0 (settled) to 1 (very upset).</summary>
        float Tension { get; }

        /// <summary>Band derived from <see cref="Tension"/>.</summary>
        MoodBand Band { get; }

        /// <summary>Applies a change and clamps the result to 0–1.</summary>
        /// <param name="delta">Change to apply.</param>
        void Apply(MoodDelta delta);
    }

    /// <summary>Clamped tension model used by every scenario.</summary>
    public sealed class MoodModel : IMoodModel
    {
        /// <summary>
        /// Creates a model. Values outside 0–1 are clamped so a bad authoring value cannot break a session.
        /// </summary>
        /// <param name="tension">Starting tension.</param>
        public MoodModel(float tension)
        {
            Tension = Clamp(tension);
        }

        /// <inheritdoc />
        public float Tension { get; private set; }

        /// <inheritdoc />
        public MoodBand Band
        {
            get
            {
                if (Tension < MoodThresholds.CalmBelow)
                    return MoodBand.Calm;
                if (Tension < MoodThresholds.UpsetAt)
                    return MoodBand.Neutral;
                return MoodBand.Upset;
            }
        }

        /// <inheritdoc />
        public void Apply(MoodDelta delta)
        {
            Tension = Clamp(Tension + delta.Tension);
        }

        /// <summary>True when <paramref name="tension"/> is inside the legal 0–1 range.</summary>
        /// <param name="tension">Value to test.</param>
        public static bool IsInRange(float tension) => tension >= 0f && tension <= 1f;

        static float Clamp(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
