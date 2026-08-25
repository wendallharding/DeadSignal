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
- Shift / gamepad south performs a short four-Signal dash with a visible cooldown, creating deliberate telegraph and route counterplay.
- Objective and emergency guidance route through reachable authored-obstacle detours, while pause exposes a compact tactical map of safe nodes, caches, threats, and the next corridor turn.
- Movement in dead zones, attacks, tower activation, and enemy impacts visibly consume Signal.
- One nearby tower can be activated with E, replenishes Signal, and produces obvious cyan powered territory.
- Tower activation launches a cyan circuit sweep from the tower to the powered boundary, visibly selling the network's expansion while respecting pause and Reduced Flashes.
- The station deck is assembled from reusable authored floor modules carrying original dark-alloy plating art while powered/dead-zone overlays remain readable.
- The playable room perimeter is an authored reusable shell with textured bulkheads and explicit machine sockets rather than hard-coded wall and prop placement.
- A high-angle perspective tactical camera follows the maintenance drone with restrained movement look-ahead while clamping its visible deck footprint to authored arena edges, preserving navigation readability while revealing the station's modeled depth.
- The central tower approach is a scene-placed modular junction whose authored coolant-manifold obstacles create distinct safe and exposed lanes for the player and awakened threats.
- The extraction dock opens into a scene-placed capacitor channel aligned toward the tower, creating a readable first movement lane and a clear powered-to-dead-zone threshold.
- Five scene-authored illuminated floor inlays form a continuous Signal spine from extraction toward the tower, making the first objective readable from the environment without requiring the HUD.
- A scene-authored cyan-to-amber floor threshold marks the exact opening-route edge of extraction's powered field, teaching the safe-territory boundary before Signal begins draining.
- The northeast salvage cache sits inside a scene-placed cargo annex whose single readable entrance turns an optional reward into a positioning commitment without moving the objective.
- The southeast salvage cache sits inside a scene-placed coolant reclamation gauntlet whose staggered baffles create a pressured collection lane without moving the objective.
- The northwest salvage cache sits beyond a scene-placed relay fork whose tight central throat and longer outside approaches create a readable route decision without moving the objective.
- An optional fourth salvage cache sits at the far end of the scene-authored Capacitor Spine beyond the east transfer vault and Relay Foundry; players still need only three, turning the extended expedition into a route-selection decision rather than a fixed checklist. After extraction becomes ready, the unchosen cache remains a disclosed greed route that restores up to 18 Signal without adding another security tier.
- Authored cover, room walls, and the closed shortcut gate intercept Signal bolts with a brief cyan impact flash, so combat sightlines obey the same spatial rules as movement while an opened gate remains a valid firing lane.
- The central Signal tower is a reusable authored assembly with original control-panel housing art while its dormant/online state remains unmistakable.
- The extraction dock is a reusable authored assembly with original radial docking art while its safe-home and final-objective read remains unmistakable.
- The optional Signal-cost shortcut is a reusable authored assembly with original powered-lock art while both free detours and its closed/open state remain unmistakable.
- Beyond the east vault, a scene-authored Relay Foundry adds a second activatable Signal tower, two turbine-side approaches, a distinctive induction landmark, two safe reinforcement gates, and an activation-opened return bulkhead. Its 16-by-14-metre footprint expands the original arena by roughly 32 percent without changing the first tower's rules.
- Beyond the Relay Foundry, a scene-authored Capacitor Spine adds two east approaches around a projectile-blocking transfer bank, a protected cover lane, an exposed greed lane, the relocated optional cache, and a dormant third-tower berth. The module extends the meaningful outward and return journey without changing required salvage, enemy stats, reinforcement count, or Signal rewards.
- North of the Spine, a two-doorway Induction Gallery now opens into a 14-by-8-metre Convergence Chamber. A purpose-built busbar and rotated baffles create a deep cover loop; one far-side security gate changes reinforcement direction after the player withdraws, while Spine power turns both rooms into a safer return network without increasing the response cap.
- A compact west-side Flux Bypass links the Induction Gallery directly to the Convergence Chamber. Its narrow shunt lane and angled cover avoid the chamber's central kill line outbound, then become a powered return flank after Spine activation without adding another reward, objective, or security response.
- The powered network's floor routing is a reusable authored assembly with original cyan conduit art while its dormant/online state remains unmistakable.
- Six socket-driven station machines use a reusable authored console assembly with original dark-alloy control-surface art and alternating readable status lights.
- Three salvage objectives use a reusable authored cache assembly with original amber containment art while collection and beacon guidance remain unchanged.
- The maintenance drone uses four purpose-built, UV-mapped low-poly meshes with original white-ceramic Signal art; its chassis faces resolved travel while the stabilized core/tool turret independently follows aim, making strafing and retained momentum readable without changing movement or firing rules.
- The maintenance drone leaves a short twin cyan Signal wake whose intensity follows actual flight speed, making acceleration and coasting readable without changing movement rules.
- Player shots use a reusable two-part, UV-mapped maintenance-pulse prefab with original white-ceramic/cyan Signal art while cost, speed, lifetime, and hit rules remain unchanged.
- Each maintenance pulse leaves a brief authored cyan circuit afterimage, making shot direction and speed readable without obscuring enemies or changing projectile rules.
- Activating the Relay Foundry offers one run-long weapon calibration: Piercing Pulse continues through one threat into a second aligned role, while Controlled Ricochet redirects once from authored cover toward a nearby unobstructed role. Both retain the five-Signal shot cost and terminate at cover or after their bounded hit budget.
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
- An avoidance route's first salvage escalation deploys a red-and-amber Interceptor from the safer of two scene-authored flank gates; it moves toward the extraction route, shows a locked charge line, and commits to an avoidable dash that combines with Warden pursuit and Sapper tower pressure.
- Once a reinforcement warning begins, an amber world-space marker commits that response to one authored entrance; approaching the gate turns the marker red and pauses the countdown, while crossing the arena cannot silently move the eventual deployment.
- When an Interceptor survives into a Suppressor sweep, it predicts the drone's most obvious ring exit and approaches that edge before using its existing locked dash, creating a readable pincer while perpendicular escape angles remain open.
- When an Interceptor overlaps a latched Sapper outside extraction suppression, it contests only the nearer perpendicular approach at a tuned 3.6-metre offset; the mirrored flank and a 2.25-metre close breach remain open, creating a side-switch decision without adding another threat.
- The three bounded salvage responses adapt to the opening fight: an early Warden or Sapper purge restores that role before the Interceptor, avoidance keeps the Interceptor first, and every route receives each distinct role exactly once without added threat count.
- The final bounded extraction response is a magenta Suppressor that is promoted ahead of unresolved salvage reserves, enters through the safer authored flank gate, then locks its first telegraphed slowing Signal-drain field to the drone's position so the six-second uplink always demands one avoidable movement decision.
- Three warm salvage pickups can be collected outside the safe starting area.
- The first two salvage caches create four paired run builds: Chain Arc converts either auxiliary trigger into one primed double jump, while Overdrive converts it into a short escape surge; Capacitor triggers at low reserve and Feedback Shield triggers on a blocked threat hit.
- Returning with all salvage offers a free six-second stable uplink or a 12-Signal 4.75-second overdrive; both keep movement and combat live and trigger the same bounded response. Stable rewards each purge with 0.9 seconds of progress, while Overdrive grants 0.25 seconds, creating explicit combat and evasion extraction plans.
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
- Reaching zero Signal opens a five-second emergency link instead of ending the run immediately; reaching cyan power or earning a recovery can rescue the drone, turning depletion into a final readable decision rather than a surprise failure.
- Emergency dashes are free, blocked dashes refund their cost, and a zero-Signal tower activation can finance its own rescue so every emergency prompt names a viable action.
- A branded 64-bit Windows development build launches the complete runtime outside the Editor and provides an automated packaged-player health check.
- Core resource/objective transitions have deterministic EditMode tests, and the Unity project compiles in batch mode.

