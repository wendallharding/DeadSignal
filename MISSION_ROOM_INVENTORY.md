# DEAD SIGNAL — Current station room inventory

Status: Phase 0 current-state and space-classification contract, 2026-08-27. This records what the playable scene and runtime do now; it is not the target mission order. The room-purpose ledger, schematic target route, and measured route baselines remain later Phase 0 work.

## Authority and coordinate contract

- Layout authority is `Assets/DeadSignal/Scenes/SampleScene.unity` plus the scene-placed environment prefabs it references. Setup scripts are reproducible authoring tools, not runtime layout authority.
- `DeadSignalSceneReferences` explicitly binds the Dock, Central Tower, central shortcut, Relay Tower, relay shortcut, Capacitor Spine, persistent actors, and arena bounds. Several older landmarks remain top-level scene objects discovered globally by component registration; the Departure Channel is still found by its root name.
- `RunModel` owns the current seven-stage progression. `DeadSignalGame` owns orchestration and interactions. `DeadSignalWorld` binds the authored scene, registers all `AuthoredMapObstacle`, `AuthoredPoweredTerritory`, `AuthoredSalvageSocket`, and reinforcement-entry components, and rebuilds one runtime NavMesh from arena bounds and active blockers.
- Movement and projectile collision use the same oriented authored blockers. The runtime NavMesh is route-planning assistance; it is rebuilt when a registered door or shortcut opens and is not collision authority.
- Coordinates below are world X/Z metres for scene roots or the nearest stable authored anchor. A child room is identified by its prefab hierarchy path and its adjacency is the opening created in that parent prefab.

## Current topology

```text
Dock — Departure Channel — Central Maintenance Concourse — East Transfer Vault — Relay Foundry — Capacitor Spine
Central Maintenance Concourse — {Cargo Annex, Coolant Reclamation, Relay Fork, Warden Bay, Sapper Cradle}
Relay Foundry — Cooling Gantry
Capacitor Spine — {Discharge Trench, Induction Gallery}
Induction Gallery — {Flux Bypass, Convergence Chamber}
Convergence Chamber — {Breaker Gallery, Arc Furnace}
Arc Furnace — {Quench Loop, Room A — Room B — Room C}
```

The central landmarks overlap the open maintenance floor rather than forming sealed door-to-door rooms. Their edges are therefore walkable spatial adjacencies, not guaranteed portal boundaries. The east/deep chain uses explicit parent-wall openings. Security Trial Rooms A–C are a single north-running wing attached to the Furnace.

## Authoritative space classification

These classes describe each space's intended player-facing function in the cohesive mission, without assigning its later objective contract:

- **Mission room:** must own a required transaction, process, trial, or completion beat. A mission room may be an open floor landmark rather than a sealed chamber.
- **Traversal connector:** primarily carries the player between mission rooms and earns its place through route state or navigation, not a standalone objective.
- **Combat landmark:** anchors a recognizable enemy identity, pressure direction, or cover decision while progression remains owned elsewhere.
- **Decorative pocket:** may enrich presentation but must not be made required merely to justify existing geometry.

