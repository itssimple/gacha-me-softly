using System.Collections.Generic;
using Chris.PachiRogue.Core;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Pure draft logic: rarity-weighted pick of 3 distinct upgrades from the
    /// eligible pool. Luck shifts weight toward Rare/Epic. Deterministic for
    /// a given RNG stream — statistically tested over 10k seeded rolls.
    /// </summary>
    public static class DraftService
    {
        public const int DraftSize = 3;

        public static int Weight(UpgradeRarity rarity, float luck)
        {
            switch (rarity)
            {
                case UpgradeRarity.Common:
                    return 100;
                case UpgradeRarity.Rare:
                    return 40 + (int)(luck * 8f);
                case UpgradeRarity.Epic:
                    return 12 + (int)(luck * 4f);
                default:
                    return 1;
            }
        }

        /// <summary>Picks up to 3 distinct upgrades (fewer if the pool is smaller).</summary>
        public static List<UpgradeData> PickThree(
            IReadOnlyList<UpgradeData> pool,
            float luck,
            IRngService rng)
        {
            var remaining = new List<UpgradeData>(pool);
            var picks = new List<UpgradeData>(DraftSize);

            while (picks.Count < DraftSize && remaining.Count > 0)
            {
                int totalWeight = 0;
                foreach (UpgradeData upgrade in remaining)
                {
                    totalWeight += Weight(upgrade.Rarity, luck);
                }

                int roll = rng.NextInt(0, totalWeight);
                int cursor = 0;
                for (int i = 0; i < remaining.Count; i++)
                {
                    cursor += Weight(remaining[i].Rarity, luck);
                    if (roll < cursor)
                    {
                        picks.Add(remaining[i]);
                        remaining.RemoveAt(i); // no duplicates within one draft
                        break;
                    }
                }
            }

            return picks;
        }
    }
}
