# DEAD SIGNAL — Presentation Effect Foundation

Audit date: 2026-08-29  
Unity baseline: 6000.3.11f1  
Scope: first-party runtime presentation under `Assets/DeadSignal`, excluding Editor-only debug visualization and persistent environment materials.

## Player benefit and scope cap

This foundation protects combat and objective readability before the next effect pass. The current game already has useful impact, recovery, movement, threat, tower, and low-Signal feedback, but its recurring effects use mixed ownership and several allocate a new runtime object for every event. This audit defines the lifetime, reuse, contrast, and comfort limits that future presentation slices must preserve.

No gameplay timing, collision, damage, Signal economy, encounter population, scene geometry, prefab, material, texture, post-processing, or effect behavior changes in this slice. The next implementation slice is limited to pooled projectile-impact and enemy-purge presentation.

## Current authored and runtime baseline

- Authored effect components: `SignalBoltAssembly.prefab` contains the only prefab-authored `TrailRenderer`. No first-party prefab or scene contains an authored `ParticleSystem`, `LineRenderer`, or rendering `Volume`.
- Runtime-created recurring components: four particle-system paths, five production line/trail paths, sprite impact bursts, one muzzle light, and one camera-offset impulse owner.
- Post-processing: none. Do not introduce a global Volume merely to make an isolated event brighter.
- Generated materials: `DeadSignalPalette` clones authored palette materials once per runtime; `PlayerDroneSignalWake`, `SignalDustController`, and `SignalSapperTelegraph` each own one runtime material and destroy it with their owner. Event-level effects use shared materials and do not create a material per event.
- Accessibility authority: `ComfortSettings` owns Reduced Flashes, Steady Camera (`CameraImpulseEnabled == false`), and High Contrast. Effect code must read this shared service rather than add a second preference.

## Effect inventory and limits

| Event / presentation | Current owner and implementation | Current lifetime / population | Allocation and reuse rule | Contrast and accessibility limit |
|---|---|---|---|---|
| Player bolt trail | `DeadSignalWorld` configures the authored `SignalBoltAssembly` trail from `SignalBoltPresentationTuning` | 0.16 s trail; projectile lifetime 1.5 s | Projectile lifecycle remains authoritative; retain one trail per bolt and do not add a second trail object | Cyan core, 0.86 maximum alpha, transparent tail; never widen beyond projectile/telegraph separation without dense-fire evidence |
| Player muzzle recoil / burst | `PlayerCombatPresentation` moves the authored turret, then creates one particle object and optional point light | recoil 0.11 s; particles 0.045–0.11 s; light 0.075 s | Recoil allocates nothing. Muzzle particles and light are recurring fire-path allocations and must be pooled or removed before promotion work | Reduced Flashes uses 3 rather than 7 particles, caps color alpha at 0.3, and suppresses the point light completely |
| Player dash echo / wake burst | `PlayerCombatPresentation` creates one line and one particle object per dash | line 0.24 s; particles 0.12–0.28 s | Low-frequency but recurring; maximum two simultaneous dash presentations, then reuse the oldest inactive instance | Cyan only; Reduced Flashes caps alpha at 0.3 and uses 8 rather than 16 particles; must not cover the landing lane |
| Continuous drone wake | `PlayerDroneSignalWake` owns two runtime-created trails and one material | owner lifetime; trail duration from movement tuning | Fixed two-trail ownership is acceptable; clear on pause and destroy the material with the owner | Speed-reactive cyan; no flash or camera motion; High Contrast may strengthen separation but may not widen the trail over hazards |
| Enemy / shield / player / wall impact | `CombatFeedbackController` reuses sprite and spark particle pools; decisive hits reserve their two-layer read | sprite 0.22 s; sparks 0.08–0.18 s; camera impulse 0.16 s; hit-stop 0.035/0.06 s | Prewarm 12 sprite slots and 12 spark slots; hard cap 16 of each; reuse the oldest ordinary visual rather than allocate above cap | Enemy hit white/cyan, shield cyan/white, player damage red, Sapper magenta, wall amber/white. Reduced Flashes caps sprite alpha at 0.3 and uses 3 rather than 6 sparks. Steady Camera immediately cancels and restores camera offset |
| Enemy purge reaction | `CombatFeedbackController` combines a priority two-layer impact, pooled target-scale reaction, and priority Signal-recovery burst | impact 0.22 s; scale punch 0.16 s; recovery burst 0.22 s | Shares the impact pool; priority purge and recovery reads replace the oldest ordinary effect under saturation. Reaction records are prewarmed to the threat cap | Purge is larger in silhouette, not whiter than the 0.3 Reduced-Flashes cap; no additional hit-stop or full-screen flash |
| Chain-arc links | `CombatFeedbackController` reuses a bounded `LineRenderer` pool | 0.18 s | Prewarm 6, hard cap 8, reuse the oldest active link at cap; material remains shared and positions update without a temporary array | Cyan with transparent end; Reduced Flashes caps alpha at 0.3; links remain thinner than enemy telegraphs and never become screen-space overlays |
| Signal recovery / salvage chain | `CombatFeedbackController` creates sprite bursts; salvage controller can trigger several sequentially | 0.22 s per burst | Share impact sprite pool with two reserved reward slots; no particles added until allocation capture proves headroom | Recovery white/cyan; salvage amber. Reduced Flashes cap 0.3. Reward feedback must stay world-local and never mask the objective beacon |
| Tower activation sweep | `TowerActivationSweepController` owns one persistent sprite and replays it | 1.2 s, one active sweep | Already reused; do not instantiate per tower | Standard alpha 0.78; Reduced Flashes 0.28. Preserve amber-to-cyan machinery state beneath the sweep |
| Station machinery state transition | `StationStateFeedbackController` reuses the authored transparent state glyph for successful towers, installations, passages, and machinery completions | 0.82 s; four warmed world-space slots | Fixed four-slot pool; saturated events replace the oldest slot without changing the resolved mutation | Amber resolves to cyan; standard alpha 0.62, Reduced Flashes 0.28; transparent center and no camera motion preserve prompts, telegraphs, and escape lanes |
| Low-Signal vignette | `LowSignalWarningController` owns an authored HUD image | persistent only below 30 Signal; live pulse 2.4 rad/s | Fixed owner, no recurring allocation | Standard alpha 0.09–0.18; Reduced Flashes uses a steady 0.1. Keep the screen center and objective/prompt regions unobscured |
| Signal dust | `SignalDustController` owns one runtime particle system and material | owner lifetime; 56-particle cap; 1.5 dead / 5 powered particles per second; 2.8–5.2 s lifetime | Fixed field, no event allocation; retain the 56-particle cap | Cyan powered / subdued dead-zone tint; High Contrast may change tint only. Dust stays background-depth and must not resemble projectiles |
| Sapper, Warden, Suppressor, Interceptor, and reinforcement telegraphs | Focused telegraph components and `DeadSignalWorld` own persistent renderers/lines and toggle state | driven by attack warning/cooldown authority | Fixed owner renderers are acceptable; no per-frame or per-pulse object creation | Red/magenta danger remains distinct from cyan player fire and amber objectives. Reduced Flashes must suppress optional pulse flashes while preserving timing geometry |
| Product-shell fades and terminal outcomes | `DeadSignalShellController` and `DeadSignalHud` use authored `CanvasGroup` opacity | 0.18 s standard / 0.28 s Reduced Flashes | Fixed authored groups; unscaled update; no allocation during transition | No camera motion, no full-screen white flash, and focus/raycast ownership must remain deterministic |

