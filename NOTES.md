# NOTES.md — session notes, API discrepancies, manual editor steps

## Phase 0 — Documentation Discovery (session: 2026-07-14, Phases 0–1)

Allowed APIs verified against current Unity 6 documentation/discussions
before writing code:

1. **Object find APIs** — `Object.FindObjectOfType` / `FindObjectsOfType` are
   deprecated in Unity 6. Replacements: `FindFirstObjectByType` (deterministic
   order), `FindAnyObjectByType` (fastest, no order), `FindObjectsByType`
   (takes a sort mode). Confirmed. Project code uses **none of them** — not
   even in bootstrap, which owns its objects directly.
2. **Physics 2D** — `Rigidbody2D.velocity` is obsolete in Unity 6; the
   property is `linearVelocity` (plus `linearVelocityX`/`linearVelocityY`).
   The 2D material class is still named `PhysicsMaterial2D` (properties
   `bounciness`, `friction`). No physics code in Phase 1; recorded for
   Phase 2.
3. **Unity Localization** — current package line is 1.5.x.
   `LocalizedString` + string tables via
   `LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, entry)`
   (async: check `IsDone` / `Completed` / `WaitForCompletion`). SmartFormat
   applies when `IsSmart` is set. No localization code in Phase 1; recorded
   for Phase 4.
4. **Input System** — `EnhancedTouch` enabled via
   `EnhancedTouchSupport.Enable()`. Sensors (`AttitudeSensor`, `Gyroscope`)
   must be enabled explicitly via `InputSystem.EnableDevice()`. Per current
   docs, WebGL does have *sensor* support on mobile browsers since 2021.2,
   but our design keeps gyro assist Android-only (`#if !UNITY_WEBGL`, PLAN.md
   Phase 5) — pointer input is the WebGL path. No input code in Phase 1;
   recorded for Phase 2/5.
5. **WebGL persistence** — PlayerPrefs on WebGL is backed by the browser's
   IndexedDB through Emscripten's virtual filesystem; writes are async and
   there is **no reliable application-quit event in a browser**, so
   `PlayerPrefs.Save()` must be called explicitly after every write.
   `PlayerPrefsSaveBackend` does exactly that. Raw `System.IO` writes to
   `Application.persistentDataPath` on WebGL would additionally require a
   manual `FS.syncfs` JS call — which is why the WebGL save path uses
   PlayerPrefs only, per CLAUDE.md.

## Discrepancies vs PLAN.md / CLAUDE.md assumptions

- **TextMeshPro is not a standalone package in Unity 6.** The
  `com.unity.textmeshpro` package is deprecated; TMP ships inside
  `com.unity.ugui` 2.0 (import "TMP Essential Resources" once in the editor
  when UI work starts in Phase 4). `Packages/manifest.json` therefore lists
  `com.unity.ugui: 2.0.0` instead of a TextMeshPro package. PLAN.md's package
  list has been annotated.
- **JsonUtility + ulong**: Unity's serializer support for `ulong` has been
  inconsistent across versions, so `SaveModelV1` stores the run seed as
  `long runSeedBits` (bit-cast) with a non-serialized `RunSeed` ulong
  accessor. DTO stays primitives/lists-only per CLAUDE.md.
- **GameBootstrap seed field** is `long seedOverride` (not `ulong`) for the
  same serializer-compatibility reason; it is bit-cast to `ulong` at boot.

## Environment limitation (this session)

Unity CLI is **not available** in this environment, and neither is a .NET
compiler. Everything below was generated as source + hand-authored
YAML/meta files so the project opens and compiles in Unity 6 LTS:

- All C# sources, five runtime asmdefs + two test asmdefs.
- `.meta` files with stable GUIDs for every asset/folder (generated; the
  Boot scene references `GameBootstrap.cs` by its meta GUID).
- `Boot.unity` scene YAML (Main Camera + `Bootstrap` GameObject holding
  `GameBootstrap`), registered in `ProjectSettings/EditorBuildSettings.asset`.
- `Packages/manifest.json` with the locked package set.
- `ProjectSettings/ProjectVersion.txt` pinned to `6000.0.32f1` — opening
  with any newer Unity 6 LTS patch is fine; Unity upgrades in place.

### Manual editor steps required (first open, Unity 6 LTS)

1. Open the project folder in Unity Hub with a Unity 6 LTS (6000.0.x)
   editor; let the Package Manager resolve `Packages/manifest.json` and the
   asset database import everything. Unity will generate the remaining
   `ProjectSettings/*.asset` files with defaults.
2. **URP setup (2D)**: Assets → Create → Rendering → URP Asset (with 2D
   Renderer), then assign it under Project Settings → Graphics (and Quality
   levels). This cannot be authored reliably outside the editor.
3. Open `Assets/_Project/Scenes/Boot.unity`. If Unity ever refuses the
   hand-authored scene (it shouldn't — the format is the standard empty-scene
   layout), recreate it: new scene → add empty GameObject `Bootstrap` → add
   component `GameBootstrap` → save over `Boot.unity`. Keep it first in
   Build Settings.
