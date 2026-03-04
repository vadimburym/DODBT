<p align="center">
  <a href="README.en.md">English</a> |
  <a href="README.md">Русский</a>
</p>

## 10⭐ Why DODBT? (Data-oriented design Behaviour Tree)

⭐ **0 MonoBehaviour**  
Completely decoupled from the Unity lifecycle.

⭐ **0 runtime allocations**  
Predictable execution without GC.

⭐ **1 BT instance for N agents**  
Memory-efficient architecture.

⭐ **Runtime Debug Mode**  
Clean and convenient debugging.

⭐ **Full-featured BT Graph Editor**  
Logs, hints, and clear visualization.

⭐ **Graph and Runtime are separated**  
The graph is compiled into a compact and performant BT.  
Neither graph data nor editor tools are included in builds.

⭐ **BT is responsible only for behavior selection**  
The tree does not tick the behavior itself.  
BT can be updated at any frequency (for example once every N seconds).

⭐ **No Blackboard**  
Dependencies are passed directly via DI or ServiceLocator.

⭐ **ECS-friendly**  
Works great with ECS architecture.

⭐ **Production-ready**  
Game designers can configure BTs without touching code.

> ⚠ **IMPORTANT!** To access `EDITOR` and `DEBUG` tools you must install the `Odin Inspector` plugin. All other functionality (including builds) works without it, meaning only the game designer needs `Odin Inspector`.

> ⚠ **IMPORTANT!** DODBT has been tested on `UNITY 6000.051f1` and `Odin Inspector 4.0`. Compatibility with earlier versions is not guaranteed.

> ⚠ **IMPORTANT!** DODBT was originally built for internal use and is currently in early access. The plugin is already usable in production, but development, improvements, and bug fixing are ongoing.

<p align="center">
  <img src="docs~/logo.png" width="1050">
</p>

## 📑 Table of Contents