The salvage-area layout deliberately gives each cache a different spatial verb: commit through the annex entrance, thread the coolant lane, then choose direct or wide at the relay fork. This was selected over another enclosure or a purely decorative landmark because variety in navigation decisions adds more replay value than repeating the same room pattern or merely enlarging the arena.

The first threat encounter is anchored by the Warden bay rather than another objective room: visible containment architecture promises danger before tower activation, then converts into combat cover. A comfortably wide west entrance now joins the bay approach to the northeast salvage annex, with large cyan floor chevrons distinguishing the onward route from the bay's combat pocket. This was selected over a Sapper cradle or shortcut checkpoint because the Warden is the player's most immediate post-activation threat and therefore gives the landmark the clearest first-minute payoff.

The second threat is now anchored by a Sapper service cradle at the northwest edge: an L-shaped pair of magenta siphon pylons makes the dormant saboteur legible before tower activation, then leaves it a clear southeast emergence path and gives the player cover if combat returns to that corner. This was selected over a central debris pinch or southwest optional pocket because it turns an existing open-floor enemy spawn into anticipation, navigation, and tactical positioning without adding a new mechanic or objective.

The camera now prioritizes local tactical readability and miniature-machine depth over displaying the entire station at once: a tunable, high-angle perspective follow rig uses a restrained field of view, keeps the drone low in the composition, adds gentle movement-direction look-ahead, and clamps the projected deck footprint so the current arena never exposes empty space beyond its authored edges. Planar movement, aim, collision, combat rules, and telegraphs remain authoritative. This was selected over a low shoulder camera because tactical threats and floor state must remain readable, while perspective better presents the authored machinery, layered models, and lighting than a perfectly orthographic view.

