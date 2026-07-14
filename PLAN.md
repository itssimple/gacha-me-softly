# PLAN.md — Phased Implementation Plan

Phases are self-contained and executable in fresh Claude Code sessions.
Each phase ends with a verification checklist; do not proceed on red.
Conventions and hard rules: see CLAUDE.md. Design: see GAME_DESIGN.md.

Expected packages (add via Package Manager, note versions in this file when
locked): 2D URP template defaults, Unity Localization, Input System,
Test Framework, TextMeshPro. Nothing else without updating this list.

Locked versions (Phase 1, Packages/manifest.json):
- com.unity.render-pipelines.universal 17.0.4
- com.unity.feature.2d 2.0.1
- com.unity.inputsystem 1.11.2
- com.unity.localization 1.5.12
- com.unity.test-framework 1.4.6
- com.unity.ugui 2.0.0 (TextMeshPro is merged into uGUI in Unity 6 — the
  standalone com.unity.textmeshpro package is deprecated; see NOTES.md)

---

## Phase 0 — Documentation Discovery (ALWAYS FIRST, every session)

Before implementing, verify actual APIs against Unity 6 documentation.
Known traps to check explicitly:

1. `Object.FindObjectOfType` is deprecated → `FindFirstObjectByType` /
   `FindAnyObjectByType` (and we forbid both outside bootstrap anyway).
2. Physics 2D: confirm `Rigidbody2D.linearVelocity` (renamed from `velocity`
   in Unity 6) and `PhysicsMaterial2D` property names before use.
3. Unity Localization: confirm current `LocalizedString` binding workflow and
   string table API; do not assume 2021-era tutorials are accurate.
4. Input System: confirm `EnhancedTouch` API for drag-aim and attitude sensor
   API for gyro (`AttitudeSensor`, availability differs on WebGL — gyro is
   Android-only; WebGL falls back to pointer input).
5. WebGL: confirm current build settings API and PlayerPrefs persistence
   behavior.

Output of Phase 0 per session: a short "Allowed APIs" note in the session
before writing code that uses any of the above areas.

---

## Phase 1 — Project skeleton & Core services

**Implement**
- Unity project (6 LTS, 2D URP), folder layout and asmdefs exactly as in
  CLAUDE.md. `.gitignore` (Unity standard), `Boot` scene.
- `Chris.PachiRogue.Core`:
  - `ServiceRegistry` composition root (plain C# class + one bootstrap
    MonoBehaviour in Boot scene).
  - `IRngService` / `RngService`: xoshiro256** or `System.Random` wrapper,
    constructed from `ulong` seed; supports forking child streams
    (`Fork(string label)`) so ProcGen and loot use independent streams.
  - Typed event bus: `IEventBus` with `Publish<T>(T evt)` /
    `Subscribe<T>(Action<T>)`, struct events.
  - `ISaveService` / JSON save with `SaveModelV1` DTO; WebGL PlayerPrefs
    backend, Android `Application.persistentDataPath` backend behind the
    same interface.

**Verification**
- [ ] EditMode tests: RNG determinism (seed → identical sequence; forked
      streams independent), save round-trip (model → json → model equality).
- [ ] Boot scene enters play mode with zero errors/warnings.
- [ ] Greps from CLAUDE.md all clean.
- [ ] Android and WebGL builds compile (empty scene is fine).

**Anti-pattern guards:** no singletons; no `UnityEngine.Random` anywhere in
Core; DTOs contain only primitives/lists.

---

## Phase 2 — Launch physics & mutator system

**Implement**
- `Chris.PachiRogue.Physics`:
  - `LauncherController`: drag-to-aim (EnhancedTouch + mouse fallback),
    clamped power, trajectory preview (sampled ballistic arc, first 0.5s,
    rendered via LineRenderer).
  - `PhysicsBody`: wrapper on the character's `Rigidbody2D`; exposes
    launch, reset, and a mutator application pipeline.
  - `PhysicsMutatorSO : ScriptableObject` base with
    `Apply(PhysicsBodyContext ctx)` / `Remove(...)`; context carries
    rigidbody, material instance, and event bus.
  - Three concrete mutators to prove composability: `BouncyBootsSO`
    (restitution +), `HeavyCoreSO` (mass +, damage scalar +), `MoonGravitySO`
    (timed gravityScale change on peg-hit event).
  - `CollisionRouter`: single contact listener on the body that publishes
    typed events (`PegHit`, `CupEntered`, `HazardHit`) — content objects
    carry ID components, no per-peg scripts with logic.
- Graybox test scene: static pegs, three cups, launchable body.

**Verification**
- [ ] EditMode tests: mutator stacking (BouncyBoots + HeavyCore both applied
      and both removed cleanly; material instance not shared with asset).
- [ ] PlayMode test: launch in graybox scene resolves; `CupEntered` event
      received.
- [ ] All physics changes occur in FixedUpdate paths; visual child object
      only for squash/stretch.
