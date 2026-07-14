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

### Deferred decisions / open questions

- CI (GitHub Actions + Unity CLI, per CLAUDE.md Testing) needs a licensed
  Unity runner (e.g. game-ci) — not set up in this session; propose wiring it
  when the first physics phase lands.
- `com.unity.ide.*` packages (Rider/VS/VSCode integration) were left out of
  the manifest to keep it minimal; Unity adds them automatically per user
  preference.
