# DEAD SIGNAL — Presentation Quality Baseline

Published for Presentation Run P01 on 2026-08-30. This is the measurable acceptance contract for Runs P02–P60. It does not change gameplay, assets, lighting, materials, geometry, UI, or effects.

## Purpose and review targets

Presentation work must make the existing mission easier to read and more cohesive at the production camera. It must not trade away player, projectile, threat, objective, prompt, or escape-lane clarity. Review at:

- 1280×720 and 1600×900, with 2560×1080 added for shell/UI gates;
- default comfort, Reduced Flashes, and Reduced Flashes plus Steady Camera;
- representative dormant, available, active, complete, combat-pressure, and terminal states;
- the same gameplay state and camera pose for every before/after comparison.

Automated tests can prove bindings, reset, allocation, and state authority. They do not prove visual hierarchy, comfort, material quality, or fun.

## Current inventory

The P01 read-only inventory found:

| Area | Current evidence | Baseline interpretation |
| --- | --- | --- |
| Textures | 98 PNG resources: 90 at 1254², four at 2172×724, and four other sizes from 768² to 1774×887 | Source dimensions are generation-oriented rather than a consistent runtime density contract. World imports commonly cap authored panels at 1024 and retain bilinear filtering; each owning run must choose an intentional import cap. |
| Materials | 120 resource materials | Reuse is established, but the total is not a license for one-off room materials. New variants must communicate a distinct material or gameplay state visible at the production camera. |
| Shaders | 93 URP Lit, 20 URP Unlit, six URP Particles Unlit, one custom Powered Territory material | These four families are the approved baseline. A new shader family requires a named visual need, accessibility behavior, and performance evidence. |
| Emission | 42 sampled materials at zero emission, 20 at `(0, 0.5]`, 27 at `(0.5, 2.0]`, and 10 above `2.0`; current peak channel is `3.2` | High emission is already reserved for Signal, danger, projectile, and hero-energy roles. Later runs should reduce competing coverage before increasing intensity. |
| Authored meshes | 51 serialized mesh assets, 1,750 vertices total, 96 vertices maximum in one current authored mesh asset | Existing custom geometry is intentionally low-cost. Production-camera contour and material breakup matter more than invisible subdivision. |
| Built-in primitive exposure | 258 serialized built-in mesh references across 28 scene/prefab files; the largest concentrations are Relay Foundry (30), Security Trial (24), Convergence Chamber (23), and Arc Furnace (20) | Built-in primitives remain valid for hidden collision, simple structural backing, and authored modular construction. They are presentation debt when a focal machine, door, reward, or hero landmark reads as an untreated cube/cylinder. |
| Actor presentation | Current newly authored Interceptor, Suppressor, and Swarmer assemblies use four collider-free render parts each; their serialized custom actor meshes are 10–16 vertices per part | Four readable parts are sufficient for role grammar. Do not add renderers, materials, bones, or texture channels that disappear at gameplay distance. |
| UI | HUD uses a 1920×1080 CanvasScaler reference. Authored text sizes range from 10 to 38; shell text ranges from 12 to 54. Runtime readability guards raise the key HUD fields to 15–20 or more | The 10–13 authored sizes are explicit review debt at 1280×720. Final acceptance uses resolved screen pixels, not prefab point values alone. |
| VFX | The bounded owners, lifetimes, pooling, Reduced-Flashes ceilings, and camera rules are catalogued in `PRESENTATION_EFFECT_AUDIT.md` | New presentation work must reuse that ownership model and may not add unbounded recurring allocations or a second camera writer. |

## Environment budget

| Measure | Budget / acceptance rule |
| --- | --- |
| Surface density | Repeated structure targets 64–128 source pixels per world metre; hero machinery and interaction surfaces target 128–256 px/m. A surface may exceed this only for a readable status face or decal. Adjacent surfaces in the same comparison frame should stay within a 2:1 effective-density ratio unless contrast is intentional. |
| Import size | Default world albedo/status cap is 1024. Use 2048 only for a hero atlas or large unique surface proven to retain visible detail at 1600×900. UI backdrops may use 2048 when their native screen footprint justifies it. Mipmaps stay enabled for perspective world surfaces; bilinear is the minimum filter. |
| Readable feature size | At 1280×720, a required interaction/state edge must remain at least 3 screen pixels thick and a primary landmark contour break at least 8 pixels. Decorative detail may disappear; required state information may not. |
| Material hierarchy | One dominant structural family, one supporting material family, and no more than three role/state accent colors may compete in a representative room frame. White ceramic is a focal separator, not blanket trim. |
| Material count | Prefer shared project materials. A bounded room finish may add at most four new shared materials unless a before/after frame proves each additional material owns a distinct readable surface role. No per-instance runtime material creation for static environment finish. |
| Primitive exposure | Zero untreated built-in primitives as the primary read of a hero landmark, required machine, mission door, reward cradle, or extraction device. Structural primitives are acceptable when their silhouette, adjacency, and authored materials make them read as a deliberate module. |
| Emission | Background detail: peak channel ≤1.0. Persistent route/state machinery: ≤1.85. Values above 1.85 are reserved for short role-critical Signal, danger, projectile, or hero-energy reads, with the existing absolute peak of 3.2. No representative frame should contain more than three simultaneous high-emission focal areas. |
| Shader usage | URP Lit for physical station surfaces; URP Unlit for deliberate status/decal reads; Particles Unlit for bounded VFX; Powered Territory only for its established territory role. Do not solve material hierarchy with a new shader. |
| Silhouette and clearance | Refinement may not narrow traversable space, alter collision/projectile/NavMesh authority, obscure an interaction side, or reduce a required escape lane. Foreground culling remains disabled. |

