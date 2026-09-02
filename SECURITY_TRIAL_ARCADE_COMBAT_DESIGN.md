# DEAD SIGNAL — Security Trial Arcade Combat Design Specification

Status: Approved design direction; implementation not started  
Scope: Security Trial Room B and its dedicated combat-tuning scene  
Current production scaffold: three untuned clear-all phases, 11 total Swarmers, five-threat peak; scheduled for replacement  
Target production duration: approximately 75–90 seconds, subject to human play evidence

## 1. Purpose

Room B should become a dense, readable, twin-stick arena climax inspired by the enemy ecology, formation grammar, population pressure, and escalation rhythm of *Geometry Wars: Retro Evolved*. It should provide a strong mechanical baseline that can be tuned into DEAD SIGNAL's wider combat and Signal economy without copying Geometry Wars' visual identity, names, audio, user interface, score tables, or other expressive assets.

This specification defines the intended player experience, encounter rules, enemy roles, spawning system, software responsibilities, tuning data, performance constraints, rollout gates, and validation evidence. It does not authorize copying source code or extracting assets or data from another game. Exact Retro Evolved implementation details and timing tables are not publicly documented; any claim of parity must therefore be based on measured observable behavior and matched play, not assumed internals.

## 2. Product decision

Build one reusable arcade-pressure framework with two profiles:

- **Retro Baseline** isolates the analogous enemy behaviors, formation patterns, and population cadence. It is the reference used to tune movement pressure and readability.
- **DEAD SIGNAL Remix** starts from the accepted baseline and selectively introduces the established Warden, Sapper, and Suppressor roles, Signal consequences, authored cover, and station-fiction presentation.

Production Room B uses a bounded encounter with an explicit final clear and Room C release. The tuning scene may also offer an endless diagnostic mode. No other mission room adopts the full pattern until Room B passes human comparison, route regression, performance, economy, and accessibility gates.

The existing three-phase encounter is not a gameplay baseline and is not a profile that must be preserved. It has not received meaningful tuning and does not currently provide the intended fun, density, rhythm, or room-scale experience. Preserve only the integration contracts it happens to exercise: Room A commitment, Room B lockdown, Room C reward, doors, navigation, objective authority, reset, and run completion. Replace its phase scheduling and composition when the new bounded director is integrated, and remove obsolete configuration after the replacement passes those integration contracts.

## 3. Goals

- Create sustained movement-as-defense rather than a sequence of isolated targets.
- Make pressure rise mainly through population, formation, and enemy combination rather than health inflation.
- Give every enemy a strong, learnable movement rule, clear counterplay, and distinct presentation.
- Use time and alive-population pressure together so skilled play accelerates the encounter while slower play receives bounded breathing room.
- Prefer entry near the visible play boundary, while supporting deliberate corner, perimeter, and player-relative formations.
- Expand Room B horizontally until its usable arena-to-player scale and traversal time approximate the Retro Evolved playfield; retain the current vertical extent initially unless measurement shows that it also misses the target.
- Preserve the combat camera's zero-yaw contract and current Room B pullback transition.
- Preserve DEAD SIGNAL's free basic fire, Signal decisions, authored station environment, route structure, and reward authority.
- Support deterministic simulation, repeatable seeds, focused automated tests, and useful human-play telemetry.
- Sustain the target population at 60 frames per second on the project's target Windows configuration.

## 4. Non-goals

- Reproducing Geometry Wars' exact score, multiplier, life, bomb, weapon-upgrade, achievement, HUD, audio, particle, grid, or color systems.
- Creating a frame-for-frame or asset-for-asset copy.
- Turning the complete mission into an endless score-attack game.
- Replicating Room B's full wave structure in Rooms A, C, or other mission spaces.
- Replacing the mission objective graph, authored Security Trial doors, capacitor reward, or withdrawal route.
- Increasing enemy health merely to extend encounter duration.
- Requiring NavMesh agents for high-volume arena actors.
- Coupling deterministic scheduling rules directly to `Camera`, `Transform`, frame rate, or global scene searches.

## 5. Current scaffold and migration constraints

The current chamber has three untuned strict phases:

1. Three Swarmers.
2. Four Swarmers and one Warden.
3. Four Swarmers and one Sapper.

