# Changelog

## 1.0.0 — Unreleased

### Changed
- Renamed the package to `com.vadimburym.dodbt.light` and the API namespace to `VadimBurym.DodBehaviourTree.Light`.
- Added independent Context and LeafState selection in the graph toolbar.
- Added `BtLeafStateAttribute` and a New LeafState command.
- Preserved context and state discovery through existing `ILeaf<TContext, TLeafState>` implementations.
- Added joint Context/LeafState validation for leaf selection and compilation.
- Added BT Debugger with agent search, pages, and active leaf lists.
- Added Scene View Observe markers and one-call debugging setup through `BtMonoDebug.Attach`.
- Added compiled revision checks and support for observing older assets without revision metadata.
- Fixed empty inline leaf inspectors and missing composite settings in icon mode.
- Fixed stale graph bindings and debug state cleanup when agents are detached or reused.
- Simplified Example2D while preserving LeoEcsLite, LeoEcsLite.Di, ServiceLocator, and gameplay.
- Added a single Example2D scene, ExampleBrain assets, and a movement controls HUD.
- Removed Odin and unused example infrastructure.
- Fixed projectile spawning, collision handling, pool visibility, and session cleanup.

### Shared editor and packaging
- Added separate DODBT-Light and DODBT-Dots 1.0.0 packages with isolated namespaces, assemblies, GUIDs, and editor settings.
- Added compact launch windows under Tools → VadimBurym for each version.
- Added context and leaf creation windows with searchable type selection and remembered settings.
- Added optional composite node icons and custom leaf icons through `LeafIconAttribute`.
- Added context mismatch warnings directly on graph nodes.
- Improved inline inspectors, type picker responsiveness, toolbar layout, and node settings.
- Reduced debug repaint work, redundant graph updates, repeated type searches, and inspector allocations.
- Added English and Russian guides, API references, screenshots, and animated examples.
- Moved optional examples to `Samples~` for Package Manager import.
- Unified the license for both plugin versions.
- Expanded runtime, compiler, serialization, and editor regression tests.


## [0.4.0] - 2026-10-12

### Added
- Editor-only linked branch groups with Create Linked Group, Duplicate Group, and Unlink Group commands. Edits in any instance synchronize serialized node settings, leaf types and fields, structure, and relative layout while retaining independent managed data.
- Group root icons, yellow highlighting of related nodes and internal edges, and a Groups tab with editable names and instance counts. Groups cannot overlap or nest; invalid connections report warnings in the graph editor's own console.
- Group-aware deletion, disconnection, clipboard handling, and Undo/Redo. Group metadata persists in graph assets and is absent from compiled runtime assets.
- `TickWithStatus` returns the root result while retaining the existing `Tick` API.
- Optional Timeout, Cooldown, and Inverter nodes behind `DODBT_DECORATORS`, with one-child graph ports, inline/Details settings, and compiler validation.
- Explicit timestamp overloads for timed trees, independent per-agent deadlines, deterministic deadline boundaries, and abort propagation. Cooldown starts after child completion and retains an existing deadline across aborts.
- Runtime compatibility checks and build checks for scene, Resources, and preloaded-asset dependencies. Disabling decorators preserves their serialized descriptions and rejects unsupported execution.
- Graph search by title, node kind, leaf class, and namespace, with result highlighting and navigation in editing and Debug Mode.
- Copy/paste and duplicate for individual nodes and selected blocks. Copies retain internal connections, relative positions, managed-reference data, and Unity asset references; use new GUIDs; and support Undo/Redo. External connections are excluded.

### Changed
- Dragging a linked-group root previews its members' positions continuously. The graph asset and Undo are updated when the drag finishes, without cloning leaf data on pointer movement. Escape cancels the move; losing mouse capture finishes it and stops native edge panning.
- Type pickers distinguish node leaf implementations (white leaf icons) from nested managed-reference values (neutral object icons). The field's role determines the icon, including nested collection elements.
- Restyled the inline and Details leaf selector as a rounded dark row with a white leaf icon, hover/focus feedback, and a namespace tooltip. Root Leaf fields are always visible below a subtle divider, with no parameter foldout. Nested managed references retain their own foldouts.

### Fixed
- Replacing connections on occupied single-capacity ports reports warnings in the graph editor's own console, naming the removed and new connections and the one-parent/one-child restriction. Rejected cycles and linked-group conflicts use the same internal console, without writing to the Unity Console. Free ports and unchanged connections do not warn.
- Clipboard insertion rejects cycles, duplicate/shared children, inconsistent parents, invalid decorator child counts, and non-finite positions before changing the destination graph. Incomplete and disconnected valid selections remain supported.
- The edge drop handler ignores read-only graphs and incomplete endpoints before changing displayed connections.
- Ordinary connections keep a stable width and color when the pointer passes over nodes or edges. Linked-group highlighting survives GraphView hit testing, layout updates, and selection changes.
- Multi-selection deletion and disconnection across linked group instances now combine all changes before synchronization, preventing deleted nodes or connections from returning. Undo/Redo restores the entire operation together.
- Deleting nodes schedules one graph rebuild and preserves the selection of surviving nodes.
- The focused leaf selector now opens with Space as well as Enter and the down arrow.
- Replacing a connection on a single-capacity port updates both graph data and displayed edges.
- Editing a graph without a compiled asset no longer tries to mark a null object dirty.
- Build compatibility inspection only loads behavior-tree asset paths, avoiding unrelated scene and assembly importers during player builds.
- Changing a decorator in Details refreshes its inline node inspector.