| Space | Classification | Classification evidence and boundary |
| --- | --- | --- |
| Extraction Dock | Mission room | Owns the opening spawn and final live extraction transaction. |
| Departure Channel | Traversal connector | Connects Dock to Central and communicates the changed withdrawal lane; the one-shot surge supports traversal rather than becoming another objective. |
| Central Maintenance Concourse | Mission room | Owns the Central Tower transaction and the first powered foothold. |
| Cargo Annex | Mission room | The cohesive route assigns it the required coupling-retrieval function; its current alternative cache is temporary compatibility behavior. |
| Coolant Reclamation | Mission room | The cohesive route assigns it the required seal-retrieval function; baffle traversal distinguishes it from Cargo. |
| Relay Fork | Mission room | Must own routing the two Central components into the transfer vault, not remain set dressing. |
| Warden Bay | Combat landmark | Establishes Warden silhouette, cover, and withdrawal pursuit identity; it does not need an independent switch or pickup. |
| Sapper Cradle | Combat landmark | Establishes Sapper warning, cover, and withdrawal pursuit identity; progression remains with the withdrawal route. |
| East Transfer Vault | Mission room | Owns transfer-vault assembly after Relay Fork routing while also bridging Central to Relay. |
| Relay Foundry | Mission room | Owns Relay activation, payload calibration/installation, and the weapon-transformation decision. |
| Cooling Gantry | Mission room | Owns payload stabilization through its exchanger route before the Foundry return. |
| Capacitor Spine | Mission room | Owns the third-tower activation and later completed-core installation. |
| Spine Discharge Trench | Mission room | Owns berth venting before the Spine transaction. |
| Induction Gallery | Mission room | Owns charging the empty core lattice. |
| Flux Bypass | Mission room | Owns the shunt/reroute that enables Convergence and changes the later return. |
| Convergence Chamber | Mission room | Owns the bounded calibration holdout, distinct from Room B's full lockdown. |
| Breaker Gallery | Mission room | Owns the distribution reset that unlocks Furnace processing. |
| Arc Furnace | Mission room | Owns forging the charged lattice and gates the final processing/trial chain. |
| Quench Loop | Mission room | Owns lattice stabilization and the existing return-shortcut mutation. |
| Room A / Commitment Room | Mission room | Owns warning and irreversible commitment to the final trial. |
| Room B / Lockdown Arena | Mission room | Owns the mission's single full Geometry Wars-inspired lockdown climax. |
| Room C / Reward Vault | Mission room | Owns recovery of the mission-critical station capacitor after trial clearance. |

Classification count: **19 mission rooms, 1 traversal connector, 2 combat landmarks, and 0 decorative pockets (22 total)**. No decorative pocket appears in this major-space inventory: decorative machinery and bypass niches exist within the listed spaces, but they are not independent rooms and must not be promoted into objectives. The connector and combat-landmark classifications are deliberate exceptions to the required-room objective pattern; those spaces earn required traversal through route change, combat readability, or pursuit rather than filler interactions.

## Opening and Central inventory

