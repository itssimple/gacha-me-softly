using System;
using System.Collections.Generic;

namespace Chris.PachiRogue.UI
{
    /// <summary>
    /// Pure-C# state for the story intro: name sanitation and page
    /// sequencing. No UnityEngine types so it is unit-testable headlessly;
    /// <see cref="StoryIntroController"/> renders it.
    /// </summary>
    public sealed class StoryIntroModel
    {
        public const int MaxNameLength = 16;

        private readonly IReadOnlyList<string> _pageKeys;
        private int _pageIndex;

        /// <param name="returningPlayer">
        /// True when a saved player name exists — the returning player gets a
        /// short "welcome back" beat instead of the full intro.
        /// </param>
        public StoryIntroModel(bool returningPlayer)
        {
            _pageKeys = returningPlayer
                ? new[] { StoryKeys.WelcomeBack }
                : new[] { StoryKeys.Page1, StoryKeys.Page2, StoryKeys.Page3 };
        }

        public string CurrentPageKey => _pageKeys[_pageIndex];

        /// <summary>True when the current page is the last story page (its button reads "Begin").</summary>
        public bool IsLastPage => _pageIndex == _pageKeys.Count - 1;

        public int PageCount => _pageKeys.Count;

        /// <summary>
        /// Moves to the next page. Returns false if already on the last page
        /// (the caller should treat the confirming click as "begin").
        /// </summary>
        public bool Advance()
        {
            if (IsLastPage)
            {
                return false;
            }

            _pageIndex++;
            return true;
        }

        /// <summary>
        /// Normalizes raw name input: trims whitespace, collapses internal
        /// whitespace runs to single spaces, clamps to
        /// <see cref="MaxNameLength"/>. Returns an empty string when nothing
        /// usable remains — the caller substitutes the localized default name.
        /// </summary>
        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string[] parts = raw.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string collapsed = string.Join(" ", parts);

            return collapsed.Length <= MaxNameLength
                ? collapsed
                : collapsed.Substring(0, MaxNameLength).TrimEnd();
        }
    }
}
