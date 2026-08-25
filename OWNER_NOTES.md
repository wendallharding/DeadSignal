# DEAD SIGNAL — Owner Action Plan

**Updated:** 2026-08-25  
**Target milestone:** A readable, combat-capable Full Extraction run that is fun to replay

## What the team should accomplish next

Do not add another region, tower, enemy role, or upgrade layer yet. The next milestone is to make the existing journey consistently playable, readable, and useful for balance decisions.

Complete these work packages in order. A package is finished only when its acceptance checks and validation recipe pass.

## 1. Repair the Eastern Room Combat scenario

**Priority:** P0  
**Outcome:** One button reliably opens a 30-second combat laboratory where the player and active threats remain visible and alive long enough to evaluate them.

### Implementation tasks

- Add a scene-authored combat-scenario anchor inside the eastern/deep room with:
  - player spawn position and facing;
  - Warden, Sapper, Interceptor, and Suppressor staging positions;
  - a camera framing volume or validated target area;
  - safe distances from walls, gates, and obstacle corners.
- Update EasternRoomCombat in DeadSignalGame.DebugApplyScenario to use those authored anchors instead of an arena-edge coordinate.
- Give the scenario an explicit state preset:
  - prerequisite towers and weapon calibration resolved;
  - no pending upgrade prompt;
  - objective text set to a combat-lab instruction;
  - enough starting Signal for at least 30 seconds of normal threat activity;
  - no unrelated reinforcement, extraction, or salvage transition.
- Spawn or reposition each threat through DeadSignalThreatController; do not duplicate threat lifecycle logic in the scenario method.
- Reset projectile, telegraph, drain, dash, suppression, feedback, and camera-impulse state whenever the scenario loads.
- Keep the player inside authored collision and NavMesh bounds.
- Add a debug status line showing scenario time, Signal, active threats, and whether each threat is inside the viewport.

### Acceptance criteria

- Loading the scenario five times never causes an immediate Drone Offline outcome.
- At 1600×900, the player remains between 15% and 85% of viewport width and height.
- During every telegraph, the attacking threat or its origin is visible.
- No more than 15% of the frame is empty black space caused by camera overshoot.
- Each threat completes at least one readable attack within 30 seconds.
- No objective, upgrade, salvage, or extraction prompt appears during the scenario.
- No NavMesh failure, missing reference, exception, or route recovery appears in the log.

### Required validation

- Add focused PlayMode coverage to DeadSignalDebugMenuPlayModeTests.cs for scenario state, spawn bounds, camera containment, and 30 seconds of survival.
- Capture one frame for each threat telegraph and one mixed-combat frame.
- Run focused PlayMode tests, then the full PlayMode suite.

## 2. Give LiveBalance automation combat and evasion behavior

**Priority:** P0  
**Outcome:** LiveBalance evaluates the fight/flee economy instead of walking through attacks with zero shots.

### Implementation tasks

- Extract a focused combat/evasion policy from DeadSignalGame; do not continue expanding the bootstrap controller.
- While route driving, prioritize:
  1. imminent Interceptor dash or Suppressor field;
  2. active Sapper drain;
  3. Warden strike;
  4. route objective.
- Add bounded actions for:
  - steering out of telegraphed areas;
  - breaking Sapper range or line of sight;
  - baiting an Interceptor into authored cover when practical;
  - aiming and firing only when a threat is targetable;
  - resuming the route after the danger window;
  - abandoning optional greed below a designer-tuned reserve threshold.
- Keep LiveBalance on normal Signal, normal damage, active threats, and normal weapon cost. Do not add hidden refills, invulnerability, teleport recovery, or threat freezes.
- Store thresholds in a focused ScriptableObject rather than hardcoding them in the route driver.
- Extend telemetry with:
  - attacks, hits, and successful dodges by role;
  - seconds under Sapper drain;
  - shots, weapon hits, purges, and weapon Signal spent;
  - combat pause time and lowest Signal;
  - optional-cache decision and reason;
  - enemy no-progress/stuck duration;
  - final terminal outcome.
- Make failures name the actual cause: navigation stall, impact destruction, Signal depletion, failed interaction, or timeout.

### Acceptance criteria