A phase advances only after its complete population is purged. This scaffold exercises Room A commitment, Room B lockdown, Room C reward release, door collision, navigation refresh, reset, and complete-route integration. Its combat timing, composition, population, and feel are not acceptance evidence. It does not prove dense continuous pressure, viewport-aware spawning, large populations, a broad enemy ecology, or a fun encounter.

The migration must preserve:

- Room A remains non-combat commitment and warning.
- Crossing the authored Room B threshold remains the lockdown authority.
- Both doors remain collision- and presentation-authoritative.
- Room C remains inaccessible until the bounded Room B encounter clears.
- The capacitor remains a one-shot reward restoring up to 20 Signal unless later economy evidence approves a change.
- Cleared revisits and the powered withdrawal route remain unchanged.
- Death, restart, reload, debug completion, and interrupted phase transitions recover deterministically.
- The camera may change pitch, distance, height, and field of view through its existing combat profile, but this feature never changes yaw.

### 5.1 Arena footprint and scale

Room B's usable horizontal span is currently too small for the intended arcade circulation and must be widened before formation and population tuning. The current vertical depth may be retained if measurement shows that it already produces an appropriate player-to-arena relationship. The target is perceptual and mechanical equivalence to the Retro Evolved playfield, not a guessed conversion from another game's hidden world units.

Measure both the reference playfield and Room B using normalized quantities:

- Usable arena width divided by player visual width and collision diameter.
- Usable arena height divided by player visual height and collision diameter.
- Seconds required to traverse 80 percent of width and height at sustained normal movement.
- Visible floor width and height at the settled combat camera.
- Percentage of the arena outside the viewport when the player occupies the center, horizontal extremes, and vertical extremes.
- Minimum turning/circulation radius around authored cover.
- Time for a basic pursuer to cross the arena relative to the player.

Reference footage must be captured or annotated at a known aspect ratio and a stable playfield view. Perspective, bloom, and visual effects make pixel measurements approximate, so use multiple frames and report a range. The first authored change should expand the east/west Room B floor, walls, backing, arena boundary, and circulation lanes while leaving the north/south relationship between Rooms A, B, and C intact. Reposition or revise central deflectors only when necessary to preserve broad loops, projectile lanes, and at least two credible circulation routes.

The combat camera should then be recalibrated against the expanded geometry so player screen size and visible navigable area approach the reference relationship. This may change combat pitch, height, distance, or field of view, but yaw remains zero and is never an encounter parameter. The result must not expose accidental black void, hide door state, or make Room A/C visible in a way that weakens lockdown composition.

Initial scale acceptance requires:

- A documented reference range and measured Room B range for every normalized quantity above.
- Horizontal arena-to-player scale within 10 percent of the selected reference target, unless human play explicitly approves a documented deviation.
- Vertical arena-to-player scale either within the same tolerance or intentionally retained with a recorded reason.
- Horizontal circulation no longer feels constrained in matched human play.
- Valid outside-viewport spawn floor exists through a useful portion of normal movement, with safe visible-perimeter fallback when the arena edge is inside the viewport.
- Room A commitment, Room C reward, doors, collision, navigation, camera transitions, and return-route composition remain valid.

## 6. Experience pillars

### 6.1 Readable chaos

The screen may become busy, but the player must be able to predict what each actor will do. Complexity should emerge from combinations of simple roles. Enemy silhouettes, motion, spawn audio, telegraphs, and threat effects must remain distinguishable at 1280x720, 1600x900, and 3440x1440.

### 6.2 Movement creates survival

Direct pursuers encourage circulation, chargers punish straight-line escape, evasive enemies contest careless spray, segmented threats obstruct firing lanes, and gravity hazards reshape local movement. No normal formation may close every viable escape lane at activation.

### 6.3 Pressure has rhythm

The encounter alternates between pressure growth, low-water reinforcement, short recovery opportunities, and deliberate climaxes. It should not pause after every small group, and it should not maintain maximum density continuously.

### 6.4 Original station fiction

Every mesh and effect is built from security hardware, broken maintenance machinery, capacitor components, cable systems, station ceramics, and Signal energy. Geometry communicates behavior, but it must not reproduce the recognizable shapes or trade dress of the reference game.

## 7. Terminology