- [ ] Runs at 60 fps in editor profiler with 200 pegs.

**Anti-pattern guards:** do not mutate shared `PhysicsMaterial2D` assets —
instantiate per body; verify `linearVelocity` naming per Phase 0.

---

## Phase 3 — ProcGen: chunk library & stage assembler

**Implement**
- `Chris.PachiRogue.ProcGen`:
  - `ChunkSO : ScriptableObject`: prefab reference + metadata (difficulty
    band, theme tags, top/bottom connector profile, reward budget,
    hazard count, allow-mirror flag).
  - `StageRecipeSO`: stage index → target height, difficulty band, reward
    budget range, hazard density cap.
  - `StageAssembler` (pure C#, unit-testable): given `(ulong stageSeed,
    StageRecipeSO recipe, IReadOnlyList<ChunkSO> library)` returns a
    `StagePlan` (ordered chunk picks + per-chunk jitter/mirror decisions).
    All randomness via injected `IRngService`.
  - `StageValidator` (pure C#): connector compatibility, reward budget within
    range, hazard cap, no impassable seams. Assembler retries with next
    candidate on failure (bounded retries, then relaxes constraints —
    log a warning).
  - `StageBuilder` (MonoBehaviour side): instantiates a `StagePlan` into the
    scene, applies theme skin.
- Author 8–10 graybox chunks as content to exercise the system.

**Verification**
- [ ] EditMode tests: same seed + recipe + library → byte-identical
      `StagePlan` (this is the determinism contract); validator rejects
      crafted-bad plans; retry path terminates.
- [ ] PlayMode: build 20 stages from sequential seeds without exception;
      body can traverse each (automated: raycast corridor check + one
      scripted launch reaching the bottom sensor).
- [ ] Grep: no `UnityEngine.Random` in ProcGen.

**Anti-pattern guards:** `StageAssembler`/`StageValidator` must not touch
UnityEngine scene APIs (keep them pure for tests); no physics dependence in
validation (layout math only).

---

## Phase 4 — Run loop, drafting & upgrades

**Implement**
- `Chris.PachiRogue.Run`:
  - `RunStateMachine`: `Aiming → Resolving → StageClear → Drafting →
    Advancing → (BossIntro) → … → RunEnd`. Driven by event bus.
  - `RunState`: seed, stage index, launches remaining, HP/score, acquired
    upgrade list; serializable into `SaveModelV1` for mid-run resume.
  - `UpgradeSO`: wraps zero-or-more `PhysicsMutatorSO` refs + stat deltas +
    rarity + localized name/description keys. Draft system: weighted pick of
    3 from eligible pool, no duplicates in one draft.
  - Score gate + launch economy per GAME_DESIGN.md §2; boss stage flag on
    every 5th stage (boss itself can be a placeholder heavy-HP target in
    this phase).
  - Meta-currency award on run end; persisted unlock flags (data only, no
    shop UI yet).
- Minimal HUD (launches left, score, stage index) and draft screen — all
  strings via localization tables (en + sv).

**Verification**
- [ ] EditMode tests: draft weighting (statistical over 10k rolls with fixed
      seed), no-duplicate rule, upgrade stat stacking, run-state save/resume
      round-trip mid-run.
- [ ] PlayMode: full 3-stage mini-run completes end-to-end without input
      (scripted launches).
- [ ] Kill app during Drafting state → relaunch → run resumes at same state.
- [ ] i18n grep clean; switching locale to `sv` at runtime updates HUD.

**Anti-pattern guards:** state machine transitions only via events; no UI
code reaching into Run internals (UI subscribes to events / reads a
read-only view interface).

---

## Phase 5 — Mobile & WebGL hardening

**Implement**
- Android: Input System touch path verified on device, safe-area handling,
  target frame rate 60, texture compression ASTC, app pause = auto-save.
- Gyro assist (Android only): `AttitudeSensor`-driven lateral nudge with
  strength setting and off-by-default; WebGL compiles it out
  (`#if !UNITY_WEBGL`).
- WebGL: build size pass (strip engine code, Brotli), PlayerPrefs save path
  verified, pointer input parity.
- Performance pass: object pooling for pegs/particles, physics layer matrix
  minimized, profiler capture on device attached to repo notes.

**Verification**
- [ ] Android build on a physical mid-range device: 60 fps median in a
      dense stage; pause/resume preserves run.
- [ ] WebGL build < 30 MB compressed; runs in Chrome + Firefox; save
      survives page reload.
- [ ] No `#if` blocks other than the gyro guard without a comment
      referencing this plan.

---

## Phase 6 — Final verification & content ramp entry

1. Re-run all greps and the full test suite; fix any drift.
2. Cross-check implemented APIs against Phase 0 notes (no invented APIs).
3. Confirm every UpgradeSO/ChunkSO added has en + sv strings and appears in
   at least one automated test or validation pass.
4. Tag `v0.1-vertical-slice`. Everything after this is content authoring
   (chunks, upgrades, art themes, bosses) on stable systems.
