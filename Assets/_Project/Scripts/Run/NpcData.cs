namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Plain-C# snapshot of an <see cref="NpcSO"/>, so
    /// <see cref="InterludePlanner"/> stays free of UnityEngine types and
    /// unit-testable headlessly.
    /// </summary>
    public sealed class NpcData
    {
        public string Id;
        public string NameKey;
        public string MeetLineKey;
        public int Act;
        public string GrantsAbilityId;
    }
}
