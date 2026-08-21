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
- [ ] Add remappable input actions and button-glyph detection
- [ ] Replace bootstrap arena with modular authored room prefabs
- [x] Add a Signal-cost shortcut gate and first route choice
- [x] Add a second enemy archetype that pressures powered territory
- [x] Telegraph the Sapper's tower target and timed drain pulses in-world
- [x] Add a safe pause/resume overlay for keyboard and controller
- [x] Add combat hit-stop, camera impulse, and generated impact-burst feedback
- [ ] Add layered procedural audio and broader ambient particles
- [ ] Tune resource economy from recorded five-minute play sessions
- [x] Add an end-of-run performance report for balance sessions
- [x] Add dynamic tower, nearest-salvage, and extraction objective guidance
- [x] Add a persisted Steady Camera comfort toggle to disable combat camera impulse
- [x] Add a persisted Reduced Flashes mode for combat and Sapper feedback
- [x] Add a persisted High Contrast mode for world and HUD readability
- [ ] Add a build-validation test and Windows development build

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
