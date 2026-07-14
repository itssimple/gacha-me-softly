using System;
using System.Collections.Generic;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Versioned save DTO. Contains only primitives and lists — never live
    /// game objects (see CLAUDE.md "Saves are JSON"). Serialized with
    /// <c>UnityEngine.JsonUtility</c>; a new version of this model means a new
    /// class (SaveModelV2) plus a migration step, not edits to this one once
    /// shipped.
    /// </summary>
    [Serializable]
    public class SaveModelV1
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        // Player profile. Empty playerName means the intro (name entry) has
        // not been completed yet.
        public string playerName = "";

        // Run seed stored as long bits: JsonUtility's ulong support has been
        // inconsistent across Unity versions, long is safe everywhere.
        public long runSeedBits;

        public bool isRunActive;
        public int stageIndex;
        public int launchesRemaining;
        public int hp;
        public long score;
        public List<string> acquiredUpgradeIds = new List<string>();

        // Meta progression: star-shards (the meta currency), befriended NPCs,
        // and passive-ability levels as parallel lists (JsonUtility has no
        // dictionary support).
        public long metaCurrency;
        public List<string> metNpcIds = new List<string>();
        public List<string> passiveAbilityIds = new List<string>();
        public List<int> passiveAbilityLevels = new List<int>();
        public List<string> unlockFlags = new List<string>();

        public long savedAtUnixUtc;

        /// <summary>Convenience accessor for the run seed as ulong (not serialized).</summary>
        public ulong RunSeed
        {
            get { return unchecked((ulong)runSeedBits); }
            set { runSeedBits = unchecked((long)value); }
        }
    }
}
