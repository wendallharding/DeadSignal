## P0.1 — Open an authored tactical window through foreground shells

### Implementation

- [ ] Keep `ForegroundOcclusionController` disabled; do not restore runtime renderer culling, footprint spawning, hierarchy searches, or per-frame material work.
- [ ] In the existing opening and Spine/Quench-return prefabs, identify the renderer faces that enter the camera-to-drone corridor or cover the central 40% × 55% tactical window.
- [ ] Split presentation-only wall faces from their collision owners where necessary, then shorten, bevel, recess, or move only those render meshes so the drone, nearest threat, telegraph, shot path, and one escape lane remain visible.
- [ ] Preserve every `AuthoredMapObstacle`, collider, transformed local axis, NavMesh surface, projectile blocker, safe entrance, powered territory, and prefab GUID.
- [ ] Keep the nine `AuthoredForegroundCutaway` bindings and packaged footprint resources valid for compatibility, but do not reactivate them.
- [ ] Start with the single Spine/Quench return composition; apply the same authored treatment to the opening only after the deep-room comparison passes.
- [ ] Add an Editor/development diagnostic that records the names and projected screen coverage of renderers intersecting the tactical window without changing runtime presentation.

### Acceptance

- [ ] At 1280×720 and 1600×900, the drone, nearest actionable threat or warning, active projectile path, and one continuous escape lane are simultaneously visible.
- [ ] No single foreground renderer covers more than 20% of the tactical window for two consecutive event-timed captures.
- [ ] Warden screening, Sapper priority warnings, Interceptor flank cues, Suppressor denial, and Swarmer approach silhouettes remain distinguishable from the floor and each other.
- [ ] HUD panels remain inside the viewport and do not overlap each other or the drone.
- [ ] Obstacle count, collision results, NavMesh corners, objectives, enemy statistics, spawn timing, Signal rules, salvage, and extraction outcomes remain unchanged.

### Validation

- [ ] Add focused PlayMode coverage proving the edited render meshes no longer cover the tactical-window samples while their collision owners still block movement and projectiles.
- [ ] Keep `AuthoredMapObstacleTests.OverlapsCircle_UsesObjectAlignedBounds` passing.
- [ ] Run focused PlayMode, full EditMode, full PlayMode, Windows development build, and packaged smoke.
- [ ] Run matched before/after `OpeningLoop` and Full Extraction captures at both required resolutions.
- [ ] Run three matched before/after LiveBalance Full Extraction playthroughs with identical route/profile settings.

## P0.2 — Keep or reject the eastern-lab Swarmer pressure tier

### Implementation

- [ ] Keep Swarmers confined to `EasternRoomCombat`; do not add them to the mission route, director, extraction, or authored arenas during this package.
- [ ] Add a debug-scenario toggle and command-line preset that run the same 30-second eastern laboratory with Swarmers off or on, the same seed, the same Warden/Sapper/Interceptor/Suppressor state, and the same selected build.
- [ ] Resolve all pending primary, auxiliary, and weapon choices before the comparison begins so held fire and target-priority behavior are exercised.
- [ ] Record minimum/final Signal, movement spend, hostile drains, shots, weapon hits, purges by target, Swarmer spawned/peak/purged/contacts, time to first contact, stationary-fire time, evasive direction changes, role attacks, no-progress time, and viewport failures.
- [ ] Preserve the current bounded contract unless matched play requires rejection: one free basic bolt purges a Swarmer; two waves of three; second wave after 4 seconds; maximum six; 4.5-metre safe spawn distance; 10 Signal contact loss; 3 Signal purge recovery.
- [ ] Treat continuous strafing plus rapid retargeting as the intended player counter.
- [ ] Treat attention pulled away from Sapper/Warden priority, lost firing alignment, and abandonment of safe cover as the positional/opportunity cost.
- [ ] Keep the Signal exchange unfavorable on contact; do not turn Swarmers into a positive farming loop.
- [ ] Reuse the existing geometric prefab, materials, projectile, hit, and purge feedback; scope cost is tooling, telemetry, tests, and at most one small silhouette/telegraph correction.

### Acceptance and rejection

- [ ] Keep the tier only if movement decisions, short target-switch loops, combat readability, weapon satisfaction, encounter variety, fun, and replay intent improve in matched human play.
- [ ] Reject or remove the tier if stationary held fire remains optimal, Swarmers duplicate Interceptor pressure, specialist target priority becomes unreadable, contacts feel unavoidable, or the tier reduces fun or replay intent.
- [ ] Reject any solution requiring higher enemy health, damage, count, raw speed, a larger arena, a larger map, or weaker specialist roles.
- [ ] No existing build may lead clear time, reserve, purge efficiency, damage avoidance, and player preference simultaneously.

### Validation

- [ ] Add pure tests for two-wave timing, maximum population, safe spawn distance, contact cooldown, reset, one-hit purge, and the 3-reward/10-loss bound.
- [ ] Extend PlayMode coverage for Swarmers off/on, keyboard/mouse and controller fire paths, viewport containment, collision, NavMesh progress, and clean scenario reload.
- [ ] Run at least three matched off/on samples per build at 1280×720 and 1600×900.
- [ ] Run human keyboard/mouse and controller comparisons for stationary held fire, circular strafing, and specialist-first targeting.
- [ ] Score weapon satisfaction, movement decisions, role distinction, readability, pressure, fun, and replay intent immediately after each pair.

