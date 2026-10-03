# Game Design

This document establishes the game design specifications for a whimsical, tactile, local 2-player cooperative top-down 3D game. Two miniature characters carry an oversized retro camera together across an indoor gallery floor, coordinating their movement and heading to frame, focus, and photograph a centerpiece target object while navigating around view-blocking partition walls.

The specification covers three design domains:
- **UI Design:** Diegetic lens cone and viewfinder reticle, framing status indicators, polaroid capture review card, and co-op button cues.
- **Asset Design:** Visual identity, miniature stop-motion/diorama aesthetic, palette definitions, scale hierarchies, and room layout props.
- **Game Feedback:** Sensory feedback for cooperative movement, sightline obstruction, shutter release, screen flash, hitstop photo evaluation, and rating fanfare.

---

## UI Design

### Color System
The UI palette draws inspiration from vintage photography, warm gallery lighting, and tactile instant film paper. High contrast against dark wood flooring ensures immediate readability.

- **Primary Neutrals (Surfaces & Backings):**
  - Instant Film Base: `#FBF8EE` (Warm Antique Cream) — main review card paper.
  - Camera Chassis Dark: `#25211E` (Warm Espresso Black) — framing cards and HUD dark accents.
  - Studio Muted Neutral: `#7D7268` (Warm Slate Gray) — secondary labels, camera metadata.
- **Player Identification Tints:**
  - Player 1 (Left Grip): `#FFAA2C` (Marigold Amber) — player ring indicator, left handle guide.
  - Player 2 (Right Grip): `#2CA5A5` (Cornflower Teal) — player ring indicator, right handle guide.
- **Feedback & Intent Palette:**
  - In Focus / Clear Line of Sight: `#FFC83B` (Vibrant Flash Gold) — target framed and unblocked.
  - Perfect Framing Ring: `#68D391` (Soft Jade Green) — bullseye alignment indicator.
  - Blocked / Obstructed: `#E53E3E` (Terracotta Vermillion) — wall obstructing optical line of sight.
  - Seeking / Idle: `#E2D9C8` (Muted Warm Bone) — neutral floor cone projection.

### Typography
- **Display & Rating (Headlines):** Chunky, rounded retro slab serif with friendly mechanical character. Used for review card ratings ("PERFECT SHOT!", "3 STARS") and major callouts.
- **Body & Prompts:** Warm, rounded humanist sans-serif with open apertures and high legibility at steep camera angles. Used for player guidance and UI button badges.
- **Technical & Metadata Stamp:** Monospaced vintage camera date/time stamp font. Used for diegetic shutter readouts (`1/250s`, `f/2.8`, `ISO 100`, `SYNC: 98%`).

### Hierarchy & Proportions
- **Score & Rating:** Prominent, playful display scale (36-48pt equivalent).
- **Player Prompts:** Compact, high-visibility badges with distinct controller glyphs (20-24pt equivalent).
- **Camera Metadata:** Subdued, thin industrial stamp (12-14pt equivalent).

### Layout & Depth Strategy
- **In-World Diegetic HUD:** The primary HUD lives on the gallery floor and on the camera body itself, avoiding flat 2D screen clutter. The projection frustum emanates directly from the camera lens across the floor towards the target.
- **Screen-Space Photo Review Card:** When a photo is taken, the review card slides into view from the bottom right as an elevated physical instant photo, slightly rotated (3-5 degrees) with a soft ambient shadow underneath to convey depth and physical presence.

### Components

#### 1. Floor Viewfinder Projection Cone `(core)`
- **Visual Structure:** A 60-degree flat projected cone extending from the camera lens out to a 6-meter focal distance, with an arced boundary and a central crosshair reticle.
- **States:**
  - *Searching (No Target in Cone):* Soft translucent ivory lines with 30% opacity, faint dashed perimeter.
  - *Target In Cone (Clear Sightline):* Cone boundary thickens and pulses with warm amber gold glow; reticle brackets lock onto the target's base.
  - *Target Obstructed by Wall:* Cone boundary clips against the obstacle surface; obstructed segment turns dashed terracotta red with a subtle warning slash glyph over the target icon.

#### 2. Framing Quality Meter `(core)`
- **Visual Structure:** A circular 3-segment ring displayed diegetically around the camera's rear viewfinder glass.
- **States:**
  - *1 Segment (Fair):* Target inside cone periphery or partially turned away.
  - *2 Segments (Good):* Target well within central 30 degrees, distance appropriate.
  - *3 Segments (Flawless):* Target dead-center within optimal focal zone. Outer ring pulses with a soft rotating shimmer.

