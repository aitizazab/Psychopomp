# Hell: the art pass (research, directions, build list)

Written 2026-10-17 (event day 1, late evening) for the art lane of `HELL-SPRINT.md` (items 6 and 8). Nothing here
touches Unity or game code; it is the survey, the evidence and the plan. The review page for the creator is
`FableAnimationTests/art/review/2026-10-17/hell-art-pass.html`. Everything downloaded sits outside this repo in
`FableAnimationTests/art/hell-pass/art/downloads/<source>/` (62 MB: previews, 19 small CC0 models, 16 OFL fonts).

**The rules this follows.** The repo goes public with every asset, so only CC0 (first) and CC-BY (credit recorded)
are used. "Free but no redistribution" licences are out: that rules out the **Unity Asset Store** (its EULA forbids
shipping the source assets in a public repo), **Quaternius' newer QAL packs** (the Bestiary kit: "you may not
extract, repackage ... or otherwise redistribute the Assets"), **Mixamo** characters and animations as raw files,
and every Sketchfab "CC-BY" model that is a fan rip or remake of a game (DOOM imps, Dungeon Keeper reapers and the
like; filtered out). The order was the creator's: free content first, then premier examples, then what we make.

---

## 1. The answer first (for the creator)

**My pick: direction A, "Inked Inferno"**: the Skyline comic-ink style carried into Hell, with the light-takes-over
rule doing the heavy lifting (every lava basin floods its room with banded orange light), and **floor 3 (The Black
Glass) run as a colour-free zone** (direction C inside A), so the descent reads as ash-grey, then molten red iron,
then black glass where only fire has colour. Enemies get the kit's thick hued outline, but **in Hell the hostile hue
must change from red-magenta to acid lime `#A6FF00`** (measured below: red-magenta sits inside the fire palette).

**The questions for the creator** (top of the review page):
1. Which direction: A Inked Inferno (my pick), B Molten Grit (photo textures, DOOM-lite), or C Black and Ember
   (colour-free stone, only fire coloured)?
2. The hostile outline in Hell: acid lime (my pick) or cyan? The kit's red-magenta disappears into lava light.
3. The title font: Pirata One, New Rocker or Metal Mania (my order)? The HUD keeps Luckiest Guy from Skyline.
4. May agents download the CC-BY Sketchfab picks (the fire imp, the scythe, the altar, the hanging brazier)? They
   need a Sketchfab login and a credit line each. Without it, everything still works from CC0 sources.

---

## 2. Free content survey, by need

Columns: what it is, licence and source, size, fit (1-5 for Hell), and the gap it leaves. Poly Pizza's CC-BY is
3.0; Sketchfab's "CC Attribution" is CC BY 4.0. Previews are on the review page.

### 2.1 Surfaces: rock, basalt, obsidian, ash, lava, iron

