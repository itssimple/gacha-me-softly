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
                    "väntar ditt första kast …"),

                // --- UI chrome / interlude screen -------------------------------
                ["ui.continue"] = (
                    "Continue",
                    "Fortsätt"),
                ["ui.upgrade_button"] = (
                    "{0} — Lv {1}/{2} · {3} shards",
                    "{0} — nivå {1}/{2} · {3} skärvor"),
                ["ui.upgrade_maxed"] = (
                    "{0} — Lv {1} (MAX)",
                    "{0} — nivå {1} (MAX)"),
                ["interlude.header"] = (
                    "Stage {0} cleared!",
                    "Nivå {0} avklarad!"),
                ["interlude.victory_header"] = (
                    "The Everlight is whole again!",
                    "Det eviga ljuset är helt igen!"),
                ["interlude.shards"] = (
                    "You gathered {0} star-shards (satchel: {1}).",
                    "Du samlade {0} stjärnskärvor (i väskan: {1})."),
                ["interlude.joined"] = (
                    "{0} joined your journey!",
                    "{0} har slagit följe med dig!"),
                ["interlude.upgrade_title"] = (
                    "Spend star-shards on your friends' gifts:",
                    "Lägg stjärnskärvor på dina vänners gåvor:"),

                // --- Interlude story beats (after clearing stage N; {0} = name) --
                ["interlude.stage1"] = (
                    "One shard rests safe in Puni's satchel. The meadow grass leans " +
                    "toward it, drinking the glow. \"Fourteen to go,\" you whisper, " +
                    "{0}, and start running.",
                    "En skärva vilar tryggt i Punis väska. Ängens gräs böjer sig mot " +
                    "den och dricker skenet. \"Fjorton kvar,\" viskar du, {0}, och " +
                    "börjar springa."),
                ["interlude.stage2"] = (
                    "The moonberry rows glimmer faintly again. Word of the runner " +
                    "with the bouncing star spreads from burrow to burrow.",
                    "Månbärsraderna skimrar svagt igen. Ryktet om löparen med den " +
                    "studsande stjärnan sprids från gryt till gryt."),
                ["interlude.stage3"] = (
                    "You cross the old fence at the meadow's edge — farther from " +
                    "home than you've ever been, {0}.",
                    "Du korsar den gamla gärdesgården vid ängens rand — längre " +
                    "hemifrån än du någonsin varit, {0}."),
                ["interlude.stage4"] = (
                    "Unlit lanterns line the road to the bathhouses. Not for long, " +
                    "if you can help it.",
                    "Olyckta lyktor kantar vägen mot badhusen. Inte länge till, om " +
                    "du får bestämma."),
                ["interlude.stage5"] = (
                    "The Meadow Warden bows its mossy head and lets you pass. The " +
                    "first act of the tale is yours, {0}.",
                    "Ängsväktaren böjer sitt mossiga huvud och släpper förbi dig. " +
                    "Sagans första akt är din, {0}."),
                ["interlude.stage6"] = (
                    "Steam curls over Onsen Town's rooftops. The water runs " +
                    "lukewarm — but the shards you carry make it shiver with sparks.",
                    "Ånga ringlar över Onsenstadens tak. Vattnet är ljummet — men " +
                    "skärvorna du bär får det att gnistra."),
                ["interlude.stage7"] = (
                    "The bathhouse lanterns flicker back to life one by one as you " +
                    "pass, {0}.",
                    "Badhusens lyktor flämtar till liv en efter en där du passerar, " +
                    "{0}."),
                ["interlude.stage8"] = (
                    "Halfway. You count the shards through the satchel cloth by " +
                    "their warmth alone.",
                    "Halvvägs. Du räknar skärvorna genom väskans tyg, bara på " +
                    "värmen."),
                ["interlude.stage9"] = (
                    "The town's oldest bath master says the shrine's stair has gone " +
                    "dark. You will need friends up there.",
                    "Stadens äldsta badmästare säger att helgedomens trappa har " +
                    "slocknat. Du kommer att behöva vänner där uppe."),
                ["interlude.stage10"] = (
                    "The Onsen Guardian sinks back into its pool, soothed. Above " +
                    "the steam you can see the shrine's silhouette now, {0}.",
                    "Onsenväktaren sjunker tillbaka ner i sin källa, blidkad. " +
                    "Ovanför ångan skymtar du nu helgedomens silhuett, {0}."),
                ["interlude.stage11"] = (
                    "The stair of cloud-stone holds your weight — barely. Don't " +
                    "look down. Puni does, and giggles.",
                    "Trappan av molnsten bär din vikt — nätt och jämnt. Titta inte " +
                    "ner. Puni gör det, och fnittrar."),
                ["interlude.stage12"] = (
                    "Wind chimes ring with no wind. The shrine knows you're coming, " +
                    "{0}.",
                    "Vindspel klingar utan vind. Helgedomen vet att du är på väg, " +
                    "{0}."),
                ["interlude.stage13"] = (
                    "The Everlight's cradle is close — a crown of cold prongs " +
                    "against the stars, waiting to be lit.",
                    "Det eviga ljusets vagga är nära — en krona av kalla taggar mot " +
                    "stjärnorna, som väntar på att tändas."),
                ["interlude.stage14"] = (
                    "One last ledge. Every friend you've made is watching from " +
                    "below. Run, {0}.",
                    "En sista avsats. Alla vänner du funnit ser på därifrån " +
                    "nedanför. Spring, {0}."),
                ["interlude.victory"] = (
                    "The Everlight blooms. Warm rain turns to warm light over " +
                    "meadow, town and shrine — and the tale, {0}, will remember the " +
                    "one who ran.",
                    "Det eviga ljuset slår ut i blom. Det varma regnet blir varmt " +
                    "ljus över äng, stad och helgedom — och sagan, {0}, kommer att " +
                    "minnas den som sprang."),

                // --- NPCs ({0} = player name in meet lines) ----------------------
                ["npc.pip.name"] = (
                    "Pip",
                    "Pip"),
                ["npc.pip.meet"] = (
                    "A moonberry sprite tumbles out of a wilted bush and salutes. " +
                    "\"Pip! At your service, {0}! I know where the sweet ones grow.\"",
                    "En månbärsalv ramlar ut ur en vissnad buske och gör honnör. " +
                    "\"Pip! Till din tjänst, {0}! Jag vet var de söta växer.\""),
                ["npc.mirabel.name"] = (
                    "Mirabel",
                    "Mirabel"),
                ["npc.mirabel.meet"] = (
                    "A moth-fairy drifts down, lantern first. \"Mirabel, keeper of " +
                    "lights. Yours, {0}, is the brightest I've seen in years.\"",
                    "En malfé dalar ner, med lyktan först. \"Mirabel, ljusens " +
                    "väkterska. Ditt ljus, {0}, är det klaraste jag sett på åratal.\""),
                ["npc.kapp.name"] = (
                    "Kapp",
                    "Kapp"),
                ["npc.kapp.meet"] = (
                    "A capybara in a steam-fogged pince-nez nods slowly. \"Kapp. " +
                    "The water remembers you, little runner. Rest here whenever.\"",
                    "En kapybara i immig pincené nickar långsamt. \"Kapp. Vattnet " +
                    "minns dig, lilla löpare. Vila här när du vill.\""),
                ["npc.momo.name"] = (
                    "Momo",
                    "Momo"),
                ["npc.momo.meet"] = (
                    "A red panda peers over a tower of folded towels. \"Momo! " +
                    "Careful — oh, you're the shard runner! Take the fluffy one.\"",
                    "En röd panda kikar fram över ett torn av vikta handdukar. " +
                    "\"Momo! Försiktigt — åh, det är du som är skärvlöparen! Ta den " +
                    "fluffiga.\""),
                ["npc.sora.name"] = (
                    "Sora",
                    "Sora"),
                ["npc.sora.meet"] = (
                    "A koi of cloud and starlight circles you once. \"Sora. I swim " +
                    "the sky roads. Hold fast to me when the wind turns, {0}.\"",
                    "En koi av moln och stjärnljus kretsar ett varv runt dig. " +
                    "\"Sora. Jag simmar längs himlens vägar. Håll fast i mig när " +
                    "vinden vänder, {0}.\""),
                ["npc.yuki.name"] = (
                    "Yuki",
                    "Yuki"),
                ["npc.yuki.meet"] = (
                    "A snow fox bows, tails brushing frost from the steps. \"Yuki. " +
                    "I kept the shrine while the light slept. It dreams of you, {0}.\"",
                    "En snöräv bugar, och svansarna sopar frost från trappstegen. " +
                    "\"Yuki. Jag vaktade helgedomen medan ljuset sov. Det drömmer " +
                    "om dig, {0}.\""),

                // --- Passive abilities -------------------------------------------
                ["ability.moonberry_snacks.name"] = (
                    "Moonberry Snacks",
                    "Månbärssnacks"),
                ["ability.moonberry_snacks.desc"] = (
                    "Pip packs provisions. Each level raises Puni's maximum health.",
                    "Pip packar matsäck. Varje nivå höjer Punis maxhälsa."),
                ["ability.glowheart.name"] = (
                    "Glowheart",
                    "Glödhjärta"),
                ["ability.glowheart.desc"] = (
                    "Mirabel's lantern-blessing. Each level increases the " +
                    "star-shards you gather.",
                    "Mirabels lyktvälsignelse. Varje nivå ökar stjärnskärvorna du " +
                    "samlar."),
                ["ability.springy_soak.name"] = (
                    "Springy Soak",
                    "Studsigt bad"),
                ["ability.springy_soak.desc"] = (
                    "Kapp's mineral waters. Each level makes Puni bounce a little " +
                    "livelier.",
                    "Kapps mineralvatten. Varje nivå får Puni att studsa lite " +
                    "livligare."),
                ["ability.lucky_towel.name"] = (
                    "Lucky Towel",
                    "Turhandduk"),
                ["ability.lucky_towel.desc"] = (
                    "Momo's fluffiest fold. Each level nudges fortune toward rarer " +
                    "finds.",
                    "Momos fluffigaste vikning. Varje nivå knuffar turen mot " +
                    "sällsyntare fynd."),
                ["ability.tailwind.name"] = (
                    "Tailwind",
                    "Medvind"),
                ["ability.tailwind.desc"] = (
                    "Sora swims ahead of your launches. Each level adds launch power.",
                    "Sora simmar före dina kast. Varje nivå ger mer kastkraft."),
                ["ability.steady_paws.name"] = (
                    "Steady Paws",
                    "Stadiga tassar"),
                ["ability.steady_paws.desc"] = (
                    "Yuki steadies your aim on the ledge. Each level improves " +
                    "control.",
                    "Yuki stadgar ditt sikte på avsatsen. Varje nivå förbättrar " +
                    "kontrollen.")
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