#### 3. Instant Photo Review Card `(core)`
- **Visual Structure:** Classic instant square photo frame with wide bottom chin.
- **Top Image Window:** Clean square capture displaying the exact frame captured from the camera lens's perspective.
- **Bottom Label Chin:**
  - Title badge ("SNAPSHOT #01").
  - 1-3 Gold Star rating icons with tactile pop animation.
  - Evaluation tags: "Framing: Perfect (+500)", "Obstruction: Clear (+300)".
- **Exit Prompt:** Soft pill button at the bottom: "Press (A) to Continue".

#### 4. Player Action Glyphs `(core)`
- **Visual Structure:** Floating controller button glyph (South / 'A' button) resting above the camera body.
- **Behavior:** Pulses gently when the target is fully framed and unblocked, inviting either player to press the shutter.

#### 5. Co-op Grip HUD Indicators `(optional)`
- **Visual Structure:** Subtle color-coded dotted arc connecting each player to their respective camera handle (Marigold for P1, Teal for P2).
- **Behavior:** Turns into tension rippling lines if players pull in opposite directions, visually signaling steering drag.

---

## Asset Design

### Visual Identity & Art Direction
- **Style:** Tactile miniature stop-motion diorama. The world feels like a real tabletop set built with authentic miniature materials: polished hardwood, brass hardware, matte leatherette, textured cloth, and smooth painted clay.
- **Scale Contrast:** The characters are tiny (approx. 0.7m in-world miniature scale), while the camera is comically oversized (approx. 1.8m wide, 1.3m long), requiring both players to grip brass side handles.
- **Lighting & Ambiance:** Warm exhibition studio or private gallery. Soft overhead gallery spotlighting, warm ambient bounce from the wooden parquet floor, and crisp specular glints on metallic and glass surfaces.

### Color Palette by Category
- **Environment Props & Architecture:**
  - Gallery Floor: Warm golden oak parquet (`#8C5827`, `#A36B35`).
  - Partition Display Walls: Warm matte off-white museum plaster (`#EAE6DC`) with dark walnut baseboards (`#3B2A1D`).
  - Target Pedestal: Royal crimson velvet drapery (`#7A1C24`) over a fluted marble column (`#DCD9D0`).
- **The Giant Camera:**
  - Body Shell: Textured dark brown pebbled camera leatherette (`#2F241F`).
  - Metal Accents & Handles: Polished brass and brushed chrome (`#D4AF37`, `#CCCCCC`).
  - Accordion Bellows: Deep folded burgundy fabric (`#4A1E24`).
  - Optical Lens: Multicoated convex glass with violet-cyan reflection coating (`#3A506B`).
- **Centerpiece Target (The Subject):**
  - Primary Subject: Antique Mechanical Clockwork Brass Songbird or Golden Animal Figurine (`#F6C844` with polished specular highlights).
  - Silhouetted against the velvet pedestal for instant visual recognition from high top-down angles.
- **Player Characters:**
  - Player 1: Knitted yellow-mustard wool sweater, denim canvas trousers, brass goggles.
  - Player 2: Knitted seafoam-teal wool sweater, corduroy canvas trousers, brass monocle.

### Composition Rules & Readability
- **Top-Down Clarity:** Since the camera angle looks down at approximately 55-65 degrees, character heads, shoulder grips, and the camera's upper profile must have strong, unmistakable silhouettes.
- **Sightline Obstacle Height:** Gallery partition walls are 1.4m high — tall enough to completely block the optical path of the camera's lens (mounted at 0.5m height) to the target, but low enough that players and the overall room layout remain fully visible to the player's top-down perspective.
- **Directional Readout:** The camera lens element features a high-contrast brass bevel and an illuminated optical rim so players instantly know which way the camera is facing, even when rotating rapidly.

### Asset Manifest

