# DEAD SIGNAL — Scheduled Development Instructions

## P0 — Build one cohesive station mission

- [ ] Treat `GAME_VISION.md` → “Current product strategy — one cohesive station mission” and `BACKLOG.md` → “P0 — Cohesive station mission restructuring” as the controlling direction for every scheduled development run.
- [ ] Work strictly from the first incomplete phase and highest actionable checkbox in that backlog section.
- [ ] Implement exactly one bounded, player-facing mission-flow slice per run. A slice may include its necessary deterministic rule, authored room state, interaction, guidance, tests, and minimal presentation, but not a second unrelated mechanic.
- [ ] Preserve a completable run after every slice. Use compatibility adapters or temporary migration states rather than replacing the full mission in one pass.
- [ ] Do not add another room, tower, enemy archetype, upgrade, collectible, arena, optional branch, or cosmetic workstream unless fresh evidence shows the active mission slice cannot succeed without it.
- [ ] Do not spend a run polishing an isolated room that still lacks a documented entry condition, verb, completion condition, world-state change, and place in the required route.

## P0.1 — Required implementation path

- [ ] Phase 0: inventory rooms and adjacency; establish the room-purpose ledger, schematic route, and current journey baseline.
- [ ] Phase 1: introduce a deterministic objective graph and prove parity with the existing seven-stage route before changing progression.
- [ ] Phase 2: implement the Central coupling/coolant/assembly/installation act.
- [ ] Phase 3: implement Relay processing, Foundry calibration, Spine venting, and third-tower activation.
- [ ] Phase 4: implement Induction → Flux → Convergence → Breaker → Furnace → Quench → Room A → Room B → Room C → Spine core installation.
- [ ] Phase 5: implement the powered withdrawal through Warden Bay, Sapper Cradle, Departure surge, and live extraction.
- [ ] Perform whole-run Signal, rewards, population, spawn, and pacing retuning only after the complete required route is connected.
- [ ] Phase 6: after the route is connected, continue through `BACKLOG.md` → “Unattended product shell and presentation hardening” one bounded slice per run: game shell/outcomes, presentation effects, environmental state/readability, then validation.
- [ ] When Phase 6 is implementation-complete, continue into `BACKLOG.md` → “P0 — Geometry Wars-inspired combat proof” from Gate A. Human-only evidence remains explicitly unproven but does not prevent safe technical baselines, readability work, regression coverage, performance validation, or later actionable gates.

## P0.2 — Quality rules for each slice

- [ ] State the player-facing hypothesis, before-state, scope cap, acceptance criteria, and rollback criteria before editing.
- [ ] Give adjacent rooms different verbs. Prefer retrieve, process, reroute, defend, install, pursue, recover, and extract over repeated pickups or switches.
- [ ] Make completion visibly change the station: power territory, open a door or shortcut, alter the return path, grant a build decision, release a recovery resource, or transition combat state.
- [ ] Limit required backtracking to one meaningful installation return per act; reject travel that exists only to lengthen the run.
- [ ] Keep Room B as the only full lockdown-wave arena unless human evidence demonstrates a second arena is necessary. Other combat rooms should use shorter and mechanically distinct pressure.
- [ ] Preserve scene-authored geometry and serialized anchors. Use code for reusable objective rules and orchestration, and focused tuning assets for adjustable timings, encounters, rewards, and economy.
- [ ] Keep `DeadSignalGame` from becoming the permanent owner of room-specific state. Put deterministic progression in engine-independent rules and focused runtime components.

## P1 — Evidence and validation

- [ ] Run the smallest focused EditMode and PlayMode coverage for the active slice, then complete-route regression when progression or navigation changes.
- [ ] Validate objective guidance, tactical map, command strip, collision/projectile blocking, NavMesh, doors, death/restart/re-entry, interrupted interactions, reward idempotency, and keyboard/controller completion.
- [ ] Measure route duration, objective recognition, wrong turns, room entry, backtracking, dead-zone exposure, combat time, Signal minimum/final reserve, failure location, and altered-return recognition.
- [ ] Build and smoke-test the Windows player for integration milestones. Never claim subjective fun from automation; leave a concise human play script when human evidence is unavailable.
- [ ] Update `GAME_VISION.md` only for material decisions, keep the controlling backlog section prioritized, and append exact evidence and limitations to `DEVLOG.md`.

## Evidence-directed priority override

- [ ] The playtest-review automation may replace only the checklist items in this section when fresh evidence identifies a correctness, comprehension, pacing, or fun blocker to the active cohesive-mission phase.
- [ ] No current override. Continue with the first incomplete cohesive-mission phase.

## Blocker before lower-priority work

- [ ] Do not resume general combat expansion, new content, or speculative refactoring while an actionable cohesive-mission implementation phase remains, unless a measured correctness, usability, or playability defect blocks that phase.
- [ ] Once Phases 4 and 5 have connected the required route, unresolved human-only acceptance checks do not block the explicitly authorized Phase 6 product-shell and presentation queue.
- [ ] Phase 6 polish must be player-facing, bounded, testable, presentation-only where stated, and accessibility-safe; do not use it as permission for combat-stat changes, population promotion, new enemies, another arena, procedural generation, save progression, or unrelated cleanup.

## Overall Definition of Done

- [ ] Every major mission room is required and has a distinct understandable purpose, while decorative pockets are honestly classified rather than padded.
- [ ] The complete station journey reads as restart → extend → rebuild → withdraw and finishes in approximately 20–25 minutes for a first successful human run.
- [ ] Each act contains at most one meaningful installation return and the return route visibly benefits from earlier actions.
- [ ] Room B delivers the primary Geometry Wars-inspired climax without turning the rest of the station into repetitive arenas.
- [ ] Signal economy, enemy pressure, weapon progression, navigation, objectives, room state, and extraction form one coherent escalation curve.
- [ ] Applicable Unity suites, full journey, Windows build/smoke, and manual keyboard/mouse plus controller routes pass with no filler-room feedback.
