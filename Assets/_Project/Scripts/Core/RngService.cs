using System;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// xoshiro256** implementation of <see cref="IRngService"/>.
    /// State is initialised from a ulong seed via SplitMix64, per the
    /// reference recommendation (https://prng.di.unimi.it/).
    /// Plain C# — no UnityEngine dependency, fully unit-testable.
    /// </summary>
    public sealed class RngService : IRngService
    {
        private ulong _s0, _s1, _s2, _s3;

        public ulong Seed { get; }

        public RngService(ulong seed)
        {
            Seed = seed;

            // SplitMix64 to spread the seed across the four state words.
            ulong sm = seed;
            _s0 = SplitMix64(ref sm);
            _s1 = SplitMix64(ref sm);
            _s2 = SplitMix64(ref sm);
            _s3 = SplitMix64(ref sm);

            // xoshiro must never have an all-zero state.
            if ((_s0 | _s1 | _s2 | _s3) == 0UL)
            {
                _s0 = 1UL;
            }
        }

        public ulong NextULong()
        {
            ulong result = RotL(_s1 * 5UL, 7) * 9UL;
            ulong t = _s1 << 17;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = RotL(_s3, 45);

            return result;
        }

        public double NextDouble()
        {
            // Top 53 bits → uniform double in [0, 1).
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        public float NextFloat()
        {
            // Top 24 bits → uniform float in [0, 1).
            return (NextULong() >> 40) * (1.0f / (1U << 24));
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    $"maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");
            }

            ulong range = (ulong)((long)maxExclusive - minInclusive);

            // Rejection sampling to avoid modulo bias: reject the bottom
            // (2^64 mod range) values so the remaining count divides evenly.
            ulong threshold = unchecked(0UL - range) % range;
            ulong r;
            do
            {
                r = NextULong();
            }
            while (r < threshold);

            return (int)(minInclusive + (long)(r % range));
        }

        public float NextFloat(float minInclusive, float maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxInclusive),
                    $"maxInclusive ({maxInclusive}) must not be less than minInclusive ({minInclusive}).");
            }

            return minInclusive + (maxInclusive - minInclusive) * NextFloat();
        }

        public bool NextBool(double probability = 0.5)
        {
            return NextDouble() < probability;
        }

        public IRngService Fork(string label)
        {
            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            // Child seed depends only on (parent seed, label) so forks are
            // stable no matter how much of the parent stream was consumed.
            ulong mixed = Seed ^ Fnv1a64(label);
            ulong childSeed = SplitMix64(ref mixed);
            return new RngService(childSeed);
        }

        private static ulong SplitMix64(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static ulong RotL(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        // FNV-1a 64-bit. string.GetHashCode is not stable across runtimes, so
        // fork labels are hashed with an explicit, deterministic function.
        private static ulong Fnv1a64(string s)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 1099511628211UL;
            }

            return hash;
        }
    }
}
