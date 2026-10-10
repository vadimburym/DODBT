# Odin removal in the example project

Work branch: `feature/odin-removal`, based on `example-project` at `56808ed`.
Unity: `6000.0.51f1`. The embedded DODBT C# sources match main at `81019bf`.

## Working agreement

- Develop and validate in this complete Unity project.
- Commit and push each completed stage to the work branch.
- Keep the main branch and standalone package untouched. Porting a tested release is a separate iteration.
- Preserve script meta GUIDs, serialized field names, and existing graph/compiled assets.
- Report unverified platforms and unresolved issues explicitly; passing editor compilation is not a player test.

## Stages

1. [x] Create the isolated branch and record the baseline and acceptance scope.
2. [ ] Replace Odin inspectors, polymorphic editing, and compiler cloning with Unity-backed implementations; remove all source dependencies, including the example helpers.
3. [ ] Add and run serialization/compiler/editor regression tests against both fixtures and the existing example tree; harden validation and editing lifecycle.
4. [ ] Build and exercise the example without Odin, document verified targets and outstanding platform/UI checks.

## Acceptance scope

- Leaf type discovery and selection, including nested `IComparison` managed references.
- Editing inline and in Details, nested serialized fields, collections, Unity asset references, Undo/Redo, and read-only debugging.
- Graph metadata, compile warnings, open-editor/open-debug buttons, and live running-leaf display.
- Patrol point collection and read-only generated patrol data in the example.
- Independent compiled leaf snapshots; saved/reloaded graph and compiled assets retain data and object references.
- Existing asset compatibility, graph copy, invalid graph/type diagnostics, and preservation of the last good compiled tree after failure.
- No Odin/Sirenix assemblies or symbols required. Editor code excluded from players.
- Existing runtime tests plus new serialized-asset and compiler tests.
- Windows and WebGL checks using installed support. Other targets require their SDKs/build environments and are not certified by these runs.

## Evidence

- Initial checkout is clean and tracks origin/example-project.
- No Odin plugin files are present in the tracked project.
- Baseline: 39 runtime tests; no compiler/serialized-asset regression suite.
- Validation results will be recorded as stages complete. Generated logs and builds remain under ignored Logs/ and Builds/.