- A LiveBalance route records at least one shot or an explicit flee decision when attacked.
- The bot never remains in Sapper drain for more than two consecutive pulses without changing plan.
- No enemy reports more than two seconds of unexplained movement no-progress.
- Three consecutive Full Extraction runs finish without automation errors.
- At least two reach extraction with 20 or more Signal.
- Any failure has a valid gameplay cause and its report matches the rendered terminal outcome.
- SafeNavigation behavior and deterministic route timing remain unchanged.

### Required validation

- Add EditMode tests for threat prioritization and Signal thresholds.
- Add PlayMode tests for Sapper disengagement, Interceptor response, combat-to-route resumption, and terminal report accuracy.
- Run three packaged LiveBalance Full Extraction playthroughs and preserve all reports.

## 3. Correct camera framing and combat-effect composition

**Priority:** P0  
**Outcome:** The player, threat, cover, and objective remain readable in eastern rooms and at extraction.

### Implementation tasks

- Add framing constraints for Relay, Spine, Furnace/Quench, and extraction spaces through PlayerFollowCamera tuning or scene-authored bounds.
- Prevent the camera from exposing unbuilt black space at arena boundaries.
- Add a development-only viewport warning when the player or active telegraph origin leaves the safe frame.
- Separate gameplay radius from visual opacity for Sapper and Suppressor fields.
- Reduce field opacity or add a softer inner/outer gradient so cover and silhouettes remain visible.
- Remove redundant powered-territory layers or reduce alpha where cyan circles overlap at extraction.
- Ensure objective rays and wedges do not cross the drone or hide threats.
- Keep world threat visuals below HUD sorting layers and outside the upper-right HUD footprint.

### Acceptance criteria

- Eastern combat, Quench return, and extraction screenshots contain no large unintended black void.
- The player silhouette remains readable in powered and dead-zone lighting.
- Sapper, Suppressor, objective, and powered-area effects can overlap without hiding the player or threat.
- The extraction threat remains visible outside the HUD footprint.
- Camera changes do not reveal geometry outside authored rooms.

### Required validation

- Extend PlayerFollowCameraTests.cs and PlayerFollowCameraPlayModeTests.cs with eastern and extraction cases.
- Capture opening, Relay, Spine, eastern combat, optional cache, and extraction frames.
- Perform an interactive 1600×900 visual pass after automated tests.

## 4. Make HUD prompts and terminal debriefs readable

**Priority:** P0  
**Outcome:** Every decision, objective, failure, and victory metric is legible at 1280×720 and 1600×900.

### Implementation tasks

- Fix the upgrade prompt so FIRE and USE always include complete choice labels.
- Give objective, decision, and extraction panels exclusive bottom-center slots.
- When an uplink starts, replace RETURN TO EXTRACTION with mode, remaining time, and the current instruction.
- Clamp world-objective labels away from the player marker and screen edges.
- Rebuild Drone Offline and Signal Recovered layouts in DeadSignalHud/RunDebrief:
  - headline;
  - one-sentence outcome;
  - five to seven useful metrics;
  - restart instruction;
  - optional detailed diagnostics for development builds.
- Put font sizes, padding, spacing, and panel limits in SignalHudTuning.
- Define a minimum readable body size at 1280×720.
- Source the rendered failure cause and route-report outcome from the same terminal state.

### Acceptance criteria

- No text truncates, overlaps, or falls below the minimum size at 1280×720, 1600×900, or 1920×1080.
- Upgrade prompts show two complete choices.
- Extraction copy changes immediately when the uplink begins.
- Failure and victory screens are readable without zooming.
- Reports and rendered screens always agree on the terminal outcome.

### Required validation

- Add focused tests to SignalHudPresentationTests.cs and RunDebriefTests.cs.
- Add a PlayMode viewport test covering all prompts and both terminal outcomes at 1280×720.
- Capture and inspect every required resolution.

## 5. Fix the AutoUI review workflow

**Priority:** P1  
**Outcome:** Daily review tools produce trustworthy evidence without manual timing tricks.

### Implementation tasks

- Repair Overview row heights so diagnostics never overlap immediately after F5 opens.
- Make every page fit at 1280×720 without relying on a later layout pass.
- Change Combat Frame capture to:
  1. queue the requested combat state;
  2. close or hide AutoUI;
  3. wait for end of frame;
  4. capture gameplay;
  5. optionally reopen AutoUI.