| Space and authored evidence | Actual adjacency | Current gameplay authority and ownership | Power state | Doors, collision, NavMesh, and return value |
| --- | --- | --- | --- | --- |
| **Extraction Dock** — `DEAD SIGNAL — Authored World/Environment/Extraction Pad Assembly`; anchor `(-9.2, -5.6)` | Departure Channel and Central Maintenance Concourse | Stage 7 extraction target. `ExtractionUplink` plus `DeadSignalGame` own stable/overdrive interaction and completion pressure. No pickup; initial player spawn is here. | Always powered inside the 3.6 m starting radius. | No dock door. The Departure cargo shutter is the readiness gate on its approach. Required return destination and final live uplink. |
| **Departure Channel** — top-level `Extraction Departure Channel`; root `(-7.2, -4.2)`, rotated -35° | Dock ↔ Central Maintenance Concourse | No outbound objective. When all three towers and regional payloads are complete, `DeadSignalWorld` opens the cargo shutter. Direct inward crossing can consume the one-shot 12-Signal surge; flanks avoid it. | Mostly covered by the Dock starting radius; its return signal appears only at extraction readiness. | Three oriented blockers: two capacitors and the cargo shutter. Readiness disables the shutter blocker and rebuilds NavMesh. Withdrawal payoff: changed lane, cyan return decal, optional direct surge. |
| **Central Maintenance Concourse** — `DEAD SIGNAL — Authored World`; Central Tower anchor `(-0.6, 0.4)` | Departure Channel, Cargo Annex, Coolant Reclamation, Relay Fork, Warden Bay, Sapper Cradle, East Transfer Vault | Stage 1 Central Tower interaction. `RunModel.TryActivateTower`, `DeadSignalGame`, and `DeadSignalWorld.ActivateTower` own the cost/refill, Warden/Sapper release, tower core, and signal-line mutation. The central shortcut is a separate optional interaction. | Dead outside the Dock radius until Central activation; Central then powers a 7.2 m radius around the tower. | Maintenance shell and machinery use runtime blockers; Central Tower has a runtime square blocker. The central shortcut at `(4.0, 0.4)` costs 16 Signal, disables its gate, and rebuilds NavMesh. Required withdrawal revisits this powered foothold. |
| **Cargo Annex** — top-level `Northeast Salvage Annex`; `(9.7, 6.3)` | Open maintenance floor ↔ Central Maintenance Concourse; overlaps the Warden-side approach | One of two current Stage 2 Central payload alternatives. The cache is runtime-created at or near this root, and `DeadSignalSalvageController`/`RunModel` own collection, retirement of the Coolant alternative, reward, and primary overclock prompt. No room-specific rule. | Dead territory; the cache becomes collectible only after Central Tower activation. | Three scene-authored cargo barriers provide oriented collision. No door or local NavMesh mutation. Current return value ends when its payload alternative is retired/collected. |
| **Coolant Reclamation** — top-level `Southeast Coolant Gauntlet`; `(10.4, -6.4)` | Open maintenance floor ↔ Central Maintenance Concourse; south route toward East Transfer Vault | The other current Stage 2 Central payload alternative. Runtime cache collection has the same authority and consequences as Cargo Annex. The baffles affect approach but there is no coolant-specific rule. | Dead territory; collectible only after Central Tower activation. | Two authored baffle blockers; no door or local NavMesh mutation. Current return value ends when its payload alternative is retired/collected. |
| **Relay Fork** — top-level `Northwest Relay Fork`; `(-5.8, 7.2)` | Central Maintenance Concourse ↔ Sapper-side/northwest maintenance approach | No objective, payload, or persistent mutation. Two relay-bank obstacles are presentation/collision only. | Dead territory; no authored power component or activation mutation. | Two oriented blockers; no door, shortcut, or local NavMesh mutation. No current required return value. |
| **Warden Bay** — top-level `Security Warden Staging Bay`; `(6.8, 4.7)` | Central Maintenance Concourse ↔ Cargo Annex/east approach | Owns Warden staging silhouette and a north bypass lane. The actual persistent Warden actor and its activation/combat state are owned by scene references, threat controller, and Central activation—not by the bay prefab. | At/just beyond the Central circular edge depending on position; no authored territory of its own. | Three authored shield blockers and two route markers; no dynamic door. Current route value is combat cover/approach shaping, not an objective. |
| **Sapper Cradle** — top-level `Signal Sapper Service Cradle`; `(-10.8, 5.7)` | Central Maintenance Concourse ↔ Relay Fork/northwest approach | Owns Sapper landmark/pylon presentation. The persistent Sapper actor, latch, pulses, and target state are owned by scene references and threat controller after Central activation. | Dead territory; no authored territory or local mutation. | Two authored pylon blockers; no dynamic door. Current route value is combat landmark/cover only. |
| **East Transfer Vault** — top-level `Optional East Salvage Vault`; `(16.7, 0.0)` | Central Maintenance Concourse ↔ Relay Foundry | Traversal shell only. Its legacy cache socket was deliberately removed; it has no objective, pickup, enemy, or mutation owner. | Dead territory; no authored power component. | Six authored boundary/gate blockers create the east passage. No runtime door mutation. It is the only current spatial bridge into the east chain and the same corridor is used on withdrawal. |

## Relay and Spine inventory

