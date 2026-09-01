# DEAD SIGNAL — Product Backlog

## P0 — Cohesive station mission restructuring

This is the controlling workstream for scheduled development. Complete it from top to bottom, one playable slice per run. Do not add rooms, enemies, upgrades, isolated polish, or optional rewards unless the active slice demonstrates they are necessary to close a specific mission-flow gap.

### Phase 0 — Authoritative map and objective contract

- [x] Inventory every major room, its actual adjacency, current gameplay authority, objective/pickup/enemy ownership, powered/dead state, doors, shortcuts, collision, NavMesh links, and return-route value
- [x] Classify each space as mission room, traversal connector, combat landmark, or decorative pocket; do not force decorative pockets to masquerade as objectives
- [x] Give every mission room an entry condition, player verb, completion condition, persistent world-state change, guidance target/copy, and reason to revisit or pass through
- [x] Add a schematic route and ordered room-purpose ledger to the project documentation and protect critical adjacency/anchor assumptions with focused tests
- [x] Establish measured current-route baselines for completion time, objective recognition, wrong turns, backtracking, dead-zone time, combat time, Signal minimum/final reserve, and rooms never entered

### Phase 1 — Objective graph foundation

- [x] Introduce a deterministic objective graph that can express prerequisites, owning room/anchor, completion rules, world mutations, rewards, guidance, and successor objectives without expanding `DeadSignalGame` into a room-specific state machine
- [x] Keep designer-facing objective and encounter values in focused authored configuration; keep scene layout, anchors, doors, and hazards scene-authored
- [x] Reproduce the current seven-stage journey through the new graph before changing mission order
- [x] Update the objective indicator, command strip, tactical map, debug routes, restart/reset, and run-report telemetry from the same authoritative objective state
- [x] Gate migration on EditMode rule tests, PlayMode route parity, death/restart/re-entry coverage, complete-run regression, and a Windows build/smoke when integration changes

### Phase 2 — Act I: restart the station

- [x] Central Tower activation unlocks two distinct required jobs rather than interchangeable payloads
- [x] Cargo Annex supplies the power coupling through its commit-and-withdraw spatial verb
- [x] Coolant Reclamation supplies the coolant seal through its baffle-threading spatial verb
- [x] Relay Fork routes both components into the transfer vault; the vault assembles the Central payload
- [x] Returning the assembled payload to the Central Tower installs it, changes the station state, and opens the Relay route
- [x] Keep the act concise, permit the two component rooms in either order, and reject repetitive pickup-only interactions

### Phase 3 — Act II: extend the network

- [x] Relay Foundry activation creates the second powered foothold and unlocks payload processing
- [x] Cooling Gantry stabilizes the Relay payload; Foundry calibration installs it and owns the weapon-transformation choice
- [x] Capacitor Spine establishes the third-tower objective; Spine Discharge Trench vents the berth before interaction
- [x] Spine Tower activation installs the Relay result, evolves the weapon, powers the deep return network, and opens the core-rebuild act
- [x] Prove that every room changes traversal, power, build state, or combat pressure and that only one meaningful installation return is required

### Phase 4 — Act III: rebuild the Signal core

- [x] Induction Gallery charges the empty lattice
- [x] Flux Bypass throws the shunt that makes Convergence calibration possible and changes the later return route
- [x] Convergence Chamber runs one bounded, shorter calibration holdout distinct from the full lockdown trial
- [x] Breaker Gallery resets distribution and unlocks the Furnace process
- [x] Arc Furnace forges the charged lattice; Quench Loop stabilizes it while opening its existing return shortcut
- [x] Room A commits the player to the final trial, Room B provides the required Geometry Wars-inspired combat climax, and Room C supplies the mission-critical station capacitor
- [x] Returning the completed core to the Spine Tower installs the final payload and enables withdrawal
- [x] Do not replicate the Room B wave structure in the other deep rooms; use routing, processing, short defense, or mixed-role pressure instead

### Phase 5 — Act IV: changed-station withdrawal

- [x] Route the required withdrawal through visibly powered territory and shortcuts opened by earlier objectives rather than retracing the outbound path unchanged
- [x] Give Warden Bay and Sapper Cradle explicit return-pursuit functions using their established enemy identities and counterplay
- [x] Preserve the Departure Channel cargo release and one-shot surge as the final recovery/readability beat
- [x] Complete the live extraction uplink at the Dock with movement, combat, and chosen build still relevant
- [ ] Retune whole-run Signal, rewards, enemy density, spawn timing, and encounter mix only after the complete route exists — **DEFERRED:** Runs 157–163 exhausted the current isolated automated levers; resume only with new evidence, a human playtest, or a materially different implementation approach. This does not block Phase 6.

### Phase 6 — Unattended product shell and presentation hardening

Work through this section after the required mission route is connected. Human-only mission acceptance evidence does not block these low-risk slices. Implement one bounded player-facing improvement per run, preserve gameplay authority, and keep every effect tunable and accessibility-safe.

#### Game shell and outcome flow

- [x] Audit the existing boot, pause, outcome, restart, input-focus, and scene-lifetime paths; protect the current playable route with a focused lifecycle test before changing navigation
- [x] Add an authored main-menu shell around the existing boot flow with Start Run, Settings, Controls, and Quit; support keyboard/mouse and controller without introducing speculative save/continue behavior
- [x] Add reliable Return to Menu actions from pause, defeat, and victory; prove repeated menu → run → restart/menu loops do not duplicate runtime services, input actions, audio, or scene state
- [x] Turn the existing defeat overlay into a proper game-over presentation with a clear failure cause, concise run summary, Restart, and Main Menu
- [x] Turn the existing victory overlay into a distinct completion presentation with mission time, room/combat/Signal highlights, Restart, and Main Menu
- [x] Add short menu-to-run, defeat, and victory transitions that preserve input focus, pause semantics, Reduced Flashes, and Steady Camera

#### Presentation-effect foundation

- [x] Inventory current particles, trails, flashes, camera impulses, post-processing, generated materials, and effect ownership; define per-effect lifetime, pooling/allocation, contrast, and accessibility limits before adding effects — see `PRESENTATION_EFFECT_AUDIT.md`
- [x] Add or refine pooled projectile-impact and enemy-purge effects with distinct enemy, wall, and shield reads; keep projectile collision and damage rules unchanged
- [x] Add restrained directional damage and critical-Signal screen feedback that never obscures projectiles, interaction prompts, enemy telegraphs, or escape lanes
- [x] Add authored activation/completion effects for towers, payload installation, doors, shortcuts, and machinery state changes using the established amber → cyan language
- [x] Add bounded lockdown-entry, phase-transition, room-clear, capacitor/salvage recovery, and reward-release effects without changing encounter timing, population, rewards, or door authority
- [x] Make weapon transformation and evolution visibly distinct for each established build without changing targeting, cadence, damage, Signal, or enemy counters
- [x] Add extraction startup, progress, completion, defeat, and victory effects with Reduced-Flashes alternatives and no persistent full-screen clutter

#### Environmental state and readability polish

- [ ] Complete the required-machinery and mission-door readability queue below in order, improving one coherent room or tightly coupled room pair per run rather than applying a global cosmetic rewrite
  - [x] **Run 1 — Act I, Central Tower and Transfer Vault:** distinguish the Central Tower's dormant, activation-available, activating, powered, payload-install-available, and payload-installed states; give the Transfer Vault assembler readable locked, available, processing, and assembled states; preserve the Central transaction, objective authority, and amber → cyan language
  - [x] **Run 2 — Act I, Cargo Annex:** add a persistent dormant/locked read before the coupling job becomes available while preserving the existing commit, withdrawal, and secured progression; ensure the machine never appears interactable before objective authority permits it
  - [x] **Run 3 — Act I, Coolant Reclamation:** distinguish dormant/locked machinery from the existing first-baffle, second-baffle, release, and stable states without obscuring the threading route or interaction prompt
  - [x] **Run 4 — Act I, Relay Fork and Central route doors:** distinguish Relay Fork dormant/locked, routing-available, routing-active, and routed states; give the Central shortcut gate and Central Relay Route Gate persistent, locally readable locked and open/complete doorway states instead of relying on the blocking slab disappearing
  - [x] **Run 5 — Act II, Relay Foundry and Cooling Gantry:** distinguish Relay Tower dormant, activation-available, activating, and powered states; distinguish Cooling Gantry prerequisite-locked, processing-available, active, and stabilized states; preserve the Foundry refill, weapon path, powered territory, and reinforcement behavior
  - [x] **Run 6 — Act II, Foundry calibration and Relay Return Bulkhead:** distinguish payload stabilized, installation available, installation active, and installed states; give the Relay Return Bulkhead persistent locked and opened/complete reads without changing collision, NavMesh, or shortcut authority
  - [x] **Run 7 — Act II, Spine Discharge Trench and Spine Tower:** make the berth visibly dormant/pressurized before vent authority, vent-available, venting-active, and vented; make the Spine Tower visibly locked while pressurized, available after venting, activating, and powered; preserve the evolved-weapon and deep-network transitions
  - [x] **Run 8 — Act II, Spine Return Gate:** give the return gate persistent locked and opened/complete threshold reads tied to Spine power instead of relying on disappearance; preserve both authored approaches, projectile collision, NavMesh rebuilding, and powered-return routing
  - [x] **Run 9 — Act III, Induction Gallery and Flux Bypass:** distinguish prerequisite-locked, available, active, and complete states for the lattice charger and Flux shunt; keep their different charging and routing verbs legible and preserve the powered return flank
  - [x] **Run 10 — Act III, Convergence Chamber:** use the existing available, active, and complete presentation as the reference implementation, then add distinct dormant and prerequisite-locked reads without hiding threats, the calibration volume, progress feedback, or escape lanes
  - [x] **Run 11 — Act III, Breaker Gallery:** distinguish distribution-locked, reset-available, reset-active, and reset-complete states while preserving the room's lateral combat loop and Furnace prerequisite authority
  - [x] **Run 12 — Act III, Arc Furnace and Quench Loop:** distinguish locked, available, processing-active, and complete states for forging and stabilization; give the Quench Pressure Shutter persistent locked and released/open reads without changing the required process, optional cache authority, or return shortcut
  - [x] **Run 13 — Act III, Security Trial commitment room:** clarify the breaker lifecycle from dormant/locked through commitment-available, committed/active, and complete while preserving the red no-return warning, exact threshold authority, and Room A's non-combat purpose
  - [x] **Run 14 — Act III, Lockdown Chamber and Reward Vault:** retain the chamber's full dormant, available, locked, active, cleared, and reward-release lifecycle; make the Lockdown Entry Door and Reward Vault Door readable when sealed and after release without relying only on disappearance; distinguish capacitor available, collected, and empty-vault complete states
  - [x] **Run 15 — Act III, Spine core installation:** distinguish installation locked, completed-core available, installation active, and final installed states at the Spine socket without weakening the changed-station withdrawal handoff
  - [x] **Run 16 — Act IV, Departure Channel:** distinguish the cargo shutter and surge machinery's dormant/locked, release-available, release-active, open, surge-available, and surge-consumed states; retain the one-shot recovery, both flanks, collision, and extraction-readiness authority
  - [x] **Run 17 — Act IV, Extraction Dock:** align the physical uplink with its existing locked, available, active-progress, complete, defeat, and victory feedback; add a deliberate dormant read and keep movement, combat, Reduced Flashes, and Steady Camera behavior intact
  - [x] **Run 18 — Act I presentation composition:** revisit the Central Tower, Cargo Annex, Coolant Reclamation, Relay Fork, and Transfer Vault using the 2026-08-29 presentation captures as baseline evidence; replace or reduce oversized flat-color primitive state markers that overpower the authored machinery, and add bounded station underdeck, wall-back, threshold, or shadow-backed composition where the gameplay camera exposes black void or abrupt room cutoffs; do not expand traversable space, alter objective authority, or globally redesign the act
  - [x] **Run 19 — Act II presentation composition:** revisit the Relay Foundry, Cooling Gantry, Capacitor Spine, Discharge Trench, and their return gates; preserve the new machinery/door lifecycle language while replacing visually dominant primitive markers and closing camera-visible voids at the authored room boundaries with non-colliding station structure or backdrop treatment; preserve both approach choices, tactical windows, projectile authority, NavMesh, and powered return routes
  - [x] **Run 20 — Act III deep-core presentation composition:** revisit Induction, Flux, Convergence, Breaker, Furnace, and Quench as one connected deep-core visual sequence; keep each generated glyph and machine verb distinct, reduce remaining yellow/orange/white/cyan primitive slabs that compete with them, and give every required gameplay-camera view a deliberate station edge instead of an accidental black void; preserve cover silhouettes, optional-cache readability, threats, projectiles, and all route geometry
  - [x] **Run 21 — Act III Security Trial presentation composition:** revisit Room A, Room B, and the Reward Vault using the armed, cleared, capacitor-available, and empty-complete captures as baseline evidence; refine oversized warning/state primitives, persistent door thresholds, capacitor cradle hierarchy, and room-edge backing so the commitment, lockdown, reward, and completed states remain dominant without obscuring combat silhouettes or exposing accidental voids; preserve the exact no-return threshold, arena population, door collision, NavMesh rebuilding, and reward authority
  - [x] **Run 22 — Act IV withdrawal and Dock composition:** revisit the Departure Channel and Extraction Dock after their lifecycle work; ensure shutter, surge, and uplink machinery read above legacy primitives, close camera-visible room-edge voids with authored non-traversable station backing, and preserve the wide withdrawal/extraction escape lanes, combat visibility, outcome UI, and both accessibility settings
  - [ ] In each run, inspect the targeted machinery and doors for prominent primitive-only or basic-geometry presentation; when a required interactable or frequently viewed landmark still relies on cubes, cylinders, planes, or similarly generic construction, create original purpose-built mesh and texture assets for that room instead of merely recoloring or rearranging the primitives; retain simple hidden geometry only where it remains appropriate for collision, triggers, bounds, diagnostics, or genuinely minor background dressing
  - [ ] Package every new mesh, texture, material, animation, and controller as a proper Unity asset with its `.meta` file; preserve existing GUIDs when replacing references, keep gameplay collision and objective authority independent from presentation meshes, and verify import, build inclusion, materials, lighting, tactical-window coverage, and authored prefab/scene bindings
  - [ ] Add bounded authored animation when motion materially improves state recognition, such as tower startup, machinery spin-up/processing/cooldown, breaker throws, shutters retracting, doors sealing/releasing, payload insertion, or uplink activation; provide a readable static end state, avoid decorative perpetual motion that implies false interactivity, and preserve Reduced Flashes, Steady Camera, timing authority, interaction timing, collision, NavMesh, and reset/re-entry behavior
  - [ ] For every run, prove state authority and reset/re-entry behavior with focused EditMode or PlayMode coverage, then inspect the changed room interactively at 1280x720 and 1600x900 with Reduced Flashes on/off and Steady Camera on/off
  - [ ] For every stateful door, preserve movement collision, projectile authority, NavMesh links/rebuilds, enemy routing, and the existing open condition; use a persistent open-frame, retracted-panel, threshold light, or route glyph where needed so "open" is not communicated only by removing the slab
  - [ ] Do not treat reinforcement entrances as player progression doors, and do not add lock/readiness effects to permanent authored thresholds whose historical bulkheads were intentionally removed
  - [ ] For Runs 18–22, capture matched before/after gameplay-camera frames from the same player position and state at 1280x720 and 1600x900; reject any revision that merely moves the black void elsewhere, makes a state marker more visually dominant than its owning machine, hides the drone or interaction prompt, or weakens a learned route landmark
  - [ ] Fix exposed room edges with authored non-traversable shell, underdeck, backdrop, parapet, shadow, or bounded camera composition appropriate to the station; do not add fake playable floor, broaden world bounds, move collision to match presentation, or tighten the camera in a way that harms combat awareness
