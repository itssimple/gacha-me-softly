using System;
using Chris.PachiRogue.ProcGen;

namespace Chris.PachiRogue.Run
{
    public enum LaunchOutcome
    {
        NextLaunch,
        StageClear,
        Defeat
    }

    /// <summary>
    /// Pure run-economy math (score gates, shard awards, launch outcomes) so
    /// the state machine's decisions are unit-testable without a scene.
    /// </summary>
    public static class RunLogic
    {
        public static bool IsBossStage(int stageIndex)
        {
            return stageIndex % 5 == 0;
        }

        public static long ScoreGate(RecipeData recipe, int stageIndex)
        {
            long gate = recipe.ScoreGateBase + (long)recipe.ScoreGatePerStage * (stageIndex - 1);
            if (IsBossStage(stageIndex))
            {
                gate = (long)Math.Round(gate * recipe.BossGateMultiplier);
            }

            return gate;
        }

        /// <summary>Called after every resolved launch.</summary>
        public static LaunchOutcome Evaluate(long stageScore, long gate, int launchesRemaining)
        {
            if (stageScore >= gate)
            {
                return LaunchOutcome.StageClear;
            }

            return launchesRemaining > 0 ? LaunchOutcome.NextLaunch : LaunchOutcome.Defeat;
        }

        /// <summary>Score contributed by one launch: accumulated peg/bumper value × cup multiplier.</summary>
        public static long LaunchScore(long accumulated, float cupMultiplier)
        {
            return (long)Math.Round(accumulated * Math.Max(1f, cupMultiplier));
        }

        /// <summary>Base star-shards for clearing a stage (before the ShardGain stat scales it).</summary>
        public static int ShardAward(int stageIndex, int launchesLeft)
        {
            return 10 + stageIndex * 3 + launchesLeft * 5;
        }

        /// <summary>Run-end meta bonus: victory pays big, defeat still pays out progress.</summary>
        public static int RunEndBonus(bool victory, int stageReached)
        {
            return victory ? 150 : 5 * stageReached;
        }
    }
}
