# Changelog

## [Unreleased]

### Added
- `TickWithStatus` returns the root result while retaining the existing `Tick` API.
- Optional Timeout, Cooldown, and Inverter nodes behind `DODBT_DECORATORS`, with one-child graph ports, inline/Details settings, and compiler validation.
- Explicit timestamp overloads for timed trees, independent per-agent deadlines, deterministic deadline boundaries, and abort propagation. Cooldown starts after child completion and retains an existing deadline across aborts.
- Runtime compatibility checks and build checks for scene, Resources, and preloaded-asset dependencies. Disabling decorators preserves their serialized descriptions and rejects unsupported execution.
- Graph search by title, node kind, leaf class, and namespace, with result highlighting and navigation in editing and Debug Mode.
- Copy/paste and duplicate for individual nodes and selected blocks. Copies retain internal connections, relative positions, managed-reference data, and Unity asset references; use new GUIDs; and support Undo/Redo. External connections are excluded.

### Fixed
- Replacing a connection on a single-capacity port updates both graph data and displayed edges.
- Editing a graph without a compiled asset no longer tries to mark a null object dirty.

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
