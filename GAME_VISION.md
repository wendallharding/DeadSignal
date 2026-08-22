# DEAD SIGNAL — Game Vision

## Commercial pitch

**DEAD SIGNAL** is a compact top-down action roguelite about pushing a fragile network through a dying orbital station. You are a maintenance drone whose movement, weapons, and machinery all draw from one dwindling Signal reserve. Signal towers turn darkness into safe, luminous territory—but every restored node wakes more of the station's security. Leave the network to recover rare salvage, then make it home before your power, health, and options all disappear together.

The hook is a readable risk loop: **power territory, provoke danger, raid the dark, extract before Signal runs out**. A finished run should fit into 15–25 minutes, with fast restarts and route-changing upgrades.

## Design pillars

1. **One resource, tense decisions.** Signal is mobility, ammunition, machinery access, and life support. Every meaningful action has an opportunity cost.
2. **Safety changes the map.** Activated towers create unmistakable cyan territory and new tactical footholds while escalating security pressure.
3. **The dark is lucrative.** Warm-gold salvage lives beyond the network, where passive Signal loss creates a strict expedition clock.
4. **Miniature machine drama.** Chunky geometric machinery, crisp silhouettes, dark station decking, cyan network light, amber rewards, and red threats make state readable at a glance.
5. **Short runs, immediate mastery.** Responsive controls, clear failures, rapid restarts, and deterministic core rules support learning without friction.

## MVP scope

The commercial MVP expands the first playable into 15–25 minute runs containing:

- One station biome assembled from modular rooms and alternate routes.
- Three tower choices per run; powering territory also increases threat intensity.
- Four enemy archetypes and one station guardian.
- Six drone tools sharing the Signal economy.
- Salvage-driven upgrades, a small permanent unlock track, and route decisions.
- Keyboard/mouse and controller support, options, pause, audio, onboarding, and save data.

Out of scope until the loop proves fun: online features, co-op, procedural narrative, multiple biomes, voice acting, and live-service systems.

## First playable acceptance criteria

This autonomous run is accepted when a fresh Play session provides all of the following through a gradually scene-authored modular map and focused runtime systems:

