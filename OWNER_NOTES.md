# DEAD SIGNAL — Next Development Pass

Work in order. Do not start a lower-priority package until its blocker is cleared.

## P0 — Make combat spaces readable

### Implementation

- [ ] Reproduce the opening and deep-route foreground obstruction at 1280×720 and 1600×900 with `OpeningLoop` and LiveBalance `FullExtraction`.
- [ ] Update `ForegroundOcclusionController` so authored renderers cut away when they occupy either the camera-to-player corridor or a bounded tactical window around the player.
- [ ] Preserve every `AuthoredMapObstacle` as collision authority; presentation cutaways must never disable collision.
- [ ] Audit all nine `AuthoredForegroundCutaway` bindings in the opening, Spine Induction Gallery, Convergence Chamber, Arc Furnace, and Quench Loop.
- [ ] Bind only the large foreground pieces that obscure the drone, nearby threats, telegraphs, or escape lanes. Keep midground machinery visible.
- [ ] Reuse the existing authored and wide footprint materials. Do not allocate materials or search the hierarchy per frame.
- [ ] Add debug telemetry for the active cutaway reason and renderer count.
- [ ] Add event-timed captures for the opening return, Quench return with active threats, and extraction approach.

### Acceptance

- [ ] The drone, nearest actionable threat or telegraph, and at least one continuous escape lane remain visible in every required capture.
- [ ] No foreground renderer covers the drone or more than 20% of the central tactical window for two consecutive captured frames.
- [ ] Warden, Sapper, Interceptor, and Suppressor telegraphs remain visible when they are inside the tactical window.
- [ ] All 96 authored obstacles remain registered.
- [ ] All nine cutaway bindings retain valid renderers and collision owners.
- [ ] HUD panels stay inside the viewport and do not overlap each other, the drone, or an active threat marker.
- [ ] No Signal costs, rewards, damage, enemy health, enemy count, spawn timing, or run rules change in this package.

### Validation

- [ ] Extend `StationBackdropPlayModeTests` for direct occlusion, wide foreground, and tactical-window cutaways.
- [ ] Prove renderers hide and restore correctly while collision remains enabled.
- [ ] Extend bootstrap coverage for nine valid cutaway bindings, 96 obstacles, and packaged cutaway Resources.
- [ ] Run focused PlayMode tests, full EditMode, full PlayMode, the Windows development build, and packaged smoke.
- [ ] Run matched before/after `OpeningLoop` captures at 1280×720 and 1600×900.
- [ ] Run at least three matched before/after LiveBalance Full Extraction playthroughs.

### Blocker

- [ ] Do not tune weapons, encounters, or enemy pressure until combat visibility passes.

## P0 — Complete combat comparison telemetry

### Implementation

- [ ] Track minimum Signal reserve for the entire run.
- [ ] Report passive, movement, firing, and extraction Signal spend separately.
- [ ] Track shots, weapon hits, piercing follow-throughs, ricochets, chain arcs, and purge efficiency.
- [ ] Attribute damage, drains, and purges to Warden, Sapper, Interceptor, and Suppressor roles.
- [ ] Track peak simultaneous roles, active role combinations, evasion responses, optional-cache decisions, navigation recoveries, and threat no-progress/sticking time.
- [ ] Store route seed, automation profile, selected build, resolution, build identity, report path, and capture paths.
- [ ] Add a matched comparison summary for weapon satisfaction, movement decisions, encounter variety, combat readability, completion pressure, build diversity, replay intent, dominant-build risk, and map-growth pressure.
- [ ] Keep deterministic counters in plain C# and avoid per-frame diagnostic logging.

### Acceptance

- [ ] Every comparison field reports a value or `not observable`.
- [ ] Every hit, drain, and purge can be attributed to a role.
- [ ] Every weapon hit can be attributed to the active weapon behavior.
- [ ] Repeating a deterministic route produces identical deterministic counters.
- [ ] Telemetry adds no allocation-heavy frame path and does not change gameplay.

### Validation

- [ ] Add EditMode tests for every counter and minimum-reserve transition.
- [ ] Add a PlayMode test that records one event from every enemy role, one weapon hit, one purge, traversal spend, and extraction spend.
- [ ] Validate report creation in one packaged deterministic route and one packaged LiveBalance route.

### Blocker

- [ ] Do not authorize a new weapon modifier or enemy behavior until matched reports can detect dominance and lost counterplay.

## P1 — Prove the existing builds create different tactics

### Implementation

- [ ] Add automation profile selection for these four builds:
  - [ ] Chain Arc + Emergency Capacitor + Piercing Pulse.
  - [ ] Chain Arc + Feedback Shield + Controlled Ricochet.
  - [ ] Overdrive Thrusters + Emergency Capacitor + Piercing Pulse.
  - [ ] Overdrive Thrusters + Feedback Shield + Controlled Ricochet.
- [ ] Run matched builds with the same route seed and encounter schedule.
- [ ] Capture the first evolved-weapon hit, densest mixed-role phase, optional-cache decision, and extraction response.
- [ ] Compare weapon hits per shot, Signal per purge, movement and evasion responses, abandoned routes, reserve at each payload, completion, and replay preference.
- [ ] Keep the existing 5-Signal shot cost and weapon-specific extraction responses unchanged during the comparison.

### Combat targets

