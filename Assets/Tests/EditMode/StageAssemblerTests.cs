using System.Collections.Generic;
using Chris.PachiRogue.Core;
using Chris.PachiRogue.ProcGen;
using NUnit.Framework;
using UnityEngine;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class StageAssemblerTests
    {
        private static ChunkData Chunk(string id, int band, ConnectorProfile top, ConnectorProfile bottom,
            int reward, bool mirror = true)
        {
            return new ChunkData
            {
                Id = id,
                DifficultyBand = band,
                Top = top,
                Bottom = bottom,
                RewardBudget = reward,
                AllowMirror = mirror,
                Height = 5f,
                HazardCount = 0,
                Pegs = new List<Vector2>(),
                Bumpers = new List<Vector2>(),
                Hazards = new List<Vector2>()
            };
        }

        private static List<ChunkData> Library()
        {
            return new List<ChunkData>
            {
                Chunk("a", 0, ConnectorProfile.Open, ConnectorProfile.Open, 100),
                Chunk("b", 0, ConnectorProfile.Open, ConnectorProfile.Open, 80),
                Chunk("c", 1, ConnectorProfile.Open, ConnectorProfile.Narrow, 90),
                Chunk("d", 1, ConnectorProfile.Narrow, ConnectorProfile.Open, 90)
            };
        }

        private static RecipeData Recipe(int chunkCount = 3, int maxBand = 2, int rMin = 100, int rMax = 600, int hazardCap = 5)
        {
            return new RecipeData
            {
                Id = "test",
                ChunkCount = chunkCount,
                MaxDifficultyBand = maxBand,
                RewardBudgetMin = rMin,
                RewardBudgetMax = rMax,
                HazardCap = hazardCap,
                ScoreGateBase = 100,
                ScoreGatePerStage = 50,
                BossGateMultiplier = 2f,
                LaunchesPerStage = 5
            };
        }

        private static Dictionary<string, ChunkData> ById(List<ChunkData> library)
        {
            var byId = new Dictionary<string, ChunkData>();
            foreach (ChunkData chunk in library)
            {
                byId[chunk.Id] = chunk;
            }

            return byId;
        }

        [Test]
        public void SameSeed_ProducesByteIdenticalPlan()
        {
            StagePlan a = StageAssembler.Assemble(new RngService(42UL).Fork("stage3"), Recipe(), Library());
            StagePlan b = StageAssembler.Assemble(new RngService(42UL).Fork("stage3"), Recipe(), Library());

            Assert.AreEqual(a.Placements.Count, b.Placements.Count);
            for (int i = 0; i < a.Placements.Count; i++)
            {
                Assert.AreEqual(a.Placements[i].ChunkId, b.Placements[i].ChunkId, $"chunk at {i}");
                Assert.AreEqual(a.Placements[i].Mirror, b.Placements[i].Mirror, $"mirror at {i}");
                Assert.AreEqual(a.Placements[i].JitterX, b.Placements[i].JitterX, $"jitter at {i}");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentPlansEventually()
        {
            StagePlan reference = StageAssembler.Assemble(new RngService(1UL), Recipe(), Library());

            bool anyDifference = false;
            for (ulong seed = 2; seed < 12 && !anyDifference; seed++)
            {
                StagePlan other = StageAssembler.Assemble(new RngService(seed), Recipe(), Library());
                for (int i = 0; i < reference.Placements.Count; i++)
                {
                    if (reference.Placements[i].ChunkId != other.Placements[i].ChunkId ||
                        reference.Placements[i].JitterX != other.Placements[i].JitterX)
                    {
                        anyDifference = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(anyDifference, "Ten different seeds produced identical plans.");
        }

        [Test]
        public void AssembledPlans_AlwaysPassSeamValidation()
        {
            List<ChunkData> library = Library();
            Dictionary<string, ChunkData> byId = ById(library);

            for (ulong seed = 0; seed < 50; seed++)
            {
                StagePlan plan = StageAssembler.Assemble(new RngService(seed), Recipe(), library);
                ValidationResult result = StageValidator.ValidateSeamsOnly(plan, byId);
                Assert.IsTrue(result.IsValid, $"seed {seed}: {result.Reason}");
            }
        }

        [Test]
        public void Assemble_RespectsDifficultyBand()
        {
            RecipeData recipe = Recipe(maxBand: 0);
            for (ulong seed = 0; seed < 20; seed++)
            {
                StagePlan plan = StageAssembler.Assemble(new RngService(seed), recipe, Library());
                foreach (ChunkPlacement placement in plan.Placements)
                {
                    Assert.IsTrue(placement.ChunkId == "a" || placement.ChunkId == "b",
                        $"band-1 chunk '{placement.ChunkId}' in a band-0 recipe (seed {seed})");
                }
            }
        }

        [Test]
        public void ImpossibleBudget_TerminatesWithRelaxedFlag()
        {
            // No combination of three chunks reaches 10000 reward.
            RecipeData impossible = Recipe(rMin: 10000, rMax: 20000);

            StagePlan plan = StageAssembler.Assemble(new RngService(7UL), impossible, Library());

            Assert.IsNotNull(plan);
            Assert.AreEqual(3, plan.Placements.Count);
            Assert.IsTrue(plan.ConstraintsRelaxed, "Impossible constraints must end in a relaxed plan.");
            Assert.IsTrue(StageValidator.ValidateSeamsOnly(plan, ById(Library())).IsValid,
                "Even relaxed plans must keep passable seams.");
        }

        [Test]
        public void Validator_RejectsNarrowIntoNarrowSeam()
        {
            List<ChunkData> library = Library();
            var plan = new StagePlan();
            plan.Placements.Add(new ChunkPlacement { ChunkId = "c" }); // bottom Narrow
            plan.Placements.Add(new ChunkPlacement { ChunkId = "d" }); // top Narrow
            plan.Placements.Add(new ChunkPlacement { ChunkId = "a" });

            ValidationResult result = StageValidator.Validate(plan, ById(library), Recipe(rMin: 0, rMax: 9999));

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("seam", result.Reason);
        }

        [Test]
        public void Validator_RejectsBudgetOutOfRange()
        {
            var plan = new StagePlan();
            plan.Placements.Add(new ChunkPlacement { ChunkId = "a" }); // 100
            plan.Placements.Add(new ChunkPlacement { ChunkId = "b" }); // 80

            ValidationResult tooLow = StageValidator.Validate(plan, ById(Library()), Recipe(chunkCount: 2, rMin: 500, rMax: 900));
            Assert.IsFalse(tooLow.IsValid);
            StringAssert.Contains("budget", tooLow.Reason);
        }

        [Test]
        public void Validator_RejectsHazardOverCap()
        {
            ChunkData spiky = Chunk("spiky", 0, ConnectorProfile.Open, ConnectorProfile.Open, 100);
            spiky.HazardCount = 3;
            var library = new List<ChunkData> { spiky };

            var plan = new StagePlan();
            plan.Placements.Add(new ChunkPlacement { ChunkId = "spiky" });
            plan.Placements.Add(new ChunkPlacement { ChunkId = "spiky" });

            ValidationResult result = StageValidator.Validate(plan, ById(library), Recipe(hazardCap: 4, rMin: 0, rMax: 9999));

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("hazard", result.Reason);
        }
    }
}
