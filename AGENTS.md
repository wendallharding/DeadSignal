# Repository Instructions for Codex

These instructions apply to all files in this repository unless a more specific `AGENTS.md` exists in a subdirectory.

## Project Context

- This is the Unity project for **DEAD SIGNAL**, pinned to Unity `6000.3.11f1` and using C# 9-compatible language features.
- Primary first-party runtime code lives under `Assets/DeadSignal/Runtime`.
- EditMode tests live under `Assets/DeadSignal/Tests`; PlayMode tests live under `Assets/DeadSignal/Tests/PlayMode`.
- The current playable bootstraps from `Assets/Scenes/SampleScene.unity` and constructs prototype content at runtime.
- Core project packages include URP, Unity Input System, and Unity Test Framework. Do not assume Whitechapel-specific systems such as Fusion, Reflex, Wwise, Addressables, Odin, DOTween, or Vivox are available.
- `GAME_VISION.md` defines the product direction, `BACKLOG.md` tracks priorities, and `DEVLOG.md` records completed autonomous work and validation evidence.
- This file is the repository-wide source of truth for coding conventions. Match the surrounding file when applying a convention mechanically would create unrelated churn.

## General Approach

- Prefer small, focused changes that solve the requested problem without broad unrelated cleanup.
- Preserve existing behavior unless the task explicitly requests a behavior change.
- Fix correctness first, then style.
- Keep behavior changes separate from style-only changes when practical.
- Do not reformat, reorder, or rename unrelated code while touching a file.
- Avoid introducing new packages, frameworks, namespaces, or architectural patterns unless they fit the project and materially help the requested work.
- Inspect nearby implementations, assembly definitions, tests, product notes, and bootstrap/composition code before introducing a new pattern.
- Preserve user changes and do not modify unrelated files, even when the workspace has no Git baseline.
- Keep the established DEAD SIGNAL concept coherent. Do not replace the core vision casually; record material product decisions in the project documentation.

## Repository and Unity Asset Safety

- Treat third-party, package-cache, template, and generated content as read-only unless the task explicitly targets it.
- Do not manually edit Unity-generated `.csproj`, `.sln`, or `.slnx` files. They are derived artifacts.
- Do not hand-edit content under `Library`, `Temp`, `Logs`, or `UserSettings`. Unity and test commands may write their normal generated output there.
- Only change `ProjectSettings`, `Packages/manifest.json`, or `Packages/packages-lock.json` when the task requires it. Call out these project-wide changes in the handoff.
- Keep a Unity asset and its `.meta` file together. Preserve the existing `.meta` GUID when moving or renaming an asset, and never replace an existing `.meta` merely to regenerate it.
- When adding a Unity asset, ensure its `.meta` is generated and included after Unity imports it. If Unity import cannot run, explicitly report that the `.meta` still needs generation and validation.
- Avoid hand-editing Unity YAML scenes, prefabs, and `.asset` files when an Editor-based change is safer. If text editing is necessary, keep the change minimal and preserve file IDs and GUID references.
- Do not rewrite large binary, scene, prefab, or imported assets unless required by the task.

## Generated Files

- Do not hand-edit generated source. Change its source asset or generator and regenerate it through the established Unity workflow.
- Treat Unity-generated IDE projects and code generated from `.inputactions` assets as derived artifacts.
- Preserve generated naming and formatting even when it differs from first-party conventions.

## Formatting and Language Style

- Use four spaces for C# indentation and keep lines within 140 characters where practical.
- Do not leave trailing spaces, trailing tabs, or trailing blank lines.
- Prefer `var` for local variables when the assigned type is apparent from the right-hand side.
- Use block-scoped namespaces; do not introduce file-scoped namespaces.
- Be explicit with access modifiers on non-interface members.
- Use built-in type keywords such as `int`, `string`, and `bool`.
- Prefer guard clauses and early returns when they improve readability.
- Multiline control-flow bodies require braces. A truly single-line body may omit braces when that matches the surrounding file.
- Preserve intentional suppression comments and formatter regions unless the underlying reason is removed.

## Naming

### Types and Public API

- Use `PascalCase` for classes, structs, interfaces, enums, delegates, public properties, public fields, events, and public methods.
- Prefix interfaces with `I`.
- Prefer public properties over public mutable fields unless Unity serialization or an established local pattern requires a field.
- Use private setters when outside code should read but not mutate a property.

### Fields and Constants

- Private instance fields use `m_camelCase`.
- Private static and private static readonly fields use `s_camelCase`.
- Public static readonly fields use `PascalCase`.
- Constants use `ALL_CAPS_WITH_UNDERSCORES` regardless of accessibility.
- Do not introduce `_fieldName` for private fields.
- Preserve serialized field names unless the task includes a safe Unity serialization migration.
- Existing code predates these field and constant conventions. Apply them to new code and purposeful refactors, but do not rename unrelated existing members solely for consistency.