- WASD movement and mouse aim feel immediate; left click or Space fires.
- Movement in dead zones, attacks, tower activation, and enemy impacts visibly consume Signal.
- One nearby tower can be activated with E, replenishes Signal, and produces obvious cyan powered territory.
- Tower activation launches a cyan circuit sweep from the tower to the powered boundary, visibly selling the network's expansion while respecting pause and Reduced Flashes.
- The station deck is assembled from reusable authored floor modules carrying original dark-alloy plating art while powered/dead-zone overlays remain readable.
- The playable room perimeter is an authored reusable shell with textured bulkheads and explicit machine sockets rather than hard-coded wall and prop placement.
- A closer tactical camera follows the maintenance drone with restrained movement look-ahead while clamping to authored arena edges, keeping navigation readable now and supporting future modular room expansion.
- The central tower approach is a scene-placed modular junction whose authored coolant-manifold obstacles create distinct safe and exposed lanes for the player and awakened threats.
- The extraction dock opens into a scene-placed capacitor channel aligned toward the tower, creating a readable first movement lane and a clear powered-to-dead-zone threshold.
- Five scene-authored illuminated floor inlays form a continuous Signal spine from extraction toward the tower, making the first objective readable from the environment without requiring the HUD.
- A scene-authored cyan-to-amber floor threshold marks the exact opening-route edge of extraction's powered field, teaching the safe-territory boundary before Signal begins draining.
- The northeast salvage cache sits inside a scene-placed cargo annex whose single readable entrance turns an optional reward into a positioning commitment without moving the objective.
- The southeast salvage cache sits inside a scene-placed coolant reclamation gauntlet whose staggered baffles create a pressured collection lane without moving the objective.
- The northwest salvage cache sits beyond a scene-placed relay fork whose tight central throat and longer outside approaches create a readable route decision without moving the objective.
- An optional fourth salvage cache sits in a scene-placed east vault beyond the original room boundary; players still need only three, turning salvage recovery into a route-selection decision rather than a fixed checklist.
- Authored cover, room walls, and the closed shortcut gate intercept Signal bolts with a brief cyan impact flash, so combat sightlines obey the same spatial rules as movement while an opened gate remains a valid firing lane.
- The central Signal tower is a reusable authored assembly with original control-panel housing art while its dormant/online state remains unmistakable.
- The extraction dock is a reusable authored assembly with original radial docking art while its safe-home and final-objective read remains unmistakable.
- The optional Signal-cost shortcut is a reusable authored assembly with original powered-lock art while both free detours and its closed/open state remain unmistakable.
- The powered network's floor routing is a reusable authored assembly with original cyan conduit art while its dormant/online state remains unmistakable.
- Six socket-driven station machines use a reusable authored console assembly with original dark-alloy control-surface art and alternating readable status lights.
- Three salvage objectives use a reusable authored cache assembly with original amber containment art while collection and beacon guidance remain unchanged.
- The maintenance drone uses four purpose-built, UV-mapped low-poly meshes with original white-ceramic Signal art while movement, aim, and firing remain unchanged.
- The maintenance drone leaves a short twin cyan Signal wake whose intensity follows actual flight speed, making acceleration and coasting readable without changing movement rules.
- Player shots use a reusable two-part, UV-mapped maintenance-pulse prefab with original white-ceramic/cyan Signal art while cost, speed, lifetime, and hit rules remain unchanged.
- Each maintenance pulse leaves a brief authored cyan circuit afterimage, making shot direction and speed readable without obscuring enemies or changing projectile rules.
- The pursuing Security Warden uses three purpose-built, UV-mapped low-poly meshes with original graphite/crimson armor art while threat rules remain unchanged.
- The tower-draining Signal Sapper uses four purpose-built, UV-mapped low-poly meshes with original black-violet/magenta siphon art while drain rules remain unchanged.
- Dead zones are visually distinct, show a warning, and drain Signal quickly.
- Tower activation wakes two readable threats: a red Warden pursues the drone while a magenta Signal Sapper telegraphs its tower target and timed drain pulses; both can be destroyed.
- Purging the Warden reclaims up to 12 Signal and purging the Sapper reclaims up to 16, with live health/bounty telemetry, cap-safe deterministic restoration, and an authored cyan recovery burst.
- The Warden reveals an authored red strike-range glyph only at close proximity, escalating pursuit into a readable contact threat while Reduced Flashes preserves a static warning.
- The dormant Warden waits inside a scene-placed red security bay whose open mouth foreshadows its activation and whose shields create immediate post-activation kiting cover without trapping the threat.
- The dormant Sapper waits inside a scene-placed magenta service cradle whose southeast opening foreshadows its tower-bound emergence and whose pylons become post-activation combat cover without trapping it.
- The Sapper's successful drain uses an authored inward-pulling floor glyph whose expansion remains optional under Reduced Flashes while timing stays readable through the countdown reticle.
- The Sapper's target tether carries repeated authored energy packets toward the tower during approach and latch, making drain direction readable without changing threat timing.
- The first salvage escalation deploys a red-and-amber Interceptor from the safer of two scene-authored flank gates; it moves toward the extraction route, shows a locked charge line, and commits to an avoidable dash that combines with Warden pursuit and Sapper tower pressure.
- Three warm salvage pickups can be collected outside the safe starting area.
- The first salvage cache offers one run-long combat overclock: Chain Arc rewards grouping tactical roles, while Overdrive Thrusters rewards evasion and route greed.
- Returning to the extraction pad with all salvage starts a six-second mobile uplink; surviving the final bounded security pursuit completes the run.
- A central powered gate offers a readable choice: spend Signal for a direct salvage route or detour through the dead zone.
- Signal depletion causes death; victory/death clearly present a restart action.
- Victory and death report time, danger exposure, combat usage, damage, and remaining Signal.
- A readable HUD communicates Signal, salvage, objective, zone state, controls, and contextual prompts.
- A three-phase mission command strip names the next action, previews the tower Signal transaction, counts remaining salvage, and surfaces urgent Sapper drains without obscuring the extraction goal.
- Keyboard/mouse and gamepad can each complete the full run without switching devices.
- Escape or gamepad Menu pauses the active run without advancing Signal drain, threats, projectiles, or run time.
- Successful bolt hits, Warden impacts, and Sapper drains provide distinct world-space bursts, brief hit-stop, and restrained camera impulse.
- The pause overlay offers a persisted Steady Camera option that removes camera impulse without weakening hit-stop or impact art.
- The pause overlay offers a persisted Reduced Flashes option that softens impact bursts and removes the Sapper's expanding pulse flash while preserving combat timing and countdown readability.
- The pause overlay offers a persisted High Contrast option that immediately separates Signal, salvage, and threats with brighter world materials and clearer HUD values without changing gameplay.
- A directional objective beacon always identifies the tower, nearest remaining salvage, or extraction target with live distance.
- Control legends, contextual interactions, pause options, and restart guidance immediately follow the player's latest keyboard/mouse or gamepad input.
- The pause overlay lets keyboard players reroute Fire and Use to any key, persists those choices between sessions, and immediately updates every relevant prompt while fixed gamepad controls remain available.
- Primary keyboard rerouting rejects duplicate Fire/Use assignments, keeps the previous valid route active, and explains the conflict without leaving the pause menu.
- Move Up, Down, Left, and Right can each be rerouted independently from pause, persist across launches, update the HUD immediately, preserve arrow-key and controller fallbacks, and reject conflicts across all six primary keyboard actions.
- Five original control-family glyphs make movement, aim, fire, use, and system actions scannable at a glance while the accompanying labels continue to adapt to the latest keyboard/mouse or gamepad input.
- The Canvas Signal reserve uses original conduit art plus Stable, Strained, and Critical states; critical motion freezes with pause and becomes static under Reduced Flashes.
- Original synthesized machinery ambience shifts between dead zones and powered territory, key actions have distinct cues, and a persisted pause option can mute all audio.
- A bounded field of original Signal-dust motes becomes denser and brighter in powered territory, freezes with pause, and stays sparse in the dead zone.
- Below 30 Signal, an original amber-red screen-edge warning intensifies toward failure, stays clear of the playfield center, and respects pause, outcomes, and Reduced Flashes.
- A branded 64-bit Windows development build launches the complete runtime outside the Editor and provides an automated packaged-player health check.
- Core resource/objective transitions have deterministic EditMode tests, and the Unity project compiles in batch mode.