## Cross-effect contract

1. Gameplay authority stays outside effect code. Effects may observe a resolved hit, purge, reward, objective, or state change; they may not decide collision, damage, reward, timing, door state, or progression.
2. Recurring effects are pooled. After warmup, ordinary continuous fire and impact handling must not create `GameObject`, `Component`, `Material`, `Sprite`, array, or collection-growth allocations.
3. Pool exhaustion degrades presentation only. Reuse the oldest non-priority visual or skip a minor spark; never delay or drop gameplay events.
4. World effects stay below HUD sorting and do not occupy screen-space prompt, objective, projectile, telegraph, or escape-lane authority.
5. Reduced Flashes must cap short bright world effects at 0.3 alpha, reduce particle counts by at least half where particles remain, remove optional point lights, and replace repeated pulses with steady or slower luminance changes.
6. Steady Camera disables every camera offset immediately and restores the follow camera's rest position in the same frame. No effect may own a second camera transform writer.
7. High Contrast changes palette separation, not duration, scale, population, or gameplay state.
8. Pause, menu, defeat, victory, restart, and scene teardown must stop or clear transient effects and return pooled objects inactive. One runtime owns one pool set.
9. No effect may exceed 1.2 seconds unless it represents a persistent gameplay state. Persistent state effects must be fixed-owner renderers, not looping spawned objects.
10. Add no post-processing until a named event cannot meet readability with world/UI presentation and a Reduced-Flashes alternative is proven in the same slice.

## Validation gates for the next slice

The projectile-impact and enemy-purge slice is complete only when:

- enemy, shield, wall, player-damage, and purge reads remain visually distinct without changing projectile collision or damage;
- impact sprites, sparks, and chain links reuse bounded pools after warmup and cleanly reset on pause/restart/menu;
- a focused test proves pool caps, reuse, and one-runtime ownership;
- allocation capture under continuous fire and the existing maximum population shows no recurring managed allocation from the pooled impact family after warmup;
- Reduced Flashes on/off and Steady Camera on/off retain threat silhouettes, projectile paths, prompts, and one visible escape lane at 1280x720 and 1600x900;
- relevant focused PlayMode tests and the existing combat presentation smoke path pass; subjective impact quality remains a human acceptance check.

## Implementation result — Run 171

