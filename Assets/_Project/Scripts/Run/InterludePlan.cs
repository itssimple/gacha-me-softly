using System.Collections.Generic;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Everything the interlude screen needs to render one between-stages
    /// story beat: narrative key, optional NPC meeting, upgrade offers, and
    /// shard balances. Produced by <see cref="InterludePlanner"/>.
    /// </summary>
    public sealed class InterludePlan
    {
        public int ClearedStageIndex;
        public bool IsVictory;
        public string BeatKey;
        public string PlayerName;
        public int ShardsAwarded;
        public long ShardBalance;

        /// <summary>NPC met in this interlude, or null.</summary>
        public NpcData MetNpc;

        /// <summary>Upgrade offers for abilities taught by met friends, in content order.</summary>
        public List<AbilityOffer> Offers = new List<AbilityOffer>();
    }

    public sealed class AbilityOffer
    {
        public string AbilityId;
        public string NameKey;
        public string DescriptionKey;
        public int CurrentLevel;
        public int MaxLevel;
        public int UpgradeCost;
        public bool IsMaxed;
        public bool Affordable;
    }
}
