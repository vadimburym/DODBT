# Changelog

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