- [ ] Strengthen powered-route lighting and persistent changed-station cues on withdrawal while preserving navigation contrast, collision, NavMesh, and performance

#### Player and threat model presentation

Complete one actor per run after the room-composition queue. Give every established actor a purpose-built, UV-mapped 3D presentation with original textures and bounded animation appropriate to its role. Preserve deterministic movement roots, collision/hit radii, target points, attack timing, health, damage, speed, Signal values, spawn authority, population limits, and response budgets.

- [x] **Actor Run 1 — Maintenance Drone:** replace or refine any remaining primitive or flat presentation with a cohesive authored maintenance-drone model, original dark-alloy/white-ceramic/cyan textures, and readable idle/hover, locomotion bank, aim/turret tracking, basic-fire recoil, evolved-weapon fire, dash, damage, critical-Signal, defeat, and recovery animation layers; keep movement, aim, muzzle authority, collision, camera framing, wake trails, and controller/keyboard response unchanged
- [x] **Actor Run 2 — Security Warden:** assess the existing three-piece UV-mapped model at gameplay distance and refine its mesh, materials, and graphite/crimson texture set only where the current asset still reads as crude or primitive; add readable dormant wake-up, pursuit weight-shift, strike anticipation/commit/recovery, hit reaction, shield/armor response, and purge animation without changing contact range, pursuit, attack timing, health, bounty, or Warden–Sapper screening
- [x] **Actor Run 3 — Signal Sapper:** assess the existing four-piece UV-mapped model and refine its black-violet/magenta siphon silhouette and textures where needed; add readable cradle wake-up, tower-seeking locomotion, latch deployment, siphon buildup/pulse, interruption, hit reaction, tether ownership, and purge animation while preserving arrival timing, latch position, drain countdown, pulse interval, health, bounty, and Sapper combination rules
- [x] **Actor Run 4 — Interceptor:** replace any primitive-only presentation with an authored, textured 3D interceptor whose forward axis, charge hardware, armor break-up, and red/amber role identity remain legible from the production camera; add entry, pursuit, charge-lock anticipation, committed dash, cover crash, short/long recovery, hit reaction, and purge animations without moving the deterministic root or changing charge lines, collision, dash distance/timing, health, bounty, or mixed-role flank logic
- [x] **Actor Run 5 — Suppressor:** replace any primitive-only presentation with an authored, textured 3D suppression platform whose magenta field projector, movement direction, and vulnerable body remain distinct from the Sapper; add entry, approach, warning-ring deployment, field projection sustain, field shutdown, hit reaction, and purge animations without changing field center/radius, warning time, active duration, penalties, movement, health, bounty, extraction profiles, or Interceptor coordination
- [x] **Actor Run 6 — Security Swarmer:** upgrade the existing geometric pressure unit into a compact authored, textured 3D model while retaining its intentionally fragile one-bolt silhouette and keeping it visually subordinate to specialist enemies; add swarm flight/locomotion, convergence lean, contact wind-up, bolt hit reaction, and rapid purge animation without changing population formations, one-bolt durability, movement pressure, contact drain, purge recovery, concurrency caps, or its current promotion gate
- [ ] For every actor run, use original purpose-built meshes and UV layouts with authored albedo/base-color, emissive, metallic/roughness or mask, and normal/detail textures where those maps materially improve the production-camera read; do not add texture channels, polygons, bones, or materials that are invisible at gameplay distance, and do not discard an existing authored model merely to satisfy an asset-count goal
- [ ] Prefer an Animator, AnimationClip, or focused procedural presentation component appropriate to the actor, but keep animation presentation-only: locomotion and attacks must follow the authoritative transform/state rather than drive gameplay through root motion, animation events, collider motion, or timing changes; every transition must settle deterministically, reset on scene/restart, and remain readable with Reduced Flashes and Steady Camera
- [ ] Give each actor a distinct idle/locomotion silhouette and anticipation → active → recovery motion grammar; avoid continuous decorative motion that implies a false attack, masks the real telegraph, creates noisy synchronized crowds, or makes specialists harder to prioritize
- [ ] Validate each actor at 1280x720 and 1600x900 in representative solo and mixed-role encounters, including off-screen entry and edge-indicator handoff; capture matched before/after stills plus a short movement/attack sequence, and reject a revision that hides projectiles, telegraphs, objectives, interaction prompts, the drone, or an escape lane
- [ ] Measure renderer, material, bone, animation, and recurring-allocation cost with the maximum established combat population; use shared materials, bounded bones, pooling, GPU-friendly shaders, and LOD or simplified distant animation where evidence requires them
- [ ] Improve existing enemy entry warnings, silhouettes, projectile contrast, and specialist telegraphs alongside the owning actor only when needed to integrate its new model; do not change health, damage, speed, count, timing, Signal economy, or response budgets during an actor-presentation run

#### Release-readiness presentation queue — 60 bounded runs

Execute this queue in order after the machinery/door, room-composition, and actor-model runs above. Complete one numbered item per unattended run. These are presentation tasks, not permission to add gameplay, rooms, enemies, rewards, objectives, map area, combat tuning, packages, or speculative systems. Reuse and refine strong authored work; generate original meshes, textures, decals, animation, or VFX only where the production-camera result materially improves. Every run must leave the game playable, preserve serialized references and GUIDs, capture useful evidence, and document exact validation.

##### Environment materials and landmark finish

- [x] **Presentation Run P01 — Visual-quality baseline:** inventory production-camera texel density, material response, mesh silhouette quality, emissive intensity, primitive exposure, texture filtering, and shader usage; publish measurable environment/actor/UI/VFX presentation budgets and select representative Central, Spine, Security Trial, and Dock comparison frames without changing assets — published in `PRESENTATION_QUALITY_BASELINE.md`; four locked source captures are preserved under `ArtSource/PresentationBaseline`
- [x] **Presentation Run P02 — Central Tower hero finish:** refine the tower, activation platform, surrounding consoles, cable feeds, floor inlay, and immediate wall/deck materials into one hero landmark with authored texture wear and restrained emissive hierarchy; preserve interaction side, power radius, collision, and camera clearance
- [x] **Presentation Run P03 — Cargo Annex finish:** refine the coupling machine, sockets, withdrawal path, cargo fixtures, wall treatment, and state surfaces so retrieve-and-withdraw reads through machinery shape and materials rather than bright primitives; preserve the complete spatial verb and all authored bounds
- [x] **Presentation Run P04 — Coolant Reclamation finish:** refine manifold, baffles, seal hardware, conduits, condensation-safe surface language, and stable-state materials into a distinct coolant-processing room; preserve baffle order, prompt visibility, collision, and threading lanes
- [x] **Presentation Run P05 — Relay Fork and Transfer Vault finish:** unify routing console, fork hardware, assembler banks, route gate, floor routing, and copper/ceramic texture language while keeping routing, assembly, and route-open states visually distinct; preserve both rooms' different verbs and the gate's authority
- [x] **Presentation Run P06 — Warden Bay and Sapper Cradle finish:** refine containment architecture, dormant mounts, hazard markings, bay/cradle materials, and post-activation cover readability so each landmark foreshadows its owning enemy without becoming a false objective; preserve entrances, enemy release paths, and cover collision
- [x] **Presentation Run P07 — Relay Foundry hero finish:** refine tower, turbines, induction hardware, center bulkhead frame, reinforcement-gate housings, deck wear, and powered materials into a second-region hero space; preserve both approaches, safe gates, shortcut authority, and tactical-window composition
- [x] **Presentation Run P08 — Cooling Gantry finish:** refine cooling machinery, processing bed, pipes, vents, guard structure, status surfaces, and floor cues into a recognizable stabilization station distinct from the Foundry installation point; preserve processing authority and return traversal
- [x] **Presentation Run P09 — Capacitor Spine and Discharge Trench finish:** unify transfer bank, tower berth, pressure console, coils, ceramic shields, return threshold, and high-voltage surface language while retaining clear pressurized, vented, available, and powered states; preserve both Spine approaches and projectile collision
- [x] **Presentation Run P10 — Induction Gallery and Flux Bypass finish:** refine lattice charger, induction coil, shunt regulator, bus feeds, baffles, wall trims, and generated glyph integration so radial charging and directional rerouting remain visually different; preserve the dead-zone and powered-return routes
- [x] **Presentation Run P11 — Convergence and Breaker finish:** refine calibration aperture, busbar, breaker bank, selector hardware, ceramic shields, route thresholds, and material transitions into a connected calibration/distribution complex; preserve the holdout volume, lateral loop, cover, and escape lanes
- [x] **Presentation Run P12 — Arc Furnace and Quench finish:** refine furnace shell, lattice fixture, heat shielding, quench condenser, coolant loop, deflectors, pressure shutter frame, and process materials so forging and stabilization read as separate industrial stages; preserve optional-cache visibility and both return choices
- [x] **Presentation Run P13 — Security Trial wing finish:** refine commitment breaker, threshold architecture, arena floor/walls, phase landmark, door frames, reward-vault shell, capacitor cradle, and security material hierarchy into a cohesive three-room culmination; preserve Room A/B/C purpose separation and combat sightlines
- [x] **Presentation Run P14 — Departure Channel and Extraction Dock hero finish:** refine cargo-release machinery, capacitor banks, shutter housing, surge path, Dock uplink, pad structure, boundary treatment, and final-route materials into a release-quality opening/finale pair; preserve both flanks, escape space, and outcome presentation

##### Lighting, atmosphere, and color hierarchy

Treat lighting as the primary room-composition layer in these runs. Preserve controlled darkness between localized task-light pools; give each room one dominant practical-light role tied to its verb; and make station restoration relight local machinery, thresholds, and structure. Cyan powered territory remains authoritative but must support rather than flatten room identity or dominate ordinary frames. Every comparison must retain the drone, projectiles, telegraphs, objectives, prompts, hazards, and at least one escape lane at 1280x720 and 1600x900 with accessibility settings enabled.

