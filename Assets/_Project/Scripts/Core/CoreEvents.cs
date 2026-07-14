namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// Published when the player finishes the story intro (name confirmed,
    /// last story page dismissed). Downstream systems (Run, later) start the
    /// first run from this signal.
    /// </summary>
    public struct StoryIntroCompleted
    {
        public string PlayerName;
    }
}