### Blocker before P1

- [ ] Do not promote Swarmers, add a weapon modifier, change encounter composition, repurpose an arena, add another enemy, or enlarge the level until P0.1 passes and P0.2 produces a documented keep/remove decision from matched human evidence.

## P1.1 — Complete matched combat telemetry

### Implementation

- [ ] Track continuous minimum Signal reserve and report passive, movement, hostile-damage/drain, special-power, and extraction spend separately; basic fire remains free.
- [ ] Attribute weapon hits, follow-throughs, ricochets, chain arcs, purges, hits, and drains to Warden, Sapper, Interceptor, Suppressor, and Swarmer.
- [ ] Record peak simultaneous roles, role combinations, evasion responses, optional-cache decisions, route abandonment, navigation recoveries, no-progress time, resolution, input device, seed, selected build, and scenario toggle.
- [ ] Emit `not observable` for unavailable fields instead of silently omitting them.
- [ ] Keep deterministic counters in plain C# and avoid allocation-heavy per-frame diagnostics.

### Acceptance and validation

- [ ] Repeated deterministic runs produce identical counters.
- [ ] Every combat event is attributable to its source and active weapon behavior.
- [ ] Add EditMode tests for every counter and minimum-reserve transition.
- [ ] Add PlayMode coverage containing one event from every enabled role, one weapon hit, one purge, traversal spend, hostile drain, and extraction spend.
- [ ] Validate reports in packaged deterministic, LiveBalance, and Swarmer-off/on routes.

## P1.2 — Prove existing builds before proposing another modifier

### Implementation

- [ ] Compare Chain Arc, Overdrive Thrusters, Piercing Pulse, Controlled Ricochet, Emergency Capacitor, and Feedback Shield before creating another pickup.
- [ ] Run these four builds with the same seed and encounter schedule:
  - [ ] Chain Arc + Emergency Capacitor + Piercing Pulse.
  - [ ] Chain Arc + Feedback Shield + Controlled Ricochet.
  - [ ] Overdrive Thrusters + Emergency Capacitor + Piercing Pulse.
  - [ ] Overdrive Thrusters + Feedback Shield + Controlled Ricochet.
- [ ] Preserve free basic fire, current finite rewards, current special-power costs, and weapon-specific extraction responses.
- [ ] Verify Chain Arc rewards clustered-threat management, Overdrive rewards movement as defense, Piercing rewards alignment, and Ricochet rewards geometry without enabling safe stationary purges.
- [ ] Verify Warden screening, Sapper priority pressure, Interceptor displacement, Suppressor denial, and any retained Swarmer retargeting remain effective counters.

### Acceptance and validation

- [ ] At least two builds create measurably different movement, targeting, routing, or greed decisions.
- [ ] No build leads completion, reserve, purge efficiency, damage avoidance, and preference simultaneously.
- [ ] No build trivializes an enemy role or requires raw-stat inflation or map growth.
- [ ] Run four matched packaged LiveBalance routes per seed and human keyboard/mouse plus controller comparisons for all four builds.
- [ ] Do not prototype split shot, faster fire, focused fire, delayed echo, temporary overcharge, or another modifier unless these comparisons still show tactically flat progression.
- [ ] If a future modifier is considered, define its Signal/downside, enemy counter, positional/opportunity cost, implementation scope, and rejection criteria before implementation.

## Conditional content-expansion order

- [ ] First improve combinations and encounter composition with Warden, Sapper, Interceptor, and Suppressor.
- [ ] Then add one meaningful behavioral variant using existing assets only when the existing roles cannot create the required movement decision.
- [ ] Then improve or repurpose one authored combat arena only when existing geometry blocks the required counterplay.
- [ ] Add one genuinely new enemy only when the four established roles cannot supply the missing pressure and the isolated Swarmer decision is complete.
- [ ] Enlarge the authored level only when current rooms still cannot support the desired variety after density, approach direction, spawn timing, terrain, pressure phases, and optional high-risk arenas have been tested.

## Definition of Done

- [ ] P0.1 has matched before/after visual and collision evidence at both required resolutions.
- [ ] P0.2 has a human-backed keep/remove decision and no mission promotion occurred prematurely.
- [ ] Every combat advancement has matched before/after playtests for weapon satisfaction, movement decisions, encounter variety, readability, completion pressure, build diversity, fun, and replay intent.
- [ ] No dominant weapon/build, trivialized role, positive Signal farm, raw enemy-stat inflation, or premature map growth is introduced.
- [ ] Full Extraction, optional greed, extraction modes, authored-level direction, and Signal as mobility/machinery/special-power/survival currency remain coherent.
- [ ] Keyboard/mouse and controller paths pass; HUD, silhouettes, telegraphs, firing, hits, purges, damage, and escape lanes are readable.
- [ ] Focused and full Unity suites, Windows development build, packaged smoke, route reports, log scans, and required human playthroughs pass.
- [ ] Audio feel, animation feel, difficulty, fun, and replay intent are directly evaluated rather than inferred.
- [ ] Only one bounded combat or content advancement is authorized for the following pass.