- [Installation](#установка)
- [Getting Started](#начало-работы)
- [Graph Editor](#редактор-графов)
- [Compilation and Build](#компиляция-и-сборка)
- [Debug Mode](#дебаг-режим)
- [Example Project](#проект-для-примера)
- [Public Classes and Methods](#публичные-классы-и-методы)

## Installation
- **As a Unity module** `RECOMMENDED`: Window → Package Manager → Install package from git URL:
```
https://github.com/vadimburym/DODBT.git?path=/source
```
- **As source code**: the repository can also be cloned or downloaded as an archive.

## Getting Started

- **Create a per-agent context**: a `class` representing the external runtime context in which the BT operates.
> **[EXAMPLE]** Example context for `LeoEcsLite`.
```c#
public sealed class LeoEcsContext
{
    public int AgentIndex = -1;
}
```
- **Create a per-leaf state**: a `struct` that stores the state of leaf nodes for a specific agent.
> **[EXAMPLE]** Example state for `LeoEcsLite`.
```c#
[Serializable]
public struct LeoEcsLeafState
{
    public int StateIndex;
    public void Reset() => StateIndex = -1;
}
```
- **Create leaves**: terminal BT nodes that represent condition checks, actions, or states executed within an agent context and maintaining their own execution state. Implement them by inheriting from `ILeaf<context, state>` anywhere in the project — `Odin Inspector` will automatically detect them and make them available in the graph editor. Add the `[Serializable]` attribute and mark parameters you want to edit from the graph with `[SerializeField]`.

> **[EXAMPLE]** Example leaf "Shoot the player N times" for `LeoEcsLite` using `DI`. The leaf does not tick the agent behavior itself — it only adds/removes a state from the agent. Actual behavior is ticked by separate ECS systems filtering entities by that state.
```c#
[Serializable]
public sealed class ShootPlayerLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>
{
    [SerializeField] private int _targetShots;
        
    private EcsWorld _world;
    private EcsPool<AgentEntity> _agentPool;
    private EcsPool<ShootEntityState> _shootStatePool;
    private EcsPool<PlayerVisibilitySensor> _sensorPool;
        
    [Inject]
    public void Construct(IEcsWorldsService service)
    {
        _world = service.GetWorld(EcsWorlds.BT_STATES);
        _agentPool = service.GetPool<AgentEntity>(EcsWorlds.BT_STATES);
        _shootStatePool = service.GetPool<ShootEntityState>(EcsWorlds.BT_STATES);
        _sensorPool = service.GetPool<PlayerVisibilitySensor>(EcsWorlds.DEFAULT);
    }

    public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
    {
        var status = _shootStatePool.Get(state.StateIndex).StateStatus;
        return status == NodeStatus.None ? NodeStatus.Running : status;
    }

    public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state)
    {
        var playerIndex = _sensorPool.Get(context.AgentIndex).DetectedPlayer;
        state.StateIndex = _world.NewEntity();
        _agentPool.Add(state.StateIndex).AgentIndex = context.AgentIndex;
        _shootStatePool.Add(state.StateIndex).Setup(
            targetShots: _targetShots,
            entityIndex: playerIndex);
    }

    public void OnExit(LeoEcsContext context, ref LeoEcsLeafState state, NodeStatus exitStatus)
    {
        _world.DelEntity(state.StateIndex);
    }

    public void OnAbort(LeoEcsContext context, ref LeoEcsLeafState state)
    {
        _world.DelEntity(state.StateIndex);
    }
}
```
> **[EXAMPLE]** Example leaf "Is the player visible" for `LeoEcsLite` using `DI`. The leaf itself does not perform a raycast — it only checks a value maintained by the sensor. The sensor is responsible for how and how often that value is updated.
```c#
[Serializable]
public sealed class IsPlayerRayCastLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>
{
    private EcsPool<PlayerVisibilitySensor> _sensorPool;
        
    [Inject]
    public void Construct(IEcsWorldsService service)
    {
        _sensorPool = service.GetPool<PlayerVisibilitySensor>(EcsWorlds.DEFAULT);
    }

    public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
    {
        return _sensorPool.Get(context.AgentIndex).IsPlayerRaycast ? NodeStatus.Success : NodeStatus.Failure;
    }

    public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state) { }
    public void OnExit(LeoEcsContext context, ref LeoEcsLeafState state, NodeStatus exitStatus) { }
    public void OnAbort(LeoEcsContext context, ref LeoEcsLeafState state) { }
}
```
## Graph Editor

<p align="center">
  <img src="docs~/graph-editor-00.png" width="700">
</p>

> ⚠ **IMPORTANT!** This module requires the `Odin Inspector` plugin.

- **Open the graph editor**: Tools → VadimBurym → BT Editor.

<img src="docs~/graph-editor-01.png" width="1050">

- **Top panel**: Buttons from left to right: `Create new graph asset` | `Delete current asset` | `Duplicate current asset` | `Rename current asset` | `Select asset` | `Compile current graph into a BT asset`. All assets are initially placed in `Assets/VadimBurym-DODBT`, from where you can move them anywhere in your project. Create a new graph asset.
<img src="docs~/graph-editor-02.png" width="1050">

- **Canvas**: Right-click → Add/... to create a new node. Execution order for composite children goes from left to right. Build your behavior graph using available nodes and your custom leaves.
- **Side panel**: The `Details` panel displays information about the selected node. Here you can rename the node, verify child execution order, edit node settings, and read a description of how the node works.
<img src="docs~/graph-editor-03.png" width="1050">

- **Bottom panel**: The `Outputs` panel shows logs. It may include compilation errors, warnings about cyclic dependencies, or notifications about changes in composite child order.

> ⚠ **IMPORTANT!** As a respected AI Lead from a AAA project once said, decorators are basically crutches in the Behaviour Tree world and he tries to avoid them. I agree with this approach, and since this plugin was originally built for personal use, decorators are currently not implemented. Available nodes at the moment are: `Selector`, `Sequence`, `MemorySelector`, `MemorySequence`, `Parallel`. These are enough to build most BTs, but decorators may be added later if there is demand from the community.

## Compilation and Build

- **Compile the graph**: Click the compile button in the top panel to produce a compiled `BehaviourTreeAsset` (not to be confused with the graph asset). Place the compiled asset somewhere accessible from code.

- **Create a runtime BT for each BT asset**: Collect all your `BehaviourTreeAsset` instances and create a `BehaviourTree` runtime instance for each.

> ⚠ **IMPORTANT!** This is done once before the game starts.

> **[EXAMPLE]** Creating a runtime BT from the compiled BT asset used in the previous examples.
```c#
var btAsset = //Reference to your BehaviourTreeAsset
var runtimeBt = new BehaviourTree<LeoEcsContext, LeoEcsLeafState>();
runtimeBt.Construct(asset);
```
- **Inject dependencies into every leaf of each runtime BT**: Construct leaves using your DI container or ServiceLocator.

> ⚠ **IMPORTANT!** This is done once before the game starts.

> **[EXAMPLE]** Using `DI`.
```c#
var leafs = runtimeBt.Leafs;
for (int i = 0; i < leafs.Length; i++)
    _diContainer.Inject(leafs[i]); //Your DIContainer
```
> **[EXAMPLE]** Using `ServiceLocator`.
```c#
var leafs = runtimeBt.Leafs;
for (int i = 0; i < leafs.Length; i++)
    if (leafs[i] is IConstruct constructable)
        constructable.Construct();
```
- **Create agent contexts and states**: Wherever agents are created, create a context and a `BtState` for each agent. Initialize the state from the desired `BehaviourTree`.

> **[EXAMPLE]** Example for each agent in `LeoEcsLite`. It is recommended to use a MemoryPool for `btContext` and `btState`.
```c#
var btContext = new LeoEcsContext();
var btState = new BtState<LeoEcsLeafState>();
runtimeBt.FillInitialState(btState);
for (int i = 0; i < btState.LeafStates.Length; i++)
    btState.LeafStates[i].Reset();

var agent = _world.NewEntity();
btContext.AgentIndex = agent;
```
- **Tick the runtime BT**: Update each agent through its `BehaviourTree` at any frequency you prefer.

> ⚠ **IMPORTANT!** Do not create a separate `BehaviourTree` for each agent. Only one runtime BT exists per BT asset. Each agent only needs its own context and BT state.
```c#
runtimeBt.Tick(btContext, btState);
```
## Debug Mode

<p align="center">
  <img src="docs~/debug-00.png" width="1050">
</p>

> ⚠ **IMPORTANT!** This module requires the `Odin Inspector` plugin.

- **Add debugging for your agent**: Add the `BtMonoDebug` component and link it to the specific BT asset and the agent's state reference.

<p align="center">
  <img src="docs~/debug-01.png" width="450">
</p>

```c#
#if UNITY_EDITOR
_btMonoDebug.Construct(btAsset, btState);
#endif
```
- **Open Debug Mode**: Debug mode opens the graph and highlights nodes according to the current state of a specific agent: `green - success`, `red - failure`, `blue - running`, `gray - none`.  
`BtMonoDebug` also exposes the public field `RunningLeafs`, which can be used for additional debugging — for example displaying currently running leaves as text above each agent.

## Example Project

The `example-project` branch contains a project demonstrating how to use this plugin. The project is built with `LeoEcsLite`. You can run the gameplay scene to see Debug Mode in action, inspect the graph, and observe the full build and runtime integration process in a working example.

## Public Classes and Methods

| **Class/Struct/Interface** | **Method/Field** | **Description** |
|------------|------------|-----------------|
| class `BehaviourTree` <TContext, TLeafState> |  | Runtime BT. Exactly one instance is created per compiled BT asset before the game starts. TContext — your BT context per agent. TLeafState — your leaf state type |
|  | ILeaf[] `Leafs` | List of all leaves in the initialized runtime BT. Used for dependency injection through DIContainer or ServiceLocator |
|  | void `Construct`( BehaviourTreeAsset) | Initializes the runtime BT with the compiled asset |
|  | void `FillInitialState`( BtState< TLeafState>) | Fills BtState with the initial values required for this runtime BT |
|  | void `Tick`(TContext, BtState< TLeafState>) | Ticks the runtime BT for a specific agent starting from the root node |
|  | void `Abort`(TContext, BtState< TLeafState>) | Aborts the runtime BT for a specific agent starting from the root node |
| class `BehaviourTreeAsset` |  | BT asset created by compiling a graph asset through the BT Editor |
|  | string `GUID` | Unique global identifier |
| class `BtMonoDebug` |  | MonoBehaviour responsible for the agent debug state. Editor-only |
|  | void `Construct`( BehaviourTreeAsset, IBtDebugState) | Initializes debugging with the specified asset and the agent's BtState |
|  | IReadOnlyList< string> `RunningLeafs` | List of leaf names currently in the Running state |
| struct `BtState`< TLeafState> |  | Runtime BT state data for a single agent |
|  | TLeafState[] `LeafStates` | List of your leaf states. Useful if you need to manually reset or manipulate them |
| interface `ILeaf`<in TContext, TLeafState> |  | Interface for implementing custom leaves within your context and leaf state |
| interface `ILeaf` |  | Marker interface used internally to detect all leaves |
| interface `IBtDebugState` |  | Interface representing debug state. Implemented by BtState. Editor-only |
| enum `NodeStatus` |  | Node execution status |