The salvage-area layout deliberately gives each cache a different spatial verb: commit through the annex entrance, thread the coolant lane, then choose direct or wide at the relay fork. This was selected over another enclosure or a purely decorative landmark because variety in navigation decisions adds more replay value than repeating the same room pattern or merely enlarging the arena.

The first threat encounter is anchored by the Warden bay rather than another objective room: visible containment architecture promises danger before tower activation, then converts into combat cover. A comfortably wide west entrance now joins the bay approach to the northeast salvage annex, with large cyan floor chevrons distinguishing the onward route from the bay's combat pocket. This was selected over a Sapper cradle or shortcut checkpoint because the Warden is the player's most immediate post-activation threat and therefore gives the landmark the clearest first-minute payoff.

The second threat is now anchored by a Sapper service cradle at the northwest edge: an L-shaped pair of magenta siphon pylons makes the dormant saboteur legible before tower activation, then leaves it a clear southeast emergence path and gives the player cover if combat returns to that corner. This was selected over a central debris pinch or southwest optional pocket because it turns an existing open-floor enemy spawn into anticipation, navigation, and tactical positioning without adding a new mechanic or objective.

The camera now prioritizes local tactical readability over displaying the entire station at once: a tunable follow rig frames the drone at roughly 1.8 times the previous scale, adds gentle movement-direction look-ahead, and clamps so the current arena never exposes empty space beyond its authored edges. This was selected before another room expansion because new geometry would remain hard to read at the old full-map scale; a static room landmark would not solve that foundation, while an immediate additive-scene conversion would add production architecture without improving the current minute-to-minute play experience.