4. Run tests: Window → General → Test Runner → run **EditMode** (RNG
   determinism, save round-trip, event bus, registry) and **PlayMode**
   (bootstrap smoke) suites. All must be green before Phase 2.
5. Enter play mode in Boot and confirm zero errors/warnings in the console.
6. Build checks (Phase 1 checklist): switch platform to Android → build an
   empty development build; repeat for WebGL. Both should compile with the
   Boot scene only.

## Story intro (user-requested, ahead of Phase 4 UI)

"The Last Light of the Sky Shrine": on first boot the player is asked for
their name, which is woven into a three-page intro about running to save the
Sky Shrine's shattered Everlight (ties into the GDD's Meadow / Onsen Town /
Sky Shrine acts). The name persists in `SaveModelV1.playerName`; returning
players get a personalized welcome-back beat instead. `StoryIntroCompleted`
is published on the event bus when the player hits "Begin the run" — the
Phase 4 run loop will start from that signal.

Implementation notes:

- All text lives in the Unity Localization **"Story"** table (en + sv), with
  `{0}` as the player-name argument (plain `String.Format`, not SmartFormat).
  The table assets are generated programmatically by
  `PachiRogue > Localization > Create Story Tables`
  (`Assets/_Project/Scripts/Editor/LocalizationStorySetup.cs`) so both
  locales are authored in code and reviewed together. Keys are constants in
  `Chris.PachiRogue.UI.StoryKeys`.
- The uGUI hierarchy is **built at runtime** (`RuntimeUiFactory`) so the Boot
  scene needs no hand-wired canvas or package-GUID references. It uses the
  legacy `Text`/`InputField` with the built-in `LegacyRuntime.ttf` font
  (`Arial.ttf` was removed from Unity — Phase 0 check). This is a deliberate
  placeholder: Phase 4's real HUD/draft UI moves to TextMeshPro after the
  TMP Essential Resources import.
- `GameBootstrap` now injects services into any scene component implementing
  `Chris.PachiRogue.Core.IServiceConsumer` (scene-wide lookup is allowed in
  bootstrap only, per CLAUDE.md).
- `RuntimeUiFactory.EnsureEventSystem()` has the project's one non-gyro `#if`
  (`ENABLE_INPUT_SYSTEM`) to pick the matching UI input module — commented
  with the PLAN.md reference as required.
- Localized strings are fetched by yielding on `GetLocalizedStringAsync`
  handles — never `WaitForCompletion`, which is unsupported on WebGL.
- New Editor-only assembly `Chris.PachiRogue.Editor`
  (`Assets/_Project/Scripts/Editor`) — not part of the CLAUDE.md runtime
  layout; it holds editor tooling only and never ships in builds.
- Added because SaveModelV1 had not shipped yet: `playerName` field (still
  primitives-only). Had a save been in the wild this would have been a
  SaveModelV2 + migration instead.

### Story intro — manual editor steps

1. After opening the project, run **PachiRogue → Localization → Create Story
   Tables** once (regenerates safely; overwrites entry values).
2. If prompted by the Input System package to enable the new input backends,
   accept — the story UI's event system supports either backend.
3. Enter play mode in `Boot`: name prompt → three story pages (your name
   woven in) → "Begin the run" → outro. Restart play mode to see the
   welcome-back path. Delete the save (or PlayerPrefs key
   `chris.pachirogue.save.v1` on WebGL) to reset.
4. To proof the Swedish text: Window → Asset Management → Localization Scene
   Controls, switch the active locale to `sv` while in play mode.

## Interludes: between-stage story, friends & passive abilities

After every cleared stage an interlude shows: a story beat advancing the
Everlight arc (player name woven in, keys `interlude.stage1..14` +
`interlude.victory`), scripted NPC meetings, and passive-ability upgrades.

- **NPC friends** (content assets in `Assets/_Project/Content/Npcs`): Pip &
  Mirabel (Meadow), Kapp & Momo (Onsen Town), Sora & Yuki (Sky Shrine). New
  friends are met after stages 2, 4, 7, 9 and 12 — the planner picks a
  random unmet NPC from the current act's pool via a forked `IRngService`
  stream (`interlude.stageN`), so which of Sora/Yuki you meet varies by
  seed. Friends persist in `SaveModelV1.metNpcIds`.
- **Passive abilities** (`Content/Abilities`): each friend teaches one —
  Moonberry Snacks (+MaxHealth), Glowheart (+ShardGain), Springy Soak
  (+Bounciness), Lucky Towel (+Luck), Tailwind (+LaunchPower), Steady Paws
  (+AimControl). Levels cost star-shards (`metaCurrency`), cost = base +
  perLevel × currentLevel; levels persist as parallel lists in the save.
  `PlayerStats` computes effective stats (base + Σ bonus×level) — launch
  physics/drafting consume these in Phases 2/4. Shard awards are already
  scaled by the ShardGain stat.
