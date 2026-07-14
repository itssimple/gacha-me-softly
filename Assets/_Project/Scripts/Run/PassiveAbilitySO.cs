using UnityEngine;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// A passive, levelable ability taught by a friend NPC. Each level adds a
    /// flat bonus to one base stat. Pure content — adding an ability is a new
    /// asset plus localization entries, zero code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "PachiRogue/Passive Ability", fileName = "Ability_")]
    public sealed class PassiveAbilitySO : ScriptableObject
    {
        [Tooltip("Stable content id, e.g. 'glowheart'. Persisted in the save file.")]
        [SerializeField] private string id;

        [SerializeField] private string nameKey;
        [SerializeField] private string descriptionKey;

        [Tooltip("Which base stat each level increases.")]
        [SerializeField] private StatType stat;

        [Tooltip("Flat bonus added to the base stat per level.")]
        [SerializeField] private float perLevelBonus;

        [SerializeField] private int maxLevel = 5;

        [Tooltip("Shard cost of level 1.")]
        [SerializeField] private int baseCost = 15;

        [Tooltip("Additional shard cost per already-owned level.")]
        [SerializeField] private int costPerLevel = 10;

        public AbilityData ToData()
        {
            return new AbilityData
            {
                Id = id,
                NameKey = nameKey,
                DescriptionKey = descriptionKey,
                Stat = stat,
                PerLevelBonus = perLevelBonus,
                MaxLevel = maxLevel,
                BaseCost = baseCost,
                CostPerLevel = costPerLevel
            };
        }
    }
}