- Show capture status and store the absolute output path.
- Record resolution, route seed, scenario, Signal, active threats, and camera-safe-frame status in capture metadata.
- Replace ambiguous Shortcut CLOSED reporting with terminology that distinguishes the original shortcut from the Quench return.

### Acceptance criteria

- Overview has zero overlapping labels on first open.
- Combat Frame produces gameplay, not a paused menu.
- Capture metadata can reproduce the state.
- Route reports use unambiguous shortcut/return terminology.
- Development-build-only and release-player exclusions remain intact.

### Required validation

- Extend DeadSignalDebugMenuPlayModeTests.cs and DebugRouteSequencerTests.cs.
- Verify every page and capture action at 1280×720 and 1600×900.
- Confirm the menu remains absent from a non-development player.

## 6. Tune the existing run before adding content

**Priority:** P1, after packages 1–5  
**Outcome:** The current three-region run demonstrates meaningful choices and repeatable fun.

### Human playtest matrix

Run at least three attempts for each comparison:

- optional Quench cache versus direct withdrawal;
- Chain Arc versus Overdrive Thrusters;
- Emergency Capacitor versus Feedback Shield;
- Piercing Pulse versus Controlled Ricochet;
- Stable versus Overdrive extraction;
- at least two route seeds;
- keyboard/mouse and physical controller.

### Record for every run

- outcome and completion time;
- lowest and final Signal;
- Signal spent on movement, weapons, towers, and extraction;
- recovery sources;
- hits and drains by role;
- shots, hit rate, purges, and fight time;
- optional-greed decision;
- chosen protected/exposed route;
- tense, confusing, repetitive, and satisfying moments;
- whether the player immediately wanted another run.

### Tuning rules

- Change one economy or threat variable family at a time.
- Store changes in existing tuning ScriptableObjects.
- Do not increase enemy count to solve low pressure.
- Do not add raw health to solve unclear counterplay.
- Preserve the four-response cap and safe-entry rules.
- Prefer stronger decisions, clearer telegraphs, and terrain use over stat inflation.

### Milestone acceptance criteria

- At least 70% of first-session human runs reach the extraction choice.
- Both extraction modes are selected for understandable reasons.
- Optional greed is neither always correct nor always avoided.
- Every enemy role produces a distinct, correctly identified response.
- At least two upgrade combinations change routing or combat.
- No dominant route/loadout appears across matched runs.
- A majority of testers choose to replay without prompting.

## 7. Perform the manual feel pass automation cannot replace

**Priority:** P1  
**Outcome:** Animation, camera response, audio, and controller usability receive a real sensory review.

### Checklist

- Verify acceleration, stopping, dash recovery, and collision feel.
- Verify enemy windups, attacks, hit reactions, purge reactions, and stuck recovery.
- Listen to movement, tower, salvage, weapon, drain, impact, low-Signal, extraction, victory, and failure cues.
- Check whether simultaneous cues mask the most important warning.
- Test default camera impulse, steady camera, and reduced flashes.
- Verify controller focus, selected-control styling, F5/LB+Menu opening, and every combat/extraction choice.
- Record cues that are late, repetitive, too quiet, too loud, or visually disconnected.

## Definition of done for the next milestone

Do not begin another content-expansion pass until all are true:

- Eastern Room Combat passes five consecutive loads.
- Three packaged LiveBalance routes produce combat/evasion telemetry and no automation errors.
- Camera and VFX acceptance frames pass at eastern combat and extraction.
- HUD, debrief, and AutoUI layouts pass at 1280×720 and 1600×900.
- Combat Frame captures gameplay correctly.
- Route reports match terminal outcomes and use clear shortcut terminology.
- Full EditMode and PlayMode suites pass in Unity 6000.3.11f1.
- Windows development build and packaged smoke pass.
- Human playtests provide evidence that the current run is replayable and fun.

## Current evidence baseline

Use these numbers only as the before-state:

- Deterministic Full Extraction: 42.14s and 41.68s, both 9/9, no route recoveries.
- LiveBalance Full Extraction: failed at 35.04s, final Signal 0, 16 Sapper drains, 0 shots, 0 purges.
- Packaged composition: 96 authored obstacles and 7 salvage instances.
- Runtime scans: no compiler errors, null/missing references, assertions, unhandled exceptions, build failures, or NavMesh errors.