The first true room extension is an optional east salvage vault connected through a guarded opening in the original shell. Its copper route splitter offers two internal lanes, its amber lock lighting distinguishes it from the central station, and its fourth cache lets the player skip one of four salvage routes while still meeting the three-cache extraction requirement. This was selected over a mandatory corridor because route choice creates replay value, and over a new hazard because the established dead-zone clock, Warden pressure, and salvage economy already supply risk without another system.

Combat now respects that authored layout: every registered object-aligned movement blocker also stops the player's fast Signal bolts, including rotated shields, pylons, room walls, and the closed shortcut gate. A short original cyan impact bloom confirms the interception without adding hit-stop or camera impulse. This was selected over another room, reward, or enemy because the expanding map's cover choices were misleading while projectiles ignored them; shared spatial rules make existing encounter spaces tactical before more content is added.

The opening route now carries an authored Signal spine: five large cyan maintenance inlays advance continuously from the extraction dock toward the central tower without collision or gameplay authority. This was selected over another HUD prompt because the first objective should be understandable from the station itself, and over a new branch because clear environmental navigation is the prerequisite for making later route choices feel intentional.

The opening departure channel now embeds an authored Signal boundary threshold at the extraction field's gameplay radius. Its cyan maintenance side faces safety while amber hazard plating faces the dead zone, previewing the shared-resource risk before the HUD reports drain. This was selected over another room prop because the player's first boundary crossing is the earliest teachable expression of DEAD SIGNAL's core hook, and it improves comprehension without adding rules, collision, or route padding.

Primary keyboard controls now use persistent Input System actions rather than fixed polling: the pause overlay exposes Fire and Use as readable control-routing buttons, listens for the next key across connected keyboards, lets Escape cancel safely, and updates live HUD prompts after reassignment. This was selected before more content because input ownership is a commercial-readiness requirement, and a focused primary-action pass proves the persistence and UI pattern without risking movement, aim, controller support, or gameplay balance.

The same control-routing panel now provides one-click recovery to the documented Space Fire and E Use defaults. Reset cancels any active key capture, removes both saved override paths, and refreshes the visible prompts immediately. This was selected over expanding the remapping surface because customization needs a trustworthy escape hatch before more actions and device glyph families add complexity.

Primary control routing now prevents Fire and Use from sharing one keyboard key. A rejected route keeps listening, preserves the existing binding and persisted preference, and replaces the routing emblem with an amber-red conflict indicator until the player chooses another key or cancels. This was selected before movement remapping because a larger binding surface would multiply silent conflicts; the routing system needs a clear invariant first.

Responsive drone flight now carries a short twin Signal wake driven by resolved speed. This was selected over another movement mechanic because the retained-velocity model needs immediate visual confirmation before additional abilities complicate handling; the wake strengthens the miniature-machine fantasy without consuming Signal or changing collision, acceleration, or braking.

The adaptive control legend now leads with a five-glyph visual language for movement, aim, fire, use, and system actions. This was selected before more content because the complete input-routing surface had become dense and text-heavy; persistent, device-neutral action silhouettes improve first-minute scanning while live keyboard and gamepad labels retain exact control authority.

The shared Signal reserve now reads as operational telemetry rather than a generic progress bar: original conduit art moves from cyan Stable through amber Strained to red Critical, names the state in plain language, and uses restrained critical motion only when the player allows it. This was selected over a new objective because Signal is the game's differentiating resource and must communicate urgency instantly in the newly authored Canvas without requiring players to infer meaning from a number alone.

## Experience target

### Run 60 product decision — salvage chain momentum

Consecutive cache recoveries now form a 12-second chain that pays 4 Signal on the second cache and 8 on the third. This turns the authored route network into a readable tempo decision without making deliberate exploration invalid: the first cache remains safe, chain rewards are capped by the existing 100-Signal authority, and the HUD exposes the remaining window. Best chain and actual recovery appear in the debrief so future tuning can use play evidence. This was selected over another room or enemy because the current map already offers route choice but lacked a positive incentive to execute a fast line.