- **Actor:** One active enemy or hazard with independent collision and lifecycle.
- **Archetype:** A behavior and presentation contract shared by actors of one role.
- **Formation:** One scheduled spawn request containing an archetype mixture and placement pattern.
- **Pressure cost:** The weight an actor contributes to the alive population budget.
- **Low-water mark:** The alive pressure at or below which another formation may deploy.
- **Pressure ceiling:** The maximum alive pressure allowed for the current band.
- **Band:** A bounded portion of the encounter that unlocks formation and archetype choices.
- **Spawn warning:** The period during which an arrival location is visible/audible but not yet dangerous.
- **Viewport footprint:** The portion of the arena floor visible through the active gameplay camera.
- **Arena boundary:** The authored traversable Room B polygon or set of convex regions.
- **Force time:** The latest time a formation may remain pending before the director resolves a safe deployment or records a recoverable failure.

## 8. Encounter lifecycle

| State | Responsibility | Exit condition |
| --- | --- | --- |
| Dormant | Preserve the stable authored room and mission state | Room A commitment succeeds |
| Armed | Open the entry and await the exact threshold crossing | Player crosses the Room B trigger depth |
| Locking | Seal doors, activate camera pullback, clear unrelated threats, and provide entry grace | Camera/door transition and grace complete |
| Active | Run the bounded formation schedule and population director | All required formations committed |
| FinalClear | Stop new formations and resolve every owned actor/hazard | Alive pressure and pending children reach zero |
| Cleared | Release both doors, play room-clear feedback, and expose Room C reward | Persistent until run reset |
| Recovery | Repair or retire stuck/invalid owned state without rewarding the player | State becomes valid or encounter fails safely |

The director owns encounter scheduling only. `AuthoredCombatChamber` continues to own room state and doors. Mission progression continues to own objective completion and the capacitor reward.

## 9. Scheduling model

Each formation entry contains:

- Stable identifier.
- Earliest encounter time.
- Force time.
- Required band.
- Pattern and archetype composition.
- Spawn warning duration.
- Minimum and preferred player distance.
- Low-water pressure threshold.
- Maximum pressure after deployment.
- Required predecessor identifiers, if any.
- Repeat limit and selection weight for endless mode.
- Whether it is required for bounded completion.

A pending formation becomes deployable when:

```text
encounter time >= earliest time
and all required predecessors have committed
and alive pressure <= low-water mark
and alive pressure + requested pressure <= pressure ceiling
and the spawn resolver returns a valid placement
```

If force time is reached, the director may relax preferred distance and offscreen preference, but it may never violate hard collision, arena-boundary, player-exclusion, telegraph, or pressure-ceiling rules. If no valid placement exists, it records a diagnostic reason and retries. A bounded retry timeout enters Recovery rather than silently blocking progression.

Killing the last actors below a low-water threshold can therefore advance the cadence immediately. Preserving one actor can create a brief pause, but force time prevents indefinite stalling. This behavior is intentional and must be tuned rather than treated as an incidental side effect.

### 9.1 Initial pressure-band hypothesis

These values are starting points for prototype tuning, not acceptance values:

| Band | Time window | Suggested ceiling | Formation emphasis |
| --- | --- | --- | --- |
| Orientation | 0–12 s | 10–12 | Loose Rotors and Security Pursuers |
| Evasion | 12–28 s | 16–20 | Pursuers, Evasive Relays, small Fragment groups |
| Multiplication | 28–45 s | 22–28 | Fragment Carriers, corner deployments, Breach Interceptors |
| Obstruction | 45–62 s | 26–34 | Conduit Trains, mixed pressure, first Flux Sink |
| Climax | 62–82 s | 32–40 | Maintenance Clouds, mixed corners, hazard interaction |
| Final clear | 82–90 s | No new pressure | Purge all remaining owned threats |

The implementation must expose these through tuning assets. It must not hardcode this table in the runtime director.

## 10. Spawn placement

### 10.1 Viewport footprint

The Unity adapter projects the active camera's viewport corners onto the combat floor and clips the result against the authored arena boundary. It supplies the deterministic resolver with a two-dimensional visible polygon, arena polygon, player position, occupied circles, and authored perimeter lanes. The rules do not read camera yaw or mutate any camera property.

The footprint must be recalculated when camera framing, aspect ratio, resolution, or player-follow position materially changes. It does not need to allocate every frame; cached geometry may be refreshed at a bounded cadence or on relevant changes.

### 10.2 Candidate order