```csharp
public const int MAXIMUM_SALVAGE = 3;
public static readonly Color DefaultSignalColor = Color.cyan;

private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
private float m_currentSignal;
```

### Local Variables and Parameters

- Use `camelCase` for local variables and parameters.
- Prefer descriptive names over abbreviations except for established domain terms such as `dt`, `hud`, or `ui`.

### Methods and Callbacks

- Public methods use `PascalCase`.
- Custom private and protected helper methods start with `_` followed by `camelCase`, such as `_updateSignalHud`.
- Framework, generated, reflected, interface, and override methods retain their required names and do not receive the private-helper prefix.
- Preserve Unity lifecycle and message names such as `Awake`, `Update`, `OnEnable`, `OnDisable`, `OnDestroy`, `OnGUI`, and `OnValidate`.
- NUnit setup/teardown, Unity test callbacks, Editor callbacks, gizmo drawers, animation events, and serialized UnityEvent targets retain their prescribed names.
- Before renaming a method referenced by a UnityEvent, animation event, Visual Scripting graph, reflection, or serialized string, verify and migrate every reference.
- Existing helpers predate the underscore-prefix convention. Do not create a broad rename-only diff; use the convention for new helpers and migrate old helpers only during a focused refactor.

## Class Organization

Preferred layout, organize class members in this order:
1. Fields with attributes
2. Public fields
3. Public properties
4. Unity / Fusion lifecycle methods
5. Public methods
6. Private methods
7. Private fields

- When modifying a class reorder the existing class if it does not satisfy the preferred layout.
- Keep related state and behavior close enough that ownership is easy to understand.
- Group serialized fields by Inspector purpose, using attributes such as `[Header]` where that improves authoring clarity.
- Keep public and interface methods before private helpers when consistent with the local file.
- Keep one primary type per file unless a small helper type is meaningful only to that file.
- File names should match their primary type.
- Avoid letting the runtime bootstrap controller become a permanent home for unrelated systems. Extract focused components or pure C# services as features gain independent state, dependencies, lifecycle, or tests.

## Composition and Class Responsibility

- Prefer composition and dedicated, narrowly scoped classes over expanding an existing class beyond its responsibility.
- A class or MonoBehaviour being used in only one place is not a reason to merge it into another class when it owns independent state, lifecycle, configuration, or behavior.
- Add behavior to an existing class only when that class clearly owns it and the change preserves its invariants.
- For Unity presentation features, prefer a dedicated MonoBehaviour on the GameObject it controls once the feature is more than a small part of the current prototype bootstrap.
- Keep MonoBehaviours focused on Unity lifecycle, scene references, presentation, and orchestration. Move deterministic game rules into plain C# classes when practical.
- The existing split between `RunModel` and `DeadSignalGame` is the preferred direction: deterministic rules should remain engine-independent and directly testable.
- Avoid unnecessary global state, singleton access, string-based scene lookups, and hidden dependencies.
- Prefer event-driven or callback-based communication over polling when it makes ownership clearer.
- Subscribe and unsubscribe symmetrically. `OnEnable` normally pairs with `OnDisable`; owned disposable resources must be cleaned up when their owner ends.

## Tuning Data

- Prioritize ScriptableObject assets for designer-facing gameplay, balance, AI, movement, combat, economy, camera, audio, VFX, and presentation tuning instead of hardcoding adjustable values in scripts.
- Treat values that may change during playtesting, differ by difficulty or content variant, or need coordinated adjustment as tuning data. Group related values into focused configuration assets rather than creating one oversized global settings asset.
- Keep true code invariants, fixed protocol values, array bounds, and values required by compile-time APIs as constants or static readonly members; do not move values into ScriptableObjects merely to eliminate every literal.
- Pass tuning assets through serialized references or explicit composition. Keep deterministic rules directly testable by accepting the required configuration or copied immutable values rather than reaching into global assets.
- Give tuning assets safe defaults and validate invalid ranges or relationships with `OnValidate`, Editor validation, or focused tests where appropriate.
- When changing a system that contains hardcoded tuning, migrate the values relevant to the requested work when practical. Avoid broad unrelated tuning migrations solely for consistency.

## Unity Serialization and Object Semantics

- Use `[SerializeField] private` for Inspector references that should not be publicly mutable.
- Use `[SerializeReference]` only for justified polymorphic serialization and verify every concrete type remains serializable.
- If a serialized field must be renamed, preserve data with `[FormerlySerializedAs("oldName")]` and validate affected scenes, prefabs, and assets in Unity.
- Type or namespace moves can break serialized references. Use Unity migration attributes such as `[MovedFrom]` when appropriate and validate the migration in the Editor.
- Preserve Unity's destroyed-object null semantics. Do not mechanically replace Unity object truthiness or `== null` checks with `ReferenceEquals` or pattern matching.
- Avoid changing serialized collection shapes, enum numeric values, ScriptableObject contracts, or save-data formats without a migration plan.
- Prefer serialized references, registries, factories, or explicit composition over runtime hierarchy searches outside bootstrap, composition, test, and Editor code.
- When a search is justified, use current Unity APIs such as `FindAnyObjectByType`, `FindFirstObjectByType`, or `FindObjectsByType` with explicit inactive/sort behavior.

