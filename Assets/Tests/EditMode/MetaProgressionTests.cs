using Chris.PachiRogue.Core;
using NUnit.Framework;
using Chris.PachiRogue.Run;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class MetaProgressionTests
    {
        [Test]
        public void AwardAndSpendShards_UpdateBalance()
        {
            var model = new SaveModelV1();
            var progression = new MetaProgression(model);

            progression.AwardShards(50);
            Assert.AreEqual(50, progression.Shards);
            Assert.AreEqual(50, model.metaCurrency, "Balance must live in the DTO.");

            Assert.IsTrue(progression.TrySpendShards(30));
            Assert.AreEqual(20, progression.Shards);
        }

        [Test]
        public void SpendingMoreThanBalance_FailsAndKeepsBalance()
        {
            var progression = new MetaProgression(new SaveModelV1 { metaCurrency = 10 });

            Assert.IsFalse(progression.TrySpendShards(11));
            Assert.AreEqual(10, progression.Shards);
        }

        [Test]
        public void MeetNpc_IsIdempotent()
        {
            var model = new SaveModelV1();
            var progression = new MetaProgression(model);

            progression.MeetNpc("pip");
            progression.MeetNpc("pip");

            Assert.IsTrue(progression.HasMet("pip"));
            Assert.AreEqual(1, model.metNpcIds.Count);
        }

        [Test]
        public void AbilityLevels_DefaultZero_SetAndGet()
        {
            var progression = new MetaProgression(new SaveModelV1());

            Assert.AreEqual(0, progression.GetAbilityLevel("glow"));

            progression.SetAbilityLevel("glow", 3);
            Assert.AreEqual(3, progression.GetAbilityLevel("glow"));

            progression.SetAbilityLevel("glow", 4);
            Assert.AreEqual(4, progression.GetAbilityLevel("glow"));
        }

        [Test]
        public void AbilityLevels_SurviveSaveModelRoundTripLists()
        {
            var model = new SaveModelV1();
            var progression = new MetaProgression(model);
            progression.SetAbilityLevel("glow", 2);
            progression.SetAbilityLevel("snacks", 5);

            // Same parallel lists read back through a fresh facade.
            var reread = new MetaProgression(model);
            Assert.AreEqual(2, reread.GetAbilityLevel("glow"));
            Assert.AreEqual(5, reread.GetAbilityLevel("snacks"));
        }

        [Test]
        public void DriftedParallelLists_AreRepaired()
        {
            var model = new SaveModelV1();
            model.passiveAbilityIds.Add("glow");
            model.passiveAbilityIds.Add("snacks");
            model.passiveAbilityLevels.Add(2); // levels list too short

            var progression = new MetaProgression(model);

            Assert.AreEqual(2, progression.GetAbilityLevel("glow"));
            Assert.AreEqual(0, progression.GetAbilityLevel("snacks"));
            Assert.AreEqual(model.passiveAbilityIds.Count, model.passiveAbilityLevels.Count);
        }
    }
}
