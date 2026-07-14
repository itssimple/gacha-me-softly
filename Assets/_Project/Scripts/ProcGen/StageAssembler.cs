using System.Collections.Generic;
using Chris.PachiRogue.Core;

namespace Chris.PachiRogue.ProcGen
{
    /// <summary>
    /// Pure C# stage assembly: given a seeded RNG stream, a recipe, and the
    /// chunk library, produces a deterministic <see cref="StagePlan"/>.
    /// Bounded retries against the full validator; if exhausted, constraints
    /// relax to the hard seam rule only and the plan is flagged so the
    /// builder can log a warning (PLAN.md Phase 3).
    /// </summary>
    public static class StageAssembler
    {
        public const int MaxAttempts = 20;
        public const float JitterRange = 0.5f;

        public static StagePlan Assemble(
            IRngService rng,
            RecipeData recipe,
            IReadOnlyList<ChunkData> library)
        {
            var chunksById = new Dictionary<string, ChunkData>();
            var eligible = new List<ChunkData>();
            foreach (ChunkData chunk in library)
            {
                chunksById[chunk.Id] = chunk;
                if (chunk.DifficultyBand <= recipe.MaxDifficultyBand)
                {
                    eligible.Add(chunk);
                }
            }

            if (eligible.Count == 0)
            {
                // Degenerate library: fall back to the full library so a
                // stage always exists (flagged as relaxed).
                eligible.AddRange(library);
            }

            for (int attempt = 0; attempt < MaxAttempts * 2; attempt++)
            {
                bool relaxed = attempt >= MaxAttempts;
                StagePlan candidate = BuildCandidate(rng, recipe, eligible, chunksById);

                ValidationResult result = relaxed
                    ? StageValidator.ValidateSeamsOnly(candidate, chunksById)
                    : StageValidator.Validate(candidate, chunksById, recipe);

                if (result.IsValid)
                {
                    candidate.ConstraintsRelaxed = relaxed;
                    return candidate;
                }
            }

            // Guaranteed termination: force a seam-safe plan by repeating an
            // Open/Open chunk (every act library authors at least one).
            ChunkData safe = FindOpenChunk(eligible);
            var fallback = new StagePlan { ConstraintsRelaxed = true };
            float height = 0f;
            for (int i = 0; i < recipe.ChunkCount; i++)
            {
                fallback.Placements.Add(new ChunkPlacement
                {
                    ChunkId = safe.Id,
                    Mirror = false,
                    JitterX = 0f
                });
                height += safe.Height;
            }

            fallback.TotalHeight = height;
            return fallback;
        }

        private static StagePlan BuildCandidate(
            IRngService rng,
            RecipeData recipe,
            IReadOnlyList<ChunkData> eligible,
            IReadOnlyDictionary<string, ChunkData> chunksById)
        {
            var plan = new StagePlan();
            string previousId = null;
            float height = 0f;

            for (int i = 0; i < recipe.ChunkCount; i++)
            {
                // Avoid immediate repeats when the pool allows it.
                ChunkData pick;
                int guard = 0;
                do
                {
                    pick = eligible[rng.NextInt(0, eligible.Count)];
                    guard++;
                }
                while (eligible.Count > 1 && pick.Id == previousId && guard < 8);

                plan.Placements.Add(new ChunkPlacement
                {
                    ChunkId = pick.Id,
                    Mirror = pick.AllowMirror && rng.NextBool(),
                    JitterX = rng.NextFloat(-JitterRange, JitterRange)
                });

                previousId = pick.Id;
                height += pick.Height;
            }

            plan.TotalHeight = height;
            return plan;
        }

        private static ChunkData FindOpenChunk(IReadOnlyList<ChunkData> chunks)
        {
            foreach (ChunkData chunk in chunks)
            {
                if (chunk.Top == ConnectorProfile.Open && chunk.Bottom == ConnectorProfile.Open)
                {
                    return chunk;
                }
            }

            return chunks[0];
        }
    }
}