### Run 59 product decision — security-purge recovery

Destroyed threats now return part of the shared resource they forced the player to risk: the Warden offsets most of its minimum ammunition cost, while the time-critical Sapper is deliberately net-positive if intercepted cleanly. This was selected over another room or HUD-only pass because combat previously consumed Signal without creating an economic reason to engage. The asymmetric 12/16 rewards preserve avoidance as a valid Warden choice while making a fast Sapper purge an active recovery tactic. Enemy health, bounty values, actual reclaimed Signal, and purge counts are visible and deterministic; all prior enemy behavior is preserved in a new designer-facing tuning asset.

### Run 58 product decision — mission command strip

The live objective area now behaves like compact operational guidance rather than a generic quest label. It exposes the complete three-step run structure, names the next physical action, previews the opening tower's exact Signal exchange, removes mental subtraction from salvage routing, and interrupts routine advice with the live Sapper drain countdown. This was selected over another room or combat rule because first-minute understanding remains the prerequisite for evaluating the established economy and authored routes. The evaluator is deterministic and presentation-only; it changes no costs, timing, enemy behavior, objectives, scene layout, or input.

### Run 57 product decision — actionable mission debrief

The outcome screen now converts telemetry into five concise readings: overall grade, Signal reserve quality, combat discipline, dead-zone exposure, and shortcut-versus-conservation route. This preserves the established extraction loop while making improvement goals immediately understandable and giving repeated runs a light mastery target. The grade is intentionally local and deterministic; it adds no grind, account system, or balance change.

The first minute should teach the entire promise without a tutorial panel: leave the small powered dock, feel the dead-zone drain, ignite the tower, see the station turn cyan and the security unit wake, spend Signal to survive, sweep the outskirts for gold salvage, and retreat to extraction under pressure.

### Run 61 product decision — bounded security escalation

Salvage progress now raises a deterministic three-tier security alert instead of leaving the opening pair as the run's entire combat budget. Each required cache banks one bounded reinforcement, but a role can enter only when its current unit is absent, the player has left a six-metre exclusion around its authored entrance, and a 2.5-second warning window elapses. This was selected over adding a larger map or simply scaling enemy stats because the current arena needs repeated mixed-role pressure and a dangerous return leg. The fixed three-unit reserve keeps pressure legible and bounded while preserving avoidance: players who refuse a purge do not receive an unavoidable duplicate of that role.

### Run 62 product decision — flanking Interceptor

The first salvage escalation now introduces an Interceptor before the existing Warden and Sapper reserves. It selects the farther of two scene-authored edge gates, advances toward a point between the drone and extraction, locks a visible line for 0.8 seconds, and then commits to a short collision-bounded dash. This was selected over increasing enemy health, speed, or count because the return leg needed a threat that changes route and dodge decisions while combining cleanly with direct pursuit and tower denial. Three health and a 14-Signal purge bounty keep fighting economically competitive without making a clean purge profitable after its minimum shot cost.

### Run 63 product decision — extraction pursuit uplink

Extraction now begins a six-second mobile uplink instead of granting instant victory. The player keeps full movement and combat control while one additional bounded security response enters the existing queue, retaining role uniqueness, authored entrances, safe-entry distance, and warning time. This was selected over a stationary capture circle because the climax should reward fighting, fleeing, and route use rather than trap the drone at one point, and over raw stat escalation because one readable tactical response creates pressure without invalidating learned counterplay.

### Run 64 product decision — first-cache Signal overclock

The first secured cache now offers one run-long choice while threats and movement remain live. Chain Arc jumps each successful bolt to one nearby secondary role, rewarding deliberate mixed-enemy alignment; Overdrive Thrusters raises speed and acceleration, rewarding dodges, retreat routing, and dead-zone greed. Fire and Use select the two branches through existing keyboard/controller routes, and the choice input cannot also fire or operate machinery. This was selected over random drops or a passive stat reward because the current short run needs an early, legible build fork that changes fighting versus fleeing without diluting the shared Signal economy.
