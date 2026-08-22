# DEAD SIGNAL — Product Backlog

## P0 — First playable (this run)

- [x] Runtime-built arena with readable powered/dead-zone states
- [x] Responsive keyboard movement, mouse aim, and projectile attack
- [x] Shared Signal economy and dead-zone countdown
- [x] Activatable Signal tower with refill and visible territory
- [x] One dormant-then-awakened security enemy
- [x] Three salvage pickups and extraction requirement
- [x] Death, victory, and instant restart flow
- [x] Minimal HUD, prompts, and controls legend
- [x] Deterministic EditMode tests and Unity batch validation

## P1 — Prove the loop

- [x] Split runtime orchestration into focused input, world, combat, salvage, and HUD classes
- [x] Add controller support for the complete run loop
- [x] Add adaptive keyboard/mouse and gamepad guidance with generated input-link art
- [x] Add persistent remappable Input Actions for Fire, Use, and all four movement directions with conflict-safe reset recovery
- [x] Add adaptive action-family glyphs for movement, aim, fire, use, and system prompts

### Run 55 adaptive control-glyph ideas

- [x] Movement glyph — player value: the locomotion route is identifiable before reading bindings; acceptance: original transparent four-way art loads, appears in the live legend, and retains adaptive device text.
- [x] Aim glyph — player value: mouse versus right-stick aiming is easier to scan; acceptance: original reticle art loads beside the active aim label without changing aim behavior.
- [x] Fire glyph — player value: the shared-Signal attack reads as an action at a glance; acceptance: original pulse-emitter art loads beside live keyboard or gamepad fire labels.
- [x] Use glyph — player value: tower, shortcut, and extraction interactions share one recognizable visual verb; acceptance: original connector art appears in both the legend and contextual prompts.
- [x] System glyph — player value: pause and restart recovery remain discoverable during play and outcomes; acceptance: original system-ring art loads in the legend and outcome screen while adaptive labels remain authoritative.
- [ ] Add hardware-family-specific face-button variants after visual testing confirms the device-neutral action set

### Run 54 control-routing ideas