All CC0, all with colour, normal, roughness, displacement at 1k-8k. For direction A they serve only as **soft
painted variation** (a low-contrast greyscale map fed to InkLit's optional painted map, or a normal map); for
direction B they are the surfaces.

| Asset | Source / licence | Fit | Use and gap |
|---|---|---|---|
| `dark_rock`, `dark_rock_02` | [Poly Haven](https://polyhaven.com/a/dark_rock) CC0 | 5 | Near-black faceted basalt: floor 3 walls (B), variation map (A) |
| `burned_ground_01` | [Poly Haven](https://polyhaven.com/a/burned_ground_01) CC0 | 5 | Ash and cinders: floor 1 ground |
| `volcanic_rock_tiles`, `volcanic_herringbone_01` | [Poly Haven](https://polyhaven.com/a/volcanic_rock_tiles) CC0 | 4 | Laid black volcanic stone: walkways = "safe path" read (man-made = safe) |
| `castle_brick_01`, `dark_brick_wall` | Poly Haven CC0 | 3 | Built walls for vestibules and shafts |
| `rusty_metal_02`, `rust_coarse_01`, `metal_grate_rusty` | Poly Haven CC0 | 4 | Floor 2 iron: bridges, grates, gate |
| `Lava001`-`Lava005` | [ambientCG](https://ambientcg.com/list?q=lava) CC0 | 4 (B) / 2 (A) | Photo lava with dark crust (001-003) and bright molten (004). For A we write a cel lava shader instead |
| `Rock035`, `Rock031`, `Rock012`, `Rock033` | ambientCG CC0 | 3 | Blue-black layered rock: obsidian stand-in |
| `MetalPlates011/013`, `Metal041C` | ambientCG CC0 | 3 | Riveted iron plates for floor 2 |

**Gap:** there is no free obsidian (glossy black glass); in A it is a flat palette colour with a sharp specular band,
in B a dark rock with low roughness. Free lava is photographic, so a stylised lava is ours to write (a shader).

### 2.2 Modular cave / dungeon / hell kits

| Kit | Source / licence | Fit | Notes |
|---|---|---|---|
| KayKit Dungeon Remastered (200+ pieces, 1k gradient atlas) | [itch](https://kaylousberg.itch.io/kaykit-dungeon-remastered) CC0 (free tier) | 3 | Walls, floors, stairs, doors, chests, banners, traps. Chunky toy proportions; good for vestibules and shafts only |
| Quaternius Modular Dungeon / Ultimate Modular Ruins | [quaternius.com](https://quaternius.com/packs/ultimatemodularruins.html) CC0 | 2 | Pale stone; usable re-coloured, but top-down proportions |
| Kenney Graveyard Kit v5 (90 files, iron fences, crypts, candles) | [kenney.nl](https://kenney.nl/assets/graveyard-kit) CC0 | 2 | Too cute and too small for 40-180 m rooms |
| Quaternius Bestiary - Dungeon Monsters | QAL | 0 | **Excluded: QAL forbids redistribution** |

**Gap (the big one):** no free kit builds 40 x 180 m lava caverns. Good news: our rooms are already built from data
(`HellRoomLayouts`, one box collider per solid), so the room shapes stay ours. What the rooms need is **edge
detail** (Team Fortress 2's rule: detail on the edges and the skyline, floors clean): broken-rim trims on lava
edges, rock spires and stalactite silhouettes on the far walls, chains spanning the ceiling. Those are cheaper to
generate procedurally in the builder (extruded jagged rims, lathe spires, torus-link chains) than to import.

### 2.3 Enemies

| Need | Candidate | Source / licence | Size | Fit | Gap |
|---|---|---|---|---|---|
| **Fire imp** (floating) | Quaternius "Demon" (round red body, bat wings, animated) | [Poly Pizza](https://poly.pizza/m/Mo2ky6vkf8) **CC0** | 3.7k tris, animated | 4 | Cute eyes: re-paint charred black with ember cracks and glowing eyes/mouth; its flying clip carries the hover. **Downloaded.** |
| | "Imp" by Spinnee (hand-painted, red-black, glowing fire belly) | [Sketchfab](https://sketchfab.com/3d-models/4e7cf9b9f42d43529f2fb477f34e4435) CC BY 4.0 | 4.2k faces, not rigged | 5 (look) | Crouched, no wings, no rig: code bob + add wings; needs login. Credit: "Imp" by Spinnee, CC BY 4.0 |
| | "Flying Imp" by Alexander Leth (winged, trident) | [Sketchfab](https://sketchfab.com/3d-models/8b4e8e199c3648748395f4db14183ef3) CC BY 4.0 | 1.1k faces | 3 | Thin wings read badly at range |
| | Quaternius "Demon" (red devil, halo) | [Poly Pizza](https://poly.pizza/m/LnfIziKv4o) CC0 | 5.8k, animated | 3 | Humanoid, walks; backup. **Downloaded.** |
| **Scythe demon** (ground, 2.8 m) | no free full-body fit | | | | Free demons are fan rips, concept-art remakes ("Nightmare" is based on YIN ZHE's concept: avoid) or huge sculpts (1-2M faces) |
| | Quaternius "Scythe" | [Poly Pizza](https://poly.pizza/m/yE9TKCewO8) CC0 | small | 4 | The weapon. **Downloaded.** |
| | "FREE - Desolate Devil Scythe (GameReady)" by nodgerty | [Sketchfab](https://sketchfab.com/3d-models/476cab8e624a4875a8d722b8578ffc93) CC BY 4.0 | 3.9k faces, 4k PBR | 5 | The hero scythe (red-black, jagged blade). Credit line needed |
| | Quaternius "Ghost Skull" / KayKit "Skull" | Poly Pizza CC0 | 2k / 0.3k | 3 | A horned skull head for a robed body |
| **Lava ooze** (pulsating) | Quaternius "Green Blob", "Slime", "Slime Enemy" | [Poly Pizza](https://poly.pizza/m/y4kJh8EeYS) CC0 | 1.4-1.7k, animated | 4 | Shape only; the look is our lava shader + the existing throb. **Downloaded.** |
| | "Slime 2" by 3DHaupt (dripping goo, shape keys) | Sketchfab CC BY 4.0 | 13k | 2 | A cloth sim, not an enemy |
| **Boss (Vesper)** | Quaternius Universal Base Characters + Universal Animation Library (120+ clips, CC0) | [quaternius.com](https://quaternius.com/packs/universalanimationlibrary.html) CC0 (free tier) | | 3 | A plain humanoid to dress (shadow cloak, ember crown); only if the boss needs a body tonight |

**Conclusion:** the imp and the ooze are covered by CC0 models plus our paint and shader. **The scythe demon is the
gap.** My plan for it: a **robed reaper** built in Blender from primitives (a lathe-revolved ragged robe, bone
shoulder ribs, the existing glowing chest core, a horned skull from the CC0 skulls, the CC0 or CC-BY scythe). A
robe hides the legs, so it glides with no walk cycle (the creator's no-walk-cycle-to-camera rule stays easy) and
gives the clearest silhouette at range: a tall dark triangle with a curved blade above it. Tripo (`tools/tripo/gen.mjs`,
CC BY 4.0 free tier) is the fallback, only after the creator answers the credit question already pending in the kit.

### 2.4 Props: chains, sigils, braziers, cages, lanterns, altars

| Prop | Candidate | Source / licence | Size | Fit |
|---|---|---|---|---|
| Hanging lantern / post lantern | KayKit "Hanging Lantern", "Post Lantern" | [Poly Pizza](https://poly.pizza/m/3jzk3YShv1) CC0 | 472 / 96 tris | 4 (the Soul Lantern itself, and walkway lights). **Downloaded** |
| Candles, candle shrine, skull | KayKit "Candles", "Shrine", "Skull" | Poly Pizza CC0 | 0.2-0.4k | 4 (sigil plinths, altar dressing). **Downloaded** |
| Iron fence | KayKit "Iron Fence" | Poly Pizza CC0 | 312 | 4 (safe walkway edges = "this way is safe"). **Downloaded** |
| Bonfire / brazier fire | Quaternius "Bonfire"; "Fire" (low-poly flame mesh) by Jakob Hippe | Poly Pizza CC0 / CC-BY 3.0 | 0.6k / 0.7k | 4 (braziers; the flame mesh suits the cel look). **Downloaded** |
| Brazier bowl | "Hanging brazier" by alexalbinyana; "Medieval Brazier" by Sky_Hunter; "Primitive Brazier (Free)" by wolfgar74 | Sketchfab CC BY 4.0 | 7.5k / 1.2k / 2.4k | 4 / 3 / 3 |
| Cage | "Cage" by iPoly3D | Poly Pizza CC0 | 2.2k | 4 (hanging cages over lava). **Downloaded**; "Hanging Cage" by BillyGreen (Sketchfab CC-BY, 12.8k) as alternate |
| Chains | "Link Chain" by Jarlan Perez; "Hook and chain" by Zacharylll; "Hanging wall chains" by karyu.lucca | Poly Pizza CC-BY 3.0; Sketchfab CC-BY | 5.5k / small / 2.3k | 3: **make our own instead** (a procedural torus-link chain in the builder: any length, sags, swings, costs nothing, no credit) |
| Altar | "Sacrificial Altar" by Jochon (glowing red rune block) | [Sketchfab](https://sketchfab.com/3d-models/2044bb250c864c76897949382062f865) CC BY 4.0 | 128 faces | 5 (the Lantern room's altar) |
| Spikes, columns | Quaternius "Spikes", "Column" | Poly Pizza CC0 | 1.6k / 0.5k | 3 (re-coloured). **Downloaded** |
| Bones, skull piles | "Pile of Bones" by Zsky | Poly Pizza CC-BY 3.0 | 0.3k | 3 |
| Sigils | none fit | | | **Make**: a decal/emissive ring shader (rotating runes drawn as SDF arcs, no font needed), one per sigil A-F, lime when touched |
| Quaternius Fantasy Props MegaKit (200+ medieval props, chandeliers on chains, cauldrons, anvils) | | [quaternius.com](https://quaternius.com/packs/fantasypropsmegakit.html) CC0 (free tier 60-70%) | | 3: a big download (zip); worth it only for chandeliers/cauldrons |

### 2.5 Sky and fog

Hell's rooms are caverns, so no HDRI fits (Poly Haven's skies are real places). **Make**: the Skyline painted-gradient
sky reused with Hell colours (a near-black zenith to a smouldering red-brown horizon per floor), linear fog that
eats the far walls (floor 1 warm ash-brown, floor 2 deep red, floor 3 violet-black), and slow drifting ash particles
for depth. Zero assets, about an hour.

### 2.6 VFX (fire, embers, smoke, heat shimmer)

| Need | Source | Plan |
|---|---|---|
| Sprites | **Kenney Particle Pack** (80 sprites at 512 px: flames, smoke puffs, sparks, flares, rings, slashes) [kenney.nl](https://kenney.nl/assets/particle-pack) **CC0** | The base for every effect; download the zip when building |
| Fire on braziers | Kenney flame sprites, or the low-poly "Fire" mesh scaled and scrolled | Shuriken with `Universal Render Pipeline/Particles/Unlit`, additive for A/B; in A, posterised to 3 flat colours |
| Embers | Kenney spark sprites | One world-space Shuriken per room over the lava: small, fast-rising, flickering, 200-400 particles; cheap |
| Smoke | Kenney smoke puffs | The abilities lane already draws "opaque, ink-style" smoke blobs (`HellFx`): reuse for brazier and vent smoke |
| Lava bubbles / spurts | none | A short burst emitter at random points on each basin, plus a crust that cracks in the lava shader |
| Heat shimmer | none free for URP | A full-screen distortion needs the opaque texture (one more copy of the screen: costs frame time). **Skip it**; fake heat with the embers and a slow vertical wobble in the lava light's cookie. Not worth 120 fps |
| Rewind / Shadow Step / Nightfall | already in `HellFx` | Restyle colours only |

Unity's own free VFX samples sit on the Asset Store (no redistribution) and are VFX Graph (GPU compute), so they are
out on both counts.

### 2.7 Fonts (OFL 1.1, Google Fonts; ship `OFL.txt` with the build)

Specimens rendered on the review page (`hell-art-pass/fonts/`).

| Use | Pick | Why |
|---|---|---|
| Title | **Pirata One** (1st), New Rocker (2nd), Metal Mania (3rd) | Blackletter that stays readable at a glance in a 3-minute trailer; UnifrakturMaguntia and Jacquard 24 are too hard to read; Eater and Germania One read as Halloween or sports |
| Room and floor banners | Cinzel (already in the kit's list) | Carved Roman capitals: "THE EMBER GATE" |
| HUD numbers | **Luckiest Guy** (Skyline's pick, Apache 2.0), Barlow Condensed for small labels | Continuity with the kit; Barlow Condensed is the plain fallback for B and C |

---

## 3. Premier examples: what makes them read, and what we can copy in 1.5 days

Screenshots are from the games' Steam store pages (for reference on the review page only; nothing of theirs ships).

**DOOM Eternal (id Software).** Hell is rust-brown and bone with rivers of saturated orange lava; the levels get
their depth from **layered fog**: near rock is dark, each layer behind it is lighter and hazier orange, the far
megastructures are flat silhouettes. Demons are big and chunky, and each has a glowing part (a mouth, an arm
cannon, a cracked chest) that doubles as the weak point; a staggered demon **flashes** a bright colour (the
"glory kill ready" signal), so state reads without text. *Copy:* the fog layering, one glowing weak point per enemy,
a stagger flash. *Too costly:* its dense hand-made textures and lighting artistry.

**Ultrakill (Arsi "Hakita" Patala).** Low-resolution textures and simple geometry, but each layer of Hell has **one
dominant light colour flooding everything**: one level is drenched in blood red, another in sickly yellow, another
cold blue. Enemies are simple shapes that read by motion and silhouette, and blood paints the floor in flat red.
*Copy:* a single per-floor colour flood (cheap with a fog colour plus ambient), fast punchy VFX. It proves a tiny
team gets a strong look from colour discipline, not detail.

**Dusk (David Szymanski).** Deliberately 90s: chunky low poly, blurry textures, dark rooms lit by red skies, fires
and torches. *Lesson, mostly negative:* in its dark rooms enemies are **easy to lose**: they share the walls' brown
and dim values. Fine for horror, wrong for us.

**Metal: Hellsinger (The Outsiders).** Ornate, high-detail Hell: orange fire against cold teal-grey stone (the
classic orange and teal contrast), spiky winged demons with rim light. *Copy:* a cold fill light against the warm
key, so shapes separate. *Too costly:* the detail; and the busy backgrounds make enemies compete with scenery.

**Hades (Supergiant).** Top-down, painted, with thick black ink lines and flat colour. Its fire level paints the
lava as **flat bands** (yellow-white at the shore, orange, then deep red) with a crisp bright rim where stone meets
lava, and the walkable stone islands sit dark against it: figure against a bright ground. Enemies carry strong
outlines and bright accents. *Copy:* the banded cel lava with a bright shore rim. This is the lava shader for A.

**Cultic (Jasozz Games).** Dark, low-resolution, heavy shadow; the lighter's small flame and gunfire light the
scene; enemy cultists wear **saturated red robes** that pop against the desaturated browns and greys. *Copy:* keep
enemy colour out of the scenery palette. *Lesson:* at range, its enemies still get lost in the dark.

**Devil Daggers (Sorath).** The extreme: a pure black void, a single small lit floor, and enemies made of
brown-bone shapes with **glowing gems** (red, orange, green): the gem is the weak point and the only bright thing.
Readability is perfect with almost no art. *Copy:* glowing cores as the hottest pixels on screen; darkness as a
design tool (this is direction C).

**Deep Rock Galactic (Ghost Ship Games).** Low-poly flat-shaded caves with very low ambient light; a flare or a
lava pool **takes over the whole cave** with its colour; its magma biome is orange light on faceted rock. Bugs read
by glowing sacs and simple silhouettes. Already the kit's rule 3. *Copy:* exactly that, with lava as the light.

### Enemy clarity against dark, fire-lit backgrounds (judged)

| Game | Clarity at range | How |
|---|---|---|
| Devil Daggers | Best | Black void + glowing gem weak points |
| Hades | Very good | Outlines, flat colour, figure-ground |
| DOOM Eternal | Good | Size, glowing parts, the stagger flash, fog behind them |
| Ultrakill | Good | Simple silhouettes, motion, clean colour floods |
| Deep Rock Galactic | Good | Glowing sacs, low-poly silhouettes, light pools |
| Metal: Hellsinger | Medium | Rim light, but scenery detail competes |
| Cultic, Dusk | Weak at range | Enemies share the dark scenery's values (intended horror) |

**The principle we take:** value contrast first (a dark body against lava light, an outline or rim against dark
rock), one hue no scenery ever uses, and the weak point as the brightest thing on the body.

**Measured: why the kit's hostile hue fails in Hell.** Over 13 fire-heavy frames from these games (pixels with
saturation > 0.4 and value > 0.3), 74% of the coloured pixels sit within 20 degrees of lava orange. Within 20 degrees
of the kit's hostile red-magenta `#FF2E63`: 10.8%; of its weak-spot yellow `#FFD60A`: 14.7%. Of cyan `#2EF0FF`: 2.3%;
of acid lime `#A6FF00`: 2.1%; of violet `#B36BFF`: 0.7%. Violet is the objective hue and the Shadow theme, and cyan
is too close to the safe-path studs' cold blue (`#8FD3FF`), so **lime is the hostile hue for Hell**, and the **weak
spot becomes white-hot** (near-white pixels: 0.9% of these frames) instead of yellow. 42% of all pixels are dark
(value < 0.25), so an outline is what carries an enemy in the dark half of the screen.

---

## 4. Three style directions

Hours are agent hours (the creator's note: my estimates run long; these are already trimmed). The trailer is
judged first, at 90-120 fps.

### A. Inked Inferno (the Skyline comic ink, adapted) **(my pick)**

*Looks like:* flat painted rock in three bands of light, thin dark ink on edges, every lava basin flooding its room
with hard-edged orange light (Deep Rock Galactic's magma caves drawn like Hades), enemies as dark shapes with a thick
lime outline and a white-hot core. Floor 3 drops all colour from the stone (the kit's rule 4) so only fire, the
shadow pall and the outlines are coloured.

- **Free assets:** Quaternius Demon (imp), Green Blob/Slime (ooze shape), Quaternius Scythe; KayKit lanterns,
  candles, shrine, skull, iron fence; Quaternius bonfire, spikes, column, cage (iPoly3D); Kenney Particle Pack;
  Pirata One, Cinzel, Luckiest Guy. Optional CC-BY: Spinnee's Imp, nodgerty's scythe, Jochon's altar, the hanging
  brazier. Poly Haven `dark_rock` / `burned_ground_01` only as faint variation maps.
- **Made by us:** the Hell palette table in the ink converter; a cel lava shader (banded, scrolling crust, bright
  shore rim); procedural edge trims, spires, chains; the robed scythe demon (Blender); sigil rings; spawn fire seal;
  title screen.
- **Cost:** about 23 h (palette 1.5, lava 2, lighting/fog/sky 1, outline roles 1, enemies 7, props 3, VFX 3, title
  1.5, stills + judge loop 3). Three lanes in parallel: about 8-9 h of wall time.
- **Trailer:** the strongest. It is a recognisable look (not "a Unity asset flip"), lava-as-light gives huge
  colour changes room to room, the outlines keep enemies legible at speed, and the three floors read as a
  descent.
- **Risk:** the ink pass's cost. The last measurement (2026-10-15, before the rebuild) was 6.6 ms average and
  30 ms 1% lows with ink on, and the rebuilt pass is unmeasured. **Gate:** run the perf A/B on a Hell room first; if it
  misses 120 fps, drop the scenery ink and keep the banded lights and the hued outlines (still A, cheaper).

### B. Molten Grit (photo textures, PBR-lite: DOOM Eternal lite)

*Looks like:* real basalt, ash and rusted iron under orange lava light, bloom, fog layers, HDR lava.

- **Free assets:** Poly Haven `dark_rock`, `dark_rock_02`, `burned_ground_01`, `volcanic_rock_tiles`,
  `rusty_metal_02`, `metal_grate_rusty`; ambientCG `Lava002`/`Lava004`, `MetalPlates011`; the same models.
- **Made by us:** a triplanar URP Lit material (the room boxes have no good UVs), lava emission scroll, post (bloom,
  ACES, vignette), fog layers.
- **Cost:** about 18 h.
- **Trailer:** rich in stills, but generic in motion: photo surfaces next to flat low-poly CC0 props clash; enemies
  are hardest to read here (texture noise everywhere); it competes with DOOM on DOOM's terms and loses.

### C. Black and Ember (colour-free stone; only fire has colour: Devil Daggers / Cultic)

*Looks like:* near-black greyscale stone fading into black fog; lava, braziers, embers and enemy cores the only
colour; lime outlines on enemies.

- **Free assets:** the same models and Kenney sprites; no textures.
- **Made by us:** InkLit at full desaturation, black fog, cel lava, glowing cores.
- **Cost:** about 17 h.
- **Trailer:** striking for 10 seconds and the clearest enemies, but one look for all three floors; Fire reads, the
  descent doesn't. Best used inside A as floor 3 (The Black Glass is "obsidian and shadow" by design).

**Why A:** it reuses a finished, tested pipeline (InkLit, the ink feature, the converter, outline roles), it answers
enemy clarity with a measured hue, its rule 3 (light takes over) is exactly what lava does, it matches the CC0
low-poly props instead of fighting them, and it gives the trailer a progression: grey ash, red iron, black glass.

---

## 5. Build list for direction A, in order

Each line names the file area it touches, so lanes can split by file ownership.

1. **Perf gate (main session, 15 min):** the Hell scene with the ink volume on vs `-skyline-no-ink` on the Sigils
   room with 6 imps; 120 fps at 1080p decides full ink or outlines-only.
2. **Room surfaces** (`SkylineArenaBuilder.Hell.cs` materials, the converter's palette table):
   - stone per floor: ash `#4A403C` walls / `#6E5D50` floors; iron `#6A3A26` / `#3B2A24`; obsidian `#1E1A26` with
     a hard specular band, desaturated (rule 4);
   - walkways on the safe path a lighter laid-stone colour with KayKit iron fence posts on lava edges;
   - collapse platforms: cracked darker stone with the ember rim pulsing faster in the shake;
   - wall-run walls: the ember stripe kept, as an emissive band the light catches;
   - dark zones: the shadow pall `#2A1640`.
3. **Lava** (new shader + `HellLava` material): banded `#FFE9A8` / `#FF9A2E` / `#E2401C` with a scrolling dark
   crust `#4A1008`, a bright shore rim, a slow heave; the lava's lights (`LightSpacing`) banded by InkLit so they
   paint the walls.
4. **Light, fog, sky** (`SkylineLook` Hell rig): low ambient, per-floor fog colour, painted void sky, bloom kept for
   lava, vignette.
5. **Enemy readability** (`HellLook`, outline roles): hostile outline lime `#A6FF00`, weak points white-hot with the
   pulse, spawn warning = a fire seal on the ground 1 s before (sprint item 6).
6. **Enemies** (`HellBodies` prefab overrides):
   - fire imp: Quaternius Demon re-painted (char black, ember cracks, white-hot belly core), its flap clip, code hover;
   - lava ooze: Quaternius Green Blob mesh with the lava shader and the existing throb;
   - scythe demon: the robed reaper (Blender: lathe robe, bone ribs, chest core, horned skull, scythe), gliding.
7. **Props per room** (builder dressing lists): Sigils: plinth + candle shrine + rune ring per sigil. Lantern: the
   KayKit hanging lantern as the carried lantern (its flame violet, the objective hue), altar
   (Jochon's or a built block with rune emission). Chains: procedural chains from the anchors to the gate. Brand:
   the brand circle as a rune ring. Collapse: hanging cages over the lava, falling debris. Wells: braziers round
   the ring walkway. Everywhere: edge trims, far-wall spires, ceiling chains, skull and bone clusters against walls
   (never on routes).
8. **VFX** (`HellFx` + new emitters): embers over lava, brazier fire, lava bubbles and spurts, imp bolt trails
   (lime-tinted head so bolts read as hostile), scythe beam as a hard white-orange line, death bursts in char and
   ember.
9. **Title screen**: Pirata One title over a slow camera across the Sigils room, embers drifting, Cinzel menu.
10. **Stills and judge loop**: one still per floor per room type + enemies at 10/25/40 m on lava and on dark rock;
    an adversarial judge first, then the creator.

**Credits to record if the CC-BY picks are used** (credits screen and `ASSETS.md`): "Imp" by Spinnee; "FREE -
Desolate Devil Scythe (GameReady)" by nodgerty; "Sacrificial Altar" by Jochon; "Hanging brazier" by alexalbinyana
(all Sketchfab, CC BY 4.0); "Fire" by Jakob Hippe; "Pile of Bones" by Zsky (Poly Pizza, CC BY 3.0). Everything
else listed is CC0 (Quaternius, Kay Lousberg/KayKit, iPoly3D, Pichuliru, Kenney, Poly Haven, ambientCG) or OFL /
Apache (fonts).

## 6. Files

- Review page: `FableAnimationTests/art/review/2026-10-17/hell-art-pass.html` (images in `hell-art-pass/`).
- Downloads (outside the repo): `FableAnimationTests/art/hell-pass/art/downloads/`
  - `polypizza-models/`: 19 GLBs (17 CC0 + the CC-BY "Fire"), with `_licences.json` (title, author, licence,
    triangles, Poly Pizza id);
  - `google-fonts/`: 16 font files; `steam-screens/`: reference screenshots (not for shipping);
  - thumbnails: `polyhaven-thumbs/`, `ambientcg-thumbs/`, `polypizza-thumbs/`, `sketchfab-thumbs/` (+
    `sketchfab-meta.json`), `quaternius-previews/`, `kenney-previews/`, `kaykit-previews/`.
- Not yet downloaded (choose first): Poly Haven / ambientCG texture sets, Kenney Particle Pack zip, KayKit Dungeon
  zip, Sketchfab CC-BY models (login needed).