- [x] **Presentation Run P15 — Lighting-tuning foundation:** move adjustable environment-light intensity, range, color, shadow, emissive, ambient floor, fog, exposure, bloom, and post-processing values into focused authored tuning; define dominant/secondary task-light roles, practical-light ownership, cyan-overlay restraint, state-driven relighting, performance ceilings, and luminance-comparison evidence without changing gameplay visibility authority
- [x] **Presentation Run P16 — Opening and Central lighting:** shape the Dock-to-Central opening with controlled darkness and localized task-light pools for immediate drone/objective recognition, safe movement, and one strong Central focal hierarchy; make tower restoration relight nearby machinery and thresholds while reducing dependence on broad cyan coverage and preserving both opening flanks
- [x] **Presentation Run P17 — Act I branch lighting:** give Cargo, Coolant, Relay Fork, and Transfer Vault distinct practical-light identities tied to retrieve, thread, route, and assemble; use value, direction, source shape, and local completion relighting—not hue alone—to mark return direction while keeping enemies and projectiles readable
- [x] **Presentation Run P18 — Relay-region lighting:** establish the Foundry as an amber induction/turbine landmark and the Gantry as a colder stabilization space through bounded task lights, directional spill, and dormant-to-powered relighting; preserve turbine lanes, gate warnings, weapon-calibration focus, magenta/red threat contrast, and restrained return-route cyan
- [x] **Presentation Run P19 — Spine-region lighting:** use localized high-voltage fixtures and controlled shadow to separate protected and exposed approaches, pressure warning, vent transition, tower activation, and powered return across Spine and Trench; make completed machinery relight the route without obscuring projectile paths or either approach
- [x] **Presentation Run P20 — Deep-core lighting:** create a deliberate value progression from dead-zone Induction/Flux through Convergence/Breaker to Furnace/Quench using localized task light, restrained heat/coolant contrast, and state-driven practical fixtures; preserve darkness as atmosphere while keeping navigation, prompts, hazards, and return cues unambiguous
- [x] **Presentation Run P21 — Security Trial lighting:** stage Room A warning, Room B phase escalation/clear, and Room C reward/recovery as three distinct localized-light compositions; use darkness to frame the climax without washing out enemy silhouettes, red warnings, magenta siphons, cyan projectiles, door states, or escape lanes
- [x] **Presentation Run P22 — Withdrawal and Dock lighting:** make earlier restoration visibly repaint the return through relit footholds, pursuit landmarks, Departure machinery, and Dock uplink stages; ensure the powered withdrawal differs from the outbound route through practical light and structure rather than larger cyan fields, while retaining live-threat visibility
- [x] **Presentation Run P23 — Shadow, probe, exposure, and post-process consistency:** audit shadow direction/softness/resolution, light leakage, reflection/probe coverage, ambient floor, bloom, exposure, vignette, fog, and grading across representative dormant/powered/combat frames; correct evidence-backed discontinuities, cyan dominance, crushed navigation values, and accessibility contrast without flattening local room identities
- [x] **Presentation Run P23B — Alien Swarm-style lighting perceptibility correction:** add a persistent player-centered traversal pool and aim-following soft-shadow spotlight, strengthen localized landmark pools, allow the nearest authored projector lights to cast soft shadows, and reduce global key, ambient, and powered-territory dominance so the lighting work remains visible during ordinary play without changing gameplay authority
- [ ] **Human lighting acceptance:** play the complete route at 1280x720 and 1600x900 with keyboard/mouse and controller, including High Contrast and Reduced Flashes, and confirm moving player light, shadow occlusion, threats, projectiles, prompts, escape lanes, and GPU cost remain acceptable

##### Modular station detail and world cohesion

- [x] **Presentation Run P24 — Underdeck and void treatment:** extend the authored non-traversable underdeck/backdrop language beneath every required gameplay-camera view that still exposes accidental pure-black void, using bounded modular structure without adding playable floor or changing world bounds
- [x] **Presentation Run P25 — Wall and parapet kit:** create or refine reusable authored wall faces, corner caps, parapets, supports, backs, and end pieces that eliminate abrupt primitive cutoffs while preserving foreground cutaway ownership and collision footprints
- [x] **Presentation Run P26 — Stateful door-frame kit:** refine reusable frames, tracks, pistons, seals, warning lamps, retracted-panel pockets, and open-threshold treatment for progression doors without changing their blocker objects, NavMesh authority, or open conditions
- [x] **Presentation Run P27 — Navigation-signage kit:** establish restrained text-free station-sector symbols, hazard bands, directional chevrons, room identifiers, and powered-return decals that support learned routes without duplicating the HUD or creating false objectives
- [x] **Presentation Run P28 — Cable, conduit, and pipe integration:** add bounded authored cable trays, power buses, coolant pipes, junctions, and termination details where major machines currently appear unconnected to the station; keep them collider-free unless an existing authored obstacle owns the footprint
- [x] **Presentation Run P29 — Floor finish and wear:** refine floor panels, seams, thresholds, scorch/wear, maintenance markings, and local decals by room function while preserving objective icons, projectile contrast, hazard boundaries, and collision-authoritative geometry
- [x] **Presentation Run P30 — Functional prop kit:** create a small reusable set of release-quality crates, tool carts, service canisters, cable reels, guard rails, and maintenance fixtures; place them only where they strengthen scale and composition without narrowing routes, adding cover authority, or implying pickups
- [x] **Presentation Run P31 — Station depth and parallax:** refine distant superstructure, underdeck layers, shafts, machinery silhouettes, and bounded parallax cues visible beyond room edges so the station feels spatially continuous without distracting motion or fake traversable surfaces

##### HUD, menus, prompts, and outcome presentation

- [x] **Presentation Run P32 — HUD composition:** refine overall HUD alignment, spacing, scale, safe-zone behavior, panel hierarchy, typography, and visual weight at 1280x720, 1600x900, and ultrawide while keeping the tactical field unobstructed
- [x] **Presentation Run P33 — Signal meter:** turn Signal reserve, drain, recovery, critical state, and transaction preview into one polished instrument with readable motion and color hierarchy; preserve exact values, warning timing, Reduced Flashes, and color-independent critical communication
- [x] **Presentation Run P34 — Objective card and edge indicator:** refine icon, title, verb, hint, distance, on-screen collapse, off-screen tail, animation, and room identity so guidance feels authored and calm rather than debug-like; preserve objective authority and three-indicator cap
- [x] **Presentation Run P35 — Interaction prompts:** refine prompt anchoring, device glyphs, action wording, affordability/prerequisite presentation, in-range transition, and contrast for keyboard/mouse and controller without delaying or changing interaction input
- [x] **Presentation Run P36 — Threat HUD and edge indicators:** refine specialist health, role identity, urgency, off-screen direction, grouped Swarmer count, attack-state emphasis, and purge feedback while preserving target-priority clarity and indicator caps
- [x] **Presentation Run P37 — Tactical map:** refine room silhouettes, powered territory, objectives, doors/shortcuts, current position, route changes, legend, zoom/fit, and controller navigation so it reads as a finished station schematic rather than diagnostic geometry
- [x] **Presentation Run P38 — Main menu:** refine title treatment, animated but restrained background, Start/Settings/Controls/Quit hierarchy, focus states, controller navigation, transitions, and branding at all target aspect ratios without inventing save/continue behavior
- [x] **Presentation Run P39 — Pause, Settings, and Controls:** refine panels, tabs, focus, sliders/toggles, input diagrams, accessibility explanations, confirmation states, and return actions while preserving pause ownership and immediate input-mode switching
- [x] **Presentation Run P40 — Defeat presentation:** refine failure-cause hierarchy, environmental dimming, run-summary reveal, Restart/Main Menu focus, transition timing, and accessibility alternatives into a deliberate terminal state without hiding useful evidence
- [ ] **Presentation Run P41 — Victory and debrief presentation:** refine extraction completion, title, mission-time/room/combat/Signal highlights, chosen build, staged reveal, Restart/Main Menu focus, and background treatment into a distinct release-quality payoff

##### Combat, objective, and ambient VFX finish

- [ ] **Presentation Run P42 — Basic fire and muzzle finish:** refine drone muzzle geometry, bolt launch, trail, cadence readability, light contribution, and firing recoil so free continuous fire feels responsive without widening projectiles or changing damage/collision
- [ ] **Presentation Run P43 — Evolved weapon finish:** give Piercing Pulse and Controlled Ricochet distinct launch, flight, continuation/redirect, impact, and termination effects that expose their tactical behavior without obscuring aim lines, cover, or hit authority
- [ ] **Presentation Run P44 — Hit and purge language:** refine enemy-hit, armor-hit, wall-hit, shield-hit, purge, bounty recovery, and chain feedback into consistent layered effects with pooled ownership and no per-hit material creation
- [ ] **Presentation Run P45 — Warden effects:** integrate wake-up, strike anticipation, contact impact, armor response, recovery opening, and purge effects with the finished Warden model while preserving its red role language and exact attack timing
- [ ] **Presentation Run P46 — Sapper effects:** integrate emergence, target acquisition, tether packets, latch, countdown, interrupted pulse, successful drain, and purge effects with the finished Sapper model while retaining clear magenta direction and Reduced Flashes support
- [ ] **Presentation Run P47 — Interceptor effects:** refine gate entry, target lock, charge line, dash wake, cover crash, recovery vulnerability, hit, and purge effects around the finished model without changing the avoidance window or masking perpendicular escape routes
- [ ] **Presentation Run P48 — Suppressor effects:** refine entry warning, field forecast, activation edge, sustained projection, caught-player response, exit, shutdown, and purge effects while maintaining a transparent tactical center and build-specific extraction profiles
- [ ] **Presentation Run P49 — Swarmer effects:** refine group entry, movement wake, contact warning, one-bolt hit, rapid purge, and grouped pressure feedback so populations feel energetic but remain subordinate to specialists and cheap enough at the established cap
- [ ] **Presentation Run P50 — Machinery and door transition effects:** unify tower startup, processing, payload insertion, breaker throws, door seal/release, shutter retraction, shortcut opening, and static completed-state effects after the room/model passes; preserve timing authority and avoid effect duplication
- [ ] **Presentation Run P51 — Trial and extraction climax effects:** refine Room A commitment, lockdown phases, room clear, capacitor release/collection, core installation, Departure surge, uplink startup/progress/completion, defeat, and victory effect hierarchy without stacking unreadable full-screen flashes
- [ ] **Presentation Run P52 — Ambient station effects:** add restrained dust, sparks, steam, coolant mist, heat shimmer, electrical drift, venting, and powered-machine ambience only at authored emitters with pooling, distance culling, accessibility limits, and no false hazard or interaction reads

##### Camera, accessibility, performance, and final presentation gates

- [ ] **Presentation Run P53 — Gameplay-camera framing:** audit objective approach, combat, backtracking, narrow-room, and room-edge framing across the required route; fix bounded camera focus/limits only where evidence shows poor composition, without shrinking tactical awareness or revealing new collision problems
- [ ] **Presentation Run P54 — Foreground cutaway finish:** inspect every owned foreground wall/face transition, fade footprint, material response, restoration, and camera-boundary case; eliminate popping, opaque sibling leaks, and distracting cyan footprints while preserving collision and route readability
- [ ] **Presentation Run P55 — Contrast and color-independence:** validate drone, enemies, projectiles, objectives, doors, hazards, powered routes, and UI under normal, Reduced Flashes, high-contrast, and common color-vision simulations; add shape/value/motion redundancy rather than relying only on hue
- [ ] **Presentation Run P56 — Resolution and aspect-ratio visual QA:** capture the full required route at 1280x720, 1600x900, and one ultrawide target; fix clipping, camera-edge voids, tiny details, oversized markers, HUD overlap, unsafe prompts, and composition failures one bounded owner at a time
- [ ] **Presentation Run P57 — Rendering and overdraw budget:** profile representative quiet, mixed-combat, Security Trial, and extraction frames; reduce transparent overdraw, excessive lights/shadows, material instances, particle counts, and shader cost without flattening the established visual hierarchy
- [ ] **Presentation Run P58 — Asset and import audit:** verify every presentation mesh, texture, material, animation, VFX asset, prefab, scene binding, Resources reference, importer setting, GUID, `.meta`, Windows inclusion, and reproducible setup hook; remove only confirmed orphaned duplicates created by the presentation work
- [ ] **Presentation Run P59 — Complete-route capture review:** record a labeled before/after gallery for every major mission room in dormant/locked, available, active where applicable, complete, and powered-return states; review it as one visual journey and create only bounded follow-up defects with exact frame evidence
- [ ] **Presentation Run P60 — Release presentation gate:** run applicable Unity suites, repeated restart/re-entry/outcome cycles, Windows development build, D3D11 packaged smoke, full required and optional journeys, target-resolution captures, and final critical-log scan; report remaining human-only visual checks and do not mark presentation complete from automation alone

#### Product-shell validation

