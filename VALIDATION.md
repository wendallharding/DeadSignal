# DEAD SIGNAL validation lanes

Use the smallest lane that can falsify the active change. Combine related focused test names with semicolons so each platform pays Unity startup/import cost once.

## Development lane — every implementation slice

1. Compile/import once with Unity `6000.3.11f1`.
2. Run focused EditMode tests for changed deterministic rules.
3. Run focused PlayMode tests for changed runtime, authored, input, presentation, or scene behavior.
4. Scan the final logs for compiler diagnostics, failed assertions, missing/null references, unhandled exceptions, and NavMesh errors.

Use `Tools/Run-DeadSignalValidation.ps1 -Lane FocusedEdit -TestFilter "Test.One;Test.Two"` and the corresponding `FocusedPlay` lane. Do not launch one Unity process per test class.

## Escalation lanes

| Lane | Category | Trigger |
| --- | --- | --- |
| Required route | `RouteRegression` | Progression, navigation, room state, doors, extraction, boot, or outcome flow changed. This is the one immediate complete-route proof. |
| Optional route | `OptionalRouteRegression` | Optional greed, weapon evolution, or optional extraction response changed. |
| Live balance | `LiveBalance` | An act completed, or combat, Signal, recovery, spawn, pacing, or route distance changed. Run at a deliberate evidence milestone, not after every objective state. |
| Combat evidence | `CombatEvidence` | Population, encounter timing, combat arena, performance population, or specialist composition changed. |
| Release validation | `ReleaseValidation` | Weekly/milestone validation, packaging/boot integration, scene architecture, or release candidate. This includes deliberately duplicated end-to-end contracts. |

Run a category with `Tools/Run-DeadSignalValidation.ps1 -Lane RouteRegression` (or the matching lane name).

## Complete regression and player build

Run `FullEdit`, `FullPlay`, the Windows development build, and packaged smoke as a separate gate at least weekly and whenever a phase/act completes, packaging or boot changes, scene architecture is integrated, or a release candidate is prepared. Keep feature implementation out of that gate so failures remain attributable.

The complete PlayMode suite is not an automatic postcondition for every bounded development slice. The Run 150 baseline took `520.49s`; seven route/combat tests accounted for `380.41s` (`73.1%`).

## Failure policy

- Rerun the exact failing test first, followed by its focused neighborhood or category.
- Repeat the complete suite only when the correction changes production code or shared setup.
- A test-only timeout or expectation correction does not by itself require another complete-suite run.
- Do not repeatedly run a known failing slow lane without a relevant change or explicit new evidence goal. Preserve and report the failure instead.
- Never describe a lane as passing unless that exact lane ran.
