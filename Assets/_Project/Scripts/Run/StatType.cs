namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Base stats that passive abilities increase. Consumed by launch physics
    /// (LaunchPower, Bounciness, AimControl), the run loop (MaxHealth,
    /// ShardGain) and drafting (Luck) as those phases land.
    /// </summary>
    public enum StatType
    {
        MaxHealth = 0,
        LaunchPower = 1,
        ShardGain = 2,
        Luck = 3,
        Bounciness = 4,
        AimControl = 5
    }
}