## Lighting and atmosphere budget

Lighting becomes the primary environment-composition layer in Runs P15–P23. Materials and geometry establish physical ownership; localized practical light decides what the player reads first and how restoration changes the station.

| Measure | Budget / acceptance rule |
| --- | --- |
| Composition | Each representative room frame has one dominant practical-light role tied to the room verb and no more than two supporting light-color roles. Adjacent rooms must differ through value, direction, source shape, or shadow pattern as well as hue. |
| Controlled darkness | Darkness may separate task-light pools and preserve atmosphere, but it may not hide the drone contour, an active specialist silhouette or telegraph, a required objective/prompt, a projectile path, a hazard boundary, or the only escape lane at 1280×720. |
| State-driven relighting | A room whose objective changes machinery or power state must change at least one locally owned practical light, emissive fixture, threshold spill, or structural light response. Broad powered-territory glow alone is not sufficient presentation evidence of restoration. |
| Cyan hierarchy | Cyan powered territory remains authoritative, but in ordinary non-transition frames it must not become both the largest luminous area and the highest-luminance focal area. Local machinery, objective state, actors, projectiles, and threat telegraphs retain separable value hierarchy inside powered territory. |
| Exposure and emission | Establish exposure and ambient floor from the darkest required combat/navigation state before increasing emissive intensity. Respect the existing emission ceilings; solve flatness first through light placement, direction, falloff, shadow, and local contrast rather than bloom or larger emissive surfaces. |
| Shadows and lanes | Shadow direction may shape protected/exposed routes and machine depth, but every active combat comparison must retain the player, incoming danger, and at least one continuous escape lane. Avoid high-frequency moving shadows, opaque foreground leaks, and false hazard silhouettes. |
| Accessibility | Reduced Flashes replaces rapid or high-contrast light changes with slower, steadier state changes while preserving the same objective meaning. Steady Camera remains independent. Lighting state must retain shape/value/source-location redundancy under high contrast and common color-vision simulations. |
| Performance | Use authored reusable light groups and bounded shadow ownership. Record realtime light count, shadow-casting light count, overlapping-light regions, GPU/frame timing, and any baked/probe changes; do not exceed the global frame budget or create per-frame light/material allocations. |

## Actor budget

| Measure | Budget / acceptance rule |
| --- | --- |
| Render structure | Maximum four ordinary mesh renderers/material slots per established actor presentation unless a comparison proves a fifth part is necessary for a unique telegraph. Shared materials only; no renderer or material creation during active combat. |
| Geometry | Prefer no more than 256 authored vertices for a complete procedural actor presentation at the current camera. Exceed only when a silhouette defect is visible at 1600×900. No invisible back-face or micro-bevel work. |
| Screen presence | At 1280×720, a Swarmer body must retain a 12-pixel primary span; specialist bodies must retain a 22-pixel primary span when on screen. A specialist telegraph edge must remain at least 2 pixels thick through its authoritative warning interval. |
| Role hierarchy | Player cyan/white remains unique; Warden red weight, Sapper black-violet/magenta, Interceptor red/amber direction, Suppressor magenta projection, and Swarmer crimson/amber fragility must be distinguishable without reading HUD text. |
| Animation | Presentation follows authoritative root/state. No root motion, animation-event gameplay, collider motion, timing changes, or continuous decorative motion that implies a false attack. Every transition resets deterministically. |
| Population cost | Maximum established population must hold the global performance budget below. After warmup, actor presentation contributes 0 B recurring managed allocation per frame. Use simplified distant motion/LOD only when capture shows a cost or hierarchy problem. |

## UI budget

| Measure | Budget / acceptance rule |
| --- | --- |
| Resolved text | At 1280×720, critical objective/outcome/action text must resolve to at least 16 screen pixels; persistent secondary status to at least 13; tertiary hints to at least 12. Anything smaller is non-authoritative decoration or must be enlarged/removed. |
| Safe area | Keep critical controls and state at least 24 screen pixels from each edge at 1280×720, 32 at 1600×900, and inside a centred 16:9 safe region at 2560×1080. |
| Occlusion | World prompts, objective indicator, threat/edge indicators, command strip, pause/menu focus, and outcome actions may not overlap one another or cover the player, a required interaction, or the only visible escape lane. |
| Hierarchy | One primary action, one primary objective, and one terminal outcome heading may dominate at a time. Debug/playtest labels are excluded from release comparisons. |
| Input parity | Every shell and outcome comparison must show valid keyboard/mouse and controller focus. No hover-only meaning and no color-only action state. |