The first true room extension is an optional east salvage vault connected through a guarded opening in the original shell. Its copper route splitter offers two internal lanes, its amber lock lighting distinguishes it from the central station, and its fourth cache lets the player skip one of four salvage routes while still meeting the three-cache extraction requirement. This was selected over a mandatory corridor because route choice creates replay value, and over a new hazard because the established dead-zone clock, Warden pressure, and salvage economy already supply risk without another system.

The second powered region is the Relay Foundry beyond that vault. A closed center bulkhead divides the approach into north and south turbine lanes; restoring the 14-Signal relay creates a second seven-metre cyan foothold, returns up to 44 Signal, and retracts the center bulkhead for a materially shorter trip home. Two far-edge authored reinforcement gates let the existing director enter safely when the player pushes east. This was selected over another isolated combat modifier because it changes traversal, Signal planning, reinforcement direction, and the return journey in one coherent space while preserving the bounded four-response roster.

Restoring that optional Relay now promotes the existing final Suppressor response into the Foundry. Its committed safe gate and full warning remain intact, then its opening field locks to the activation position so either turbine lane stays available as counterplay. The response is consumed once: extraction cannot add a fifth deployment. This was selected over stat or count inflation because the second safe territory should provoke a distinct tactical cost, make Relay greed alter the current mixed-role composition, and still reward players who survive the lockdown with a quieter extraction roster.

The leftover fourth cache now remains collectible after extraction readiness as a one-time 18-Signal greed reward. The mission strip discloses its distance and value while still presenting the dock as the safe finish, so the identity of the skipped branch becomes a late-run resource decision rather than inert scenery. This was selected over another reinforcement because the return route already carries bounded adaptive responses; a cap-safe reserve reward strengthens ammunition, Overdrive affordability, and survival decisions without increasing unavoidable threat count or weakening the extraction climax.

The expanded station is now the required mission rather than an optional sightseeing route. A valid extraction needs the Central, Relay, and Spine towers online plus one distinct payload from each region. Every region offers two payload approaches and retires the unchosen sibling once its regional objective is secured, preserving route choice without allowing three opening-room caches to bypass the authored journey. Central establishes the first build layer and awakens the core threats; Relay requires the Central payload, grants weapon calibration, and promotes its bounded lockdown; Spine requires the Relay payload, evolves the weapon, and leads to the final regional payload. The Arc Furnace/Quench cache remains a separate optional Signal greed reward after extraction readiness. This was selected over merely relocating three identical pickups because progression, navigation, build growth, enemy escalation, and extraction should describe the same three-region arc.

