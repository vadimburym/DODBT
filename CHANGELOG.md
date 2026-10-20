# Changelog

## 1.0.0 — Unreleased

Changes since DODBT 0.4.0.

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

### DODBT-Light
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

### DODBT-Dots
- Added a Unity Entities version with Burst and Jobs execution.
- Added shared immutable tree blobs and separate agent state buffers.
- Added Roslyn-generated runners and leaf state access for unmanaged contexts.
- Added typed context creation, leaf ID validation, and generated lifecycle methods.
- Added the graph editor, linked groups, search, clipboard operations, and Undo/Redo for Dots.
- Added World-aware BT Debugger, active leaf lists, Scene View Observe markers, and selected-agent snapshots.
- Excluded debug tracing components and editor tools from player builds.
- Added graph revision checks to prevent observing mismatched runtime data.
- Fixed random selector wraparound and random state preservation across ticks and aborts.
- Fixed nested cached Parallel traversal and completed node state preservation during aborts.
- Fixed small-index initialization and root markers with `DODBT_SMALL_SIZE`.
- Added validation for parent links, contexts, leaf IDs, tree limits, and abort stack capacity.
- Fixed debug cleanup after entity destruction, World disposal, and session changes.
- Fixed blob and native resource cleanup after partial initialization failures.
- Added Battle Example with 10,000 agents, an isolated ECS World, shared trees, and staggered AI ticks.
- Added ExampleContext, ExampleBrain assets, movement controls, frame/FPS and entity-count HUDs.
- Fixed scene selection markers and VAT selection bounds.
- Fixed damage overflow and battle session resource cleanup.