1. Valid floor immediately outside the viewport footprint but inside the arena.
2. Authored perimeter lanes associated with the requested pattern.
3. Visible arena-edge positions with the full spawn warning.
4. Explicit player-relative positions for an approved ambush pattern.

If the whole arena is visible, the first category is naturally empty and perimeter placement becomes normal. A visible spawn is valid behavior when clearly warned; invisibility is a preference, not a safety guarantee.

### 10.3 Hard validity rules

A candidate is rejected when it:

- Lies outside the authored safe arena.
- Overlaps a wall, door slab, deflector, hazard, actor, or another reserved spawn volume.
- Violates the configured minimum distance from the player.
- Occupies the player's immediate projected movement corridor during warning time.
- Creates a formation with no viable escape sector.
- Requires an actor to cross a sealed door or non-traversable region.
- Places a segmented actor without enough head-and-chain clearance.
- Activates a gravity hazard within its hard player-exclusion radius.

### 10.4 Candidate scoring

Valid candidates are scored by:

- Offscreen margin.
- Distance from the player relative to the preferred range.
- Angular separation from recent formations.
- Distribution across underused perimeter sectors.
- Formation fit and available local area.
- Distance from current projectile saturation.
- Distance from doors during lockdown transitions.

Seeded tie-breaking keeps runs reproducible while allowing controlled variation.

### 10.5 Formation catalog

- **Random perimeter:** One or more small groups distributed among safe edge sectors.
- **Four corners:** Mirrored groups at four arena corners or the nearest valid authored equivalents.
- **Ambush ring:** A warned partial or full ring around the player, always leaving at least one escape sector.
- **Corner stream:** A sustained, rate-limited stream from multiple corners rather than one frame spike.
- **Mixed corners:** Complementary archetypes placed symmetrically or diagonally.
- **Opposed lanes:** Pressure enters from two edges to create a traversal decision.
- **Hazard and pursuit:** A Flux Sink appears first, followed by actors whose trajectories interact with it.

Every pattern declares the archetypes it supports and its minimum arena clearance. Unsupported combinations fail validation in the Editor.

## 11. Enemy roster

All names are DEAD SIGNAL working names. Final names may change without changing their behavioral contracts.

### 11.1 Loose Rotor

**Purpose:** Low-threat motion texture and early aiming practice.  
**Behavior:** Travels in a broad revolving or looping path independent of the player, reflects or redirects at arena boundaries, and does not deliberately pursue.  
**Counterplay:** Predict the path or ignore it temporarily while addressing active pursuers.  
**Pressure cost:** 1.  
**Presentation:** A detached ventilation, flywheel, or generator rotor with an unstable amber bearing.  
**Preferred patterns:** Random perimeter and sparse four-corner placement.

### 11.2 Security Pursuer

**Purpose:** Foundational population pressure.  
**Behavior:** Turns toward the player and advances at a speed the player can outrun in open space. Uses lightweight separation so groups compress without perfectly stacking.  
**Counterplay:** Maintain circulation, fire into the pursuing mass, and cut through low-density edges.  
**Pressure cost:** 1.  
**Presentation:** Reuse and extend the existing Swarmer chassis and presentation.  
**Preferred patterns:** All patterns except hazard-only deployments.

### 11.3 Evasive Relay

**Purpose:** Punish indiscriminate lateral spray and require deliberate aim.  
**Behavior:** Pursues the player, predicts nearby projectile lines, and chooses a bounded sidestep that still tends to close distance. It must not react to projectiles behind occluding cover or dodge every possible shot.  
**Counterplay:** Aim directly, bracket with spread/ricochet, or attack while its dodge cooldown is active.  
**Pressure cost:** 1.5.  
**Presentation:** A green/cyan sensor lattice whose side vanes articulate before a dodge.  
**Preferred patterns:** Random perimeter, ambush ring, and mixed corners.

### 11.4 Fragment Carrier

**Purpose:** Convert careless kills into temporary population growth.  
**Behavior:** Fast direct pursuit. On purge, it schedules three weaker fragments with inherited momentum and a short non-damaging separation interval. Children belong to the same formation and population ledger.  
**Counterplay:** Create room before destroying it, use piercing/chain effects, or purge it at the edge of the arena.  
**Pressure cost:** 2 before splitting; children total no more than the parent's configured post-split budget.  
**Presentation:** Cracked capacitor or relay housing that separates into energized service fragments.  
**Preferred patterns:** Corner groups, ambush ring, and mixed corners.