The Warden and Sapper now combine as a screening pair once the Sapper latches: the Warden leaves direct pursuit for a tuned point on the player's approach to the siphon, while a close breach immediately restores its contact attack. This was selected over more health or another reinforcement because it turns the same two readable roles into a flank, armor-break, or continued-objective decision while leaving perpendicular approaches open.

The Sapper and Interceptor now form a one-sided flank cut when their pressure overlaps: the Interceptor chooses the perpendicular Sapper approach nearest its current position, while the mirrored side and a close breach stay open. Suppressor-exit interception retains priority during extraction. This was selected over faster dashes or another reinforcement because it makes the player's side choice matter, gives the existing charge telegraph a tactical setup, and preserves an explicit escape route.

Combat now respects that authored layout: every registered object-aligned movement blocker also stops the player's fast Signal bolts, including rotated shields, pylons, room walls, and the closed shortcut gate. A short original cyan impact bloom confirms the interception without adding hit-stop or camera impulse. This was selected over another room, reward, or enemy because the expanding map's cover choices were misleading while projectiles ignored them; shared spatial rules make existing encounter spaces tactical before more content is added.

The Interceptor and Suppressor now coordinate only when their readable movement threats overlap: the Interceptor predicts the drone's current ring-exit direction, contests a tuned point just beyond that edge, and retains its locked charge line before committing. A centered drone is pressured away from the Interceptor, while every perpendicular exit remains open. This was selected over another reinforcement or stat increase because mixed roles should create surprising positional problems through combination and counterplay rather than attrition.

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

### Run 83 product decision — persistent reinforcement entry

Once a reinforcement's readable entry warning begins, crossing into its six-metre authored gate exclusion pauses the remaining countdown instead of erasing it. The held role never deploys while the entrance is unsafe, but leaving the gate resumes the same warning rather than granting another full delay. This was selected over another enemy or higher stats because a repeatable gate-feint exploit removed route pressure from the existing bounded encounter budget; persistent locks preserve safe counterplay while making a banked response an enduring route constraint.

### Run 63 product decision — extraction pursuit uplink

Extraction now begins a six-second mobile uplink instead of granting instant victory. The player keeps full movement and combat control while one additional bounded security response enters the existing queue, retaining role uniqueness, authored entrances, safe-entry distance, and warning time. This was selected over a stationary capture circle because the climax should reward fighting, fleeing, and route use rather than trap the drone at one point, and over raw stat escalation because one readable tactical response creates pressure without invalidating learned counterplay.

### Run 64 product decision — first-cache Signal overclock

The first secured cache now offers one run-long choice while threats and movement remain live. Chain Arc jumps each successful bolt to one nearby secondary role, rewarding deliberate mixed-enemy alignment; Overdrive Thrusters raises speed and acceleration, rewarding dodges, retreat routing, and dead-zone greed. Fire and Use select the two branches through existing keyboard/controller routes, and the choice input cannot also fire or operate machinery. This was selected over random drops or a passive stat reward because the current short run needs an early, legible build fork that changes fighting versus fleeing without diluting the shared Signal economy.

### Run 65 product decision — extraction Suppressor

The fourth bounded security response is now a Suppressor rather than another Interceptor. It reuses the safer authored flank gate, advances toward the lane between the drone and extraction, warns with an amber 3.25-metre ring for one second, then projects a magenta field for 2.5 seconds. A caught drone retains control but moves at 55% speed and loses 4 Signal per second until it exits; three hits purge the unit and reclaim up to 15 Signal. This was selected over raw pursuit scaling because extraction needed a fourth tactical role that denies comfortable space while preserving readable counterplay through movement, route abandonment, or combat.

### Run 66 product decision — second-cache auxiliary overclock

The second secured cache now adds one economy-defense choice on top of the first cache's combat-mobility fork, creating four possible run builds. Emergency Capacitor performs one automatic 22-Signal refill at 25 Signal or lower; Feedback Shield negates one discrete enemy impact or pulse and recharges only when a threat is purged. Both retain passive dead-zone pressure and use the existing Fire/Use routes without also firing or interacting. This was selected over more random drops or a larger map because the current short run needs surprising combinations and a reason to change fighting, fleeing, and reserve-management plans before adding another region.

