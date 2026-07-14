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

    /// <summary>Run lifecycle phases (transitions only via events — CLAUDE.md).</summary>
    public enum RunPhase
    {
        Idle,
        Advancing,
        BossIntro,
        Aiming,
        Resolving,
        StageClear,
        Interlude,
        Drafting,
        RunEnd
    }

    public struct RunStarted
    {
        public ulong Seed;
    }

    public struct RunPhaseChanged
    {
        public RunPhase Phase;
    }

    public struct StageStarted
    {
        public int StageIndex;
        public bool IsBoss;
        public long ScoreGate;
    }

    public struct LaunchesChanged
    {
        public int Remaining;
    }

    public struct ScoreChanged
    {
        public long StageScore;
        public long Gate;
    }

    public struct HealthChanged
    {
        public int Current;
        public int Max;
    }

    /// <summary>Three draft choices are ready for the player.</summary>
    public struct UpgradeChoicesReady
    {
        public System.Collections.Generic.List<UpgradeData> Choices;
    }

    /// <summary>UI request to draft one of the offered upgrades.</summary>
    public struct UpgradePickRequested
    {
        public string UpgradeId;
    }

    public struct UpgradeDrafted
    {
        public string UpgradeId;
    }

    public struct RunEnded
    {
        public bool Victory;
        public int StageReached;
        public int ShardBonus;
    }

    /// <summary>UI request to start a fresh run after RunEnd.</summary>
    public struct RunRestartRequested
    {
    }
}
