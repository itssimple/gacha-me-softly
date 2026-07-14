using UnityEngine;

namespace Chris.PachiRogue.ProcGen
{
    /// <summary>Per-act stage parameters consumed by the assembler and the run loop.</summary>
    [CreateAssetMenu(menuName = "PachiRogue/Stage Recipe", fileName = "Recipe_")]
    public sealed class StageRecipeSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private int chunkCount = 3;
        [SerializeField] private int maxDifficultyBand;
        [SerializeField] private int rewardBudgetMin = 100;
        [SerializeField] private int rewardBudgetMax = 700;
        [SerializeField] private int hazardCap = 3;
        [SerializeField] private int scoreGateBase = 200;
        [SerializeField] private int scoreGatePerStage = 60;
        [SerializeField] private float bossGateMultiplier = 2f;
        [SerializeField] private int launchesPerStage = 5;

        public RecipeData ToData()
        {
            return new RecipeData
            {
                Id = id,
                ChunkCount = chunkCount,
                MaxDifficultyBand = maxDifficultyBand,
                RewardBudgetMin = rewardBudgetMin,
                RewardBudgetMax = rewardBudgetMax,
                HazardCap = hazardCap,
                ScoreGateBase = scoreGateBase,
                ScoreGatePerStage = scoreGatePerStage,
                BossGateMultiplier = bossGateMultiplier,
                LaunchesPerStage = launchesPerStage
            };
        }
    }

    public sealed class RecipeData
    {
        public string Id;
        public int ChunkCount;
        public int MaxDifficultyBand;
        public int RewardBudgetMin;
        public int RewardBudgetMax;
        public int HazardCap;
        public int ScoreGateBase;
        public int ScoreGatePerStage;
        public float BossGateMultiplier;
        public int LaunchesPerStage;
    }
}