### Run 67 product decision — adaptive security response

The core security reserves react to the player's purge order instead of always deploying Warden then Sapper. Run 76 extends this rule to the first cache: an early core purge now restores that missing role before the Interceptor, while an avoidance route still receives the authored Interceptor cutoff first. The remaining distinct roles follow without repeats; when both core roles were cleared before selection, a per-run tie-breaker varies their order. This was selected over more enemies, higher stats, or a second region because replayability currently suffers most from a fixed encounter script. The response remains bounded, role-unique, entrance-safe, and fully telegraphed, while the player's decision to fight the pursuer or protect the tower now changes later mixed-role pressure.

### Run 68 product decision — dead-zone security trace

After tower activation, remaining outside powered territory for eight continuous seconds now completes a security trace and banks the existing first Interceptor response before salvage is secured. Returning to powered territory clears partial progress, and cache one shares the same response slot, so the trace changes timing rather than increasing the four-response cap. This was selected over a larger map or stronger enemies because dead-zone greed currently costs Signal but does not change tactical pressure; the trace gives players a readable choice between extending a risky route and retreating to break lock-on while preserving authored entrances and established counterplay.

### Run 69 product decision — extraction response priority

Starting the uplink now promotes the existing Suppressor response ahead of any unresolved salvage reinforcements. The promoted role restarts the full safe-entry warning, deploys only once, and leaves earlier bounded reserves available without raising the four-response cap. This was selected over a longer uplink or added wave because the climax already owns a distinct denial threat; guaranteeing its timely introduction creates a more reliable final maneuver without increasing enemy stats, quantity, or unavoidable pressure.

### Run 71 product decision — combat-assisted extraction

Each security purge during the active extraction uplink advances the link, with Run 74 superseding the original shared 0.75-second value with mode-specific combat profiles. The survival route keeps fleeing valid, while spending Signal to fight can shorten exposure and combine the existing bounty, Feedback Shield recharge, and tactical-role counterplay into one climactic decision. Credits cannot be banked before extraction and are capped by the remaining link time. This was selected over a longer holdout or added wave because the existing climax needed a positive reason to turn and fight, not more unavoidable threat count.

### Run 73 product decision — extraction link modes

The dock now offers a free six-second stable uplink or a 12-Signal 4.75-second overdrive through the existing Use and Fire routes. The fast link preserves more than one second after the promoted response's safe-entry and ring warnings, so spending reserve reduces exposure without deleting the final maneuver; the choice input cannot also fire, and an unaffordable overdrive leaves the stable route available. This was selected over another enemy, longer holdout, or larger arena because extraction needed a final expression of the shared Signal economy: conserve reserve and survive longer, or burn power now to shorten mixed-role pressure.

### Run 74 product decision — extraction combat profiles

The two link modes now commit the player to distinct responses under the same bounded pursuit. Stable advances by 0.9 seconds per purge, making its longer free link the deliberate combat route; Overdrive advances by only 0.25 seconds per purge, preserving its paid 4.75-second duration as the evasion route. Both exact credits are visible before commitment. This was selected over another threat or stat increase because the prior choice changed exposure and reserve but rewarded fighting identically, leaving high-reserve runs with an obvious faster answer instead of a tactical plan.

### Run 75 product decision — overclock pair synergies

The four existing primary/auxiliary combinations now behave as paired builds instead of unrelated bonuses. Chain Arc plus Capacitor primes one double jump when the emergency refill fires, while Chain Arc plus Feedback Shield primes it after a blocked hit. Overdrive converts those same triggers into a designer-tuned two-second 1.2× surge on top of its established thrusters. This was selected over a third choice layer, random drop, or larger region because the current run already offers four combinations but they did not create surprising interaction; tying each payoff to the established low-reserve or threat-contact risk keeps the Signal economy and bounded encounter pressure authoritative.

### Run 76 product decision — combat-reactive reinforcement order