| Space and authored evidence | Actual adjacency | Current gameplay authority and ownership | Power state | Doors, collision, NavMesh, and return value |
| --- | --- | --- | --- | --- |
| **Relay Foundry** — top-level `Relay Foundry Region`; root `(27.5, 0.0)`, Relay Tower child/anchor on its east side | East Transfer Vault ↔ Capacitor Spine; south opening ↔ Cooling Gantry | Stage 3 Relay Tower interaction and Stage 4 northern Relay payload alternative. `RunModel.TryActivateRelayTower` and world/game orchestration own tower state; an authored Relay socket supplies the local payload. Weapon calibration choice is prompted at activation. | Dead until Relay activation; then 7.2 m tower radius and its signal lines power. | Authored region blockers plus a relay shortcut gate. Relay activation disables that gate and rebuilds NavMesh. Required withdrawal revisits this powered foothold. |
| **Cooling Gantry** — `Relay Foundry Region/Relay Cooling Gantry Region`; local south offset `(0, -11.25)` | Relay Foundry only, through its south opening | Stage 4 southern Relay payload alternative at `Cooling Gantry Relay Payload Socket`. The same salvage controller/model authority retires the Foundry alternative after collection; the room has no processing rule yet. One authored reinforcement entry supplies combat approach data. | `RelayTower` authored territory; dark until Relay activation, then its routing is shown and drain protection applies. | Six authored blockers; no dynamic room door. Its route is a dead-end return to the Foundry. No value after its alternative is retired/collected. |
| **Capacitor Spine** — top-level `Capacitor Spine Region`; root `(42.5, 0.0)`, tower berth at local east `(5, 0)` | Relay Foundry ↔ Induction Gallery (north); Discharge Trench (south) | Stage 5 Spine Tower interaction uses the authored south-side activation decal. Two hard-coded Stage 6 Spine payload alternatives sit on its north/south sides. `RunModel.TryActivateSpineTower` and world/game orchestration own activation and auxiliary overclock choice. | Dead until Spine activation; then a 6.2 m radius and Spine signal lines power. | Authored perimeter/tower blockers. `Capacitor Transfer Bank` is the spine-return gate; Spine activation disables it and rebuilds NavMesh. It is the current Stage 6 hub and required withdrawal landmark. |
| **Spine Discharge Trench** — `Capacitor Spine Region/Spine Discharge Trench Region`; local south offset `(0, -8)` | Capacitor Spine only | No current objective or payload. One authored reinforcement entry supplies combat approach data. | `SpineTower` authored territory; activates with Spine. | Six authored blockers; no dynamic door. Dead-end traversal pocket with no current required return value. |
| **Induction Gallery** — top-level `Spine Induction Gallery Region`; root `(42.5, 8.5)` | Capacitor Spine ↔ Convergence Chamber; west branch ↔ Flux Bypass | No current room-specific objective or payload. | `SpineTower` authored territory; activates with Spine. | Authored perimeter blockers and explicit south/north openings. No dynamic door. It is the parent route into the deep-core cluster, but the current seven-stage journey does not require entry. |

## Deep-core inventory

