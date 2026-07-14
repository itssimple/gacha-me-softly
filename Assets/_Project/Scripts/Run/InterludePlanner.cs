using System;
using System.Collections.Generic;
using Chris.PachiRogue.Core;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Pure-C# planning of the between-stages interludes: which story beat to
    /// show, whether an NPC is met (and which one, via the injected
    /// <see cref="IRngService"/>), and the current upgrade offers. No
    /// UnityEngine types — unit-tested headlessly.
    /// </summary>
    public static class InterludePlanner
    {
        public const int FinalStage = 15;
        public const int StagesPerAct = 5;

        /// <summary>Stages after which a new friend is met (chosen from the act's unmet pool).</summary>
        private static readonly int[] MeetStages = { 2, 4, 7, 9, 12 };

        /// <summary>Localization key of the story beat shown after clearing a stage.</summary>
        public static string BeatKey(int clearedStage)
        {
            return clearedStage >= FinalStage ? "interlude.victory" : $"interlude.stage{clearedStage}";
        }

        /// <summary>0 = Meadow, 1 = Onsen Town, 2 = Sky Shrine.</summary>
        public static int ActForStage(int stage)
        {
            int act = (stage - 1) / StagesPerAct;
            return Math.Max(0, Math.Min(2, act));
        }

        public static bool IsMeetStage(int stage)
        {
            return Array.IndexOf(MeetStages, stage) >= 0;
        }

        /// <param name="allowMeet">
        /// False when replanning inside the same interlude (after an upgrade
        /// purchase) so a second NPC is not met by accident.
        /// </param>
        public static InterludePlan Plan(
            int clearedStage,
            string playerName,
            int shardsAwarded,
            IReadOnlyList<NpcData> npcs,
            IReadOnlyList<AbilityData> abilities,
            IMetaProgressionView progression,
            IRngService rng,
            bool allowMeet = true)
        {
            var plan = new InterludePlan
            {
                ClearedStageIndex = clearedStage,
                IsVictory = clearedStage >= FinalStage,
                BeatKey = BeatKey(clearedStage),
                PlayerName = playerName,
                ShardsAwarded = shardsAwarded,
                ShardBalance = progression.Shards
            };

            if (allowMeet && !plan.IsVictory && IsMeetStage(clearedStage))
            {
                plan.MetNpc = PickNpc(clearedStage, npcs, progression, rng);
            }

            BuildOffers(plan, npcs, abilities, progression);
            return plan;
        }

        private static NpcData PickNpc(
            int clearedStage,
            IReadOnlyList<NpcData> npcs,
            IMetaProgressionView progression,
            IRngService rng)
        {
            int act = ActForStage(clearedStage);

            List<NpcData> candidates = Collect(npcs, n => !progression.HasMet(n.Id) && n.Act == act);
            if (candidates.Count == 0)
            {
                // Act pool exhausted — fall back to any unmet NPC so scripted
                // meet stages never silently no-op while friends remain.
                candidates = Collect(npcs, n => !progression.HasMet(n.Id));
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[rng.NextInt(0, candidates.Count)];
        }

        private static void BuildOffers(
            InterludePlan plan,
            IReadOnlyList<NpcData> npcs,
            IReadOnlyList<AbilityData> abilities,
            IMetaProgressionView progression)
        {
            for (int i = 0; i < npcs.Count; i++)
            {
                NpcData npc = npcs[i];
                bool isFriend = progression.HasMet(npc.Id) ||
                                (plan.MetNpc != null && plan.MetNpc.Id == npc.Id);
                if (!isFriend)
                {
                    continue;
                }

                AbilityData ability = FindAbility(abilities, npc.GrantsAbilityId);
                if (ability == null)
                {
                    continue;
                }

                int level = progression.GetAbilityLevel(ability.Id);
                bool maxed = level >= ability.MaxLevel;
                int cost = maxed ? 0 : ability.UpgradeCost(level);

                plan.Offers.Add(new AbilityOffer
                {
                    AbilityId = ability.Id,
                    NameKey = ability.NameKey,
                    DescriptionKey = ability.DescriptionKey,
                    CurrentLevel = level,
                    MaxLevel = ability.MaxLevel,
                    UpgradeCost = cost,
                    IsMaxed = maxed,
                    Affordable = !maxed && progression.Shards >= cost
                });
            }
        }

        private static AbilityData FindAbility(IReadOnlyList<AbilityData> abilities, string id)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].Id == id)
                {
                    return abilities[i];
                }
            }

            return null;
        }

        private static List<NpcData> Collect(IReadOnlyList<NpcData> npcs, Predicate<NpcData> match)
        {
            var result = new List<NpcData>();
            for (int i = 0; i < npcs.Count; i++)
            {
                if (match(npcs[i]))
                {
                    result.Add(npcs[i]);
                }
            }

            return result;
        }
    }
}
