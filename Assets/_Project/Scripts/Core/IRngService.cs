using System;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Seeded, injectable random number source. All gameplay randomness must go
    /// through this interface — the engine's static random class is forbidden
    /// in ProcGen/Run (see CLAUDE.md hard rules).
    /// </summary>
    public interface IRngService
    {
        /// <summary>The seed this stream was constructed from.</summary>
        ulong Seed { get; }

        /// <summary>Next value in [0, 2^64).</summary>
        ulong NextULong();

        /// <summary>Next value in [0.0, 1.0).</summary>
        double NextDouble();

        /// <summary>Next value in [0.0f, 1.0f).</summary>
        float NextFloat();

        /// <summary>Next value in [minInclusive, maxExclusive). Unbiased.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If maxExclusive &lt;= minInclusive.</exception>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>Next value in [minInclusive, maxInclusive] as a float.</summary>
        float NextFloat(float minInclusive, float maxInclusive);

        /// <summary>True with the given probability (default fair coin).</summary>
        bool NextBool(double probability = 0.5);

        /// <summary>
        /// Creates an independent child stream derived from this stream's seed
        /// and <paramref name="label"/>. Forking is deterministic and stable:
        /// the same parent seed and label always produce the same child stream,
        /// regardless of how much of the parent stream has been consumed. This
        /// lets ProcGen and loot draw from independent streams without one
        /// perturbing the other.
        /// </summary>
        IRngService Fork(string label);
    }
}
