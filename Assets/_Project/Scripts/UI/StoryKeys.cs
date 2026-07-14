namespace Chris.PachiRogue.UI
{
    /// <summary>
    /// String table + entry keys for the story intro. Keys are the only
    /// string constants allowed here — the player-facing text itself lives in
    /// the Unity Localization "Story" table (en + sv), authored by the editor
    /// utility under Assets/_Project/Scripts/Editor.
    /// </summary>
    public static class StoryKeys
    {
        public const string Table = "Story";

        public const string NamePrompt = "story.name_prompt";
        public const string NamePlaceholder = "story.name_placeholder";
        public const string DefaultName = "story.default_name";
        public const string ConfirmName = "story.confirm_name";
        public const string Next = "story.next";
        public const string Begin = "story.begin";

        public const string Page1 = "story.page1";
        public const string Page2 = "story.page2";
        public const string Page3 = "story.page3";
        public const string WelcomeBack = "story.welcome_back";
        public const string Outro = "story.outro";
    }
}
