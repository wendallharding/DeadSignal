# DEAD SIGNAL — Product Shell Lifecycle Audit

## Scope

This audit records the pre-menu runtime contract protected before Phase 6 navigation work. It covers the existing boot, pause, outcome, restart,
input-focus, and scene-lifetime paths. It does not authorize a second scene, save/continue behavior, gameplay changes, or a persistent runtime root.

## Current authority

- `SampleScene` is the only enabled build scene and remains the authored world source of truth.
- `DeadSignalBootstrap` subscribes once to `SceneManager.sceneLoaded` and also handles the initial loaded scene. It creates the runtime only when
  `DeadSignalSceneReferences` exists and no `DeadSignalGame` is already present.
- The runtime root, HUD instance, input actions, audio, combat feedback, Signal dust, and presentation services are scene-owned. Nothing uses
  `DontDestroyOnLoad`.
- The Reflex container owns the disposable `DeadSignalInput`. `DeadSignalGame.OnDestroy` disposes the world and container when the scene unloads.
- `RunModel.Outcome` is the terminal-state authority. `DeadSignalHud` exclusively selects the run, pause, or outcome overlay from outcome and pause
  state.
- Pause input is polled by `DeadSignalGame`. `_setPaused` synchronizes combat feedback, audio, Signal dust, player wake, and `Time.timeScale`.
  Losing application focus pauses only a running run; a new runtime restores `Time.timeScale` to `1` in `Awake`.
- Victory and defeat share the authored outcome overlay. Restart is currently a keyboard/gamepad action that reloads the active scene build index.
- The HUD prefab already owns an `EventSystem` and `InputSystemUIInputModule`, so the main-menu shell can add authored selectable controls without a
  second UI input stack.

## Protected lifecycle contract

`ProductShellLifecyclePlayModeTests` now protects these prerequisites:

1. Reloading the scene while paused destroys the old runtime and creates exactly one fresh game, HUD, audio service, and combat-feedback service.
2. A fresh runtime is running, unpaused, at normal time scale, with only the run HUD visible.
3. Victory and defeat each reveal only the outcome overlay.
4. Restarting from victory and defeat creates a new runtime instance with no stale pause or outcome presentation.
5. Repeated bootstrap callbacks do not duplicate the scene-owned runtime services.

## Phase 6 navigation constraints

- Add the main menu as another authored HUD-prefab overlay around the current `SampleScene` boot path. Do not introduce new scene architecture for
  this slice.
- Give one shell controller explicit ownership of `Menu`, `Running`, `Paused`, and `Outcome` presentation state. Keep `RunModel.Outcome` as gameplay
  authority rather than teaching the model about menus.
- Starting a run must reset scene-owned gameplay state through the existing clean construction path or an equally explicit reset contract.
- Returning to menu must restore normal time scale, stop gameplay input and simulation, select a valid keyboard/controller target, and avoid a
  second `EventSystem`, input-action set, audio service, or runtime root.
- Settings and Controls should reuse the existing comfort and rebind authorities. Menu controls must preserve the current keyboard/mouse and
  controller prompt paths.
- Quit must be available only through an explicit authored action. No Continue option or speculative persistence is in scope.

## Known gaps for the next slices

- An authored UI prefab nested under the existing HUD now owns a presentation-only main menu with Start Run, Settings, Controls, Quit, deterministic initial focus, and explicit
  arrow/Enter/Escape plus gamepad navigation. Normal launches stop the untouched scene-owned runtime before its first simulation frame; automated
  `-deadSignal` routes bypass the menu without creating a second runtime.
- Settings and Controls reuse the existing comfort preferences and keyboard-rebinding authority. There is still no Continue/save progression.
- Pause now exposes authored Resume Run and Main Menu actions; defeat and victory expose authored Restart Run and Main Menu actions. Returning to menu
  reconstructs the scene-owned runtime before showing the menu, so Start Run always begins a fresh mission rather than resuming abandoned state.
- The shell reasserts menu pause ownership after scene teardown, restores deterministic Start/Resume/Restart focus, and retains exactly one EventSystem,
  game, HUD, audio, combat-feedback, and six-action DEAD SIGNAL input set through repeated pause/defeat/victory loops.
- Defeat now owns a distinct `MISSION LOST` presentation: exact Signal-collapse cause, last objective and room, a compact pressure summary,
  one prioritized next-run lesson, Restart, and Main Menu. Victory now owns a distinct `MISSION COMPLETE` presentation with mission time, grade,
  Central-to-Dock restoration milestones, combat pressure, final/low/recovered Signal, personal best, route choice, Restart, and Main Menu. Both
  deterministic reports are constrained to the existing authored report region so they cannot cover the action row.
- Start Run now releases menu pause ownership through a short unscaled opacity transition; defeat and victory reveal the existing outcome hierarchy
  through the same focused tuning. Selection remains on Start or Restart throughout, activation is blocked only during the fade, and the Reduced
  Flashes alternative lengthens the luminance change from `0.18s` to `0.28s`. These transitions never drive the camera, so Steady Camera remains
  authoritative.
- Resolution, ultrawide, repeated menu-cycle, and human keyboard/controller presentation remain unproven until the corresponding Phase 6
  validation slices. Automated controller selection, complete-route compatibility, Windows build, and packaged command-line smoke now pass.
