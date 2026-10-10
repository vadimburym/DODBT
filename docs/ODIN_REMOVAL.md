# Odin removal in the example project

Work branch: `feature/odin-removal`, based on `example-project` at `56808ed`.
Unity: `6000.0.51f1`. The embedded DODBT C# sources match main at `81019bf`.

## Working agreement

- Develop and validate in this complete Unity project.
- Commit and push each completed stage to the work branch.
- Keep the main branch and standalone package untouched. Porting a tested release is a separate iteration.
- Only replace Odin usage belonging to `Assets/VadimBurym-DODBT`. Leave Odin-dependent code and inspectors elsewhere in the example project unchanged. Example assets may be used as regression fixtures.
- Preserve script meta GUIDs, serialized field names, and existing graph/compiled assets.
- Report unverified platforms and unresolved issues explicitly; passing editor compilation is not a player test.

## Stages

1. [x] Create the isolated branch and record the baseline and acceptance scope.
2. [x] Replace the plugin's Odin inspectors, polymorphic editing, and compiler cloning with Unity-backed implementations; remove Odin source dependencies inside the plugin only.
3. [x] Add and run serialization/compiler/editor regression tests against both fixtures and the existing example tree; harden validation and editing lifecycle.
4. [x] Build and exercise the example without Odin on installed Windows/WebGL targets; document verified targets and outstanding checks.
5. [ ] Complete interactive visual acceptance of the replacement inspectors before declaring full parity or porting the package to main.

## Acceptance scope

- Leaf type discovery and selection, including nested `IComparison` managed references.
- Editing inline and in Details, nested serialized fields, collections, Unity asset references, Undo/Redo, and read-only debugging.
- Graph metadata, compile warnings, open-editor/open-debug buttons, and live running-leaf display.
- Example-specific patrol point tooling and Odin attributes are outside this migration's scope and remain unchanged.
- Independent compiled leaf snapshots; saved/reloaded graph and compiled assets retain data and object references.
- Existing asset compatibility, graph copy, invalid graph/type diagnostics, and preservation of the last good compiled tree after failure.
- The plugin requires no Odin/Sirenix assemblies or symbols. Its editor code is excluded from players. Other example-project tools may still use Odin.
- Existing runtime tests plus new serialized-asset and compiler tests.
- Windows and WebGL checks using installed support. Other targets require their SDKs/build environments and are not certified by these runs.

## Evidence

- Initial checkout is clean and tracks origin/example-project.
- No Odin plugin files are present in the tracked project.
- Baseline: 39 runtime tests; no compiler/serialized-asset regression suite.
- Validation results will be recorded as stages complete. Generated logs and builds remain under ignored Logs/ and Builds/.
- Baseline run: Unity 6000.0.51f1, 39/39 EditMode tests passed.
- Stage 2 run: editor compiles without Odin; 39/39 existing tests passed. New UI and serialized-asset paths still require stage 3 regression checks.
- Scope correction: restored the original example leaf/provider files and removed the added patrol inspector. Further migration changes are restricted to the plugin.
- Stage 3: 52/52 EditMode tests passed (39 existing, 13 new cases). Regression coverage includes nested managed references, cycles and aliases, nonserialized state, external asset references, saved/reloaded assets, the existing example tree, Undo/Redo, GUID-based inspector lookup, runtime type filtering, and invalid graph rejection without replacing the last compiled result. Interactive visual checks remain part of stage 4.

## Reproducing player verification

Use Unity 6000.0.51f1 with this project closed in other editor instances.

1. Run EditMode tests with `-batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode -testResults <results.xml> -logFile <editor.log>` (without `-quit`).
2. Build Windows with `-batchmode -nographics -quit -projectPath <project> -executeMethod VadimBurym.DodBehaviourTree.Tests.BuildVerification.Windows -logFile <build.log>`.
3. Run `Builds/WindowsSmoke/DODBT.exe -batchmode -force-d3d11 -logFile <player.log>`. Expect exit code 0 and both `DODBT_SERIALIZATION_AND_RUNTIME_PASSED` and `DODBT_SMOKE_PASSED`.
4. Build WebGL with `-batchmode -nographics -quit -projectPath <project> -buildTarget WebGL -executeMethod VadimBurym.DodBehaviourTree.Tests.BuildVerification.WebGL -logFile <build.log>`. Serve `Builds/WebGLSmoke` over HTTP with the appropriate Content-Encoding for Unity's compressed files, then check the same console markers in a WebGL2 browser.

The verification builder creates a temporary compiled tree using the new compiler, includes it in Resources, enables `DODBT_PLAYER_SMOKE` only for that build, and uses High managed stripping. It restores the stripping setting and removes temporary assets in `finally`. The verification player checks nested types, aliases, a cyclic managed reference, an external texture, excluded runtime state, and 100 tree ticks. It also runs the unchanged example scene for 10 seconds and fails on observed managed errors/assertions/exceptions. This is a smoke check, not comprehensive gameplay coverage. The smoke assembly is excluded from normal players.

### Recorded player results

- Windows x64 / Mono: ordinary example build succeeded. Verification build with High stripping succeeded (0 errors; 2 URP shader warnings). Verification player passed with Direct3D 11 and exit code 0. A headless Null-device run also passed the tree checks, but emitted expected unsupported-shader diagnostics; use the Direct3D run for graphical startup evidence. Unity/URP reports a ComputeBuffer disposal warning at shutdown; this has not been attributed to DODBT.
- WebGL / IL2CPP / High stripping: build succeeded (0 errors; 4 URP shader warnings). Served locally with Brotli Content-Encoding and ran in the Chromium-based in-app browser. Both smoke markers passed, the example scene rendered, and no browser console errors or warnings were captured.
- Final default-configuration EditMode run: 53/53 passed, including a 256-node tree capacity/execution regression.
- `DODBT_SMALL_SIZE` EditMode run: 53/53 passed, including rejection of 256 reachable nodes. The original project define settings were restored afterward.

## Remaining acceptance

- Interactive Unity visual acceptance is pending. The editor launched with a temporary copy of the example graph, but the Windows capture helper failed twice (`FrameArrived timed out`, then `window capture timed out`). No successful screenshot or interaction evidence was obtained. Do not infer visual parity from the automated tests.
- Use `Tools > VadimBurym > Verification > Open temporary example graph` for a nonpersistent test copy. Check inline and Details editors, searchable leaf and nested comparison pickers, collection add/remove/reorder, long labels, field editing/Undo/Redo, compiling, debug-mode read-only behavior and live status, then domain reload with the editor open. This test copy is not a saved project asset.
- Android, iOS, consoles, macOS and Linux players were not built or run; their SDKs/host environments were not available in this validation. Windows IL2CPP was not tested; the AOT test above is WebGL IL2CPP.
- Odin is absent from this clone. The plugin has no remaining Odin/Sirenix source dependency, but coexistence with an installed Odin package has not been run. Unrelated example-project Odin code remains identical to the base commit.
- Main and the standalone package have not been changed. Keep this branch as an experiment until the remaining acceptance is complete.