- [ ] Validate main menu, pause, prompts, outcome screens, and transitions at 1280x720, 1600x900, and one ultrawide target with keyboard/mouse and controller
- [ ] Validate every new effect with Reduced Flashes on/off and Steady Camera on/off; reject effects that hide threats, objectives, interaction prompts, or escape lanes
- [ ] Measure allocations and frame time under the existing maximum combat population; pool or simplify effects that cause recurring allocations or miss the established performance budget
- [ ] Run repeated death/restart/menu, victory/menu, scene reload, and complete-route soak cycles; scan for leaked objects, duplicate services, stale input, missing references, and non-reset presentation state
- [ ] Complete applicable Unity suites, Windows development build, and packaged smoke after each game-shell integration milestone

### Transition into Geometry Wars-inspired combat proof

- [ ] After the mission route and unattended product-shell queue are implementation-complete, make `P0 — Geometry Wars-inspired combat proof` the controlling workstream
- [ ] Begin with Gate A technical baselines and readability improvements, then Gate B composition/timing work; do not represent automated correctness or captures as human proof of fun
- [ ] Consolidate completed run-history sections into an archive/index when backlog length materially interferes with selecting the next actionable item; preserve decisions and evidence

### Cohesive-mission Definition of Done

- [ ] Every major mission room is entered on the required route and has a distinct legible purpose; decorative pockets are identified rather than padded with fake objectives
- [ ] No required interaction exists solely to add travel time, and no two consecutive rooms repeat the same pickup, switch, or wave verb
- [ ] Required backtracking is limited to one meaningful installation return per act and every return demonstrates a changed station state
- [ ] A first successful human run completes in approximately 20–25 minutes with understandable objectives, manageable Signal pressure, and no filler-room feedback
- [ ] Keyboard/mouse and controller routes, objective guidance, collision/projectile authority, NavMesh, death/restart/re-entry, doors, rewards, combat states, extraction, Windows build, and packaged smoke remain valid
- [ ] Do not resume general map expansion until this definition is met or measured play shows a specific missing spatial function

## P0 — Geometry Wars-inspired combat proof

Work through these gates in order. Authorize one bounded combat advancement per development pass and require matched before/after evidence.

### Gate A — Immediate control and weapon feel

- [ ] Baseline keyboard/mouse and controller movement, aiming, continuous fire, collision, camera framing, projectile cadence, and Signal spend in the eastern combat laboratory and one live-balance route
- [ ] Make aim, shot paths, impacts, purges, incoming danger, and escape lanes readable during dense mixed-role combat at 1280x720 and 1600x900
- [x] Replace the persistent objective route line with a fading screen-edge objective indicator; add capped off-screen specialist indicators and one grouped Swarmer marker
- [ ] Improve the smallest demonstrated weakness in firing, hit, purge, damage, or near-danger feedback without expanding the HUD or weakening Signal commitment
- [ ] Reject the change if it harms input parity, collision authority, readability, performance, or the Signal economy

### Gate B — Mixed-role pressure

- [ ] Compare Warden screening, Sapper target priority, Interceptor displacement, and Suppressor space denial in matched routes
- [ ] Produce at least two readable, non-identical mixed-role situations by tuning telegraphs, timing, approach direction, or combination logic before changing health, damage, speed, or quantity
- [ ] Preserve a reasonable response to every threat and prevent unavoidable close spawns or attritional stat escalation

### Gate C — Weapon transformations

- [ ] Compare Chain Arc, Overdrive Thrusters, Piercing Pulse, Controlled Ricochet, Emergency Capacitor, Feedback Shield, and established weapon evolution before proposing another modifier
- [ ] If weapon progression remains tactically flat, add at most one modifier that changes targeting, positioning, cadence, or routing and does not duplicate an existing choice
- [ ] Define its Signal cost or downside, enemy counter, positional/opportunity cost, scope cost, and rejection criteria before implementation
- [ ] Reject any dominant build, trivialized enemy role, uncontrolled Signal gain, or balance response based on raw enemy-stat inflation

### Gate D — Arena and run pacing

- [ ] Repurpose or tune one existing authored combat room before adding geometry; use timing, approach direction, terrain, cover, power state, optional greed, or return-route reversal
- [ ] Add one behavioral variant using existing assets only if the four current roles cannot create a required movement decision
- [ ] Add a genuinely new enemy only if existing roles cannot supply the required counterplay
- [ ] Add level area or the station guardian only when matched human play shows the existing spaces or roster cannot sustain the intended 15–25-minute escalation and extraction climax

### Evidence-gated combat chamber prototype

- [x] Extend the Arc Furnace north branch where a bounded trial can test the committed free-fire/Swarmer model separately from open-route encounters
- [x] Author Room A as a readable commitment space with an amber security breaker, red threshold warning, and explicit no-return message
- [x] Author Room B as a bounded lockdown arena with full movement, circulation cover, collision-authoritative doors, capped phase populations, and authored spawn directions
- [x] Build a pressure-population teaching phase, pressure-plus-Warden space-management phase, and pressure-plus-Sapper target-priority phase
- [x] Author Room C as a distinct vault whose one-shot capacitor restores up to 20 Signal after the trial
- [x] Make the cleared chamber useful on withdrawal by opening both doors and energizing a cyan return spine through all three rooms
- [x] Reset chamber state deterministically with the run and handle dynamic door collision/NavMesh, phase cleanup, bounded actors, reward collection, and cleared revisits
- [x] Prove the complete three-phase transition, five-threat peak, both door releases, reward availability, persistent 11-spawn/11-purge evidence, full regression, Windows build, and packaged smoke
- [ ] Reject or revise the pattern if matched play shows Signal soft-locks, unreadable saturation, repetitive waves, weak reward value, route coercion, excessive duration, poor return value, or lower fun and replay intent
- [ ] Do not replicate the chamber pattern elsewhere until the single prototype passes focused tests, complete-run regression, Windows smoke, and a human-controlled before/after comparison

### Combat-proof Definition of Done

- [ ] Matched evidence improves weapon satisfaction, movement decisions, role distinction, encounter variety, combat readability, completion pressure, build diversity, fun, and replay intent
- [ ] No build is dominant, no role is trivialized, and Signal remains a meaningful mobility, machinery, special-power, damage, and survival tradeoff while basic fire stays free
- [ ] Focused and applicable full Unity suites pass; the Windows development player builds and smoke-tests when runtime integration changes
- [ ] A human-controlled comparison validates feel, or the DEVLOG states clearly that subjective balance and fun remain unproven and supplies a concise manual script

## P0 — Authored Spine-return tactical window (Run 118)

- [x] Keep `ForegroundOcclusionController` disabled and preserve all nine compatibility bindings/resources
- [x] Identify the actual powered-return foreground offender with a resolution-normalized renderer coverage diagnostic
- [x] Reduce only the North Capacitor Shield presentation height while preserving both object-aligned X/Z collision footprints
- [x] Lower the shield's central tactical-window coverage from 17.3 percent to 9.9 percent
- [x] Prevent the historical Capacitor Spine setup from shrinking later authored world bounds
- [x] Prove powered return state, obstacle count, collision, projectile authority, eastern-lab framing, Full Extraction, full regression, Windows build, and D3D11 packaged smoke
- [ ] Capture matched human-controlled frames at 1280x720 and 1600x900 with a threat, bolt path, and escape lane present
- [ ] Treat P0.1 as incomplete until the opening composition also passes and the human Spine/Quench comparison confirms the lower shield remains visually substantial

## P0 — Authored opening tactical window (Run 119)

- [x] Identify the opening return's actual foreground offenders with the established resolution-normalized diagnostic
- [x] Lower both departure capacitors while preserving their full-length low beacon rails and object-aligned X/Z collision footprints
- [x] Shorten only the raised armor/cell spans so no authored opening renderer covers more than 20 percent of the tactical window
- [x] Reduce north/south armor coverage from 39.3/29.6 percent to 13.0/12.1 percent at 1280x720 and 1600x900
- [x] Prove controller flank traversal, projectile blocking, 123 obstacles, opening route, matched Full Extraction, full regression, Windows build, and null/D3D11 packaged smoke
- [ ] Capture event-timed human frames with the drone, nearest threat, projectile path, and one escape lane simultaneously visible
- [ ] Treat owner P0.1 as provisionally supported, not complete, until both opening and Spine-return silhouettes remain visually substantial in human comparison

## P0 — Matched tactical-window capture presets (Run 122)

- [x] Add development-menu presets for the powered Opening return and powered Spine return
- [x] Add `-deadSignalTacticalWindow=Opening` and `-deadSignalTacticalWindow=SpineReturn` development-player presets
- [x] Stage one Sapper warning and one immediate player bolt on a camera-relative line while preserving full player control
- [x] Keep all other specialists absent, enable debug invulnerability, and leave production combat, geometry, collision, Signal, and foreground presentation unchanged
- [x] Prove player/Sapper safe framing, the 20-percent renderer limit at 1280x720 and 1600x900 after camera settle, and all 135 authored obstacles
- [ ] Capture one human-controlled frame per preset at both resolutions with the drone, Sapper warning, player bolt path, and one escape lane simultaneously visible
- [ ] Keep owner P0.1 provisional until the captured silhouettes remain substantial and readable; then complete the P0.2 Swarmer off/on human pairs

## P0 — Consecutive rendered tactical-window evidence (Run 123)

- [x] Add explicit `-deadSignalTacticalWindowCapture` automation that records two labeled frames after the existing one-second camera settle
- [x] Preserve ordinary screenshot behavior and auto-exit only command-line development-player captures
- [x] Reject hidden-window captures after inspection proved their suppressed backbuffers were black
- [x] Correct Spine-return diagnostic staging from the tower berth to the authored north-return approach without changing production geometry or combat state
- [x] Capture and inspect two visible-player frames per preset at 1280x720 and 1600x900
- [ ] Keep owner P0.1 provisional: have a human move and fire through both presets and judge lane continuity, silhouette substance, threat/bolt readability, and preference
- [ ] Do not advance P0.2 or promote Swarmers until the human P0.1 comparison is recorded

## P0 — Moving tactical-window sweep evidence (Run 124)

- [x] Add explicit `-deadSignalTacticalWindowSweep` automation for a short collision-aware left/right/return movement sample
- [x] Use non-damaging near-target traces so the staged Sapper remains present throughout all four captures
- [x] Reset owned diagnostic projectiles between presets and preserve all 135 authored obstacles
- [x] Record actor viewport positions, real distance travelled, maximum foreground coverage, and a command-line pass/fail marker
- [x] Prove Opening at 18.2/17.5 percent and Spine return at 17.3/17.8 percent maximum coverage at 1280x720/1600x900 while both actors remain safe
- [x] Inspect representative moving frames with the drone, Sapper, trace feedback, machinery, and an escape area present
- [ ] Keep owner P0.1 provisional until a human repeats both presets with keyboard/mouse and controller, pauses mid-warning, and judges threat/trace/lane continuity and silhouette preference
- [ ] Do not advance P0.2 or promote Swarmers until the human P0.1 comparison is recorded

## P0 — Eastern-lab Swarmer pressure tier (Run 117)

- [x] Add one fragile one-bolt Swarmer behavior that continuously converges and punishes stationary contact
- [x] Reuse the lab's authored Warden and Sapper staging lanes for two readable three-Swarmer formations
- [x] Cap the bounded population at six, delay the second trio, and prevent deployment inside the safe-spawn radius
- [x] Keep purge recovery below contact loss and keep the full finite tier at or below one specialist-hit recovery
- [x] Package a purpose-built geometric Swarmer prefab and focused ScriptableObject tuning without bitmap art
- [x] Prove one-bolt purge, collision, reward, contact pressure, viewport containment, density cap, and the unchanged Full Extraction route
- [ ] Compare stationary held fire against continuous strafing at 1280×720 and 1600×900 with keyboard/mouse and controller; record damage, evasions, clear time, minimum/final Signal, readability, fun, and replay intent
- [ ] Promote Swarmers beyond the combat laboratory only if human evidence shows movement pressure improves without obscuring specialist target priority

## P0 — Matched Swarmer off/on control (Run 121)

- [x] Add explicit development-menu presets for the same eastern laboratory with Swarmers on or off
- [x] Add `-deadSignalCombatLab=SwarmersOn` and `-deadSignalCombatLab=SwarmersOff` development-player presets
- [x] Preserve the resolved Chain Arc, Piercing Pulse, and Feedback Shield build, authored anchors, four specialists, attack schedule, invulnerability, and 30-second duration in both modes
- [x] Prove the specialists-only control has zero Swarmer population/contact and peak concurrency four while the unchanged pressure tier has six Swarmers and peak concurrency ten
- [x] Keep combat values, commercial routes, Security Trial composition, geometry, economy, input, visuals, audio, and assets unchanged
- [ ] Run three human pairs per mode at 1280x720 and 1600x900 with keyboard/mouse and controller; score stationary fire, circular strafing, specialist-first targeting, readability, fun, and replay intent
- [ ] Keep P0.2 open and do not promote, retune, or remove Swarmers until the human pairs produce a documented decision