### 11.5 Conduit Train

**Purpose:** Obstruct firing lanes and create large readable moving barriers.  
**Behavior:** A vulnerable head pursues with bounded turn rate; trailing segments follow recorded head positions. Segments block or deflect bolts according to tuning but cannot independently damage after the head is purged.  
**Counterplay:** Flank the head, exploit its turn radius, or use a weapon path capable of reaching it.  
**Pressure cost:** 4 per complete train.  
**Presentation:** Linked cable carriers, insulators, and energized couplers.  
**Preferred patterns:** Corners, opposed lanes, and late ambush formations with adequate clearance.

### 11.6 Breach Interceptor

**Purpose:** Break perpetual circular kiting and demand lateral evasion.  
**Behavior:** Enters, pauses to acquire the player, accelerates in a committed line, recovers after the pass, and deflects frontal fire during its guarded state. It has low turning authority during the charge.  
**Counterplay:** Move laterally after lock, attack from the side/rear, or exploit post-charge recovery.  
**Pressure cost:** 3.  
**Presentation:** Adapt the existing Security Interceptor chassis, dash telegraph, and collision behavior rather than creating a redundant role.  
**Preferred patterns:** Corners and opposed lanes; never an untelegraphed close ring.

### 11.7 Maintenance Cloud

**Purpose:** Deliver the high-count swarm climax and open temporary gaps through sustained fire.  
**Behavior:** Large numbers of tiny low-health actors stream from corners or perimeter lanes. Steering is simple and batched; actors may use reduced collision detail and presentation budgets.  
**Counterplay:** Commit fire to one escape sector, use area/chain behavior, or traverse before the stream closes.  
**Pressure cost:** Fractional per actor with both actor-count and pressure ceilings.  
**Presentation:** Energized fasteners, broken inspection drones, and service debris sharing a pooled effect.  
**Preferred patterns:** Rate-limited corner streams only.

### 11.8 Flux Sink

**Purpose:** Create a manipulable local hazard that reshapes actors, projectiles, and player routes.  
**Behavior:** Begins dormant. Player fire activates it, after which it pulls nearby actors and the player with separately tuned forces, bends or deflects bolts, accumulates absorbed pressure, and eventually ruptures into fast fragments. The pull must be clamped and must never invalidate player control.  
**Counterplay:** Leave it dormant, activate it to collect pursuers, destroy it deliberately, or avoid its rupture line.  
**Pressure cost:** 5 plus a reserved child budget.  
**Presentation:** Failed station power-routing node with expanding field rings and floor distortion; no imitation neon grid.  
**Preferred patterns:** Authored hazard positions and mixed-corner formations.

## 12. Established specialist roles

- **Warden:** Excluded from the first Retro Baseline. A Remix formation may use it as durable space-management pressure after the baseline is readable.
- **Sapper:** Excluded from the first Retro Baseline because Signal drain changes target priority and pacing. Remix introduction requires a dedicated economy gate.
- **Suppressor:** Excluded from the first Retro Baseline because movement suppression can undermine circulation. Remix introduction requires proof that an escape lane remains viable.
- **Interceptor:** Reused as the Breach Interceptor foundation; do not maintain two mechanically redundant chargers.
- **Swarmer:** Reused as the Security Pursuer foundation; preserve compatibility scenarios that require the existing authored actor.

## 13. Movement, collision, and damage

- High-volume actors use deterministic planar steering with explicit position, velocity, turn limit, collision radius, and target inputs.
- Broad-phase neighbor and projectile queries use a spatial grid or equivalent bounded partition; no all-pairs population scan is allowed at climax scale.
- Authored obstacles retain oriented local bounds. Arena actors must not convert rotated blockers into world-axis-aligned collision.
- Steering resolves arena boundary, static obstacles, separation, role intent, and external fields in a documented priority order.
- Actor movement is frame-rate independent and testable at multiple `dt` values.
- Spawn warnings are non-damaging and non-blocking until activation.
- Contact damage uses per-actor or per-player immunity windows so a dense overlap cannot apply one drain per actor in one frame.
- Child spawns, purges, and forced retirement must update the population ledger exactly once.

## 14. Signal economy

The current Swarmer contact drain and purge reward were tuned for a five-threat peak. They cannot be multiplied across 20–40 actors unchanged.

Room B receives a focused combat-economy profile containing:

