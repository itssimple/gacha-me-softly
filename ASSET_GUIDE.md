# ASSET_GUIDE.md — Art & Audio Production Guide (prompt form)

How to produce every asset the game needs, as copy-paste prompts for
generative tools, plus technical specs so the results drop into Unity
without rework. The graybox renders coloured circles/squares today; every
sprite below replaces one of those programmatically-drawn placeholders.

## 0. Recommended tools (as of mid-2026)

| Task | First choice | Alternatives | Why |
|---|---|---|---|
| Character & NPC art | **Midjourney v7** (with `--sref` style refs) | OpenAI **gpt-image-1**, Google **Imagen 4** | Best stylistic quality; `--sref`/`--cref` keep the cast consistent across dozens of images |
| Icons, UI, flat sprites | **Recraft V3** | gpt-image-1 (native transparent PNG), Ideogram 3 | Recraft outputs vector/SVG and clean transparent PNGs — ideal for 9-slice panels and icon sets |
| Game-asset pipelines w/ style lock | **Scenario** or **Layer.ai** | Local **Flux.1 [dev]** + custom LoRA | Train a style model once on approved concepts, then batch-generate pegs/props that all match |
| Backgrounds / parallax | Midjourney v7 or Imagen 4 | Flux.1 [dev] | Wide-format scenic output, strong lighting |
| Upscaling / cleanup | **Magnific**, Topaz Gigapixel | — | Print-quality upscales of low-res picks |
| Sound effects | **ElevenLabs SFX** | Stable Audio 2 | Text-to-SFX with duration control |
| Music | **Suno v4.5** or **Udio** | Stable Audio 2 | Full loops with mood control — check the commercial-license tier before shipping |
| Fonts | Google Fonts (not generated) | — | **Baloo 2** (display), **Nunito** (body) — both OFL, both have Swedish å/ä/ö. Import as TMP font assets |

**Licensing note:** we ship a *premium* game (GDD §6). Before production
use, confirm each tool's current commercial-use terms on a paid tier, and
keep a record of which tool produced which asset. Do not use tools whose
output can't be used commercially. Run generated art past a human artist
for cleanup and consistency — treat generations as 80% drafts.

**Workflow:** 1) generate the style anchor set → 2) lock it (Midjourney
`--sref`, or LoRA in Scenario/Layer/Flux) → 3) batch per-category prompts
below → 4) cleanup/upscale → 5) import per §9 specs.

## 1. Global style anchor (prepend to EVERY image prompt)

> Cute chibi anime game art, 2–3 heads tall proportions, big expressive
> eyes, thick clean outlines, soft cel shading, pastel palette (lavender,
> mint, peach, powder blue) with high-saturation warm-orange accents on
> interactive elements, gentle night-time glow, kawaii mobile game
> aesthetic in the spirit of cozy Japanese casual games. Flat 2D, no
> photorealism, no gradients banding, readable at small sizes.

Suggested negative prompt (where supported):
> photorealistic, 3D render, western cartoon, muddy colors, thin lines,
> horror, text, watermark, extra fingers, complex background clutter

## 2. Characters

### 2.1 Puni (the launched mascot — replaces the pink circle)
- `puni_idle.png`, `puni_stretch.png`, `puni_squash.png`, `puni_happy.png`, `puni_dizzy.png` — 512×512, transparent
- The physics body is a circle (radius 0.25 u); art must read as round.

> [style anchor] Character sheet of "Puni", a squishy round blob-girl
> mascot: a pastel-pink translucent jelly ball with a tiny cute anime girl
> face (big sparkly eyes, tiny mouth), two stubby nub arms, a small ahoge
> hair curl on top, faint star sparkles inside her jelly body. Poses:
> perfectly round neutral; vertically stretched mid-fall (excited); squashed
> flat on impact (cheeks puffed); happy closed-eye smile; dizzy with swirl
> eyes. White background, full body, centered, no ground shadow.

### 2.2 NPC friends (portraits for interlude meetings, 6 total)
- `npc_<id>_portrait.png` — 768×768, transparent, bust/three-quarter; ids: `pip`, `mirabel`, `kapp`, `momo`, `sora`, `yuki`
- Two expressions each: neutral + delighted.

> [style anchor] Portrait of **Pip**, a tiny moonberry sprite: a
> berry-sized fairy child with round cheeks, leaf-green bob haircut, a
> moonberry-purple onesie with a leaf hood, holding one glowing silver
> berry, mid-salute, cheeky grin. Bust shot, transparent background.

