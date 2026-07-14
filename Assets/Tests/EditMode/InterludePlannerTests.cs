using System.Collections.Generic;
using System.Linq;
using Chris.PachiRogue.Core;
using Chris.PachiRogue.Run;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class InterludePlannerTests
    {
        private static List<NpcData> Npcs()
        {
            return new List<NpcData>
            {
                new NpcData { Id = "pip", NameKey = "npc.pip.name", MeetLineKey = "npc.pip.meet", Act = 0, GrantsAbilityId = "snacks" },
                new NpcData { Id = "mira", NameKey = "npc.mira.name", MeetLineKey = "npc.mira.meet", Act = 0, GrantsAbilityId = "glow" },
                new NpcData { Id = "kapp", NameKey = "npc.kapp.name", MeetLineKey = "npc.kapp.meet", Act = 1, GrantsAbilityId = "soak" },
                new NpcData { Id = "momo", NameKey = "npc.momo.name", MeetLineKey = "npc.momo.meet", Act = 1, GrantsAbilityId = "towel" },
                new NpcData { Id = "sora", NameKey = "npc.sora.name", MeetLineKey = "npc.sora.meet", Act = 2, GrantsAbilityId = "wind" },
                new NpcData { Id = "yuki", NameKey = "npc.yuki.name", MeetLineKey = "npc.yuki.meet", Act = 2, GrantsAbilityId = "paws" }
            };
        }

        private static List<AbilityData> Abilities()
        {
            return new List<AbilityData>
            {
                new AbilityData { Id = "snacks", NameKey = "a.snacks", Stat = StatType.MaxHealth, PerLevelBonus = 10f, MaxLevel = 5, BaseCost = 15, CostPerLevel = 10 },
                new AbilityData { Id = "glow", NameKey = "a.glow", Stat = StatType.ShardGain, PerLevelBonus = 0.1f, MaxLevel = 5, BaseCost = 20, CostPerLevel = 15 },
                new AbilityData { Id = "soak", NameKey = "a.soak", Stat = StatType.Bounciness, PerLevelBonus = 0.05f, MaxLevel = 5, BaseCost = 15, CostPerLevel = 10 },
                new AbilityData { Id = "towel", NameKey = "a.towel", Stat = StatType.Luck, PerLevelBonus = 1f, MaxLevel = 5, BaseCost = 25, CostPerLevel = 20 },
                new AbilityData { Id = "wind", NameKey = "a.wind", Stat = StatType.LaunchPower, PerLevelBonus = 0.5f, MaxLevel = 5, BaseCost = 20, CostPerLevel = 15 },
                new AbilityData { Id = "paws", NameKey = "a.paws", Stat = StatType.AimControl, PerLevelBonus = 0.05f, MaxLevel = 5, BaseCost = 15, CostPerLevel = 10 }
            };
        }

        private static MetaProgression FreshProgression(long shards = 0)
        {
            var model = new SaveModelV1 { metaCurrency = shards };
            return new MetaProgression(model);
        }

        private static InterludePlan Plan(int stage, MetaProgression progression, ulong seed = 42UL, bool allowMeet = true)
        {
            var rng = new RngService(seed).Fork($"interlude.stage{stage}");
            return InterludePlanner.Plan(stage, "Testi", 10, Npcs(), Abilities(), progression, rng, allowMeet);
        }

        [Test]
        public void BeatKey_MapsStagesAndVictory()
        {
            Assert.AreEqual("interlude.stage1", InterludePlanner.BeatKey(1));
            Assert.AreEqual("interlude.stage14", InterludePlanner.BeatKey(14));
            Assert.AreEqual("interlude.victory", InterludePlanner.BeatKey(15));
        }

        [Test]
        public void ActForStage_MapsActsCorrectly()
        {
            Assert.AreEqual(0, InterludePlanner.ActForStage(1));
            Assert.AreEqual(0, InterludePlanner.ActForStage(5));
            Assert.AreEqual(1, InterludePlanner.ActForStage(6));
            Assert.AreEqual(1, InterludePlanner.ActForStage(10));
            Assert.AreEqual(2, InterludePlanner.ActForStage(11));
            Assert.AreEqual(2, InterludePlanner.ActForStage(15));
        }

        [Test]
        public void SameSeed_ProducesIdenticalPlan()
        {
            InterludePlan a = Plan(2, FreshProgression(), seed: 7UL);
            InterludePlan b = Plan(2, FreshProgression(), seed: 7UL);

            Assert.AreEqual(a.BeatKey, b.BeatKey);
            Assert.AreEqual(a.MetNpc?.Id, b.MetNpc?.Id);
            Assert.AreEqual(a.Offers.Count, b.Offers.Count);
            for (int i = 0; i < a.Offers.Count; i++)
            {
                Assert.AreEqual(a.Offers[i].AbilityId, b.Offers[i].AbilityId);
                Assert.AreEqual(a.Offers[i].UpgradeCost, b.Offers[i].UpgradeCost);
            }
        }

        [Test]
        public void MeetStage_PicksNpcFromMatchingAct()
        {
            InterludePlan plan = Plan(2, FreshProgression());

            Assert.IsNotNull(plan.MetNpc, "Stage 2 is a meet stage.");
            Assert.AreEqual(0, plan.MetNpc.Act, "Stage 2 is in the Meadow act.");

            InterludePlan onsen = Plan(7, FreshProgression());
            Assert.IsNotNull(onsen.MetNpc);
            Assert.AreEqual(1, onsen.MetNpc.Act);
        }

        [Test]
        public void NonMeetStage_MeetsNobody()
        {
            Assert.IsNull(Plan(1, FreshProgression()).MetNpc);
            Assert.IsNull(Plan(3, FreshProgression()).MetNpc);
            Assert.IsNull(Plan(15, FreshProgression()).MetNpc);
        }

        [Test]
        public void AlreadyMetNpcs_AreNeverMetTwice()
        {
            MetaProgression progression = FreshProgression();
            progression.MeetNpc("pip");
            progression.MeetNpc("mira");

            // Meadow pool exhausted — falls back to some other unmet NPC.
            InterludePlan plan = Plan(2, progression);

            Assert.IsNotNull(plan.MetNpc);
            Assert.AreNotEqual("pip", plan.MetNpc.Id);
            Assert.AreNotEqual("mira", plan.MetNpc.Id);
        }

        [Test]
        public void AllNpcsMet_MeetStageMeetsNobody()
        {
            MetaProgression progression = FreshProgression();
            foreach (NpcData npc in Npcs())
            {
                progression.MeetNpc(npc.Id);
            }

            Assert.IsNull(Plan(2, progression).MetNpc);
        }

        [Test]
        public void Replan_WithAllowMeetFalse_MeetsNobody()
        {
            Assert.IsNull(Plan(2, FreshProgression(), allowMeet: false).MetNpc);
        }

        [Test]
        public void Offers_OnlyForMetFriends_IncludingJustMet()
        {
            MetaProgression progression = FreshProgression(shards: 100);
            progression.MeetNpc("kapp");

            InterludePlan plan = Plan(2, progression);

            var expected = new List<string> { "soak" };
            if (plan.MetNpc != null)
            {
                expected.Add(Npcs().First(n => n.Id == plan.MetNpc.Id).GrantsAbilityId);
            }

            CollectionAssert.AreEquivalent(expected, plan.Offers.Select(o => o.AbilityId).ToList());
        }

        [Test]
        public void Offers_ReflectCostsLevelsAndAffordability()
        {
            MetaProgression progression = FreshProgression(shards: 30);
            progression.MeetNpc("pip");   // snacks: base 15, +10/level
            progression.MeetNpc("momo");  // towel: base 25 + 20/level
            progression.SetAbilityLevel("towel", 1);

            InterludePlan plan = Plan(1, progression);

            AbilityOffer snacks = plan.Offers.First(o => o.AbilityId == "snacks");
            Assert.AreEqual(0, snacks.CurrentLevel);
            Assert.AreEqual(15, snacks.UpgradeCost);
            Assert.IsTrue(snacks.Affordable);

            AbilityOffer towel = plan.Offers.First(o => o.AbilityId == "towel");
            Assert.AreEqual(1, towel.CurrentLevel);
            Assert.AreEqual(45, towel.UpgradeCost, "cost = base 25 + 20 × level 1");
            Assert.IsFalse(towel.Affordable, "45 > 30 shards");
        }

        [Test]
        public void Offers_MaxedAbility_IsFlagged()
        {
            MetaProgression progression = FreshProgression(shards: 1000);
            progression.MeetNpc("pip");
            progression.SetAbilityLevel("snacks", 5);

            InterludePlan plan = Plan(1, progression);

            AbilityOffer snacks = plan.Offers.First(o => o.AbilityId == "snacks");
            Assert.IsTrue(snacks.IsMaxed);
            Assert.IsFalse(snacks.Affordable);
        }

        [Test]
        public void Victory_SetsFlagAndVictoryBeat()
        {
            InterludePlan plan = Plan(15, FreshProgression());

            Assert.IsTrue(plan.IsVictory);
            Assert.AreEqual("interlude.victory", plan.BeatKey);
        }

        [Test]
        public void FullRun_MeetsAllScriptedFriendsAcrossActs()
        {
            MetaProgression progression = FreshProgression();
            var met = new List<string>();

            for (int stage = 1; stage <= 15; stage++)
            {
                InterludePlan plan = Plan(stage, progression, seed: 99UL);
                if (plan.MetNpc != null)
                {
                    progression.MeetNpc(plan.MetNpc.Id);
                    met.Add(plan.MetNpc.Id);
                }
            }

            Assert.AreEqual(5, met.Count, "Five scripted meet stages.");
            Assert.AreEqual(met.Count, met.Distinct().Count(), "No NPC met twice.");
        }
    }
}