- Contact drain by threat class.
- Shared contact grace or immunity duration.
- Purge Signal reward by threat class.
- Reward caps per second and per formation.
- Optional recovery beats between bands.
- Entry reserve floor or emergency recovery rule.
- Sapper/Suppressor Remix modifiers.
- Death and restart restoration behavior.

The first Retro Baseline should test movement and population with conservative Signal consequences. Economy acceptance occurs only after normal, low-reserve, and build-specific human runs. Infinite Signal and invulnerability remain diagnostic options and may never be enabled by the production profile.

## 15. Camera contract

- Entering Room B activates the existing combat pullback.
- Leaving lockdown transitions back through the existing camera profile.
- The encounter and spawn systems never write camera yaw.
- Visibility calculations consume the actual settled camera projection, including aspect ratio and pitch.
- Steady Camera disables or reduces combat impulses without changing spawn eligibility.
- Spawn rules must pass at 1280x720, 1600x900, and 3440x1440; aspect ratio must not materially change effective difficulty without an explicit tuning decision.
- The camera should preserve the Geometry Wars-like relationship between player size, threat density, and navigable room area without requiring the entire Room B boundary to remain visible at all times.

## 16. Presentation, audio, and accessibility

- Every archetype has a distinct silhouette, dominant motion, spawn telegraph, arrival sound, active sound, purge response, and Reduced-Flashes variant.
- Spawn audio must communicate archetype and approximate direction without requiring the spawn to be visible.
- Enemy color is supporting information, never the only differentiator.
- Spawn warnings remain legible against dormant, active, and cleared Room B lighting.
- Particle and light counts are capped per actor class; cloud actors use aggregate effects.
- Offscreen indicators group high-count basic threats and reserve individual indicators for specialists or hazards.
- Full-screen effects may not obscure projectile paths or escape lanes.
- Reduced Flashes, Steady Camera, keyboard/mouse, and controller paths remain complete.

## 17. Tuning assets

Create focused ScriptableObjects rather than adding all values to `ThreatBalanceTuning`:

- `SecurityTrialEncounterProfile`: bounded/endless mode, bands, schedule, pressure ceilings, completion rules, recovery policy, and profile identity.
- `ArenaFormationProfile`: composition, pattern, timing, population gates, placement requirements, and selection weight.
- `ArenaSpawnTuning`: floor projection, viewport margin, player exclusion, reservations, retries, telegraph timing, and candidate scoring.
- `ArenaThreatArchetypeTuning`: movement, collision, health, pressure cost, Signal values, pooling, and presentation references for one archetype.
- `SecurityTrialEconomyTuning`: contact grace, drains, rewards, caps, recovery, and entry reserve behavior.
- `SecurityTrialPerformanceBudget`: actor, segment, projectile, light, particle, and audio-voice ceilings used by diagnostics and tests.

All assets require safe defaults and `OnValidate` relationship checks. Runtime deterministic rules receive validated immutable values or copies; they do not reach into global resources during simulation.

## 18. Software responsibilities

The names below are proposed and may be adjusted to match nearby code during implementation.

### Pure C# rules

- `ArenaEncounterDirector`: lifecycle, time, band progression, pending formations, completion, and recovery.
- `ArenaPopulationLedger`: actor ownership, pressure accounting, children, purge, retirement, and invariants.
- `ArenaFormationSelector`: deterministic weighted selection for endless mode and any permitted bounded variation.
- `ArenaSpawnPlanner`: validates and scores two-dimensional spawn candidates supplied by an adapter.
- Steering policies for pursue, orbit, evade, committed charge, train following, and field influence.

### Unity adapters and presentation

- `SecurityTrialEncounterController`: bridges chamber state, tuning, simulation, owned actors, telemetry, and feedback.
- `ArenaVisibilityProvider`: projects the active camera to the arena floor without changing the camera.
- `AuthoredArenaBoundary`: scene-authored polygon/regions and perimeter lanes for Room B.
- `ArenaThreatPool`: prewarms, activates, and retires actors and presentation safely.
- Focused actor components for behaviors that require Transform, Renderer, Collider, audio, or effects.

`DeadSignalThreatController` remains the compatibility owner for current specialists and projectiles. It may expose narrow services to the new controller, but it must not absorb the complete director, roster, spawn planner, and pooling system.

## 19. Determinism, reset, and recovery