> [style anchor] Portrait of **Mirabel**, an elegant moth-fairy
> lanternkeeper: soft cream-and-lilac moth wings with eye-spots, fluffy
> collar, holding a small brass lantern with warm glow, serene smile,
> antennae with tiny lights. Bust shot, transparent background.

> [style anchor] Portrait of **Kapp**, a large calm capybara bath master:
> steam-fogged pince-nez glasses, a folded head-towel, half-lidded
> content eyes, surrounded by gentle onsen steam wisps. Bust shot,
> transparent background.

> [style anchor] Portrait of **Momo**, an eager red panda towel-folder:
> striped tail curled around a wobbling tower of pastel folded towels,
> wide sparkling eyes, one paw raised in greeting. Bust shot, transparent.

> [style anchor] Portrait of **Sora**, a koi spirit made of cloud and
> starlight: translucent white-blue koi body with drifting cloud fins,
> tiny stars trailing, calm wise anime eyes, softly luminous. Bust shot,
> transparent background.

> [style anchor] Portrait of **Yuki**, a small snow-fox shrine attendant:
> two fluffy white tails dusted with frost, a tiny red shrine-maiden bow,
> gentle bow pose, breath visible as sparkle mist. Bust shot, transparent.

### 2.3 Act bosses (placeholder heavy targets today; 3 needed later)
> [style anchor] Full-body of the **Meadow Warden**, a gentle giant boss:
> a moss-covered stone tortoise the size of a hill, wildflowers and one
> small tree growing on its shell, sleepy kind eyes, chibi proportions.
> Side view, transparent background, 1024×1024.

(Repeat for **Onsen Guardian** — a huge relaxed salamander half-submerged
in a wooden bath pool; and the **Shrine Shadow** — a sad translucent
night-sky spirit clutching the dimmed Everlight cradle.)

## 3. Stage interactables (replace the graybox circles)

All: transparent PNG, centered, generated at 512×512 (in-game display
~64 px), strong silhouette, warm-orange accent per style anchor. One set
per theme (meadow / onsen / sky) — batch with a locked style model.

- `peg_<theme>.png` — > [style anchor] A single round game peg: polished
  wooden knob with a glowing warm-orange ring and tiny star emblem,
  meadow-flower detail. Flat icon-style game sprite, centered.
- `bumper_<theme>.png` — > [style anchor] A round bouncy bumper: puffy
  pink-white mochi cushion with a glowing ring, slight squash, sparkle.
- `hazard_<theme>.png` — > [style anchor] A cute-but-clearly-dangerous
  hazard: a grumpy thorn-ball with tiny angry eyebrows and purple-red
  spikes. Reads as "avoid me" instantly.
- `cup_x2.png` / `cup_x5.png` — > [style anchor] An open reward cup:
  ceramic tea-bowl with a glowing rim, floating "×2" (gold "×5" for the
  center cup) star-shard sparkles rising from inside.
- `shard.png` (currency icon, also HUD) — > [style anchor] A single
  star-shard: a chunky broken-star crystal fragment, warm golden glow,
  faceted, floating. Icon, transparent.
- `wall_<theme>.png` — 9-sliceable vertical border strip (hedges/bamboo
  fence/cloud rail per theme), 256×1024, tileable vertically.

## 4. Backgrounds (one per act, 3 parallax layers each)

2048×4096 vertical, layered: far / mid / near (near layer transparent-cut).

> [style anchor] Vertical scrolling background for a pachinko stage set in
> a **moonlit flower meadow**: rolling pastel hills, glowing moonberry
> bushes, drifting fireflies, big soft moon, star-shard rain streaks in the
> far sky. Muted so foreground game pieces pop. No characters.

(Onsen Town: wooden bathhouses, lantern strings, rising steam, paper
windows glowing. Sky Shrine: cloud-stone stairs, torii of light, aurora,
the cracked Everlight cradle glowing faintly at the top.)

## 5. UI kit

