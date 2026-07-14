using System.Collections.Generic;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Base stats plus passive-ability bonuses. Pure C#. Effective value =
    /// base + Σ (perLevelBonus × level) over abilities targeting the stat.
    /// </summary>
    public static class PlayerStats
    {
        public const float BaseMaxHealth = 100f;
        public const float BaseLaunchPower = 10f;
        public const float BaseShardGain = 1f;
        public const float BaseLuck = 0f;
        public const float BaseBounciness = 1f;
        public const float BaseAimControl = 1f;

        public static float BaseValue(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth: return BaseMaxHealth;
                case StatType.LaunchPower: return BaseLaunchPower;
                case StatType.ShardGain: return BaseShardGain;
                case StatType.Luck: return BaseLuck;
                case StatType.Bounciness: return BaseBounciness;
                case StatType.AimControl: return BaseAimControl;
                default: return 0f;
            }
        }

        public static float GetStat(
            StatType stat,
            IReadOnlyList<AbilityData> abilities,
            IMetaProgressionView progression)
        {
            float value = BaseValue(stat);

            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityData ability = abilities[i];
                if (ability.Stat != stat)
                {
                    continue;
                }

                value += ability.PerLevelBonus * progression.GetAbilityLevel(ability.Id);
            }

            return value;
        }
    }
}
