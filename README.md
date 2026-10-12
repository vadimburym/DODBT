[English](README.md) | [Русский](README.ru.md)

# DODBT Example Project

This Unity project demonstrates basic integration and runtime debugging of **DODBT** with a player and an AI-controlled enemy. It is a focused example, not a complete game.

The plugin version is **0.4.0**, identical to the package in `main`. The [0.4.0 release](https://github.com/vadimburym/DODBT/releases/tag/v0.4.0) includes `TickWithStatus`, optional Timeout/Cooldown/Inverter nodes, graph search, node copying, and linked branch groups. DODBT does not require Odin Inspector.

See the [0.4.0 guide](Assets/VadimBurym-DODBT/Documentation~/0.4.0.md) for the implemented features, timing rules, and Abort behavior.

## Running and debugging

Run the gameplay scene to inspect the example behavior tree. The enemy appears at `World -> Enemy(Clone)` and has a `BtMonoDebug` component for inspecting the tree while the game is running.

The project demonstrates runtime integration, graph editing and compilation, and debugging node states through `BtMonoDebug`.

## Controls

- `W` — move forward.
- `A` — move left.
- `S` — move backward.
- `D` — move right.

## Optional decorators

`DODBT_DECORATORS` is enabled for Standalone and WebGL in this project. In other projects, enable it through Scripting Define Symbols for the intended target. Trees containing Timeout or Cooldown require `Tick(context, state, now)` or `TickWithStatus(context, state, now)` with monotonic time in seconds.

The example AI system supplies `Time.timeAsDouble` when the extension is enabled, so pauses and game-time scaling affect timers. Without the symbol, it retains the original `Tick` call.

## Linked groups

Right-click a branch root and choose `Create Linked Group`. `Duplicate Group` creates a synchronized instance; `Unlink Group` leaves its nodes independent. Fields, leaf types, structure, and relative positions synchronize between instances. Dragging a root moves that instance's complete branch. The `Groups` tab lists families and instance counts.

Groups exist only in the editor. Runtime execution uses ordinary compiled nodes and retains left-to-right order.