## P0 — Free continuous basic fire (Run 116)

- [x] Permanently remove Signal spend from ordinary, Piercing Pulse, and Controlled Ricochet bolts
- [x] Let keyboard, mouse, trigger, and shoulder Fire repeat at the authored 0.16-second cadence while held
- [x] Keep single presses immediate, release stopping deterministic, and hit-stop buffering intact
- [x] Preserve projectile speed, lifetime, collision, hit budgets, enemy health, enemy roles, finite purge rewards, and all non-firing Signal costs
- [x] Prove held input parity, zero firing spend at critical reserve, cadence, release behavior, and bounded reward constraints
- [ ] Compare keyboard/mouse and controller feel at 1280x720 and 1600x900; reject or add an explicit sustained-fire limiter if stationary spray dominates movement, targeting, or role counterplay

## P0 — Convergence Breaker Gallery (Run 115)

- [x] Add a scene-authored 7-by-8-metre lateral loop equal to 50 percent of the Convergence Chamber floor contract
- [x] Open two independent east thresholds around a reused breaker-bank landmark and angled ceramic firing cover
- [x] Keep the gallery a dead-zone commitment before Spine activation and make it a powered withdrawal foothold afterward
- [x] Add one safe outer reinforcement gate that redirects chamber pressure without raising the four-response cap
- [x] Package an original transparent cyan/amber route decal with a reproducible ArtSource record
- [x] Preserve Signal economy, objectives, salvage, enemy statistics, extraction rules, existing world bounds, and collision authority
- [x] Prove both thresholds, movement/projectile blocking, powered transition, entrance safety, Resources packaging, and complete regression
- [ ] Compare the chamber centerline against both breaker-gallery approaches under matched live threats; record time, Signal, hits, cover use, gate recognition, and powered-return choice

## P0 — Spine Discharge Trench (Run 114)

- [x] Add a scene-authored 60-square-metre south Spine loop, equal to 42.9 percent of the original Capacitor Spine floor contract
- [x] Open two independent thresholds around a central discharge coil and angled ceramic firing cover
- [x] Keep the trench a dead-zone risk before Spine activation and make it a powered return foothold afterward
- [x] Add one safe far-side reinforcement gate and preserve the four-response cap, enemy stats, Signal economy, objectives, and extraction
- [x] Package an original transparent cyan/amber route decal with a reproducible ArtSource record
- [x] Prove both approaches, oriented movement/projectile collision, powered-state transition, scene bounds, Resources packaging, and full regression
- [ ] Compare direct Spine transit against west-trench and east-trench routes under matched live threats; record time, Signal, hits, cover use, and gate recognition

## P0 — Relay gantry payload choice (Run 113)

- [x] Keep one scene-authored Relay payload on the protected inner Foundry route
- [x] Move the sibling Relay payload into the Cooling Gantry so its exchanger and angled cover shape the mission branch
- [x] Spawn exactly two Relay candidates from authored prefab sockets; securing either retires the other and advances to the Spine
- [x] Preserve seven total cache candidates, regional rewards, tower order, four-response cap, enemy stats, Signal economy, and extraction rules
- [x] Preserve both gantry thresholds, object-aligned movement/projectile cover, Relay-powered return state, and existing route decal
- [x] Prove socket ownership, route choice, sibling retirement, Resources packaging, full regression, Windows build, and packaged smoke
- [ ] Compare inner-Foundry and Cooling-Gantry payload routes under matched live threats; record route time, hits, shots, Signal, and cover use

## P0 — Relay Cooling Gantry (Run 112)

- [x] Add a scene-authored two-threshold cooling loop equal to roughly 40–45 percent of the Relay Foundry floor area
- [x] Make the gantry a dead-zone positioning flank before Relay activation and a powered return foothold afterward
- [x] Add an original UV-mapped exchanger, object-aligned movement/projectile cover, and one safe far-side reinforcement gate
- [x] Package an original transparent amber/cyan route decal without changing the existing Foundry art contracts
- [x] Preserve the four-response cap, enemy stats, Signal economy, payloads, extraction rules, and current scene bounds
- [x] Prove both thresholds, collision, powered-state transition, NavMesh routing, resource packaging, and complete-journey regression
- [ ] Compare direct Foundry transit against west-gantry and east-gantry routes under matched live threats

## P0 — Quench countertrace briefing (Run 111)

- [x] Forecast the weapon-specific extraction countertrace while the optional Quench cache can still be abandoned
- [x] Repeat the Quench profile at extraction-link commitment and replace the generic pursuit advice with its actionable exit response
- [x] Preserve Required withdrawal, Suppressor rules, Signal economy, mission geometry, scene assets, and the existing HUD hierarchy
- [x] Prove Piercing and Ricochet forecasts, Ricochet active-pursuit advice, and full Unity regression
- [ ] Compare Required withdrawal, Piercing greed, and Ricochet greed with the briefing visible; record recognition before cache commitment and chosen field exit

## P0 — Quench weapon countertrace (Run 110)

- [x] Preserve the established required-withdrawal extraction response
- [x] Make optional Quench greed select a bounded extraction suppression profile from the evolved Relay weapon
- [x] Offset the Piercing response across the return lane so one flank is contested without sealing the route
- [x] Flush the Ricochet response at the player's current cover while preserving the full warning and open ring exits
- [x] Reuse the existing Suppressor, safe authored gates, response slot, telegraph, field radius, timing, and penalties
- [x] Preserve enemy count, health, speed, damage, Signal economy, extraction modes, authored collision, and NavMesh
- [x] Prove deterministic profile selection, both complete commercial journeys, and full Unity regression
- [ ] Compare Required withdrawal, Piercing greed, and Ricochet greed extraction under matched live threats; record field recognition, chosen exit, hits, Signal, and final reserve

## P0 — Wide foreground shell cutaways (Run 109)

- [x] Capture a final-source Required Extraction route at 1600×900 after the explicit sibling-binding pass
- [x] Preserve direct-cover and bounded tactical-window behavior for existing foreground cutaways
- [x] Cut away only collision-authoritative faces that are nearer than the drone and occupy at least ten percent of the clipped frame
- [x] Replace wide faces with an original sparse transparent-center cyan/amber boundary cue
- [x] Keep movement, projectile, NavMesh, objective, entrance, powered-territory, threat, Signal, and journey rules unchanged
- [x] Prove the wide-face classification, resource packaging, full regression, Windows build, and final Required-versus-Full rendered comparison
- [ ] Run the watched human Required-versus-Full comparison before beginning the station guardian

## P0 — Explicit wall-shell cutaway ownership (Run 108)

- [x] Add scene-authored renderer ownership for nine non-obstacle wall/bulkhead presentation meshes
- [x] Keep existing collider, oriented-blocker, projectile, NavMesh, objective, entrance, and powered-territory authority unchanged
- [x] Give explicitly bound shells a distinct transparent-center cyan/amber collision footprint without replacing the existing obstacle cue
- [x] Restore explicitly bound renderers on reconfiguration, disable, and teardown through the existing symmetric lifecycle
- [x] Package the new texture/material and require all nine bindings in standalone smoke validation
- [x] Preserve all 96 authored obstacles and both complete commercial journeys
- [ ] Capture Central, Spine, and extraction native frames to verify which formerly opaque sibling faces now cut away
- [ ] Run the watched Required-versus-Full human comparison after the visual check; keep the guardian gated until then

## P0 — Collision-preserving foreground cutaways (Run 107)

- [x] Expand the foreground cutaway from direct drone overlap to a resolution-scaled tactical window around the player
- [x] Limit the broader rule to obstacle-owned wall-like or large projected faces so compact landmarks retain their authored presentation
- [x] Preserve movement, projectile, and NavMesh authority while obstacle renderers are hidden
- [x] Show an original collider-free cyan/amber footprint cue for every active cutaway
- [x] Restore renderers and disable/destroy footprint cues on reconfiguration, disable, and teardown
- [x] Package the generated texture/material and prove 96 obstacles plus both commercial journeys
- [x] Capture native 1600×900 Spine evidence showing obstacle-owned opaque faces replaced by readable footprint cues
- [x] Add explicit authored cutaway ownership to the remaining non-obstacle Central/Spine/extraction wall-face meshes
- [ ] Run the watched Required-versus-Full human comparison after those sibling presentation meshes are corrected

## P0 — Authored station underdeck continuity (Run 106)

- [x] Replace exposed ground-level camera void with a scene-authored, presentation-only station underdeck
- [x] Create and retain an original low-contrast dark-alloy texture through the built-in image-generation workflow
- [x] Cover every arena edge plus a 15-metre camera-correction margin without adding collision, NavMesh, objectives, or traversable space
- [x] Restore previously hidden wall renderers whenever the foreground-occlusion controller is reconfigured or disabled
- [x] Package and smoke-test the texture, material, prefab, and scene instance
- [x] Capture native 1600×900 route frames and distinguish underdeck continuity from unresolved vertical wall-face obstruction
- [ ] Finish the remaining non-obstacle black/gray authored wall faces in Central, Spine, and extraction views
- [ ] Run the watched Required-versus-Full human comparison only after the wall-face pass preserves combat and route readability

## P0 — Readable Suppressor denial field (Run 105)

- [x] Replace the opaque full-radius field primitive with an amber warning boundary and a transparent-center active edge
- [x] Preserve the established field radius, duration, cooldown, movement penalty, Signal drain, safe entrances, and collision rules
- [x] Package the original active-edge texture and validate the warning-to-active transition in the extraction pursuit
- [x] Pin commercial-journey tests to one matched route variant without leaking or overwriting the user's saved route preference
- [ ] Capture the brief active phase directly in a foreground combat-lab session; batchmode cannot invoke frame-end screenshot capture
- [ ] Continue the P0 combat-readability pass on the large gray/dark foreground faces visible in Spine/eastern routes
- [ ] Rebuild the compressed terminal debrief after active-play visibility and camera-boundary blockers are corrected

## P0 — Matched live-balance combat routes (Run 104)

- [x] Give assisted live routes conservative target selection, predictive Sapper aim, obstacle-aware intercept positions, and explicit Interceptor/Suppressor evasion
- [x] Let a distant active Sapper force a temporary route abandonment, then resume the authored objective after entering a reliable bolt lane
- [x] Route both required and optional withdrawals through the powered Spine, Relay, and Central footholds before extraction
- [x] Match both live journeys on Overdrive Thrusters, Emergency Capacitor, Piercing Pulse, and the same authored payload route
- [x] Record policy-directed shots and discrete evasion responses alongside existing combat, Signal, timing, exposure, and journey evidence
- [x] Prove Required Extraction and Full Extraction victories under real Signal, movement, enemy, and damage rules
- [x] Preserve SafeNavigation behavior, production combat/economy tuning, authored collision, entrances, response caps, and mission rules
- [ ] Run the same matched pair as a watched human comparison before authoring the station guardian

## P0 — Eastern room combat laboratory (Run 103)

- [x] Replace the arena-edge teleport with a scene-authored Arc Furnace player anchor, camera focus, four role staging points, and safe framing envelope
- [x] Preset Central, Relay, Spine, two resolved temporary choices, Piercing Pulse, full Signal, and an explicit combat-lab objective
- [x] Reset projectiles, role health, attack timers, drain/dash/suppression state, feedback objectives, and debug invulnerability on scenario transitions
- [x] Keep Warden, Sapper, Interceptor, and Suppressor behavior active while a visible debug shield prevents the 30-second readability lab from ending early
- [x] Report scenario time, Signal, active threats, completed role attacks, and 15–85 percent viewport status
- [x] Add an original transparent combat-target floor insignia and enforce its prefab/resource packaging
- [x] Prove five consecutive loads, complete attack coverage, 30-second survival, viewport containment, full regression, Windows build, and packaged smoke
- [ ] Capture and inspect one rendered 1600x900 frame per role telegraph plus one mixed-combat frame in an interactive Editor session

## P0 — Departure capacitor surge (Run 102)

- [x] Turn the released direct cargo lane into a one-shot 12-Signal return resource
- [x] Trigger only on an outer-to-inner direct centerline crossing after extraction readiness
- [x] Keep both capacitor flanks traversable without consuming the reserve
- [x] Add an original text-free surge decal and hide it after discharge
- [x] Record actual restored Signal in the existing run evidence
- [x] Correct the packaged smoke cache-count contract for the seven regional/optional candidates
- [x] Prove collision, route choice, one-shot recovery, resource packaging, full regression, and Windows smoke
- [ ] Compare direct-surge and flank returns under the same live threats; record time, hits, drains, and extraction reserve

## P0 — Departure cargo-release shortcut (Run 101)