Recraft V3 (vector) recommended. Pastel night palette from
`RuntimeUiFactory` (background #1A1729, panel #292440, accent #FFB86B,
text #F2EDFF).

- `panel_9slice.png` — > [style anchor] A rounded-corner UI panel: deep
  twilight purple with a subtle starfield texture, soft cream outline,
  small star charm on the top corner. Flat, 512×512, corners suitable for
  9-slice (uniform 64 px corner radius).
- `button_9slice.png` + pressed variant — warm-orange pill button, soft
  bevel, sparkle at the left edge.
- `ability_<id>.png` ×6 (`moonberry_snacks`, `glowheart`, `springy_soak`,
  `lucky_towel`, `tailwind`, `steady_paws`) — > [style anchor] Icon:
  {a moonberry with a bite taken / a lantern-heart glowing / a bath pail
  with bouncing water spring / a folded lucky towel with a four-pointed
  star / a koi-shaped gust of wind / a fox paw print made of frost}.
  Circular badge composition, 256×256.
- `upgrade_<id>.png` ×8 (`bouncy_boots`, `heavy_core`, `moon_gravity`,
  `power_launch`, `shard_magnet`, `lucky_star`, `tough_skin`,
  `steady_hand`) — same badge spec; motifs: springy boots / cracked iron
  ball / crescent moon with falling feather / rocket slingshot / horseshoe
  magnet with shards / winking star / heart with a bandage / steady hand
  drawing a dotted arc.
- App icon (1024×1024, no transparency) — > [style anchor] App icon: Puni
  mid-bounce catching a golden star-shard, deep twilight background,
  bold silhouette readable at 48 px, no text.
- Logo — > [style anchor] Game logo art for "PachiRogue" (working title):
  bouncy rounded letterforms with a peg-and-star motif, two-color pastel
  with warm-orange accent, transparent background. (Generate the shapes;
  rebuild final text with the licensed display font for crispness.)

## 6. Sound effects (ElevenLabs SFX — prompt + target duration)

- `sfx_launch.wav` (0.4 s): "Soft cartoon spring-boing with a sparkle tail, cute, bright"
- `sfx_peg_hit.wav` (0.15 s, 4 pitch variants): "Tiny glass marble tap with a musical bell tone, pentatonic"
- `sfx_bumper.wav` (0.3 s): "Rubbery mochi boing, deeper, comedic"
- `sfx_hazard.wav` (0.4 s): "Cute 'ouch' thorn poke, minor-key sting, not scary"
- `sfx_cup_enter.wav` (0.8 s): "Coin-fountain chime cascade landing in a ceramic bowl, rewarding"
- `sfx_stage_clear.wav` (1.5 s): "Warm ascending glockenspiel fanfare with soft crowd of tiny cheers"
- `sfx_draft_pick.wav` (0.5 s): "Magical card-flip shimmer confirm"
- `sfx_shard_spend.wav` (0.4 s): "Crystal chime purchase, two-note descending, satisfying"
- `sfx_defeat.wav` (1.2 s): "Gentle deflating jelly wobble into a hopeful single bell note — sad but encouraging"
- UI tap (0.1 s): "Soft bubble pop, pastel, quiet"

## 7. Music (Suno/Udio — one loop per act + menu/boss, 60–90 s seamless loops, ~120 BPM, instrumental)

> Cozy Japanese casual-game BGM, music-box and soft koto over lo-fi beat,
> moonlit meadow at night, gentle and curious, seamless loop, no vocals.
(Onsen: add hand percussion, water drips, warm marimba, relaxed. Sky:
airy pads, celesta, choir "ah"s, wonder and altitude. Boss: same palette
but driving taiko and playful tension, never scary. Menu/story: solo
music box with vinyl warmth, lullaby of the Everlight theme.)
Victory sting (5 s): the Everlight theme resolving into warm major chimes.

## 8. Swedish text in art

Never bake text into sprites (i18n rule). Any visual containing words —
logo excepted — must be text-free; labels come from the localization
tables at runtime.

## 9. Unity import specs

- PNG, transparent unless noted. Sprite (2D and UI), PPU 100.
- Pivot center for gameplay sprites; cups pivot bottom-center.
- Android: ASTC 6×6; WebGL: keep atlases ≤2048, use sprite atlases per theme.
- Naming exactly as listed — ids match content assets (`ability_glowheart.png` ↔ `Ability_Glowheart.asset`).
- Audio: WAV 44.1 kHz source; Unity import: SFX = Decompress on Load, music = Streaming, Vorbis q0.7.
- Wire-up: replace `GrayboxSprites` usage in `PhysicsBody.CreateVisualChild`
  / `StageBuilder` with `SpriteRenderer.sprite = <asset>` per theme, and
  assign chunk `artPrefab`s once themed chunk prefabs exist.
