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
- [ ] Playtest fight and flight extractions for ammunition cost, time saved, live threat mix, and final Signal reserve

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

- [x] Warden recovery bounty — player value: fighting the pursuer can offset most of its ammunition cost; acceptance: purging it restores up to 12 Signal exactly once.
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
