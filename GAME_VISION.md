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
- Player shots use a reusable two-part, UV-mapped maintenance-pulse prefab with original white-ceramic/cyan Signal art while cost, speed, lifetime, and hit rules remain unchanged.
- Each maintenance pulse leaves a brief authored cyan circuit afterimage, making shot direction and speed readable without obscuring enemies or changing projectile rules.
- The pursuing Security Warden uses three purpose-built, UV-mapped low-poly meshes with original graphite/crimson armor art while threat rules remain unchanged.
- The tower-draining Signal Sapper uses four purpose-built, UV-mapped low-poly meshes with original black-violet/magenta siphon art while drain rules remain unchanged.
- Dead zones are visually distinct, show a warning, and drain Signal quickly.
- Tower activation wakes two readable threats: a red Warden pursues the drone while a magenta Signal Sapper telegraphs its tower target and timed drain pulses; both can be destroyed.
- The Warden reveals an authored red strike-range glyph only at close proximity, escalating pursuit into a readable contact threat while Reduced Flashes preserves a static warning.
- The dormant Warden waits inside a scene-placed red security bay whose open mouth foreshadows its activation and whose shields create immediate post-activation kiting cover without trapping the threat.
- The dormant Sapper waits inside a scene-placed magenta service cradle whose southeast opening foreshadows its tower-bound emergence and whose pylons become post-activation combat cover without trapping it.
- The Sapper's successful drain uses an authored inward-pulling floor glyph whose expansion remains optional under Reduced Flashes while timing stays readable through the countdown reticle.
- The Sapper's target tether carries repeated authored energy packets toward the tower during approach and latch, making drain direction readable without changing threat timing.
- Three warm salvage pickups can be collected outside the safe starting area.
- Returning to the extraction pad with all salvage and pressing E wins.
- A central powered gate offers a readable choice: spend Signal for a direct salvage route or detour through the dead zone.
- Signal depletion causes death; victory/death clearly present a restart action.
- Victory and death report time, danger exposure, combat usage, damage, and remaining Signal.
- A readable HUD communicates Signal, salvage, objective, zone state, controls, and contextual prompts.
- Keyboard/mouse and gamepad can each complete the full run without switching devices.
- Escape or gamepad Menu pauses the active run without advancing Signal drain, threats, projectiles, or run time.
- Successful bolt hits, Warden impacts, and Sapper drains provide distinct world-space bursts, brief hit-stop, and restrained camera impulse.
- The pause overlay offers a persisted Steady Camera option that removes camera impulse without weakening hit-stop or impact art.
- The pause overlay offers a persisted Reduced Flashes option that softens impact bursts and removes the Sapper's expanding pulse flash while preserving combat timing and countdown readability.
- The pause overlay offers a persisted High Contrast option that immediately separates Signal, salvage, and threats with brighter world materials and clearer HUD values without changing gameplay.
- A directional objective beacon always identifies the tower, nearest remaining salvage, or extraction target with live distance.
- Control legends, contextual interactions, pause options, and restart guidance immediately follow the player's latest keyboard/mouse or gamepad input.
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

## Experience target

The first minute should teach the entire promise without a tutorial panel: leave the small powered dock, feel the dead-zone drain, ignite the tower, see the station turn cyan and the security unit wake, spend Signal to survive, sweep the outskirts for gold salvage, and retreat to extraction under pressure.
