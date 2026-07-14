# CLAUDE.md — PachiRogue (Unity physics roguelite)

Read GAME_DESIGN.md for what we're building and PLAN.md for the phased plan.
Work one phase at a time; do not start a phase until the previous phase's
verification checklist passes.

## Project facts

- Unity 6 LTS, 2D URP template. C# only.
- Platforms: Android (primary), WebGL (secondary). Test both when touching
  input, file I/O, or shaders — WebGL has no threads and no `System.IO`
  persistence beyond PlayerPrefs/IndexedDB.
- Root namespace: `Chris.PachiRogue`. One namespace per assembly.
- Assembly definitions per top-level folder (see Layout). No code in
  `Assembly-CSharp` except bootstrap.

## Layout

```
Assets/
  _Project/
    Scripts/
      Core/          # bootstrap, service registry, save, RNG      (asmdef: Chris.PachiRogue.Core)
      Physics/       # launch, mutators, collision routing          (asmdef: Chris.PachiRogue.Physics)
      ProcGen/       # chunk library, stage assembler, validation   (asmdef: Chris.PachiRogue.ProcGen)
      Run/           # run state machine, stages, drafting, meta    (asmdef: Chris.PachiRogue.Run)
      UI/            # HUD, draft screen, menus                     (asmdef: Chris.PachiRogue.UI)
    Content/         # ScriptableObject assets: upgrades, chunks, enemies, themes
    Art/  Audio/  Prefabs/  Scenes/  Localization/
  Tests/
    EditMode/        # asmdef: Chris.PachiRogue.Tests.EditMode
    PlayMode/        # asmdef: Chris.PachiRogue.Tests.PlayMode
```

## Hard rules

- **Physics in FixedUpdate only.** No transform manipulation on rigidbody
  objects; use `Rigidbody2D` APIs. Visual squash/stretch lives on a child
  sprite object, never on the collider object.
- **All randomness through `IRngService`** (seeded, injectable). Never
  `UnityEngine.Random` in ProcGen or Run assemblies — this is greppable and
  checked in verification.
- **All player-facing strings through Unity Localization string tables**
  (`en`, `sv`). No string literals in UI code. Add both locales in the same
  commit as the key.
- **ScriptableObjects for content, MonoBehaviours for scene glue.** Upgrades,
  chunks, enemies, and themes are data assets; systems consume them. Adding
  an upgrade must require zero code changes to the draft system.
- **No singletons.** A single `ServiceRegistry` composition root in the boot
  scene registers interfaces (`IRngService`, `ISaveService`, `IRunState`, …).
  Systems receive dependencies via constructor/init injection.
- **Events over polling.** Gameplay signals (`StageCleared`, `UpgradeDrafted`,
  `RunEnded`) go through a typed event bus in Core.
- **Saves are JSON** via a versioned DTO layer (`SaveModelV1`). Never
  serialize live game objects. WebGL: persist via PlayerPrefs shim.

## Testing

- EditMode tests are mandatory for: procgen determinism (same seed → identical
  chunk sequence and jitter values), stage validation rules, upgrade stacking
  math, save round-trip.
- PlayMode smoke test: boot scene loads, a run can start, one launch resolves
  without exceptions.
- Run tests via Unity CLI in CI (GitHub Actions, PowerShell steps).

## Verification greps (run before finishing any phase)

```
grep -rn "UnityEngine.Random" Assets/_Project/Scripts/ProcGen Assets/_Project/Scripts/Run   # must be empty
grep -rn "FindObjectOfType\|FindFirstObjectByType" Assets/_Project/Scripts                  # must be empty outside bootstrap
grep -rnE "SetText\(\"|text = \"" Assets/_Project/Scripts/UI                                # must be empty (i18n)
```

## Anti-patterns (do not do these)

- Inventing Unity APIs — verify against docs for Unity 6 before use;
  several 2021-era APIs are renamed or deprecated.
- Making gameplay depend on exact physics reproducibility across devices
  (design constraint — see GAME_DESIGN.md §3).
- Adding packages without noting them in PLAN.md's package list.
- Committing generated `Library/`, `Temp/`, `Logs/` — .gitignore covers this;
  don't fight it.