The first salvage response now records the opening fight instead of always beginning the same script. If both Warden and Sapper survive, cache one still dispatches the authored Interceptor cutoff; if either core role was purged first, cache one restores that missing role and moves the Interceptor to cache two. Clearing both core roles uses the existing per-run tie-breaker. Across all routes, the three salvage reserves still contain exactly one Interceptor, Warden, and Sapper, while extraction still promotes one Suppressor. This was selected over another enemy, larger region, or stat increase because fighting before salvage should create a different later threat combination rather than merely reducing health bars.

### Run 77 product decision — mode-reactive extraction suppression

The promoted Suppressor now counters the extraction plan the player chose. Stable retains a centered opening ring that asks the player to leave the lock and use its stronger purge credit; Overdrive projects its ring 3.5 metres ahead along the dock-to-drone retreat line, asking the faster drone to break course rather than hold a scripted escape. Both profiles keep the same bounded role, safe authored entrance, one-second telegraph, field radius, penalties, and extraction duration, and the dock identifies each sweep before commitment. This was selected over another wave or stat increase because the two uplink modes needed different spatial counterplay, not merely different clocks and prices.

### Run 81 product decision — Interceptor crash recovery

A committed Interceptor dash now stops when it hits authored cover and exposes a designer-tuned 1.5-second counterattack window; a cleanly avoided dash retains a shorter 0.7-second recovery. Both states block an immediate follow-up lock and are disclosed through combat feedback and live threat status. This was selected over additional health, speed, reinforcements, or a second region because the existing authored obstacles should support a deliberate bait-and-punish decision: flee across open floor, spend route distance to line up a crash, or turn the resulting opening into Signal-expensive counterfire.

### Run 86 product decision — Relay weapon calibration

Relay activation now awards a third, independent build layer. Piercing Pulse rewards lining up mixed roles in open lanes by allowing one bolt to strike two different threats; Controlled Ricochet rewards deliberate use of authored cover by redirecting one impact toward a nearby unobstructed role. Both keep the established five-Signal cost, normal one-hit damage, finite lifetime, and hard cover rules. This was selected over a generic damage percentage because the new region should change how the player reads its turbine lanes and return bulkheads, and over a random drop because meaningful region progress should produce a legible build decision.

### Run 87 product decision — Relay lockdown composition

The Relay's power exchange now spends tactical pressure as well as Signal: activation promotes the already-bounded Suppressor ahead of unresolved salvage responses, commits it to one of the Foundry's two safe authored gates, and locks an avoidable opening sweep to the activation point. That deployment consumes the same final response otherwise promoted during extraction, so the run remains capped at four and the later climax changes rather than grows. This was selected over stronger enemies or a fifth response because additional powered territory should change role combinations and return planning while preserving every learned health, damage, movement, warning, and entrance rule.

### Run 88 product decision — Capacitor Spine expedition

The former east-vault cache now terminates a deeper Capacitor Spine route beyond the Relay Foundry. Two aligned openings divide around a collision- and projectile-authoritative transfer bank: the north lane offers extra authored cover, while the south lane stays exposed and leads directly to the amber greed cache. A dormant third-tower berth makes the next region goal visible without adding a false interaction. This was selected over a third tower implemented all at once because the run first needs another readable, completable modular journey; it lengthens the outward raid and return, reuses the Foundry's safe reinforcement pair, and preserves the existing cache reward, required salvage count, bounded response roster, combat stats, and Signal drains.

### Run 89 product decision — Capacitor Spine tower evolution

The far Spine berth is now a real third Signal tower after the Relay is online. Its 18-Signal transaction preserves the drone's final point, restores 34, and creates a smaller 6.2-metre powered foothold around the deepest authored landmark. Instead of opening a fourth choice prompt, activation evolves the Relay calibration already chosen: Piercing Pulse gains a third different aligned target, while Controlled Ricochet gains a second legal cover bank. This was selected over another random reward or generic damage scaling because the third region should test and then strengthen the player's established build, while its costly outward commitment changes the optional-cache return without adding enemies, responses, or stat inflation.

### Run 90 product decision — Capacitor Spine discharge return

The Spine transfer bank now acts as a progress-gated return gate. It remains collision- and projectile-authoritative while the player chooses the protected north or exposed south approach, then retracts with the third tower to reveal a direct central route home. This was selected over more corridor area or another combat modifier because the deepest objective should transform the return journey immediately; the same landmark now creates different outward and inward navigation without changing Signal economy, enemy stats, reinforcement count, safe entrances, or the existing weapon evolution.

