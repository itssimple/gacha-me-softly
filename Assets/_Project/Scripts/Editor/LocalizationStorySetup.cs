using System.Collections.Generic;
using System.IO;
using Chris.PachiRogue.UI;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Chris.PachiRogue.Editor
{
    /// <summary>
    /// One-click authoring of the localization assets for the story intro:
    /// creates the LocalizationSettings, the en + sv locales, and the "Story"
    /// string table collection, then (re)writes every entry in both locales.
    /// Keeping the text here (instead of hand-edited assets) means both
    /// locales are versioned and reviewed together, per CLAUDE.md i18n rules.
    /// Safe to re-run: existing assets are reused, entries are overwritten.
    /// </summary>
    public static class LocalizationStorySetup
    {
        private const string LocalizationRoot = "Assets/_Project/Localization";

        // key -> (english, swedish). {0} is the player's name.
        private static readonly Dictionary<string, (string en, string sv)> StoryEntries =
            new Dictionary<string, (string en, string sv)>
            {
                [StoryKeys.NamePrompt] = (
                    "Before the tale begins… what is your name, traveler?",
                    "Innan sagan börjar … vad heter du, vandrare?"),

                [StoryKeys.NamePlaceholder] = (
                    "Traveler",
                    "Vandrare"),

                [StoryKeys.DefaultName] = (
                    "Traveler",
                    "Vandrare"),

                [StoryKeys.ConfirmName] = (
                    "That's me!",
                    "Det är jag!"),

                [StoryKeys.Next] = (
                    "Next",
                    "Nästa"),

                [StoryKeys.Begin] = (
                    "Begin the run",
                    "Börja färden"),

                [StoryKeys.Page1] = (
                    "High above the meadow, the Sky Shrine's Everlight has shattered. " +
                    "Star-shards are falling like warm rain, {0} — and every one that " +
                    "fades makes the world a little dimmer.",
                    "Högt ovanför ängen har Himmelshelgedomens eviga ljus krossats. " +
                    "Stjärnskärvor faller som varmt regn, {0} — och för varje skärva " +
                    "som slocknar blir världen lite mörkare."),

                [StoryKeys.Page2] = (
                    "The moonberries are wilting. The onsen is running cold. Puni — " +
                    "small, squishy, and far braver than she looks — refuses to let " +
                    "the light go out.",
                    "Månbären vissnar. Källbadet kallnar. Puni — liten, mjuk och långt " +
                    "modigare än hon ser ut — vägrar låta ljuset slockna."),

                [StoryKeys.Page3] = (
                    "So run, {0}. Run to the launch ledge and let Puni fly — bounce " +
                    "her through peg and cup and hazard, catch the shards before they " +
                    "gutter, and carry the light home.",
                    "Så spring, {0}. Spring till avfyrningshyllan och låt Puni flyga — " +
                    "studsa henne mellan pinnar, koppar och faror, fånga skärvorna " +
                    "innan de falnar och bär ljuset hem."),

                [StoryKeys.WelcomeBack] = (
                    "Welcome back, {0}. The shards are still falling — Puni is " +
                    "already waiting at the ledge.",
                    "Välkommen tillbaka, {0}. Skärvorna faller fortfarande — Puni " +
                    "väntar redan vid hyllan."),

                [StoryKeys.Outro] = (
                    "Take a breath at the meadow's edge, {0}. When the pegboards of " +
                    "fate are ready, your first launch awaits…",
                    "Hämta andan vid ängens rand, {0}. När ödets spelplan är redo " +
                    "väntar ditt första kast …")
            };

        [MenuItem("PachiRogue/Localization/Create Story Tables")]
        public static void CreateStoryTables()
        {
            EnsureFolder(LocalizationRoot);
            EnsureSettings();

            Locale english = EnsureLocale(SystemLanguage.English);
            Locale swedish = EnsureLocale(SystemLanguage.Swedish);

            StringTableCollection collection =
                LocalizationEditorSettings.GetStringTableCollection(StoryKeys.Table);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    StoryKeys.Table, LocalizationRoot, new List<Locale> { english, swedish });
            }

            foreach (KeyValuePair<string, (string en, string sv)> entry in StoryEntries)
            {
                SetEntry(collection, english, entry.Key, entry.Value.en);
                SetEntry(collection, swedish, entry.Key, entry.Value.sv);
            }

            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PachiRogue] Story tables written: {StoryEntries.Count} keys × 2 locales (en, sv).");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureSettings()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
            {
                return;
            }

            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Localization Settings";
            AssetDatabase.CreateAsset(settings, $"{LocalizationRoot}/LocalizationSettings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        private static Locale EnsureLocale(SystemLanguage language)
        {
            var identifier = new LocaleIdentifier(language);
            foreach (Locale existing in LocalizationEditorSettings.GetLocales())
            {
                if (existing.Identifier == identifier)
                {
                    return existing;
                }
            }

            Locale locale = Locale.CreateLocale(language);
            AssetDatabase.CreateAsset(locale, $"{LocalizationRoot}/{locale.LocaleName}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void SetEntry(StringTableCollection collection, Locale locale, string key, string value)
        {
            var table = (StringTable)collection.GetTable(locale.Identifier);
            if (table == null)
            {
                table = (StringTable)collection.AddNewTable(locale.Identifier);
            }

            StringTableEntry entry = table.GetEntry(key) ?? table.AddEntry(key, value);
            entry.Value = value;

            EditorUtility.SetDirty(table);
        }
    }
}