- Production bounded mode uses a recorded encounter seed; tests supply fixed seeds.
- The same seed, profile, input events, and fixed-step sequence produce the same formation decisions.
- Presentation randomness is isolated from gameplay randomness.
- Restart retires every owned actor, child, segment, hazard, reservation, warning, and pooled effect.
- Scene reload and debug completion cannot leave an active camera profile or sealed door behind.
- Actors outside the legal arena for longer than a tuned grace are safely repositioned or retired according to archetype policy.
- A formation that cannot place within its retry timeout produces a diagnostic failure and recoverable fallback; it never blocks the mission silently.
- Final clear requires no pending required formation, no pending child spawn, and zero owned alive pressure.

## 20. Diagnostics and telemetry

The combat-tuning scene should expose:

- Retro Baseline and DEAD SIGNAL Remix profile selection.
- Bounded and endless modes.
- Fixed/random seed selection and seed display.
- Restart encounter and jump-to-band controls.
- Infinite Signal, invulnerability, frozen AI, and presentation-only toggles.
- Optional overlays for viewport footprint, arena boundary, spawn candidates, rejected reasons, pressure, band, and pool utilization.

Record at minimum:

- Encounter seed, profile, build, resolution, and accessibility state.
- Formation requested/warned/activated times and rejection reasons.
- Archetype counts, actor peak, pressure peak, and pool peak.
- Alive pressure when each formation activates.
- Spawn classification: offscreen, perimeter-visible, corner, or player-relative.
- Minimum spawn-to-player distance and available escape-sector count.
- Purge order, contact events, Signal gained/lost, and player defeat.
- Band duration, low-pressure duration, final-clear duration, and total completion time.
- Frame-time percentiles during each band.

Telemetry is development-only unless a later product decision defines persistent analytics.

## 21. Performance budgets

Initial budgets must be validated against representative hardware before becoming acceptance thresholds:

- Target 60 frames per second in production Room B.
- Prewarm enough pooled actors for the configured bounded peak plus child reserve.
- No routine `Instantiate`, `Destroy`, scene search, material creation, or managed allocation in steady-state actor ticks.
- Enforce separate actor, segment, projectile, particle, dynamic-light, and audio-voice limits.
- Maintenance Cloud actors use simplified collision, rendering, and feedback.
- Performance overflow rejects or delays a formation rather than exceeding a hard safety ceiling.
- Diagnostics report average, 95th percentile, and worst frame time for each band.

## 22. Validation strategy

### 22.1 EditMode

- Director timing, low-water, force-time, predecessor, pressure-ceiling, and completion rules.
- Deterministic formation selection and seed replay.
- Population accounting across split, segment, purge, retirement, and reset.
- Spawn geometry, candidate scoring, arena clipping, minimum distance, and escape-sector rules.
- Steering behavior at multiple step sizes and oriented-obstacle collision.
- Tuning validation and invalid configuration rejection.

### 22.2 PlayMode

- Threshold-to-lockdown-to-clear lifecycle.
- Camera pullback activation and explicit unchanged-yaw regression.
- Visible/offscreen/perimeter placement at all required aspect ratios.
- Pool prewarm, reuse, reset, scene reload, and debug completion.
- Every archetype's presentation and counterplay contract.
- Door collision, projectile collision, NavMesh refresh, Room C release, capacitor reward, and cleared revisit.
- Reduced Flashes, Steady Camera, controller, and keyboard/mouse coverage.
- Bounded peak-population performance scenario.

### 22.3 Escalation lanes

- Run focused EditMode and PlayMode validation for every bounded slice.
- Run `CombatEvidence` when population, encounter timing, arena, performance population, or specialist composition changes.
- Run `LiveBalance` at each accepted roster/band milestone, not after every asset-only step.
- Run `RouteRegression` when Room B lifecycle, doors, progression, camera integration, reset, or reward authority changes.
- Run full EditMode/PlayMode, Windows build, and packaged smoke when the replacement profile becomes the production default.

### 22.4 Human evidence

Automated correctness cannot prove game feel. Record matched runs for:

- Current scaffold only as an integration and arena-size reference; it is not a fun or balance benchmark.
- Retro Baseline at normal and low starting Signal.
- DEAD SIGNAL Remix for every established weapon build.
- Reduced Flashes and Steady Camera.
- Keyboard/mouse and controller.

