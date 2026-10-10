# Changelog

## [Unreleased] - Odin removal experiment

- Replaced the plugin's Odin editor integration with Unity inspectors, searchable managed-reference type selection, and inline/Details editing.
- Replaced compiler binary serialization with native Unity leaf snapshots, preserving managed references and Unity asset references.
- Added serialization/compiler regression tests and opt-in Windows/WebGL player verification.
- Added invalid graph diagnostics and retained the previous compiled tree after compilation failures.
- Preserved existing serialized field names and script GUIDs. Example-specific Odin tooling remains outside the migration scope.
- This experiment is validated in the example project; package release and migration to main are separate work.

## [0.2.0-early] - 2026-04-06

### Added
- Added EditMode tests for behavior tree runtime.
- Added coverage for composite nodes and leaf lifecycle.
- Added DODBT_SMALL_SIZE compiler directive to limit graph size to 255 nodes and reduce memory usage.

### Changed
- Increased maximum number of child nodes to 255.
- Update README.md
- Update LICENSE.md