| Space and authored evidence | Actual adjacency | Current gameplay authority and ownership | Power state | Doors, collision, NavMesh, and return value |
| --- | --- | --- | --- | --- |
| **Flux Bypass** — `Spine Induction Gallery Region/Flux Bypass Region`; local west offset `(-10.5, 4.25)` | Induction Gallery only | No current objective, pickup, or world mutation. Landmark and route decal only. | `SpineTower` authored territory. | Eight authored blockers; no dynamic door. Dead-end branch with no current return value. |
| **Convergence Chamber** — `Spine Induction Gallery Region/Convergence Chamber Region`; local north offset `(0, 8.5)` | Induction Gallery ↔ Arc Furnace (north); east branch ↔ Breaker Gallery | No current objective or completion rule. One reinforcement entry supplies threat approach data. | `SpineTower` authored territory. | Region-authored collision and open parent portals; no dynamic door. Through-route to Furnace/trial, but not required by current progression. |
| **Breaker Gallery** — `.../Convergence Chamber Region/Convergence Breaker Gallery Region`; local east offset `(10.5, 0)` | Convergence Chamber only | No current objective, pickup, or persistent mutation. One reinforcement entry supplies threat approach data. | `SpineTower` authored territory. | Eight authored blockers; no dynamic door. Dead-end branch with no current return value. |
| **Arc Furnace** — `.../Convergence Chamber Region/Arc Furnace Region`; local north offset `(0, 8.5)` | Convergence Chamber ↔ Room A (north); east branch ↔ Quench Loop | Current optional Spine-region salvage socket and one reinforcement entry. Because the socket is optional, it is collectible only after Stage 6 is already complete and then owns the optional recovery/Quench-shortcut path—not a required core process. | `SpineTower` authored territory. | Authored blockers; north and east openings connect the trial and Quench. No Furnace-specific dynamic door. Current optional return starts here. |
| **Quench Loop** — `.../Arc Furnace Region/Quench Loop Region`; local east offset `(10.5, 0)` | Arc Furnace only; pressure shutter is a same-region return shortcut | No required objective. Its current value is the optional post-readiness cache route inherited from the Furnace socket and the shortcut consequence. | `SpineTower` authored territory. | Authored blockers plus `Quench Pressure Shutter`. Collecting the optional cache disables the shutter, shows the cyan return signal, and rebuilds NavMesh. |
| **Room A / Commitment Room** — `.../Arc Furnace Region/Security Trial Wing Region/Commitment Room`; wing local north offset `(0, 7.5)` | Arc Furnace ↔ Room B | `AuthoredCombatChamber` owns the amber breaker and arms the trial. It is not part of `MissionStage`; players may engage it whenever they can reach it. | No separate territory component and outside the Furnace territory bounds; remains a dead zone after Spine activation. | Entry door begins closed; breaker interaction opens it. Crossing the threshold recloses it and rebuilds NavMesh through game orchestration. Required only by the optional trial flow today. |
| **Room B / Lockdown Arena** — `.../Security Trial Wing Region/Lockdown Arena`; local north offset `(0, 21)` | Room A ↔ Room C | `AuthoredCombatChamber`, `DeadSignalGame`, and threat controller own the three-phase Swarmer → Swarmer+Warden → Swarmer+Sapper lockdown. Peak authored contract is five active threats. | No separate territory and outside the Furnace territory bounds; remains a dead zone after Spine activation. | Full authored boundary collision with centered entry/reward doors. Lockdown seals the entry; clearing removes both doors and rebuilds NavMesh. Cleared cyan return spine persists for revisit. |
| **Room C / Reward Vault** — `.../Security Trial Wing Region/Reward Vault`; local north offset `(0, 42)` | Room B only | Chamber-owned one-shot station capacitor restores up to 20 Signal after Room B clearance. It is not a regional payload and does not advance `MissionStage`. | No separate territory and outside the Furnace territory bounds; remains a dead zone after Spine activation. | Reward door blocks access until Room B clears; north/side bulkheads close the vault. Return value is the one-shot recovery plus the open cleared route. |

## Current seven-stage compatibility route

This is the progression that Phase 1 must reproduce before mission order changes:

1. Activate Central Tower.
2. Collect exactly one Central payload: Cargo Annex or Coolant Reclamation; the other retires.
3. Activate Relay Tower and choose the weapon calibration.
4. Collect exactly one Relay payload: Relay Foundry or Cooling Gantry; the other retires.
5. Activate Spine Tower and choose the auxiliary evolution.
6. Collect exactly one Spine payload from the hard-coded north/south sockets within Capacitor Spine.
7. Return through Relay and Central powered footholds to the Dock; optionally collect the Furnace-side cache to open the Quench return, cross the Departure surge lane, then complete the live extraction uplink.

The current model requires three towers and three regional payloads, but it does **not** require Relay Fork, Discharge Trench, Induction, Flux, Convergence, Breaker, Furnace processing, Quench processing, or Rooms A–C. This is the central mission-flow gap that the later phases must close without breaking compatibility first.

## Inventory evidence and open verification

- Scene/prefab evidence: `SampleScene.unity`, the environment prefabs named above, and their idempotent `DeadSignal*Setup` scripts.
- Runtime authority evidence: `RunModel`, `MissionGuidance`, `DeadSignalGame`, `DeadSignalWorld`, `DeadSignalSalvageController`, `AuthoredCombatChamber`, and `DebugRouteSequencer`.
- Existing regression evidence covers region hierarchy, wall openings, blocker counts, powered-state mutations, cache collection, dynamic gates, and the current commercial journey. Phase 0's later adjacency-contract checkbox must consolidate the critical paths/anchors into focused tests; this inventory deliberately does not claim that later item complete.
- Human route recognition, wrong turns, room-entry coverage, and duration have not been established for this inventory run. Those remain the final Phase 0 baseline checkbox.
