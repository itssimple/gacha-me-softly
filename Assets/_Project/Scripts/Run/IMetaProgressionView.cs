namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Read-only view of meta progression for planning and UI. Mutation goes
    /// through <see cref="MetaProgression"/>, owned by
    /// <see cref="InterludeDirector"/> (CLAUDE.md: UI never reaches into Run
    /// internals).
    /// </summary>
    public interface IMetaProgressionView
    {
        long Shards { get; }

        bool HasMet(string npcId);

        int GetAbilityLevel(string abilityId);
    }
}