## Level Collision Authoring

- Authored obstacle bounds must stay aligned to the obstacle's transformed local axes. Do not expand rotated scene or prefab obstacles into world-axis-aligned bounding boxes for movement collision.
- Represent a rectangular blocker with its center, scaled local half-extents, and normalized right/forward axes; test player-circle overlap in that oriented basis.
- Keep `AuthoredMapObstacleTests.OverlapsCircle_UsesObjectAlignedBounds` as a regression rule whenever authored blocker math or map-obstacle registration changes.

## Input, Frame Updates, and Async Work

- Use the installed Unity Input System for player input; preserve complete keyboard/mouse and controller paths unless the task explicitly changes supported input.
- Read frame input and perform presentation work in the appropriate Unity update loop. Keep deterministic resource and objective rules independent of frame rate.
- Avoid allocations, hierarchy searches, material creation, and noisy logging in per-frame paths unless measured and justified.
- Avoid `async void` except for Unity lifecycle methods, event callbacks, or APIs that require `void`; catch and report exceptions at those boundaries.
- Make long-running or lifetime-bound operations cancellable and cancel/dispose them when ownership ends.
- Access Unity APIs only from the Unity thread unless an API explicitly supports background use.

## Logging

- Use `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` sparingly because this project does not yet have a structured logging layer.
- Do not add noisy per-frame logging.
- Remove temporary diagnostic logs before finishing unless they provide lasting developer value.
- Do not silently swallow unexpected exceptions. Include enough context to diagnose them without exposing secrets or personal data.

## Comments and Documentation

- Comments should explain intent, constraints, lifecycle ownership, serialization requirements, units, or other non-obvious behavior.
- Avoid comments that merely restate the code.
- Document public APIs when ownership, units, side effects, or invariants are not obvious.
- Keep TODOs actionable and scoped; include the reason or missing dependency rather than a vague reminder.
- Update `GAME_VISION.md` when product direction or acceptance criteria materially change.
- Keep `BACKLOG.md` prioritized and consistent with implemented work.
- Append exact test outcomes, known limitations, and the next best step to `DEVLOG.md` for autonomous development runs.

## Tests and Validation

- Use Unity Test Framework and NUnit through the existing test assembly definitions.
- Put deterministic engine-independent tests in `Assets/DeadSignal/Tests` and Unity lifecycle/runtime tests in `Assets/DeadSignal/Tests/PlayMode`.
- Prefer EditMode tests for pure logic and PlayMode tests for scene bootstrap, Unity lifecycle, input, movement, rendering state, and runtime integration.
- Add references to an assembly definition only when a test or runtime assembly genuinely requires them.
- Run the smallest relevant test set first. For risky or cross-cutting changes, run the broader applicable EditMode and PlayMode suites.
- Unity compilation and Unity Test Runner results are authoritative. `dotnet build` or `dotnet test` against Unity-generated project files is not equivalent.
- Use the pinned Unity `6000.3.11f1` Editor for import, compilation, and tests when available.
- If the project is already open in Unity, do not close the user's Editor or risk unsaved state. Use the live Test Runner when practical or validate against a safe isolated project copy and report that distinction.
- Never claim a test passed unless it actually ran. Report the exact command or test platform, pass/fail counts, exit code, and relevant log/result paths.
- Scan final Unity logs for compiler errors and warnings, unhandled exceptions, null or missing references, and failed assertions.
- Validate scene, prefab, material, shader, and other visual changes interactively when possible; automated construction tests do not prove presentation quality or game feel.

## Refactoring Existing Code

When editing an existing file:

1. Fix correctness first.
2. Match the file's current style unless cleanup is part of the task.
3. Preserve serialized names, asset GUIDs, generated boundaries, and save-data contracts.
4. Reorder or rename members only when the task includes that cleanup and only inside touched types.
5. Avoid formatting unrelated methods.
6. Keep behavior changes separate from style-only changes when practical.
7. Add or update focused tests when the changed behavior is testable.

## Response Expectations

When Codex finishes a task:

- Mention every source, asset, configuration, and documentation file changed.
- Mention tests, compilation, formatting, Unity import, and visual validation performed.
- If validation did not run or could not run, state that clearly and explain the remaining manual check.
- Clearly call out package, project-setting, scene, prefab, serialized-data, or generated-file changes.
- Clearly call out conventions intentionally not followed and why.
- Mention any new Unity asset whose `.meta` still requires generation or validation.
