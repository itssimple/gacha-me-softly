using System.Collections.Generic;

namespace Chris.PachiRogue.ProcGen
{
    /// <summary>
    /// Deterministic output of the assembler: ordered chunk picks with their
    /// jitter/mirror decisions. Same (seed, recipe, library) → identical plan
    /// — this is the determinism contract tested in EditMode.
    /// </summary>
    public sealed class StagePlan
    {
        public List<ChunkPlacement> Placements = new List<ChunkPlacement>();

        /// <summary>True when constraints were relaxed after bounded retries (logged by the builder).</summary>
        public bool ConstraintsRelaxed;

        public float TotalHeight;
    }

    public struct ChunkPlacement
    {
        public string ChunkId;
        public bool Mirror;
        public float JitterX;
    }
}
