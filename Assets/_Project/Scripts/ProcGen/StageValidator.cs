using System.Collections.Generic;

namespace Chris.PachiRogue.ProcGen
{
    public struct ValidationResult
    {
        public bool IsValid;
        public string Reason;

        public static ValidationResult Ok()
        {
            return new ValidationResult { IsValid = true, Reason = null };
        }

        public static ValidationResult Fail(string reason)
        {
            return new ValidationResult { IsValid = false, Reason = reason };
        }
    }

    /// <summary>
    /// Pure layout-math validation (no scene APIs, no physics — CLAUDE.md):
    /// connector compatibility, reward budget within recipe range, hazard cap.
    /// </summary>
    public static class StageValidator
    {
        public static ValidationResult Validate(
            StagePlan plan,
            IReadOnlyDictionary<string, ChunkData> chunksById,
            RecipeData recipe)
        {
            if (plan.Placements.Count == 0)
            {
                return ValidationResult.Fail("empty plan");
            }

            int totalReward = 0;
            int totalHazards = 0;

            for (int i = 0; i < plan.Placements.Count; i++)
            {
                if (!chunksById.TryGetValue(plan.Placements[i].ChunkId, out ChunkData chunk))
                {
                    return ValidationResult.Fail($"unknown chunk '{plan.Placements[i].ChunkId}'");
                }

                if (chunk.DifficultyBand > recipe.MaxDifficultyBand)
                {
                    return ValidationResult.Fail($"chunk '{chunk.Id}' band {chunk.DifficultyBand} exceeds recipe max {recipe.MaxDifficultyBand}");
                }

                totalReward += chunk.RewardBudget;
                totalHazards += chunk.HazardCount;

                if (i > 0)
                {
                    ChunkData upper = chunksById[plan.Placements[i - 1].ChunkId];
                    // Mirroring flips left/right but not the connector class.
                    if (!ConnectorRules.AreCompatible(upper.Bottom, chunk.Top))
                    {
                        return ValidationResult.Fail($"impassable seam between '{upper.Id}' and '{chunk.Id}'");
                    }
                }
            }

            if (totalHazards > recipe.HazardCap)
            {
                return ValidationResult.Fail($"hazards {totalHazards} exceed cap {recipe.HazardCap}");
            }

            if (totalReward < recipe.RewardBudgetMin || totalReward > recipe.RewardBudgetMax)
            {
                return ValidationResult.Fail($"reward budget {totalReward} outside [{recipe.RewardBudgetMin}, {recipe.RewardBudgetMax}]");
            }

            return ValidationResult.Ok();
        }

        /// <summary>Relaxed pass: only the hard "impassable seam" rule (used after bounded retries).</summary>
        public static ValidationResult ValidateSeamsOnly(
            StagePlan plan,
            IReadOnlyDictionary<string, ChunkData> chunksById)
        {
            for (int i = 1; i < plan.Placements.Count; i++)
            {
                ChunkData upper = chunksById[plan.Placements[i - 1].ChunkId];
                ChunkData lower = chunksById[plan.Placements[i].ChunkId];
                if (!ConnectorRules.AreCompatible(upper.Bottom, lower.Top))
                {
                    return ValidationResult.Fail($"impassable seam between '{upper.Id}' and '{lower.Id}'");
                }
            }

            return ValidationResult.Ok();
        }
    }
}