The bounded pooling gate is complete. `CombatFeedbackTuning` now owns prewarm counts, hard caps, lifetimes, and the Reduced-Flashes alpha ceiling. Impact sprites, directional sparks, chain links, and purge scale reactions are created during warmup, reused under pressure, cleared on pause, and replaced with one fresh owner on restart. Saturation preserves priority purge and reward cues by replacing the oldest ordinary visual; gameplay collision, damage, hit-stop, camera-comfort authority, and effect colors remain unchanged.

Focused Unity evidence proved the 12 → 16 impact/spark pools, 6 → 8 chain pool, zero new Unity objects and zero managed bytes across 64 warmed continuous bulkhead-impact calls, distinct shield/red/magenta/amber reads, the 0.3 Reduced-Flashes ceiling, cleanup, and one-runtime restart ownership. The existing complete presentation smoke passed beside the new pool test. Human comparison at 1280×720 and 1600×900 remains required for silhouette hierarchy, comfort, and escape-lane visibility; automation does not establish satisfying impact feel.

## Deferred observations

- Player muzzle, dash, and chain-arc allocations are documented risks, but they are not part of the next slice unless the shared impact pool can absorb them without broadening ownership.
- Persistent machinery/door state readability belongs to the later room/act audit, not this effect foundation.
- Human comparison is still required for flash comfort, silhouette hierarchy, and whether purges feel satisfying. Automation can prove limits, cleanup, allocations, and rendered visibility only.

## Implementation result — Run 172

Resolved player damage now drives one fixed-owner HUD chevron at the relevant screen edge. Warden, Interceptor, Swarmer, and Suppressor damage use the established red danger language; Sapper drain uses magenta. Shielded or debug-invulnerable contacts do not trigger the damage cue. The two simple authored UI rails allocate nothing per event, remain outside the center and prompt regions, fade within 0.52 seconds, cap at 0.48 alpha, and use a steady 0.24 Reduced-Flashes cap. Pause, menu ownership, terminal outcomes, disable, and restart clear or replace the presentation without camera motion, so Steady Camera remains authoritative.

The existing low-Signal vignette now shares `ScreenFeedbackTuning` and gains an explicit critical tier at 25 percent reserve. Its normal maximum rises only from 0.14 warning alpha to 0.2 at failure proximity; Reduced Flashes remains a steady 0.1 ceiling. Focused Unity evidence proved tuning bounds, direction resolution, authored prefab bindings, pause cleanup, the critical reserve state, compatibility with pooled impacts, and complete runtime bootstrap. Human comparison at 1280×720, 1600×900, and ultrawide remains required to prove that the edge chevron and critical vignette stay readable without hiding threats, prompts, or one escape lane.

## Implementation result — Run 173

Resolved tower activations, payload installations, route openings, and established machinery completions now share one world-local amber-to-cyan state-transition language. `StationStateFeedbackController` prewarms four sprite owners from the authored transparent `MachineryStateTransitionGlyph`, reuses them under saturation, and clears every active glyph on pause, disable, restart, or scene teardown. `StationStateFeedbackTuning` keeps the effect at `0.82s`, caps standard alpha at `0.62`, caps Reduced Flashes at `0.28`, and scales the same centered glyph by event class so towers read wider than local machinery without becoming a screen-space flash.

The presenter observes only successful mutations already resolved by `RunModel` and `DeadSignalWorld`; it cannot open a gate, advance an objective, spend Signal, change encounter timing, or move the camera. Focused Unity evidence proved the amber-to-cyan transition, fixed four-slot saturation, authored texture/tuning availability, pause cleanup, tower event wiring, and complete runtime bootstrap. Human comparison at 1280×720, 1600×900, and ultrawide remains required to confirm that each ring stays subordinate to prompts, telegraphs, projectiles, and at least one visible escape lane.

## Implementation result — Run 174

The established four-slot station-state pool now gives the Security Trial and its existing rewards a bounded event grammar without adding another effect owner or bitmap. Lockdown entry expands red to amber at the authored arena focus; each successful phase transition reverses amber to red; room clear resolves red to cyan while the newly exposed capacitor receives a separate amber-to-recovery-cyan release. Required and optional salvage collection and the mission-critical capacitor collection use the same recovery-cyan read at their world position. Every cue retains the existing `0.82s` lifetime, `0.62` standard alpha, `0.28` Reduced-Flashes ceiling, transparent center, world-space ownership, four-slot saturation behavior, pause cleanup, and zero camera motion.

The presenter continues to observe only successful existing state changes. It does not begin or advance a phase, spawn or purge a threat, release a door, award Signal, collect a payload, or complete an objective. Focused Unity evidence preserved the three-phase sequence, five-threat peak, 11-spawn/11-purge contract, both door releases, reward availability, and exact reward collection while proving distinct lockdown, phase, clear, reward-release, and recovery emissions. Human comparison at 1280×720, 1600×900, and ultrawide remains required to judge comfort, event hierarchy, and escape-lane visibility under live combat.
