# Game Design Document — Codename "PachiRogue"

> Working title only. Physics roguelite with cute anime aesthetics and procedurally generated stages.
> Platforms: Android (primary), WebGL (secondary). Engine: Unity 6 (2D), C#.

## 1. Pitch

A run-based physics roguelite: the player aims and launches a physics-driven character
("Puni", a squishy anime blob-girl mascot — placeholder) into a procedurally generated
vertical stage filled with pegs, bumpers, hazards, enemies, and multiplier cups. Physics
resolves the chaos; the player collects rewards, then drafts one of three upgrades that
*changes the physics behavior* (bounciness, mass, splitting, gravity flips, sticky surfaces).
Runs escalate through stages to a boss. Death or victory feeds meta-currency into permanent
unlocks.

Reference points: Peglin (pachinko roguelite), Cup Heroes (multiplier cups), Vampire
Survivors (session length + meta loop). Differentiator: the upgrade system mutates the
physics simulation itself, not just numbers.

## 2. Core Loop

1. **Aim** — drag-to-aim with trajectory preview (first ~0.5s of arc only). Optional gyro
   nudge mid-fall as an assist mechanic (toggleable; must never be required).
2. **Launch & resolve** — character falls/bounces through the stage. Collisions trigger
   damage, pickups, multipliers, status effects.
3. **Score gate** — stage clears when damage/collection threshold is met within N launches.
4. **Draft** — pick 1 of 3 upgrades (rarity-weighted). Upgrades are physics mutators,
   stat boosts, or trigger relics.
5. **Advance** — next procgen stage, difficulty scales. Boss every 5th stage.
6. **Run end** — victory (stage 15 boss) or defeat. Meta-currency awarded either way.
7. **Meta** — permanent unlocks: new characters (different base physics), new upgrade
   pools, starting relics, cosmetics.

Target session: one stage = 60–90 seconds; full run = 15–25 minutes; resumable mid-run
(critical for mobile).

## 3. Physics Design

- Unity 2D physics, fixed timestep 50 Hz. All gameplay physics in `FixedUpdate`.
- **Determinism policy:** true cross-platform determinism with PhysX/Box2D floats is not
  guaranteed. We require *seed-determinism for generation only* (same seed → same stage
  layout). Physics outcomes may vary per device; design must never depend on exact
  replay of physics (no ghost replays in v1).
- Physics mutators are composable `ScriptableObject` effects applied to the character's
  rigidbody/material at launch time (see ARCHITECTURE section in PLAN.md).
- Example mutators: `BouncyBoots` (+restitution), `HeavyCore` (+mass, +damage,
  −control), `Splitter` (spawn 2 child bodies on first wall hit), `MoonGravity`
  (gravityScale 0.4 for 3s after each peg hit), `StickyPaws` (briefly adheres to
  surfaces, re-aim once per stage).

## 4. Procedural Generation

- **Chunk-stitching model:** stages are assembled from authored chunk patterns
  (ScriptableObject prefab groups tagged by difficulty, theme, and connector shape),
  selected and mutated by a seeded RNG. Guarantees playability (authored chunks) while
  keeping layouts fresh (selection + parameter jitter + mirroring).
- Seeded via `ulong` run seed → per-stage seeds. Same seed = same run layout, enabling
  daily-run mode later.
- Validation pass after assembly: reachability check (no dead zones), reward budget
  check, hazard density cap by stage index.
- Themes (art sets, one per act): Meadow, Onsen Town, Sky Shrine. Cute anime palette,
  thick outlines, cel-shade look.

## 5. Art Direction

- Chibi/cute anime style: 2–3 head-tall characters, big expressive eyes, pastel palette
  with high-saturation accents on interactables (readability on small screens is the
  priority — interactables must pop against background).
- Squash-and-stretch on the physics body (visual only; never affects colliders).
- Particles and hit-stop kept cheap: target 60 fps on mid-range Android (e.g. 2022-era
  Snapdragon 7-series).

## 6. Monetization & Distribution (v1)

- Premium one-time purchase on Android, no ads, no IAP (market data supports premium
  mobile roguelites). WebGL build as free demo (first act only) on itch.io for wishlist
  and feedback funnel.

## 7. i18n

- English + Swedish from day one. All player-facing strings via Unity Localization
  package string tables. No hardcoded strings — enforced by convention and a grep check
  in CI.

## 8. Out of Scope for v1 (do not build)

- Multiplayer, ghost replays, gacha/live-ops, cloud save, controller support,
  iOS build (deferred, not rejected).
