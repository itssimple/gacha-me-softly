namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Plain-C# snapshot of a <see cref="PassiveAbilitySO"/> for the pure
    /// planning/stat code paths.
    /// </summary>
    public sealed class AbilityData
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public StatType Stat;
        public float PerLevelBonus;
        public int MaxLevel;
        public int BaseCost;
        public int CostPerLevel;

        /// <summary>Shard cost to go from <paramref name="currentLevel"/> to the next level.</summary>
        public int UpgradeCost(int currentLevel)
        {
            return BaseCost + CostPerLevel * currentLevel;
        }
    }
}