- **Architecture**: `InterludePlanner` (pure, seeded, tested) plans;
  `InterludeDirector` (Run) owns save mutation and validates purchases; UI
  (`InterludeScreenController`) only renders `InterludePlan` snapshots from
  `InterludeReady`/`InterludeUpdated` and publishes `UpgradeRequested` /
  `InterludeCompleted` — per CLAUDE.md, no UI reach-in.
- **`DemoRunDriver` is a placeholder**: it publishes the `StageCleared`
  events real gameplay will publish (stages advance instantly on Continue).
  Remove it from the Boot scene when the Phase 2–4 run loop lands.
- Content `.asset` files were authored as YAML against our own script GUIDs
  (safe — no package GUIDs involved). Verify they load in the editor; if an
  asset shows as broken, recreate via the `PachiRogue` Create-menus with the
  same field values.
- SaveModelV1 extended again pre-ship: `metNpcIds`, `passiveAbilityIds`,
  `passiveAbilityLevels` (parallel lists — JsonUtility has no dictionaries).

Demo flow in play mode: intro → name → 3 story pages → outro → Continue →
15 stages of interludes (meet friends, buy upgrades) → victory screen.

## Phases 2–4 implemented (+ Phase 5 code-side)

The full game loop now exists in code: intro → name → story → aim & launch
(drag slingshot, trajectory preview) → pegs/bumpers/cups/hazards resolve →
score gate → interlude (story/friends/passives) → draft (1 of 3 upgrades)
→ next procgen stage → boss every 5th (placeholder: doubled gate) → stage
15 victory or defeat → meta shards → run again.

Key decisions / deviations to know about:

- **Runtime-constructed gameplay objects.** The body (Rigidbody2D +
  CircleCollider2D + CollisionRouter + squash/stretch child), launcher
  (LineRenderer preview), stage geometry, camera follow, and gyro assist
  are all created from code by `RunDirector`/`StageBuilder` — hand-authoring
  built-in component YAML blind was the riskier path. The Boot scene only
  carries plain MonoBehaviours + content-asset references.
- **Chunks are data-driven graybox layouts** (peg/bumper/hazard positions
  on the ChunkSO) instead of prefab references. The SO keeps an optional
  `artPrefab` field — assign themed prefabs later and the builder uses them
  with zero assembler changes. This deviates from PLAN.md Phase 3's
  "prefab reference" wording; recorded here per the ground rules.
- **Graybox visuals** are procedurally generated circle/square sprites
  (`GrayboxSprites`) — zero texture assets. ASSET_GUIDE.md maps every
  placeholder to a generated-art replacement.
- **Determinism**: per-stage seeds fork off the run seed
  (`RngService(runSeed).Fork("stageN")`) — same run seed replays the same
  stage layouts and drafts (daily-run ready). Physics outcomes still vary
  per device by design (GDD §3).
- **Mid-run resume**: saved at stage start and stage clear, and on
  `OnApplicationPause` (Phase 5). Resume lands at the start of the saved
  stage; a kill during Drafting skips that draft (documented compromise —
  a full Drafting-state resume can come with a SaveModelV2).
- **Victory flow**: stage 15 clear shows the victory interlude as the
  payoff screen; RunEnd overlays only a compact "Run again" strip. Defeat
  shows a full run-end panel. `interlude.victory_header`'s header shows on
  the victory interlude.
- **Phase 5 code-side done**: 60 fps target, pause auto-save, HUD safe
  area, gyro assist behind the sanctioned `#if !UNITY_WEBGL` guard
  (off by default), WebGL-safe localization/persistence throughout.
  Device profiling, ASTC/Brotli build settings, and the physics layer
  matrix are editor/build tasks — see manual steps.
- **Launch economy ballpark** (untuned): 5 launches/stage, peg 10 pts,
  gates 150+40·(stage−1) → 350+70·(stage−1) by act, boss ×2. Expect to
  tune in the editor with real physics feel.

### Phases 2–4 — manual editor steps

1. Regenerate localization: PachiRogue → Localization → Create Story
   Tables (new keys for HUD/draft/run-end/upgrades were added).
2. Play Boot: complete the intro, then drag on screen to aim (mouse in
   editor), release to launch. Clear the gate within 5 launches.
3. Check the physics 2D settings: fixed timestep 0.02 (project default) =
   the GDD's 50 Hz — no change needed unless it was edited.
4. Run the EditMode suite (now ~70 tests incl. assembler determinism,
   draft weighting over 10k rolls, mutator stacking) and the PlayMode
   smoke tests.
5. Android/WebGL: build once per platform; set Android texture compression
   to ASTC and WebGL compression to Brotli in build settings (Phase 5's
   size pass is manual).
6. Balance pass: gates/launch counts in `Content/Recipes`, peg values in
   chunk layouts, upgrade numbers in `Content/Upgrades` — all data, no code.

### Deferred decisions / open questions

- CI (GitHub Actions + Unity CLI, per CLAUDE.md Testing) needs a licensed
  Unity runner (e.g. game-ci) — not set up in this session; propose wiring it
  when the first physics phase lands.
- `com.unity.ide.*` packages (Rider/VS/VSCode integration) were left out of
  the manifest to keep it minimal; Unity adds them automatically per user
  preference.