### Verification
- Unity 6000.0.51f1: 150/150 EditMode tests with decorators enabled and 123/123 without them, each with default indices and `DODBT_SMALL_SIZE`; zero failures or skips.
- Coverage includes runtime traversal, timers and aborts, serialization, compiler limits, independent agents, steady-state allocations, linked-group synchronization, native drag events, Undo/Redo, search, clipboard validation, and warnings delivered only to the graph console.
- Windows x64 / Mono / High stripping and WebGL / IL2CPP / High stripping: successful builds with zero errors and two URP shader warnings each. The Windows verification player passed serialization, runtime, and decorator smoke checks with exit code 0. Its existing ComputeBuffer disposal warning remains at shutdown. WebGL browser execution and additional player platforms have not been verified.
- The standalone package imports without Odin Inspector or Unity Test Framework installed. Editor interaction tests require a graphics device and are explicitly skipped with `-nographics`.
- The example AI system supplies `Time.timeAsDouble` when decorators are enabled, so timers follow scaled game time.

## [0.3.1] - 2026-10-12

### Changed
- Restyled the type picker with white leaf icons, emphasized class names, muted namespace labels, roomier rows, a current-type marker, and keyboard hints. Search matches class names and namespaces; long names expose their full qualified name in a tooltip. Assembly names remain hidden.

### Fixed
- Isolated the shuffle storage of nested `MemorySelector` nodes. Random traversal no longer overwrites a parent's order, skips siblings, or visits the same child twice in a fresh traversal. Ordered selectors continue to execute left to right.
- Cleared stale running status when a non-caching `Parallel` child completes. Aborting another active child no longer resets a completed `MemorySequence` with retained failure memory.
- Corrected debug running-leaf lists after internal aborts, explicit aborts, and state initialization. Cached `Parallel` child results remain visible on subsequent ticks.

### Verification
- Added regression coverage for nested ordered/random selector combinations, independent agent states on a shared tree, steady-state managed allocations, parallel completion, and debug state cleanup.
- Extended opt-in player smoke checks with compiled nested-selector and parallel graphs.
- Existing `MemorySelector` behavior after a remembered child fails is retained: retry children from the beginning, including that child. `ResetOnAbort`, `ResetOnFailure`, and leaf callback ordering are unchanged.
- Shuffle storage is allocated once during tree construction, with a separate segment per random selector; no per-agent shuffle arrays or per-tick allocations are introduced. Existing compiled assets require no format migration.
- Unity 6000.0.51f1: 66/66 EditMode tests passed with default settings and with `DODBT_SMALL_SIZE`. Nested shared-tree traversal and active-leaf debug tracking allocated zero managed bytes in the measured steady-state tests.
- Windows x64 / Mono / High stripping: build succeeded; the verification player passed serialization, runtime audit, and example smoke checks with exit code 0. The build reported 9 NavMeshPlus/URP warnings; the existing ComputeBuffer disposal warning remains at player shutdown.

## [0.3.0] - 2026-10-10

### Changed
- Removed the plugin's Odin Inspector and Odin Serializer dependency. Graph editing, asset inspectors, runtime debugging, and graph compilation now use Unity APIs.
- Replaced compiler binary cloning with independent native Unity leaf snapshots, preserving nested managed references, cycles, aliases, and Unity asset references.
- Added searchable leaf and nested managed-reference selection, collection editing, Undo/Redo, and read-only debug inspection.
- Type picker entries display `ClassName (Namespace)` without the assembly name.
- Preserved serialized field names and script GUIDs for existing graph and compiled assets.

### Fixed
- Restored private `[SerializeField]` leaf fields in inline and Details inspectors while respecting field-level `[HideInInspector]`.
- Reject invalid graphs, duplicate GUIDs, cycles, missing leaves, invalid parallel thresholds, and trees exceeding the configured node capacity before replacing a compiled asset.
- Restore editor state after domain reload and refresh graph views after Undo/Redo.

### Tests and packaging
- Added regression coverage for serialization, compilation, asset reload, type discovery, inspector fields, and Undo/Redo.
- Marked the test assembly as optional for UPM consumers. Tests requiring example-project assets are explicitly skipped when those fixtures are absent.
- Added opt-in Windows and WebGL player smoke verification; verification code is excluded from ordinary players.
- Updated Russian and English installation documentation for `v0.3.0`. The package has no third-party runtime or editor package dependencies.

### Upgrade
- Update the package to `v0.3.0`; existing script GUIDs and serialized field names are retained.
- Custom leaves use Unity serialization: `[Serializable]` types, `[SerializeField]` fields, and `[SerializeReference]` for polymorphic fields. Odin-specific attributes and unsupported Unity field types in consumer code are outside this package migration.
- Remove Odin from a consuming project only if no other tools or game code depend on it.

## [0.2.0-early] - 2026-04-06

### Added
- Added EditMode tests for behavior tree runtime.
- Added coverage for composite nodes and leaf lifecycle.
- Added DODBT_SMALL_SIZE compiler directive to limit graph size to 255 nodes and reduce memory usage.

### Changed
- Increased maximum number of child nodes to 255.
- Update README.md
- Update LICENSE.md