Capture completion time, deaths, minimum/final Signal, perceived unfair spawns, unreadable moments, dominant tactics, breathing rhythm, and replay preference.

## 23. Acceptance criteria

The production replacement is accepted only when:

- Room B completes in approximately 75–90 seconds for the target successful run, or human evidence approves a revised range.
- Room B's widened horizontal footprint meets the normalized scale target and no longer feels cramped; its vertical footprint is measured and either accepted or deliberately revised.
- Population cadence feels continuous but includes recognizable breathing opportunities.
- No active formation produces an unavoidable spawn contact or closes every escape sector.
- Every archetype is identifiable and its counterplay is demonstrated in isolation and composition.
- Skilled purging advances pressure without permitting the director to exceed its ceiling.
- Preserving one low-cost actor cannot stall the encounter beyond its configured force time.
- Signal remains meaningful without creating a likely soft-lock or uncontrolled positive loop.
- No weapon build is mandatory and no archetype is trivialized by every build.
- Camera yaw remains unchanged throughout entry, lockdown, clear, death, and reset.
- Room A, Room C, doors, reward, withdrawal, and route completion retain authority.
- Required performance, accessibility, test, build, and human-comparison evidence passes.

## 24. Risks and mitigations

| Risk | Consequence | Mitigation |
| --- | --- | --- |
| Attempting exact undocumented parity | Endless reverse engineering and false confidence | Treat observable behavior as a benchmark and retain measured evidence |
| Population increase overwhelms current runtime | Frame spikes, allocations, collision cost | Pooling, spatial partitioning, hard budgets, staged population gates |
| Signal values scale linearly with actors | Instant depletion or unlimited recovery | Dedicated economy profile, shared contact grace, reward caps |
| Visible spawns feel unfair | Unavoidable contact or confusion | Hard exclusion, escape-sector validation, warning period, unique audio |
| Too many new roles arrive together | Unreadable balance failures | Implement and accept one behavior family at a time |
| Existing specialists obscure the baseline | Cannot identify why pacing works or fails | Separate Baseline and Remix profiles |
| Arena cover breaks simple pursuit | Stuck actors and hidden threats | Authored boundary/lane data, oriented collision, stuck recovery |
| High-count effects obscure play | Loss of projectile and escape-lane readability | Aggregate cloud effects, per-class budgets, accessibility variants |
| Feature spreads through the mission | Repetitive rooms and pacing collapse | Room B-only production authority until explicit evidence-based approval |

## 25. Open tuning decisions

These are intentionally unresolved until implementation evidence exists:

- Final production duration and pressure ceilings.
- Whether Room B exposes any score/combo feedback as a diagnostic-only measure or station-fiction presentation.
- Whether the final bounded profile uses a fixed formation sequence or bounded seeded alternatives.
- Exact Signal drains, purge rewards, entry reserve, and recovery beats.
- Whether authored central deflectors remain in the Retro Baseline or only in the Remix.
- Which Flux Sink interactions apply to each evolved weapon.
- Final names, silhouettes, colors, and audio language for new actors.
- Maximum production actor count supported by the target hardware budget.

## 26. Reference basis

- Bizarre Creations described spawning chaos, player-relative enemy movement, and emergent interaction between clearly defined roles as central to Geometry Wars: <https://www.gamespot.com/articles/qanda-bizarre-surveys-geometry-wars-2-aftermath/1100-6196237/>
- Craig Howard described difficulty as emerging from how enemies appear, their AI, and combinations of strong characters: <https://www.nintendo.com/en-gb/News/2007/Interview-Geometry-Wars-Galaxies-Wii-DS--249637.html>
- The developers described minimal scrolling, 60 fps presentation, and unique spawn audio as important to the original experience: <https://www.gamedeveloper.com/game-platforms/the-color-and-the-shape-bizarre-creations-on-i-geowars-i-sensible-aesthetic>
- A contemporary community guide documents observed random, four-corner, ambush-ring, swarm, mixed-corner, and alive-enemy pacing behavior. It is useful empirical evidence, not an authoritative source-code specification: <https://gamefaqs.gamespot.com/xbox360/930851-geometry-wars-retro-evolved/faqs/40235>
- Later series developers reiterated that high-count enemies require simple, clearly anticipated behavior: <https://blog.playstation.com/2015/04/03/creating-a-new-enemy-in-geometry-wars-3-dimensions-evolved/>
