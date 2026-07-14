namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// A stage (level) was cleared. Published by gameplay — currently by
    /// <see cref="DemoRunDriver"/> until Phases 2–4 land; the real run loop
    /// will publish the identical event.
    /// </summary>
    public struct StageCleared
    {
        public int StageIndex;
        public int ShardsAwarded;
    }

    /// <summary>An interlude has been planned and should be shown.</summary>
    public struct InterludeReady
    {
        public InterludePlan Plan;
    }

    /// <summary>The current interlude changed (e.g. after an upgrade purchase).</summary>
    public struct InterludeUpdated
    {
        public InterludePlan Plan;
    }

    /// <summary>The player dismissed the interlude and the run should move on.</summary>
    public struct InterludeCompleted
    {
        public int StageIndex;
    }

    /// <summary>UI request to spend shards on one ability level. Validated by <see cref="InterludeDirector"/>.</summary>
    public struct UpgradeRequested
    {
        public string AbilityId;
    }

    /// <summary>A passive ability level-up was applied and persisted.</summary>
    public struct PassiveAbilityUpgraded
    {
        public string AbilityId;
        public int NewLevel;
    }

    /// <summary>An NPC joined the player's journey (persisted as a friend).</summary>
    public struct FriendMet
    {
        public string NpcId;
    }
}