- [x] Add a scene-authored shutter between the extraction departure-channel capacitor banks
- [x] Force the outward journey onto two independently traversable capacitor flanks
- [x] Keep the closed shutter movement- and projectile-authoritative
- [x] Retract the shutter only when all three required regional payloads make extraction ready
- [x] Reveal an original text-free amber/cyan return decal when the direct uplink route opens
- [x] Preserve regional rewards, tower costs, enemy stats, response budget, and extraction modes
- [x] Prove both flanks, closed/open traversal, projectile blocking, Resources packaging, complete journey routing, and full Unity regression
- [ ] Compare north-out/south-back, south-out/north-back, and direct-ready returns for recognition, time, hits, Signal, and uplink reserve

## P0 — Victory-finalized live journey evidence (Run 100)

- [x] Defer successful Required Extraction and Full Extraction report persistence until the run reaches a terminal outcome
- [x] Keep failed routes and completed non-extraction routes writing immediately for useful recovery evidence
- [x] Refresh report summaries on repeated reads without duplicating or freezing metrics at uplink start
- [x] Record the terminal outcome plus post-uplink elapsed time, combat, Signal, journey, shortcut, and final-position evidence
- [x] Prove required and optional extraction reports through victory plus focused and complete Unity regression suites
- [x] Run both assisted live-balance journeys and preserve their comparable reports; keep the human guardian gate explicit

## P0 — Required three-region mission journey

- [x] Replace extraction readiness based on any three caches with one payload from Central, Relay, and Spine
- [x] Require the Central payload before Relay activation and the Relay payload before Spine activation
- [x] Require all three towers and all three regional payloads before extraction can begin
- [x] Offer two payload routes per region and retire the unchosen sibling after one regional payload is secured
- [x] Preserve the authored Arc Furnace/Quench cache as the distinct optional Signal greed reward
- [x] Route objective beacons, mission guidance, tactical-map labels, and locked ability milestones through seven mission stages
- [x] Preserve Central threat awakening, Relay lockdown, Spine weapon evolution, and the bounded extraction pursuit
- [x] Prove the complete required journey, optional cache, NavMesh traversal, combat regression, and extraction contract in Unity
- [ ] Compare the two payload choices in each region and record route time, damage, Signal, upgrade use, and final reserve

## P0 — Matched journey evaluation (Run 99)

- [x] Add a required-only extraction route that matches the full three-region progression without committing to Quench
- [x] Preserve a named report through victory for both required withdrawal and optional-greed routes
- [x] Record elapsed time, dead-zone exposure, hits, Sapper drains, shots, purges, Signal spend/recovery, and final reserve
- [x] Make the outcome debrief identify required withdrawal, shortcut use, or optional greed
- [x] Add an original route-ledger debrief insignia with validated Unity import settings
- [ ] Run both live-balance routes as a watched human comparison before authoring the station guardian

## P0 — Commercial full-journey gate (Run 98)

- [x] Make Full Extraction traverse Central, Relay, weapon calibration, and Spine progression in order
- [x] Secure the three required caches before deliberately committing to the optional Quench cache
- [x] Return to extraction with all three towers online and the Relay weapon evolved
- [x] Add a strict PlayMode contract for tower state, weapon choice, required salvage, optional salvage, and extraction
- [x] Prove the expanded route against runtime NavMesh and the complete EditMode/PlayMode regression suites
- [ ] Compare a required-only withdrawal against optional-cache Furnace/Quench routes with live threats; record time, hits, Signal, weapon use, shortcut use, and final reserve
- [ ] Author the station guardian only after the matched human comparison confirms the expanded run is readable, completable, and tactically varied

## P0 — Shared runtime NavMesh routing

- [x] Build one runtime NavMesh from the scene-authored arena bounds and authoritative oriented movement blockers
- [x] Rebuild the mesh when the central, Relay, Spine, or Quench return gates open
- [x] Route the playtest sequencer and Suppressor, Interceptor, Warden, and Sapper through complete multi-corner paths
- [x] Preserve authoritative movement collision and the previous local-detour planner as a safe fallback
- [x] Expose NavMesh availability, query status, and remaining route corners in debug telemetry
- [x] Prove a complete Full Extraction automation route and enemy traversal around authored obstacles

## P0 — Quench cache-release shortcut (Run 96)

- [x] Add a scene-authored pressure shutter that forces the outbound Quench route onto its exposed east edge
- [x] Retract the shutter only after the optional Arc Furnace cache is secured
- [x] Reveal an original cyan/amber return decal when the direct cut-through opens
- [x] Keep movement and projectile collision authoritative before release and remove both after release
- [x] Preserve cache reward, required salvage, six safe gates, four-response cap, combat stats, and Signal economy
- [x] Prove closed/open traversal, resource packaging, complete regression, Windows build, and packaged smoke
- [ ] Compare cache abandonment against Furnace-out/Quench-back and Quench-out/Furnace-back; record route time, hits, Signal, and extraction reserve

## P0 — Quench Loop return flank (Run 95)

- [x] Open two independently traversable thresholds in the Arc Furnace east wall
- [x] Add a compact scene-authored loop that changes optional-cache outbound and return routing
- [x] Add two rotated projectile-authoritative deflectors and an original UV-mapped condenser landmark
- [x] Keep the loop dead-zone outbound and power it with the Spine tower as a cyan return foothold
- [x] Preserve one optional cache, six safe gates, the four-response cap, enemy stats, objectives, and Signal economy
- [x] Expand scene-authored movement, camera, and tactical-map bounds and package one original transparent route decal
- [x] Prove both thresholds, loop traversal, cover, power transition, Resources packaging, and complete regression
- [ ] Compare Furnace-out/Quench-back, Quench-out/Furnace-back, and cache abandonment; record time, hits, Signal, and extraction reserve

## P0 — Arc Furnace greed crossing (Run 94)

- [x] Extend the Convergence Chamber through two independently traversable authored thresholds
- [x] Add a 14-by-9-metre room whose shielded switchback and exposed lane create distinct combat positioning
- [x] Relocate the optional fourth cache from the Spine into the deeper route without changing its reward or requirement
- [x] Add a UV-mapped furnace landmark that blocks movement and projectiles plus one safe far-side security gate
- [x] Keep the room dead-zone outbound and power it with the Spine tower for the return journey
- [x] Expand scene-authored movement, camera, and tactical-map bounds without changing combat stats, response cap, or Signal economy
- [x] Package an original transparent route decal and prove both thresholds, collision, power, packaging, and full regression
- [ ] Compare shielded-west and exposed-east cache raids against abandoning the optional cache; record route time, hits, Signal, and extraction reserve

## P0 — Flux Bypass return flank (Run 93)

- [x] Link the Induction Gallery and Convergence Chamber through a separate scene-authored west-side loop
- [x] Open two independently traversable thresholds so the bypass changes outward and return routing
- [x] Add angled projectile-authoritative cover and a readable modular flux landmark without adding empty floor area
- [x] Keep the bypass dead-zone outbound and power it with the Spine tower as a return foothold
- [x] Preserve five safe reinforcement entrances, the four-response cap, enemy stats, rewards, objectives, and Signal economy
- [x] Preserve the existing scene-authored bounds and package one original text-free route decal
- [x] Prove both thresholds, object-aligned cover, power transition, packaging, and complete-run regression
- [ ] Compare bypass-out/chamber-back, chamber-out/bypass-back, and direct Gallery/Spine transit; record time, Signal, cover use, and extraction reserve

## P0 — Convergence Chamber deep-route pressure (Run 92)

- [x] Extend the Induction Gallery through two independently traversable authored doorways
- [x] Add a separate 14-by-8-metre chamber with a purpose-built UV-mapped busbar landmark and rotated cover
- [x] Add one far-side authored security entrance that serves deep-route pressure without increasing the response cap
- [x] Preserve the six-metre safe-entry exclusion so pressure waits while the player occupies the chamber
- [x] Keep the chamber dead-zone outbound and power it with the Spine tower for the return journey
- [x] Make movement clamps and the tactical map consume the scene-authored bounds instead of stale code constants
- [x] Package an original transparent convergence/return decal and preserve all enemy stats, economy, and objectives
- [x] Prove both doorways, projectile cover, entrance direction, safety distance, power state, packaging, and full regression
- [ ] Compare direct Gallery transit against both chamber loops; record route time, gate warning, cover use, Signal, and extraction reserve

## P0 — Spine Induction Gallery (Run 91)

- [x] Add a separate scene-authored modular gallery beyond the Spine's protected north lane
- [x] Open two independently traversable doorways so the gallery forms an outer loop rather than a dead end
- [x] Use an induction-coil landmark and two rotated baffles for readable, projectile-authoritative cover
- [x] Keep the gallery in the dead zone on the outward journey, then power it with the third tower as a return foothold
- [x] Preserve all tower/cache/security/economy rules and the four established safe reinforcement entrances
- [x] Expand movement, camera, and tactical-map bounds and package an original text-free route/power decal
- [x] Prove both doorways, oriented cover, power-state transition, Resources packaging, and complete regression
- [ ] Compare gallery outbound/direct-center return, north outbound/gallery return, and south-cache/direct return; record time, Signal, cover use, and extraction reserve

## P0 — Capacitor Spine discharge return (Run 90)

- [x] Keep the transfer bank collision- and projectile-authoritative during the outward journey
- [x] Retract the complete nested transfer-bank assembly when the third tower comes online
- [x] Open one direct central return while preserving the protected north and exposed south approaches
- [x] Add an original text-free floor cue showing two amber approaches converging into one cyan return
- [x] Preserve the 42 authored obstacles, four entrances, cache economy, tower transaction, weapon evolution, and threat budget
- [x] Prove closed/open movement, projectile blocking, Resources packaging, full regression, and the Windows player
- [ ] Compare north retrace, south-cache retrace, and direct discharge return; record route time, live roles, Signal, and extraction reserve

## P0 — Capacitor Spine third tower (Run 89)

- [x] Convert the dormant scene-authored berth into a Relay-gated third Signal tower transaction
- [x] Spend 18 Signal while preserving one, restore 34, and power a distinct 6.2-metre Spine foothold
- [x] Evolve the chosen Relay calibration: three-target Piercing Pulse or two-bank Controlled Ricochet
- [x] Add authored dormant/active routing and an original text-free activation decal without changing collision
- [x] Preserve four caches, three required salvage, four safe entrances, the four-response cap, and all enemy stats
- [x] Prove both Spine approaches, projectile blocking, activation, powered territory, weapon evolution, and packaging
- [ ] Compare north-return, south-cache-return, and tower-abandonment runs; record activation reserve and evolved-weapon use

## P0 — Capacitor Spine expedition (Run 88)

- [x] Extend the Relay Foundry through two scene-authored east approaches without removing its safe reinforcement pair
- [x] Add a compact modular Capacitor Spine with a central projectile-blocking landmark, protected north lane, and exposed south lane
- [x] Relocate the one optional greed cache from the transit vault to the far end of the new route without changing its reward or extraction requirement
- [x] Place a dormant third-tower berth that establishes the next region goal without presenting a false interaction
- [x] Expand movement, camera, and tactical-map bounds while preserving enemy stats, response cap, Signal drains, and required salvage count
- [x] Integrate an original text-free route decal and reuse the purpose-built UV-mapped capacitor art pipeline
- [ ] Compare protected north, exposed south, and cache-abandonment returns; record route time, damage, Signal, and extraction reserve

## P0 — AutoUI feature laboratory

- [x] Add an Editor/development-build-only AutoUI debug menu opened with F5
- [x] Pause by default, block gameplay input, and allow an explicit live-simulation mode
- [x] Add live overview and composition telemetry for run, threats, upgrades, extraction, authored assets, and runtime objects
- [x] Add invariant-preserving controls for Signal, teleporting, towers, shortcut, salvage, threats, upgrades, and extraction
- [x] Add curated one-click scenarios for the major playable phases and edge cases
- [x] Add presentation and accessibility controls for VFX, comfort modes, contrast, and audio
- [x] Prove AutoUI resource packaging, runtime bootstrap, command integration, and non-development-player exclusion
- [x] Interactively inspect every page at 16:9, split the laboratory into six reachable tabs, and tune panel widths and labels from capture evidence
- [x] Give AutoUI exclusive paused-presentation ownership so the normal HUD and pause overlay cannot obscure or intercept debug controls
- [x] Add Escape close, LB+Menu controller access, command confirmations, an opaque debug backdrop, and a resolution-scaled tab strip
- [x] Prove every generated panel remains inside a 1280×720 viewport and preserve vertical scrolling for nested capture/settings tools
- [ ] Verify directional controller selection on physical hardware and add explicit selected-control styling if AutoUI's default event-system highlight is insufficient
- [ ] Add each future gameplay feature's prerequisite, trigger, edge case, telemetry, reset, and scenario controls as part of its definition of done

## P0 — Relay lockdown security composition (Run 87)

- [x] Promote the existing final Suppressor response when the optional Relay tower comes online
- [x] Consume that bounded response once so extraction cannot add a fifth deployment
- [x] Commit the lockdown to one of the Relay Foundry's safe authored entrances and preserve its full warning
- [x] Lock an avoidable suppression sweep to the Relay activation position without changing enemy stats or field tuning
- [x] Mark both Foundry reinforcement gates with an original text-free amber/red lockdown decal
- [x] Prove one-tower versus two-tower response order, safe deployment, packaging, and complete-run regression
- [ ] Compare Relay-first greed against a direct three-cache return; record live roles, sweep escape, purges, Signal, and extraction pressure

