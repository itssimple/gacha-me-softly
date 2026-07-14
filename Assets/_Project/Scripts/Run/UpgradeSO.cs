using System;
using System.Collections.Generic;
using Chris.PachiRogue.Physics;
using UnityEngine;

namespace Chris.PachiRogue.Run
{
    public enum UpgradeRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2
    }

    [Serializable]
    public struct StatDelta
    {
        public StatType stat;
        public float delta;
    }

    /// <summary>
    /// A draftable run upgrade: zero-or-more physics mutators plus flat stat
    /// deltas, with rarity and localized name/description keys. Pure content
    /// — adding an upgrade is a new asset + localization entries; the draft
    /// system needs zero code changes (CLAUDE.md).
    /// </summary>
    [CreateAssetMenu(menuName = "PachiRogue/Upgrade", fileName = "Upgrade_")]
    public sealed class UpgradeSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string nameKey;
        [SerializeField] private string descriptionKey;
        [SerializeField] private UpgradeRarity rarity;
        [SerializeField] private List<PhysicsMutatorSO> mutators = new List<PhysicsMutatorSO>();
        [SerializeField] private List<StatDelta> statDeltas = new List<StatDelta>();

        public string Id => id;
        public IReadOnlyList<PhysicsMutatorSO> Mutators => mutators;

        public UpgradeData ToData()
        {
            return new UpgradeData
            {
                Id = id,
                NameKey = nameKey,
                DescriptionKey = descriptionKey,
                Rarity = rarity,
                StatDeltas = statDeltas
            };
        }
    }

    /// <summary>Pure snapshot for the draft logic and UI.</summary>
    public sealed class UpgradeData
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public UpgradeRarity Rarity;
        public IReadOnlyList<StatDelta> StatDeltas;
    }
}
