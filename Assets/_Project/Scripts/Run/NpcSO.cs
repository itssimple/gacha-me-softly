using UnityEngine;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// An NPC the player can meet during interludes. Pure content — adding an
    /// NPC is a new asset plus its localization entries, zero code changes
    /// (CLAUDE.md "ScriptableObjects for content").
    /// </summary>
    [CreateAssetMenu(menuName = "PachiRogue/NPC", fileName = "Npc_")]
    public sealed class NpcSO : ScriptableObject
    {
        [Tooltip("Stable content id, e.g. 'pip'. Persisted in the save file.")]
        [SerializeField] private string id;

        [Tooltip("Localization key for the NPC's display name.")]
        [SerializeField] private string nameKey;

        [Tooltip("Localization key for the meeting dialogue ({0} = player name).")]
        [SerializeField] private string meetLineKey;

        [Tooltip("Act this NPC belongs to: 0 Meadow, 1 Onsen Town, 2 Sky Shrine.")]
        [SerializeField] private int act;

        [Tooltip("Id of the passive ability this friend teaches when met.")]
        [SerializeField] private string grantsAbilityId;

        public NpcData ToData()
        {
            return new NpcData
            {
                Id = id,
                NameKey = nameKey,
                MeetLineKey = meetLineKey,
                Act = act,
                GrantsAbilityId = grantsAbilityId
            };
        }
    }
}