## P0 — Relay Foundry second-region slice (Run 85)

- [x] Add a scene-authored Relay Foundry region with protected north and south turbine approaches through the east vault
- [x] Place an original turbine landmark, second Signal tower, modular deck, readable boundaries, and oriented collision
- [x] Require the first tower before activating the foundry tower; create a second powered foothold without changing base drains
- [x] Retract a scene-authored return bulkhead on activation so the journey home gains a meaningful shortcut
- [x] Keep reinforcement entrances safe and preserve existing enemy stats, rewards, role caps, and telegraphs
- [x] Prove activation order, powered-territory routing, collision/projectile blocking, camera bounds, and complete-run regression
- [ ] Manually compare the protected lane, exposed bypass, and unlocked return shortcut at 16:9 and ultrawide

## P0 — Relay weapon calibration (Run 86)

- [x] Award one independent weapon choice when the Relay tower comes online, without discarding unresolved cache choices
- [x] Add Piercing Pulse: one free basic bolt can strike two different aligned threats but never pass authored cover
- [x] Add Controlled Ricochet: one authored-cover impact can redirect toward one nearby unobstructed threat
- [x] Place an original text-free cyan/amber calibration decal beside the scene-authored Relay tower
- [x] Preserve enemy health, damage, movement, rewards, role order, response cap, and Signal drains; the former shot-cost contract is superseded by permanent free basic fire
- [x] Prove input consumption, two-target piercing, one-bounce termination, Resources packaging, and full-run regression
- [ ] Compare both calibrations on the north approach, south approach, and opened return; record hits per shot, cover rebounds, Signal spent, damage, and extraction reserve

- [x] Migrate the fixed world envelope, landmarks, camera/light rig, and persistent actors from `DeadSignalWorld` into `SampleScene` behind a validated scene-reference contract
- [x] Persist the world palette as authored material assets, serialize fixed renderer assignments in `SampleScene`, and remove obsolete material/build references from `DeadSignalWorld`

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

### Development playtest harness

- [x] Extend the existing AutoUI F5 menu instead of adding a competing debug overlay
- [x] Add serialized-landmark teleports and deterministic scenario presets
- [x] Add Signal, salvage, upgrade, tower, shortcut, invulnerability, and infinite-Signal controls
- [x] Add spawn, purge, damage, reposition, freeze, and forced-attack threat controls
- [x] Add pause, quarter-speed, half-speed, normal, double-speed, and one-frame stepping
- [x] Add free camera, room overview, blocker/entry visualization, and boundary visits
- [x] Add a deterministic named-destination route driver
- [x] Add screenshot, combat-frame capture, and validation shortcuts
- [x] Add live telemetry, focus/input status, recent event history, route seed, and replay copy
- [x] Add ordered route presets and exact-position recording for reusable A → B → C → D playtests
- [x] Add destination-specific arrival contracts, proportional braking, locked detour anchors, and overshoot correction
- [x] Add progress watchdogs, bounded recovery attempts, retry/skip/abort controls, and blocked-route diagnostics
- [x] Add verified arrival actions, route assertions, step-by-step mode, per-step captures, and structured reports
- [x] Add assisted-playthrough and deterministic-validation modes with live, safe-navigation, and combat profiles
- [x] Keep the orange development watermark and exclude the harness from non-development players
- [ ] Record one complete automated route through every authored region and add destinations where route telemetry finds gaps

### Combat VFX and foreground readability

- [x] Add a directional muzzle burst, short-lived muzzle light, and visible drone recoil
- [x] Increase projectile-core presence and author a brighter tapered trail
- [x] Differentiate metallic environment impacts, ordinary threat hits, shield absorption, and decisive purges
- [x] Add threat hit punch, stronger purge rupture, and accessibility-aware effect intensity
- [x] Add a dash afterimage ribbon and particle wake
- [x] Cut away tall authored foreground blockers when they cover the player
- [x] Move supplemental Signal economy copy below the authored upper-left status panel
- [ ] Compare reduced-flash and full-effect combat in the eastern room and tune particle counts from capture footage

### Run 84 committed reinforcement-gate ideas

- [x] Lock an announced Interceptor or Suppressor response to the safest authored flank gate when its warning begins
- [x] Mark the committed entrance with a visible amber world-space warning throughout the countdown
- [x] Turn the same marker red while the player blocks its six-metre safe-entry exclusion
- [x] Deploy from the announced gate even if later movement makes the other entrance safer
- [x] Preserve warning timing, role order, enemy balance, Signal rewards, and the four-response cap
- [ ] Playtest immediate retreat, arena crossing, and deliberate gate blocking for marker recognition and route commitment

### Run 83 persistent reinforcement-entry ideas

- [x] Preserve a started reinforcement warning when the player crosses into its authored safe-entry exclusion
- [x] Pause the remaining entry countdown while the gate is unsafe and resume it when the player clears the gate
- [x] Keep unannounced responses dormant until a safe entrance exists and never deploy an enemy beside the player
- [x] Disclose the held response as `ENTRY BLOCKED — CLEAR GATE` without changing enemy stats, entrances, or budgets
- [x] Prove warning persistence, countdown freeze, safe resume, and live runtime behavior in Unity tests
- [ ] Playtest repeated gate feints versus an immediate retreat for route commitment, warning recognition, and mixed-role pressure

### Run 82 persistent dead-zone trace ideas

- [x] Preserve partial security trace across brief returns to powered territory instead of erasing route pressure instantly
- [x] Cool trace at a designer-tuned 0.5 seconds per powered second so sustained regrouping still clears it
- [x] Distinguish active buildup from powered-territory cooling in the live threat strip
- [x] Preserve the eight-second dispatch threshold, first-response budget slot, safe entry, warning delay, and enemy balance
- [ ] Playtest shallow boundary weaving versus a four-second powered regroup for route commitment and Interceptor timing

### Player clarity and recovery pass

- [x] Keep the next tower, cache, or extraction target visible with distance and screen-edge guidance
- [x] Show live Signal drain rate and distinguish safe, exposure, and movement sources
- [x] Replace immediate zero-Signal failure with a five-second recoverable emergency link
- [x] Disclose primary and auxiliary unlock requirements before each cache milestone
- [x] Add a Signal-costed dash with a visible cooldown for route and telegraph counterplay
- [x] Mark nearby threat bounties and retain the projected aim guide and hit feedback
- [x] Reserve amber for objectives, cyan for safe recovery, and reduce the low-Signal vignette obstruction
- [x] Add exact travel/fire/recovery accounting and one targeted next-run coaching line to the debrief
- [ ] Run three matched full playthroughs and tune dash cost, recovery duration, and marker placement from completion data

### Route readability and eastern-room camera follow-up

- [x] Route objective and emergency lines through oriented-obstacle detour waypoints
- [x] Disclose corridor turns, blocked movement, and refunded blocked dashes
- [x] Add a tactical pause map, persistent player marker, and short-lived Signal event stack
- [x] Let zero-Signal tower activation rescue the run and make emergency dashes free
- [x] Grant every required cache recovery plus a temporary local safe field
- [x] Delay early Sapper pressure and let successful hits interrupt its pulse countdown
- [x] Reduce emergency visual competition and fit the expanded debrief copy
- [x] Keep the drone visible at the far side of the authored eastern room
- [ ] Complete three matched eastern-room routes and tune waypoint clearance, recovery reward, and camera offset from play data

### Run 81 Interceptor crash-recovery ideas

- [x] End a committed Interceptor dash immediately when it hits authored cover or a closed route blocker
- [x] Expose a designer-tuned 1.5-second crash recovery versus a 0.7-second clean-miss recovery
- [x] Keep health, approach speed, dash speed, damage, response count, bounty, entrances, and charge warning unchanged
- [x] Surface the recovery as an explicit counterattack window and block immediate follow-up locks
- [x] Prove duration selection, authored-bulkhead collision, dash termination, live recovery, and relock prevention in Unity tests
- [ ] Playtest open-floor dodge, cover bait, and failed bait for recognition, shots landed, hits taken, route choice, and Signal reserve

### Run 80 Sapper-Interceptor flank-cut ideas

- [x] While a Sapper is latched, route a surviving Interceptor to the nearer of two perpendicular flank points
- [x] Keep the flank offset bounded at 3.6 metres and restore ordinary retreat interception inside a 2.25-metre breach
- [x] Preserve the mirrored flank as open counterplay and retain Suppressor-exit coordination as the extraction priority
- [x] Surface the combined role through one transition callout and live threat status without changing enemy stats, count, or response budget
- [x] Prove side selection, perpendicular geometry, open counterplay, live deployment, and breach release in Unity tests
- [ ] Playtest near-flank, far-flank, and Interceptor-first responses for recognition, route switching, dashes, pulses, shots, and Signal reserve

### Run 79 Warden-Sapper screen ideas

- [x] While a Sapper is latched, route a surviving Warden to a designer-tuned point on the player's direct approach
- [x] Keep the screen offset bounded at 2.8 metres and restore normal pursuit inside a two-metre guard break
- [x] Surface the combined role through one transition callout and live threat status without changing enemy stats or count
- [x] Preserve perpendicular flanks, cover collision, projectile rules, Signal bounties, and adaptive reinforcements
- [x] Prove direct-line interception, open flank geometry, guard-break transition, and live runtime behavior in Unity tests
- [ ] Playtest direct, perpendicular, and Warden-first responses for route change, hits, pulses, shots, and Signal reserve

### Run 78 optional-cache-greed ideas

- [x] Keep the fourth authored cache active after the three-cache extraction requirement is met
- [x] Pay a designer-tuned 18 Signal once when the player raids the remaining optional cache
- [x] Surface the live optional-cache distance and exact reward beside the ready extraction route
- [x] Preserve three required salvage, two overclock choices, three salvage alert tiers, four bounded responses, and both extraction profiles
- [x] Record actual cap-safe optional recovery in the existing salvage economy report
- [x] Prove precondition, one-time payout, live HUD disclosure, collection, and unchanged alert cap in Unity tests
- [ ] Playtest immediate extraction versus optional-cache greed for route abandonment, damage, Signal gained/spent, and uplink reserve

### Run 77 mode-reactive-extraction-suppression ideas

- [x] Keep Stable's promoted Suppressor opening sweep locked to the drone's deployment-time position
- [x] Lead Overdrive's opening sweep 3.5 metres along the dock-to-drone retreat line so holding course is unsafe
- [x] Clamp the predictive ring inside the arena while preserving the same radius, one-second warning, and safe authored entry
- [x] Reveal centered versus predictive sweep counterplay before the player commits to an uplink mode
- [x] Preserve enemy count, health, damage, Signal economy, response budget, extraction durations, and purge credits
- [x] Prove both sweep profiles deterministically and through their live extraction routes
- [ ] Playtest straight-line and feinted Overdrive returns against Stable fight routes for warning recognition and mode dominance

### Run 76 combat-reactive-reinforcement ideas

- [x] Let an avoidance route provoke the established first-cache Interceptor cutoff when both opening core roles survive
- [x] Let an early Warden or Sapper purge provoke that missing role's replacement first and delay the Interceptor to cache two
- [x] Preserve one bounded Interceptor, Warden, and Sapper response across three caches regardless of purge order
- [x] Preserve role uniqueness, authored entrances, six-metre safe entry, 2.5-second warnings, enemy stats, and Signal rewards
- [x] Keep the promoted extraction Suppressor as the fourth and final bounded response
- [x] Prove avoidance, single-purge, double-purge, dead-zone-trace, and complete runtime routes in Unity tests
- [ ] Playtest avoidance and early-purge routes for response recognition, mixed-role pressure, abandoned rewards, and extraction reserve

### Run 75 overclock-pair-synergy ideas

- [x] Give Chain Arc plus Emergency Capacitor one primed double jump when the low-reserve refill fires
- [x] Give Chain Arc plus Feedback Shield the same one-shot double jump after a shielded impact or pulse
- [x] Give Overdrive plus either auxiliary a designer-tuned two-second, 1.2× escape surge when that auxiliary triggers
- [x] Keep all four triggers tied to the established Signal threshold, shield hit, and purge-recharge rules without adding drops or choices
- [x] Show each pair and its ready or timed state in the live salvage strip
- [x] Prove all four pair rules deterministically and prove a fight pair plus flight pair through their real runtime triggers
- [ ] Playtest all four builds for trigger recognition, double-jump setup, escape value, Signal reserve, and dominant pairings

### Run 74 extraction-combat-profile ideas

