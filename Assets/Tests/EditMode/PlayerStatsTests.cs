using System.Collections.Generic;
using Chris.PachiRogue.Core;
using Chris.PachiRogue.Run;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class PlayerStatsTests
    {
        private static readonly List<AbilityData> Abilities = new List<AbilityData>
        {
            new AbilityData { Id = "snacks", Stat = StatType.MaxHealth, PerLevelBonus = 10f, MaxLevel = 5 },
            new AbilityData { Id = "feast", Stat = StatType.MaxHealth, PerLevelBonus = 25f, MaxLevel = 3 },
            new AbilityData { Id = "glow", Stat = StatType.ShardGain, PerLevelBonus = 0.1f, MaxLevel = 5 }
        };

        [Test]
        public void NoLevels_ReturnsBaseValues()
        {
            var progression = new MetaProgression(new SaveModelV1());

            Assert.AreEqual(PlayerStats.BaseMaxHealth,
                PlayerStats.GetStat(StatType.MaxHealth, Abilities, progression));
            Assert.AreEqual(PlayerStats.BaseShardGain,
                PlayerStats.GetStat(StatType.ShardGain, Abilities, progression));
        }

        [Test]
        public void SingleAbility_AddsPerLevelBonus()
        {
            var progression = new MetaProgression(new SaveModelV1());
            progression.SetAbilityLevel("snacks", 3);

            Assert.AreEqual(PlayerStats.BaseMaxHealth + 30f,
                PlayerStats.GetStat(StatType.MaxHealth, Abilities, progression));
        }

        [Test]
        public void MultipleAbilities_SameStat_Stack()
        {
            var progression = new MetaProgression(new SaveModelV1());
            progression.SetAbilityLevel("snacks", 2); // +20
            progression.SetAbilityLevel("feast", 3);  // +75

            Assert.AreEqual(PlayerStats.BaseMaxHealth + 95f,
                PlayerStats.GetStat(StatType.MaxHealth, Abilities, progression));
        }

        [Test]
        public void Abilities_OnlyAffectTheirOwnStat()
        {
            var progression = new MetaProgression(new SaveModelV1());
            progression.SetAbilityLevel("glow", 5);

            Assert.AreEqual(PlayerStats.BaseMaxHealth,
                PlayerStats.GetStat(StatType.MaxHealth, Abilities, progression));
            Assert.AreEqual(PlayerStats.BaseShardGain + 0.5f,
                PlayerStats.GetStat(StatType.ShardGain, Abilities, progression), 1e-5f);
        }

        [Test]
        public void EveryStatType_HasABaseValueMapping()
        {
            var progression = new MetaProgression(new SaveModelV1());
            foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
            {
                Assert.AreEqual(PlayerStats.BaseValue(stat),
                    PlayerStats.GetStat(stat, new List<AbilityData>(), progression),
                    $"Stat {stat} must resolve to its base value with no abilities.");
            }
        }
    }
}