- [ ] Chain Arc rewards managing clustered threats without erasing target priority.
- [ ] Overdrive makes movement a stronger defense without nullifying Suppressor denial.
- [ ] Piercing rewards lining up screened threats without trivializing Warden protection.
- [ ] Ricochet rewards arena geometry without allowing safe stationary purges.
- [ ] Interceptor displacement breaks aim commitment.
- [ ] Sapper pressure forces rapid target-priority decisions.
- [ ] Optional Quench remains a meaningful greed decision.

### Acceptance

- [ ] At least two builds create measurably different movement or target-selection decisions.
- [ ] No build leads completion, reserve, purge efficiency, and player preference simultaneously.
- [ ] No weapon trivializes an existing enemy role.
- [ ] No result requires higher enemy health, damage, count, or a larger map to compensate for player power.

### Validation

- [ ] Add automated choice/profile coverage.
- [ ] Run four packaged LiveBalance routes per matched seed.
- [ ] Run human keyboard/mouse and controller playthroughs for all four builds.
- [ ] Use headphones and score weapon legibility, firing feedback, hit feedback, purge feedback, movement as defense, tactical-loop length, fun, and replay intent.

### Blocker

- [ ] Do not add another pickup until Piercing, Ricochet, Chain Arc, and Overdrive are readable, tactically distinct, and non-dominant.

## P1 — Add one Geometry Wars–inspired combat advancement only if needed

Complete this package only if the existing-build comparison still shows tactically flat weapon progression.

### Candidate: delayed echo shot

- [ ] Implement one delayed echo 0.35 seconds after a paid shot.
- [ ] Set echo damage to 45% of the normal projectile.
- [ ] Allow only one pending echo.
- [ ] Cancel the echo when its line is blocked or the target leaves normal range.
- [ ] Charge 7 Signal for the triggering volley instead of 5.
- [ ] Make the modifier occupy its run-specific opportunity slot.
- [ ] Reuse existing projectile and VFX assets.
- [ ] Put delay, damage multiplier, additional Signal cost, range rule, and pending limit in designer-facing tuning.
- [ ] Add an immediately legible firing cue, delayed-shot cue, hit cue, HUD label, and telemetry.

### Required counterplay

- [ ] Interceptor displacement can break the delayed line.
- [ ] Suppressor denial makes the extra Signal and positioning commitment risky.
- [ ] Warden screening can intercept the delayed alignment.
- [ ] Sapper pressure can force the player to change targets before the echo resolves.

### Acceptance

- [ ] The modifier improves weapon satisfaction and movement decisions in matched human playtests.
- [ ] The modifier does not win every reserve, completion, purge-efficiency, and preference comparison.
- [ ] The modifier does not trivialize any existing enemy role.
- [ ] The improvement comes from timing and repositioning, not unconditional damage growth.
- [ ] No enemy health, damage, count, arena size, or map size increases are required.
- [ ] Reject or retune the modifier if it becomes the default choice.

### Validation

- [ ] Add pure rule tests for delay, cost, damage, cancellation, range, and pending-shot limits.
- [ ] Add PlayMode coverage for presentation, HUD state, role counterplay, and packaged readiness.
- [ ] Run matched before/after LiveBalance routes across the four existing build profiles.
- [ ] Run human keyboard/mouse and controller comparisons with replay-intent scoring.

## Content-expansion order

Use this order and advance only one step at a time:

- [ ] Improve combinations and encounter composition using Warden, Sapper, Interceptor, and Suppressor.
- [ ] Add one behavioral variant with existing assets only when current roles lack a required movement decision.
- [ ] Improve or repurpose one authored combat arena only when current geometry blocks the desired counterplay.
- [ ] Add one new enemy only when the four existing roles cannot supply the missing pressure.
- [ ] Enlarge the authored level only when current rooms cannot support the desired encounter variety after density, timing, approach-direction, terrain, pressure-phase, and optional-arena experiments.

For every authorized power increase:

- [ ] Define its Signal cost or other downside.
- [ ] Define its enemy counter.
- [ ] Define its positional or opportunity cost.
- [ ] Prove it does not become dominant.
- [ ] Prove it does not force raw enemy-stat inflation or premature map growth.

## Definition of Done

- [ ] Combat spaces preserve readable silhouettes, telegraphs, movement lanes, hit feedback, purge feedback, and HUD hierarchy at 1280×720 and 1600×900.
- [ ] Matched before/after playtests improve weapon satisfaction, movement decisions, encounter variety, combat readability, completion pressure, build diversity, and replay intent.
- [ ] No weapon or build is dominant and no enemy role is trivialized.
- [ ] Full Extraction, optional greed, stable extraction, and Signal as traversal/fire/survival currency remain coherent.
- [ ] Warden screening, Sapper priority pressure, Interceptor displacement, and Suppressor movement denial retain distinct counterplay.
- [ ] Authored-level composition, collision authority, input paths, run outcomes, serialization contracts, and package set regress zero behavior.
- [ ] Focused and full Unity tests, Windows development build, packaged smoke, log scan, and required human keyboard/mouse and controller playthroughs pass.
- [ ] Audio feel, animation feel, human difficulty, fun, and replay intent are directly evaluated rather than inferred.
- [ ] Only one bounded combat or content advancement is authorized for the following pass.