## VFX and performance budget

| Measure | Budget / acceptance rule |
| --- | --- |
| Lifetime | Transient world effects remain ≤1.2 seconds. Longer reads represent persistent gameplay state and use a fixed owner rather than spawned loops. |
| Accessibility | Reduced Flashes caps short bright world effects at 0.30 alpha, persistent extraction feedback at 0.14, and terminal event layers at 0.26. It removes optional point lights and replaces repeated pulses with steady/slower luminance. Steady Camera cancels camera offset in the same frame. |
| Pooling | Existing impact/spark pools prewarm 12 and cap at 16; chain links prewarm six and cap at eight; station-state effects use four slots; weapon transformation uses two; extraction uses one progress plus two terminal owners. Saturation degrades presentation only. |
| Allocation | 0 B recurring managed allocation per frame after warmup for environment, actor presentation, UI steady state, and pooled VFX under maximum established population. |
| Frame time | At 1600×900 in a development player under maximum established combat population: 60 fps target, CPU main thread ≤11 ms, GPU ≤13 ms, frame ≤16.67 ms at the 95th percentile over a 30-second capture. |
| Render submission | Soft review threshold: 250 batches and 300 draw calls in the same maximum-population capture. Crossing it requires measured attribution and consolidation/LOD evidence; it is not permission to alter combat population. |
| Visual occupancy | A transient world effect may not cover more than 15% of the frame or fully occlude the player/threat silhouette. At least one escape lane and every active specialist telegraph must remain visible. |

## Locked comparison frames

The source captures below are preserved unchanged under `ArtSource/PresentationBaseline`. They are selection references, not proof that the current build still renders identically. P02, P09, P13, and P14 must capture a clean post-change frame at the stated resolution, camera pose, and gameplay state; compare default comfort first, then Reduced Flashes plus Steady Camera.

| Landmark | Locked source | State and framing | Primary debt to compare |
| --- | --- | --- | --- |
| Central | `P01-Central-Tower-Available-1600x900.png` | 1600×900, Central activation available, tower and east transfer approach in frame | Oversized cyan coverage, flat grey deck, competing route plates, and primitive machine framing. |
| Spine | `P01-Spine-Powered-Gate-Open-1600x900.png` | 1600×900, Spine powered and return gate open, both tower/berth and return threshold readable | Large cyan fields compete with machinery; exposed void and structural blocks interrupt the room hierarchy. |
| Security Trial | `P01-Security-Trial-Cleared-1600x900.png` | 1600×900, Room B cleared and doors open, player at the north threshold | The arena is very dark and flat; threshold/door architecture and the central clear read need stronger physical ownership without hiding combat lanes. |
| Dock | `P01-Dock-Uplink-Locked-1616x939.png` | Legacy development-player capture, uplink locked at the Dock and extraction pad visible | Large overlapping cyan fields dominate the endpoint and the capture includes development-window chrome; P14 must replace this reference with a clean 1600×900 frame before judging final quality. |

## Per-run evidence contract

Each finish run must record:

1. The locked source frame or immediately preceding accepted frame.
2. A same-state, same-camera post frame at 1280×720 and 1600×900.
3. Default and Reduced-Flashes plus Steady-Camera 1600×900 frames when emissive, lighting, VFX, or animated presentation changes.
4. Changed texture import caps, shader families, renderer/material/light counts, shadow-casting light count, primitive references, authored vertex counts, and any exposure, probe, fog, or post-process values.
5. Focused Unity validation and exact logs; route regression only when route, room state, doors, extraction, boot, or outcome authority changes.
6. A human-only verdict left explicitly unproven when no person reviewed hierarchy, comfort, and gameplay-distance readability.

Reject or revise a presentation change that misses a budget, obscures authoritative information, alters gameplay authority, or looks better only in a close editor view rather than the production camera.

## P29 floor-finish evidence

Presentation Run P29 adds one scene-authored, collider-free `Station Floor Finish` prefab across 12 required-route zones. Four shared URP Lit materials and four renderers carry 1,336 authored vertices: 576 panel-seam vertices, 360 functional-threshold vertices, 240 restrained wear/scorch vertices, and 160 maintenance-mark vertices. The kit adds no texture, shader family, emission, light, animation, collider, `AuthoredMapObstacle`, or runtime state; the established authored obstacle count remains 138.

Accepted gameplay-camera frames are `P29-Central-Floor-1600x900.png`, `P29-Relay-Floor-1280x720.png`, and `P29-Trial-Floor-1600x900.png`. They show that seams and wear remain subordinate to the drone, machinery, route signage, and cyan objective language, while the amber Trial threshold retains a color-independent segmented hazard edge. Human review of moving projectiles/enemies, controller traversal, common color-vision simulations, and subjective wear density remains required.
