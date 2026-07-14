using System.Collections.Generic;
using System.Linq;
using Chris.PachiRogue.Core;
using Chris.PachiRogue.Run;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class DraftServiceTests
    {
        private static List<UpgradeData> Pool()
        {
            return new List<UpgradeData>
            {
                new UpgradeData { Id = "c1", Rarity = UpgradeRarity.Common },
                new UpgradeData { Id = "c2", Rarity = UpgradeRarity.Common },
                new UpgradeData { Id = "c3", Rarity = UpgradeRarity.Common },
                new UpgradeData { Id = "r1", Rarity = UpgradeRarity.Rare },
                new UpgradeData { Id = "r2", Rarity = UpgradeRarity.Rare },
                new UpgradeData { Id = "e1", Rarity = UpgradeRarity.Epic }
            };
        }

        [Test]
        public void PickThree_ReturnsThreeDistinctUpgrades()
        {
            var rng = new RngService(42UL);
            for (int i = 0; i < 1000; i++)
            {
                List<UpgradeData> picks = DraftService.PickThree(Pool(), luck: 0f, rng);
                Assert.AreEqual(3, picks.Count);
                Assert.AreEqual(3, picks.Select(p => p.Id).Distinct().Count(),
                    "Duplicates in a single draft are forbidden.");
            }
        }

        [Test]
        public void PickThree_SmallPool_ReturnsWholePool()
        {
            var pool = new List<UpgradeData>
            {
                new UpgradeData { Id = "only", Rarity = UpgradeRarity.Common }
            };

            List<UpgradeData> picks = DraftService.PickThree(pool, 0f, new RngService(1UL));

            Assert.AreEqual(1, picks.Count);
        }

        [Test]
        public void PickThree_SameSeed_IsDeterministic()
        {
            List<UpgradeData> a = DraftService.PickThree(Pool(), 0f, new RngService(7UL).Fork("draft3"));
            List<UpgradeData> b = DraftService.PickThree(Pool(), 0f, new RngService(7UL).Fork("draft3"));

            CollectionAssert.AreEqual(a.Select(p => p.Id).ToList(), b.Select(p => p.Id).ToList());
        }

        [Test]
        public void Weighting_MatchesRarityDistribution_Over10kRolls()
        {
            // First-pick distribution over 10k seeded drafts should follow the
            // weights: Common 300, Rare 80, Epic 12 (luck 0) → common first
            // pick ~76%, epic ~3%.
            var rng = new RngService(1337UL);
            int commonFirst = 0, epicFirst = 0;
            const int rolls = 10000;

            for (int i = 0; i < rolls; i++)
            {
                UpgradeData first = DraftService.PickThree(Pool(), 0f, rng)[0];
                if (first.Rarity == UpgradeRarity.Common) commonFirst++;
                if (first.Rarity == UpgradeRarity.Epic) epicFirst++;
            }

            double commonShare = commonFirst / (double)rolls;
            double epicShare = epicFirst / (double)rolls;

            Assert.That(commonShare, Is.InRange(0.72, 0.80), $"common share {commonShare}");
            Assert.That(epicShare, Is.InRange(0.015, 0.05), $"epic share {epicShare}");
        }

        [Test]
        public void Luck_IncreasesRareAndEpicShare()
        {
            var rngNoLuck = new RngService(2024UL);
            var rngLucky = new RngService(2024UL);
            const int rolls = 10000;

            int rareOrEpicNoLuck = 0, rareOrEpicLucky = 0;
            for (int i = 0; i < rolls; i++)
            {
                if (DraftService.PickThree(Pool(), 0f, rngNoLuck)[0].Rarity != UpgradeRarity.Common)
                {
                    rareOrEpicNoLuck++;
                }

                if (DraftService.PickThree(Pool(), 5f, rngLucky)[0].Rarity != UpgradeRarity.Common)
                {
                    rareOrEpicLucky++;
                }
            }

            Assert.Greater(rareOrEpicLucky, rareOrEpicNoLuck,
                $"luck 5 ({rareOrEpicLucky}) must beat luck 0 ({rareOrEpicNoLuck})");
        }
    }
}