| Asset Name | Tier | Category | Style Anchor & Details | Palette | Approx In-Scene Size |
|---|---|---|---|---|---|
| **Oversized Retro Camera** | `(core)` | 3D Model / Prop | Vintage folding field camera with twin side handles, bellows, brass knobs, and large glass lens | Leatherette brown, brass, folded burgundy | 1.8m W × 1.3m L × 1.0m H |
| **Co-op Miniature Characters (x2)** | `(core)` | 3D Character | Stylized wooden/clay figurine with textured knit clothing, round heads, visible grip hands | P1: Mustard / P2: Teal | 0.7m H × 0.4m W |
| **Gallery Parquet Floor** | `(core)` | Environment Material | Glossy herringbone wooden floor tiles with subtle reflection | Warm golden oak | Covers 14m × 14m room |
| **Partition Obstacle Walls** | `(core)` | 3D Model / Prop | Free-standing museum partition flats with heavy wooden base runners | Off-white plaster, walnut trim | 2.5m L × 0.4m W × 1.4m H |
| **Centerpiece Target & Pedestal** | `(core)` | 3D Model / Prop | Polished brass clockwork figurine perched upon an ornate cylindrical pedestal | Brass gold, deep velvet crimson | 0.8m diameter × 1.2m H |
| **Floor Frustum Projector** | `(core)` | Visual / Shader | Dynamic decal/mesh cone projected along the floor showing camera view angle | Ivory to Amber Gold / Vermillion | 6.0m length, 60° arc |
| **Instant Photo Frame Asset** | `(core)` | UI Sprite / Mesh | Realistic Polaroid-style photo card border with subtle paper grain | Antique cream paper | 600px × 720px UI element |
| **Room Perimeter Gallery Walls** | `(optional)` | Environment | Solid exterior walls with vintage picture frames and studio track lights | Warm muted grey-beige | 14m perimeter × 3.0m H |
| **Brass Stanchions & Velvet Ropes** | `(optional)` | Environment Prop | Low museum velvet barriers that guide navigation routes | Polished brass, ruby rope | 1.0m H × 1.5m span |
| **Flash Bulb Pop VFX** | `(core)` | Particle / Light | Radial flash burst originating from camera bulb reflector | Pure white core, soft warm rim | 3.0m radius sphere |
| **Celebration Confetti Shower** | `(optional)` | Particle VFX | Paper confetti ribbons bursting over the target upon 3-star rating | Pastel gold, teal, marigold | 2.5m volume |

---

## Game Feedback Design

### Genre Profile & Tone Rationale
- **Selected Profile:** Hybrid High-Energy Arcade Co-op with Tactile Physical Polishment.
- **Rationale:** Moving an oversized object between two players requires snappy physical feedback, responsive turning cues, and unmistakable spatial warnings when a sightline is broken. The shutter press is the game's climax; it demands an exaggerated, highly satisfying freeze-frame sensory payoff that celebrates successful teamwork.

### Interaction Map

| Interaction | Tier | Importance | Camera | Time | Transform | Visual | Audio | Input | Rationale |
|---|---|---|---|---|---|---|---|---|---|
| **Co-op Movement & Turning** | `(core)` | Light | — | — | Slight sway/tilt of camera chassis (2-3°) in turn direction | Tiny floor scuff dust puffs at players' feet | Soft wooden wheel rattle & squeak; alternating rhythmic footsteps | Analog stick continuous haptic tick on heavy pivot | Conveys the physical weight and mechanical bulk of the shared camera. |
| **Target Enters Camera Frustum** | `(core)` | Medium | — | — | Viewfinder glass subtle forward lens click | Floor cone turns from dashed ivory to warm amber; target acquires glowing focus ring | Crisp mechanical lens aperture tick (`tick-clack`) | Subdued soft trigger haptic rumble | Confirms to players that the target is within the camera's sight range. |
| **Sightline Obstructed by Wall** | `(core)` | Medium | Micro impulse shudder (0.05s) | — | Camera bellows slightly compress | Frustum cone clips at wall; target ring turns into red warning slash | Muffled mechanical obstruction buzz / dull wood thud | Short double-pulse warning buzz | Immediately explains why the shot is currently invalid without reading text. |
| **Full Alignment (Optimal Shot)** | `(core)` | Heavy | Subtle camera zoom-in (3% FOV tightening) | — | Camera rangefinder prism glows; characters stand upright | Golden shimmer sparks emanate from lens; 3 green dots lock on reticle | Ascending dual chime (`ding-ding-chime!`) | Gentle harmonic vibration on both controllers | High anticipation cue signaling both players to slam the shutter button. |
| **Shutter Press (Photo Snap)** | `(core)` | Critical | Directional punch towards target (0.15s exponential decay) | Hitstop freeze frame (0.05s full stop) | Camera violently recoils backward (12%), bellows squeeze then spring back | Blinding full-screen white flash (2 frames) transitioning to circular vignette | Loud mechanical shutter mirror slap + sizzling bulb pop (`KER-CHAK-BOOM!`) | Heavy simultaneous motor kick on both gamepads | The ultimate physical payoff of the cooperative effort; makes the snapshot feel momentous. |
| **Photo Review Card Reveal** | `(core)` | Heavy | Soft pan/drift framing the card | Slow-motion ramp (0.4s at 50% speed) during card slide | Polaroid card slides in with 15% overshoot bounce and 4° tilt | Glossy flash shine sweeps across the photo surface | Card paper friction slide (`whoosh-thump`) followed by star score pops | Successive rhythmic haptic pops per star awarded | Satisfying tangible reward allowing players to admire the framed snapshot. |
| **Opposing Steering Drag** | `(optional)` | Light | — | — | Camera chassis wobbles sideways; player arms stretch | Sweat droplet micro-particles above characters | Straining wood creak and rubber squeak | Heavy low-frequency motor rumble on resisting player | Teaches coordination by physically displaying steering desync. |
| **Obstacle Wall Bump** | `(optional)` | Light | Micro screen shake (0.05s) | — | Camera chassis rebounds 5cm off obstacle | Tiny wood splinter/dust impact ring | Resonant hollow wood collision thud | Single blunt thud haptic | Gives physical presence to room partitions. |
| **Target Celebration Reaction** | `(optional)` | Medium | — | — | Clockwork bird flaps wings and spins on pedestal | Radial golden star burst and musical note particles | Mechanical music box melody chime | — | Rewards player success with charming diegetic world response. |