### Run 91 product decision — Spine Induction Gallery

The Capacitor Spine's north edge now opens through two authored doorways into a compact outer gallery. On the outward trip its induction coil and angled baffles offer a longer, cover-rich dead-zone approach; after the third tower comes online, the complete gallery becomes a powered return foothold while the retracted center bank remains the shortest exposed route. This was selected over another corridor, reward, or combat-stat increase because one reusable room now changes route length, cover geometry, Signal expenditure, weapon positioning, and the return decision without changing the bounded threat roster, economy, or objective sequence.

### Run 92 product decision — Convergence Chamber deep-route pressure

The Induction Gallery now opens north through two wide thresholds into a separate Convergence Chamber. Its central busbar and mirrored baffles support piercing lines, ricochet banks, and flanking movement, while a fifth authored reinforcement gate becomes the director's committed direction only when the player pushes beyond the old map edge. The existing six-metre gate exclusion holds any deployment while the player occupies the room, so the new direction produces readable pursuit on withdrawal rather than an unavoidable close spawn. Spine activation powers the room for the return. This was selected over more enemies or generic stat escalation because the same bounded response roster now combines with a new route and cover geometry, and over an isolated reward because the room's value changes between dead-zone commitment and powered retreat.

### Run 93 product decision — Flux Bypass return flank

The Gallery and Chamber now share a separate west-side bypass rather than forcing every deep-route choice through the chamber's central busbar. The 7-by-11.5-metre shunt lane enters through two mechanically proven thresholds, uses angled projectile-authoritative deflectors, and reuses the modular busbar assembly as a flux regulator landmark. It remains a dead-zone commitment before the Spine and becomes a powered flank afterward. This was selected over another reward room, fourth tower, or larger combat roster because it creates a route and weapon-positioning tradeoff in both journey directions while preserving the existing five safe gates, four-response cap, objectives, combat stats, and Signal economy.

### Run 94 product decision — Arc Furnace greed crossing

The optional fourth cache now waits beyond the Convergence Chamber in a separate Arc Furnace room instead of beside the Spine tower. Two authored thresholds divide around a collision- and projectile-authoritative furnace: the western approach is a tight ceramic-shield switchback suited to ricochet play, while the eastern approach is a long exposed lane suited to piercing fire and rapid abandonment. A sixth authored gate becomes the deep-route reinforcement direction while remaining beyond the six-metre safety exclusion, and Spine activation powers the complete room for withdrawal. This was selected over empty room area, another reward, or enemy-stat inflation because the existing greed decision now asks the player to spend more dead-zone time, choose terrain that fits the current weapon, and plan a distinct return through the same bounded four-response roster.

### Run 95 product decision — Quench Loop return flank

The Quench Loop wraps the Arc Furnace's east side as a compact two-threshold flank. Its rotated ceramic deflectors and condenser landmark offer a shielded alternative to the Furnace firing line before Spine activation, then its cyan routing turns the same lane into a powered cache-return option. This was selected over extending the run with another dead-end room because the deepest greed commitment needs a different withdrawal answer before more objectives or a guardian.

### Run 96 product decision — Quench cache-release shortcut

The optional Arc Furnace cache now transforms the Quench Loop instead of paying Signal alone. A scene-authored pressure shutter leaves a narrow exposed east-edge passage on the outward journey, then retracts when the cache is secured to reveal a direct cyan cut-through for the return. This was selected over more floor area, another reward, or stronger enemies because the greed objective should immediately reshape withdrawal through already-authored cover while preserving the same cache economy, six safe gates, four-response cap, combat stats, and Signal drains.
# Run 102 product decision — departure capacitor surge

Completing the required three-region payload journey now energizes a one-shot 12-Signal capacitor reserve in the released departure-channel centerline. Returning through the direct cut-through claims it; either authored flank remains a valid route and leaves it untouched. This turns the shortcut from pure distance reduction into a visible Signal decision while preserving the existing tower, combat, enemy-count, extraction-mode, and optional-greed contracts.