- [x] Give Stable a designer-tuned 0.9-second link advance per purge so its longer exposure supports a deliberate fight route
- [x] Limit Overdrive purges to 0.25-second advances so its 12-Signal price primarily buys a shorter evasion route
- [x] Show both exact purge credits beside duration and price before the player commits at the dock
- [x] Preserve enemy counts, stats, warnings, entrances, Signal bounties, shield recharge, and the four-response budget
- [x] Prove mode-specific credit selection, active-only rewards, remaining-time caps, and both runtime input routes
- [ ] Playtest matched Stable-combat and Overdrive-flight returns for dominance, shots, hits, purges, finish time, and final Signal

### Run 73 extraction-link-mode ideas

- [x] Offer a free six-second stable link and a designer-tuned 12-Signal 4.75-second overdrive at the extraction dock
- [x] Use existing Use and Fire routes while consuming the choice input so overdrive cannot also launch a Signal bolt
- [x] Preserve one positive Signal when the fast link is unaffordable and keep the stable route immediately available
- [x] Trigger the same promoted Suppressor, bounded response budget, movement, combat, bounty, shield, and purge-time rewards in either mode
- [x] Keep the faster duration longer than the safe-entry plus ring warning sequence so it cannot erase the final maneuver
- [x] Prove stable and overdrive selection, affordability, duration, Signal cost, input consumption, and response preservation in Unity tests
- [ ] Playtest both link modes across high- and low-reserve returns for choice clarity, Signal value, field exposure, and dominance

### Run 72 coordinated-suppression-intercept ideas

- [x] During a live Suppressor warning, route a surviving Interceptor toward the player's most obvious ring exit
- [x] Lock the existing readable dash across that predicted exit while preserving alternate escape angles
- [x] Fall back to the established extraction-route cutoff whenever no Suppressor warning or field is present
- [x] Preserve enemy counts, health, speeds, damage, Signal rewards, warning durations, and the four-response budget
- [x] Prove the coordinated target deterministically and in the complete extraction flow

### Run 71 combat-assisted-extraction ideas

- [x] Reward each threat purged during the active uplink with a designer-tuned 0.75-second link advance
- [x] Keep pre-uplink purges from banking extraction progress and cap the credit at the actual remaining link time
- [x] Preserve each role's Signal bounty and Feedback Shield recharge alongside the new combat reward
- [x] Keep fleeing viable through the unchanged six-second timer while making an extraction fight shorten exposure
- [x] Prove a Suppressor purge advances but does not bypass the remaining holdout in the complete runtime flow
- [ ] Playtest fight and flight extractions for firing commitment, time saved, live threat mix, and final Signal reserve

### Run 70 locked-extraction-sweep ideas

- [x] Keep the promoted Suppressor's full 2.5-second entry warning and farther authored safe gate
- [x] Lock its first denial ring to the drone's deployment-time position instead of waiting for a cross-arena approach
- [x] Preserve the existing one-second amber telegraph before the finite magenta field activates
- [x] Leave the six-second mobile uplink, field penalties, enemy health, bounty, and four-response budget unchanged
- [x] Prove the player has time to leave the locked ring before extraction and that later fields resume normal Suppressor positioning
- [ ] Playtest whether the remote lock reads as intentional, whether one second is a fair escape warning, and whether the final maneuver is climactic

### Run 69 extraction-response-priority ideas

- [x] Promote the existing extraction Suppressor ahead of unresolved salvage reserves when the six-second uplink begins
- [x] Restart the full designer-tuned entry warning when promotion replaces an in-progress salvage warning
- [x] Preserve the authored safer entrance, six-metre exclusion, and role-uniqueness hold for the promoted response
- [x] Keep skipped salvage responses available after deployment without duplicating the Suppressor or exceeding four total responses
- [x] Prove the unresolved-reserve extraction route in deterministic and complete-runtime tests
- [ ] Playtest clean and avoidance returns for Suppressor warning recognition, field arrival, final maneuver, and uplink reserve

### Run 68 dead-zone security-trace ideas

- [x] Accumulate a designer-tuned security trace only while the tower is online and the player remains outside powered territory
- [x] Clear partial trace progress immediately when the player returns to powered territory
- [x] Bank the existing first Interceptor response after eight continuous dead-zone seconds without increasing the four-response cap
- [x] Make cache one and the completed trace share the same first-response budget slot so neither route can duplicate the Interceptor
- [x] Show the live trace countdown before lock-on while preserving authored entrances, safe-entry distance, and warning delay
- [ ] Playtest direct and greedy opening routes for warning recognition, route abandonment, first threat timing, and Signal reserve

### Run 67 adaptive-security-response ideas

- [x] Preserve the authored Interceptor as the first salvage response and the Suppressor as the extraction-only response
- [x] Replace whichever Warden or Sapper the player purges first as the next bounded response
- [x] Reserve the other core role for the third response without repeats or additional threat count
- [x] Vary the response order per run when both core roles were already purged before the director observes them
- [x] Preserve role uniqueness, safe entry distance, warning delay, alert budget, tuning, and existing enemy counterplay
- [ ] Playtest both purge orders and one double-purge route for response readability, mixed-role pressure, and extraction reserve

### Run 66 second-cache auxiliary-overclock ideas

- [x] Offer exactly one complementary economy-defense choice after the second required cache while pressure continues
- [x] Let Fire arm Emergency Capacitor for one designer-tuned 22-Signal refill when reserve falls to 25 or lower
- [x] Let Use charge Feedback Shield to negate one discrete enemy impact or pulse without removing dead-zone drain
- [x] Recharge an empty Feedback Shield only when the player purges a threat, preserving a reason to fight
- [x] Combine either auxiliary with Chain Arc or Overdrive for four distinct run builds and keep both layers visible in the HUD
- [ ] Playtest all four combinations for choice readability, capacitor timing, shield recharge frequency, and extraction reserve

### Run 65 extraction-Suppressor ideas

- [x] Reserve the fourth bounded security response for a Suppressor instead of repeating the Interceptor during extraction
- [x] Reuse the farther of two scene-authored flank gates and the existing six-metre exclusion plus 2.5-second entry warning
- [x] Telegraph a 3.25-metre denial ring for one second before activating a finite 2.5-second field
- [x] Slow a drone caught inside to 55% and drain 4 Signal per second while leaving an immediate escape route
- [x] Give the three-health Suppressor projectile, Chain Arc, HUD, purge-recovery, authored-prefab, build, and runtime coverage
- [ ] Playtest whether the Suppressor enters often enough during the six-second uplink and whether the field is threatening without deciding the run

### Run 64 first-cache overclock ideas

- [x] Offer exactly one temporary build choice after the first salvage cache while movement and enemy pressure continue
- [x] Let Fire select Chain Arc, consuming the choice input instead of accidentally firing a Signal bolt
- [x] Let Chain Arc damage one nearest secondary threat within a designer-tuned 4.5-metre radius and draw a brief comfort-safe link
- [x] Let Use select Overdrive Thrusters for designer-tuned 1.25× top speed and 1.2× acceleration
- [x] Keep the selected overclock visible in the salvage strip through extraction and expose both choices in keyboard/controller guidance
- [ ] Playtest whether choosing under live pressure is readable and whether Chain Arc or Overdrive dominates clean and depleted routes

### Run 63 extraction-pursuit ideas

- [x] Replace instant victory with a designer-tuned six-second extraction uplink
- [x] Preserve full movement, aiming, firing, and pause authority while the dock link completes
- [x] Bank exactly one additional bounded security response when the uplink begins
- [x] Reuse role uniqueness, authored entrances, six-metre exclusion, and 2.5-second entry warnings for the pursuit
- [x] Replace routine extraction guidance with a live survival countdown and pursuit-state threat strip
- [ ] Playtest whether six seconds forces a meaningful last maneuver without making a depleted return unwinnable

### Run 62 flanking-Interceptor ideas

- [x] Deploy one dedicated Interceptor as the first salvage escalation while the opening Warden and Sapper remain active
- [x] Choose the farther of two scene-authored flank gates and preserve the six-metre exclusion plus 2.5-second entry warning
- [x] Move toward a point between the drone and extraction instead of directly pursuing the player
- [x] Telegraph a locked 0.8-second dash line, then commit to a short collision-bounded burst with an impact cooldown
- [x] Give the Interceptor tuned health, a 14-Signal purge bounty, projectile/cover interaction, and live HUD state
- [ ] Playtest whether the cutoff reliably changes the return route without making the locked dash trivial or unavoidable

### Run 61 bounded security-escalation ideas

- [x] Raise one deterministic alert tier for each required cache secured after tower activation
- [x] Bank exactly one bounded reinforcement per alert tier; the initial alternating sequence was superseded by Run 62's Interceptor/Warden/Sapper mix
- [x] Hold each deployment until its tactical role is purged and a concurrent slot is available
- [x] Prevent bay-side ambushes with a designer-tuned six-metre player exclusion radius and 2.5-second entry delay
- [x] Show the live alert tier and remaining reinforcement reserve in the threat strip
- [ ] Playtest whether all three reserves are encountered on clean and combat-avoidant routes without making extraction attritional

### Run 60 salvage-chain momentum ideas

- [x] Start a designer-tuned 12-second chain window when a cache is secured
- [x] Restore 4 Signal for the second cache collected inside the active window
- [x] Restore 8 Signal for the third cache while preserving the 100-Signal cap
- [x] Show live chain count/countdown and record best chain plus actual Signal recovered
- [x] Confirm each collection with original chain art whose scale and tint escalate by tier
- [ ] Playtest direct and conservation routes to verify 12 seconds rewards mastery without requiring the shortcut

### Run 59 security-purge recovery ideas

- [x] Warden recovery bounty — player value: fighting the pursuer can recover survival reserve; acceptance: purging it restores up to 12 Signal exactly once.
- [x] Sapper recovery bounty — player value: intercepting the network threat is a net-positive emergency play; acceptance: purging it restores up to 16 Signal exactly once.
- [x] Cap-safe Signal restoration — player value: rewards never overflow or destabilize the shared resource; acceptance: deterministic restoration clamps at 100 and reports the amount actually received.
- [x] Purge telemetry — player value: the debrief acknowledges successful threat control; acceptance: run metrics and the raw report track purge count and actual Signal reclaimed.
- [x] Readable bounty presentation — player value: enemy health, reward stakes, and payout are visible without guesswork; acceptance: the threat strip shows both health pools and rewards, while an original cyan recovery burst confirms collection in world space.

### Run 58 mission command-strip ideas

- [x] Three-step phase numbering — player value: the whole run structure is visible at a glance; acceptance: the HUD advances deterministically through Restore, Recover, and Extract phases.
- [x] Explicit next action — player value: every phase names the immediate verb and destination; acceptance: tower, amber-cache, and cyan-dock actions update with run state.
- [x] Tower transaction preview — player value: the opening Signal bargain is understandable before committing; acceptance: phase one shows the exact 10 cost and 62 refill.
- [x] Live salvage remainder — player value: route planning requires no subtraction; acceptance: phase two reports the exact number of caches still needed with singular/plural wording.
- [x] Sapper interrupt advisory — player value: an imminent network drain can supersede routine routing without hiding the mission; acceptance: latched countdown guidance appears during salvage and extraction phases.

### Run 57 actionable mission-debrief ideas

- [x] Overall debrief grade — player value: each run ends with a clear mastery target; acceptance: deterministic S–D grade reflects outcome, reserve, drains, and exposure.
- [x] Signal efficiency reading — player value: the core resource decision receives direct coaching; acceptance: final reserve reports secure, tight, or critical.
- [x] Combat discipline reading — player value: avoidable Warden hits and Sapper pulses become a visible improvement goal; acceptance: the combined drain count is reported accurately.
- [x] Dead-zone exposure reading — player value: route safety becomes understandable without studying raw seconds; acceptance: exposure ratio reports controlled, elevated, or severe.
- [x] Route-choice reading — player value: the shortcut tradeoff is acknowledged in the result; acceptance: the debrief distinguishes shortcut and conservation routes.

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

### Run 56 Signal reserve telemetry ideas

- [x] Authored conduit fill — player value: Signal reads as fragile station energy rather than a generic bar; acceptance: original transparent conduit art imports as a Sprite and fills the live Canvas bar.
- [x] Stable reserve state — player value: healthy capacity is confirmed without interpreting a number; acceptance: reserves above 60% show cyan and the explicit `STABLE` label.
- [x] Strained reserve state — player value: the player receives useful warning before imminent failure; acceptance: reserves from 25% through 60% transition to amber and the explicit `STRAINED` label.
- [x] Critical reserve state — player value: immediate danger is unmistakable; acceptance: reserves at or below 25% transition to red and the explicit `CRITICAL` label.
- [x] Comfort-safe critical pulse — player value: urgency gains motion without violating accessibility preferences; acceptance: the critical fill breathes within a restrained alpha range, freezes while paused, and remains static with Reduced Flashes.

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
- [x] Convert the follow camera to a restrained high-angle perspective composition without changing planar gameplay
- [x] Add a speed-reactive twin Signal wake to communicate drone acceleration and coasting
- [x] Separate movement-facing drone chassis presentation from the independently aimed core/tool turret
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
