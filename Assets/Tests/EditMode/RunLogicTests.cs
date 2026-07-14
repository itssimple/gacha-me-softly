using Chris.PachiRogue.ProcGen;
using Chris.PachiRogue.Run;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class RunLogicTests
    {
        private static RecipeData Recipe()
        {
            return new RecipeData
            {
                ScoreGateBase = 200,
                ScoreGatePerStage = 50,
                BossGateMultiplier = 2f,
                LaunchesPerStage = 5
            };
        }

        [Test]
        public void BossStages_AreEveryFifth()
        {
            Assert.IsFalse(RunLogic.IsBossStage(1));
            Assert.IsFalse(RunLogic.IsBossStage(4));
            Assert.IsTrue(RunLogic.IsBossStage(5));
            Assert.IsTrue(RunLogic.IsBossStage(10));
            Assert.IsTrue(RunLogic.IsBossStage(15));
        }

        [Test]
        public void ScoreGate_ScalesWithStage_AndDoublesOnBoss()
        {
            Assert.AreEqual(200, RunLogic.ScoreGate(Recipe(), 1));
            Assert.AreEqual(250, RunLogic.ScoreGate(Recipe(), 2));
            Assert.AreEqual(800, RunLogic.ScoreGate(Recipe(), 5), "boss: (200+4*50)*2");
        }

        [Test]
        public void Evaluate_ClearBeatsDefeat()
        {
            Assert.AreEqual(LaunchOutcome.StageClear, RunLogic.Evaluate(200, 200, 0));
            Assert.AreEqual(LaunchOutcome.NextLaunch, RunLogic.Evaluate(150, 200, 2));
            Assert.AreEqual(LaunchOutcome.Defeat, RunLogic.Evaluate(150, 200, 0));
        }

        [Test]
        public void LaunchScore_AppliesCupMultiplier()
        {
            Assert.AreEqual(500, RunLogic.LaunchScore(100, 5f));
            Assert.AreEqual(100, RunLogic.LaunchScore(100, 1f));
            Assert.AreEqual(100, RunLogic.LaunchScore(100, 0f), "multiplier never reduces below 1x");
        }

        [Test]
        public void ShardAward_RewardsProgressAndEfficiency()
        {
            Assert.AreEqual(10 + 3 + 5 * 4, RunLogic.ShardAward(1, 4));
            Assert.Greater(RunLogic.ShardAward(10, 0), RunLogic.ShardAward(1, 0));
            Assert.Greater(RunLogic.ShardAward(3, 3), RunLogic.ShardAward(3, 0));
        }

        [Test]
        public void RunEndBonus_VictoryOutpaysDefeat()
        {
            Assert.AreEqual(150, RunLogic.RunEndBonus(true, 15));
            Assert.AreEqual(35, RunLogic.RunEndBonus(false, 7));
            Assert.Greater(RunLogic.RunEndBonus(true, 15), RunLogic.RunEndBonus(false, 14));
        }
    }
}
