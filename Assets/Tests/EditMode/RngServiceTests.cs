using Chris.PachiRogue.Core;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class RngServiceTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var a = new RngService(123456789UL);
            var b = new RngService(123456789UL);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(a.NextULong(), b.NextULong(), $"Sequences diverged at index {i}.");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var a = new RngService(1UL);
            var b = new RngService(2UL);

            bool anyDifference = false;
            for (int i = 0; i < 16; i++)
            {
                if (a.NextULong() != b.NextULong())
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifference, "Different seeds produced identical output.");
        }

        [Test]
        public void ZeroSeed_ProducesUsableStream()
        {
            var rng = new RngService(0UL);

            ulong first = rng.NextULong();
            ulong second = rng.NextULong();

            Assert.AreNotEqual(first, second, "Zero seed produced a stuck stream.");
        }

        [Test]
        public void Fork_SameLabel_IsStableRegardlessOfParentConsumption()
        {
            var parentA = new RngService(42UL);
            var parentB = new RngService(42UL);

            // Consume different amounts from each parent before forking.
            parentA.NextULong();
            for (int i = 0; i < 100; i++)
            {
                parentB.NextULong();
            }

            IRngService forkA = parentA.Fork("procgen");
            IRngService forkB = parentB.Fork("procgen");

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(forkA.NextULong(), forkB.NextULong(), $"Forks diverged at index {i}.");
            }
        }

        [Test]
        public void Fork_DifferentLabels_ProduceIndependentStreams()
        {
            var parent = new RngService(42UL);

            IRngService procgen = parent.Fork("procgen");
            IRngService loot = parent.Fork("loot");

            bool anyDifference = false;
            for (int i = 0; i < 16; i++)
            {
                if (procgen.NextULong() != loot.NextULong())
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifference, "Forks with different labels produced identical output.");
        }

        [Test]
        public void Fork_ConsumingChild_DoesNotPerturbParent()
        {
            var reference = new RngService(7UL);
            var forkedParent = new RngService(7UL);

            IRngService child = forkedParent.Fork("loot");
            for (int i = 0; i < 50; i++)
            {
                child.NextULong();
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(reference.NextULong(), forkedParent.NextULong(),
                    $"Parent stream perturbed by child consumption at index {i}.");
            }
        }

        [Test]
        public void NextInt_StaysWithinBounds()
        {
            var rng = new RngService(99UL);

            for (int i = 0; i < 10000; i++)
            {
                int value = rng.NextInt(-3, 7);
                Assert.GreaterOrEqual(value, -3);
                Assert.Less(value, 7);
            }
        }

        [Test]
        public void NextInt_HitsAllValuesInSmallRange()
        {
            var rng = new RngService(1234UL);
            var seen = new bool[4];

            for (int i = 0; i < 1000; i++)
            {
                seen[rng.NextInt(0, 4)] = true;
            }

            for (int v = 0; v < 4; v++)
            {
                Assert.IsTrue(seen[v], $"Value {v} never produced in 1000 draws.");
            }
        }

        [Test]
        public void NextInt_InvalidRange_Throws()
        {
            var rng = new RngService(1UL);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => rng.NextInt(5, 5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => rng.NextInt(5, 4));
        }

        [Test]
        public void NextDouble_StaysWithinUnitInterval()
        {
            var rng = new RngService(555UL);

            for (int i = 0; i < 10000; i++)
            {
                double value = rng.NextDouble();
                Assert.GreaterOrEqual(value, 0.0);
                Assert.Less(value, 1.0);
            }
        }

        [Test]
        public void NextFloat_Range_StaysWithinBounds()
        {
            var rng = new RngService(777UL);

            for (int i = 0; i < 10000; i++)
            {
                float value = rng.NextFloat(-1.5f, 2.5f);
                Assert.GreaterOrEqual(value, -1.5f);
                Assert.LessOrEqual(value, 2.5f);
            }
        }

        [Test]
        public void NextBool_RespectsExtremeProbabilities()
        {
            var rng = new RngService(31337UL);

            for (int i = 0; i < 100; i++)
            {
                Assert.IsFalse(rng.NextBool(0.0), "probability 0 must never be true");
                Assert.IsTrue(rng.NextBool(1.0), "probability 1 must always be true");
            }
        }

        [Test]
        public void Seed_PropertyReportsConstructionSeed()
        {
            Assert.AreEqual(987654321UL, new RngService(987654321UL).Seed);
        }
    }
}