### Sensory Feedback Sequences

#### 1. Snapshot Capture & Flash Sequence `(core)`
```
[Button Press (0ms)]:
├── Audio: Mechanical shutter release trigger click (high pitch).
├── Transform: Camera chassis hitches forward 5%.
└── Input: Initial trigger snap haptic.

[Flash & Mirror Slap (25ms)]:
├── Time: Complete game hitstop (timescale = 0.0) for 45ms.
├── Visual: Blinding full-screen white flash (100% opacity for 2 frames).
├── Audio: Heavy mechanical mirror slap + explosive capacitor flash bulb pop.
├── Camera: Violent 0.15s impulse shake radiating backward from the lens.
└── Input: Full-strength dual motor rumble pulse (0.1s).

[Afterglow & Recovery (70ms - 250ms)]:
├── Time: Normal game time resumes smoothly.
├── Visual: White flash burns into a warm golden lens vignette that fades over 180ms; soft smoke plume drifts from flash bulb.
├── Transform: Camera bellows bounce back past rest position (15% overshoot) and settle with spring damping.
└── Audio: Sizzling chemical flash bulb cooldown hum (`whirrrrr-shhh`).

[Review Slide-in (300ms - 750ms)]:
├── Visual: Captured frame appears developed onto instant photo card; slides from lower right quadrant.
├── Transform: Card travels with ease-out cubic curve, overshoots rest position by 20px, settles with 3° playful tilt.
├── Audio: Mechanical photo ejection whirr (`bzzzz-shk`) followed by paper slap on table.
└── UI: Star ratings pop in sequentially (450ms, 550ms, 650ms) with rising musical pitches (`C5 -> E5 -> G5`).
```

#### 2. Wall Obstruction Sequence `(core)`
```
[Sightline Interrupted (0ms)]:
├── Visual: Floor frustum instantly truncates at wall intersection; target crosshair turns terracotta red and fractures into two halves.
├── Audio: Muffled warning "clunk" + low-frequency optical buzz.
└── Input: Sharp double-tap warning haptic on both gamepads.

[Obstruction Cleared (0ms)]:
├── Visual: Cone smoothly unfolds past the wall edge back to full 6m reach; red brackets snap back together and ignite with amber glow.
└── Audio: Crisp optical focus glass click (`snick`).
```

---

## Production & Implementation Notes for Downstream Agents
- **Core Priority First:** Implement the `(core)` tier items first: two players tethered to the oversized camera, the floor projection cone with wall line-of-sight truncation, the gamepad button capture trigger, the freeze-frame flash, and the instant photo card review popup.
- **Diegetic Clarity:** Keep the player camera angle fixed top-down at 55-60 degrees. Do not occlude the camera lens with tall walls; partition heights must remain strictly below 1.5m.
- **Physical Feel:** The oversized camera should feel like a coordinated steering exercise (like moving a sofa together). Turning speed should depend on both players' cooperative movement vectors.