- [x] Move Up rerouting — player value: supports alternate layouts and one-handed play; acceptance: any keyboard key persists, updates prompts, and keeps Up Arrow available.
- [x] Move Down rerouting — player value: completes vertical-layout accessibility; acceptance: any non-conflicting key persists, resets to S, and keeps Down Arrow available.
- [x] Move Left rerouting — player value: supports non-WASD hand placement; acceptance: any non-conflicting key persists, resets to A, and keeps Left Arrow available.
- [x] Move Right rerouting — player value: completes independently configurable movement; acceptance: any non-conflicting key persists, resets to D, and keeps Right Arrow available.
- [x] Six-action conflict safety — player value: prevents a valid movement/combat route from being silently lost; acceptance: Move Up/Down/Left/Right, Fire, and Use reject every duplicate primary key while capture remains active and the prior binding remains valid.
- [x] Add persisted pause-menu remapping for primary keyboard Fire and Use actions
- [x] Add one-click reset-to-default recovery for primary keyboard bindings
- [x] Reject duplicate primary keyboard bindings with persistent-safe conflict feedback
- [ ] Replace bootstrap arena with modular authored room prefabs
- [x] Add a tunable player-follow tactical camera that preserves arena-edge framing and combat impulse
- [x] Add a speed-reactive twin Signal wake to communicate drone acceleration and coasting
- [x] Connect a scene-authored optional east salvage vault and allow extraction after any three of four caches
- [x] Make authored cover and closed gates intercept Signal bolts with readable impact feedback
- [x] Add a scene-authored Signal spine that guides the opening route from extraction to the tower
- [x] Mark the extraction field's opening-route edge with a scene-authored Signal boundary threshold
- [x] Establish the first reusable authored room component with a textured maintenance-deck prefab
- [x] Author the first textured room-shell prefab with perimeter bulkheads and machine sockets
- [x] Place a reusable authored tower-approach junction in the scene with obstacle-driven movement lanes
- [x] Enclose the northeast salvage cache in a reusable authored annex with one tactical entrance
- [x] Frame the southeast salvage cache with a reusable authored coolant gauntlet and staggered tactical lane
- [x] Split the northwest salvage approach with a reusable authored relay fork and direct-versus-wide routes
- [x] Frame the extraction-to-tower opening with a reusable authored departure channel
- [x] Migrate the central Signal tower into a textured authored prefab without changing its interaction or animation
- [x] Migrate the extraction dock into a textured authored prefab without changing its safe-zone or extraction rules
- [x] Migrate the Signal-cost shortcut into a textured authored prefab without changing its route-choice rules
- [x] Migrate the powered Signal-line routing into a textured authored prefab without changing tower rules
- [x] Migrate the six runtime machine props into a textured authored prefab without changing room layout or gameplay
- [x] Migrate the three salvage pickups into a textured authored prefab without changing collection or guidance
- [x] Migrate the maintenance drone into a textured authored prefab without changing movement, aim, or firing
- [x] Replace the maintenance drone's placeholder primitive meshes with a UV-mapped Blender-authored model
- [x] Persist the maintenance drone's mapped URP materials on its prefab outside Play Mode
- [x] Replace the player's placeholder cube projectile with a textured, UV-mapped Blender-authored prefab
- [x] Add a short authored, tunable afterimage to the Signal bolt
- [x] Migrate the Security Warden into a textured authored prefab without changing pursuit, damage, or health
- [x] Replace the Security Warden's placeholder primitives with a UV-mapped Blender model and persistent URP materials
- [x] Replace the Signal Sapper's placeholder primitives with a UV-mapped Blender model and persistent URP materials
- [x] Add a Signal-cost shortcut gate and first route choice
- [x] Add a second enemy archetype that pressures powered territory
- [x] Add an authored, proximity-driven Warden strike warning that respects Reduced Flashes
- [x] Place the dormant Warden inside an authored security bay that becomes post-activation kiting cover
- [x] Open and mark a verified player-width route between the Warden bay and northeast salvage annex
- [x] Place the dormant Signal Sapper inside an authored, southeast-open service cradle that becomes combat cover
- [x] Telegraph the Sapper's tower target and timed drain pulses in-world
- [x] Replace the Sapper's primitive pulse flash with an authored, tunable drain glyph
- [x] Give the Sapper tether an authored directional energy flow without changing its threat rules
- [x] Add a safe pause/resume overlay for keyboard and controller
- [x] Add combat hit-stop, camera impulse, and generated impact-burst feedback
- [x] Add adaptive procedural Signal ambience, distinct gameplay cues, and persisted mute control
- [x] Add adaptive powered/dead-zone Signal-dust particles with a fixed performance budget
- [x] Add a pause-safe, Reduced-Flashes-aware network activation sweep with original circuit art
- [x] Add an adaptive low-Signal edge warning that respects Reduced Flashes
- [ ] Tune resource economy from recorded five-minute play sessions
- [x] Add an end-of-run performance report for balance sessions
- [x] Add dynamic tower, nearest-salvage, and extraction objective guidance
- [x] Add a persisted Steady Camera comfort toggle to disable combat camera impulse
- [x] Add a persisted Reduced Flashes mode for combat and Sapper feedback
- [x] Add a persisted High Contrast mode for world and HUD readability
- [x] Add a build-validation test and Windows development build

## P2 — MVP production

- [ ] Seeded room generation with guaranteed solvable routes
- [ ] Three tower network decisions and escalating director
- [ ] Six tools with mutually exclusive mid-run upgrades
- [ ] Four enemy archetypes plus guardian encounter
- [ ] Salvage economy, permanent unlock track, and save migration
- [ ] Full input, audio, UI, onboarding, and localization-ready text
- [ ] Performance budgets, soak tests, and release build pipeline

## Explicitly deferred

- Online/co-op, accounts, telemetry, monetization, multiple station biomes, narrative cinematics, and licensed art/audio.

## Current tuning questions

- Is dead-zone drain legible and tense without making exploration feel predetermined?
- Does activating a tower feel like a meaningful bargain when it also wakes security?
- Are attacks expensive enough to create decisions but cheap enough to feel responsive?
- Is returning through powered territory satisfying, or merely downtime?
- Does the 16-Signal shortcut price create a real choice against the longer dead-zone detour?
- Does the Sapper arrive late enough to be interceptable but early enough to threaten a greedy salvage route?
