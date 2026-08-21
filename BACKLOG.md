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
- [ ] Add remappable Input Actions and platform-specific button glyph sets
- [ ] Replace bootstrap arena with modular authored room prefabs
- [x] Establish the first reusable authored room component with a textured maintenance-deck prefab
- [x] Author the first textured room-shell prefab with perimeter bulkheads and machine sockets
- [x] Migrate the central Signal tower into a textured authored prefab without changing its interaction or animation
- [x] Migrate the extraction dock into a textured authored prefab without changing its safe-zone or extraction rules
- [x] Migrate the Signal-cost shortcut into a textured authored prefab without changing its route-choice rules
- [x] Migrate the powered Signal-line routing into a textured authored prefab without changing tower rules
- [x] Migrate the six runtime machine props into a textured authored prefab without changing room layout or gameplay
- [x] Migrate the three salvage pickups into a textured authored prefab without changing collection or guidance
- [x] Migrate the maintenance drone into a textured authored prefab without changing movement, aim, or firing
- [x] Replace the maintenance drone's placeholder primitive meshes with a UV-mapped Blender-authored model
- [x] Migrate the Security Warden into a textured authored prefab without changing pursuit, damage, or health
- [x] Add a Signal-cost shortcut gate and first route choice
- [x] Add a second enemy archetype that pressures powered territory
- [x] Telegraph the Sapper's tower target and timed drain pulses in-world
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
