<p align="right">
  🌐 <a href="DOCUMENTATION-ru.md">Русский</a> &nbsp;|&nbsp; <strong>English</strong>
</p>

<p align="center">
  <a href="../README.md#installation">Installation</a> &nbsp;·&nbsp;
  <a href="../VadimBurym-DODBT-Dots/DOCUMENTATION-en.md">DODBT-Dots</a> &nbsp;·&nbsp;
  <a href="Samples~/Example2D/README-RU.md">Example2D</a> &nbsp;·&nbsp;
  <a href="LICENSE.md">License</a>
</p>

<h1 align="center">DODBT-Light</h1>

<p align="center">Behaviour trees for Unity in pure C#.</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6-59616E?style=for-the-badge&amp;logo=unity&amp;logoColor=white&amp;labelColor=343B45" alt="Unity 6">
  <img src="https://img.shields.io/badge/Runtime-C%23-397E94?style=for-the-badge&amp;labelColor=343B45" alt="Runtime: C#">
  <img src="https://img.shields.io/badge/Integration-DI-658A5A?style=for-the-badge&amp;labelColor=343B45" alt="Integration: DI">
  <img src="https://img.shields.io/badge/Editor-Observe-756790?style=for-the-badge&amp;labelColor=343B45" alt="Editor: Observe">
</p>

> [!IMPORTANT]
> Light suits projects that do not need to run the tree inside Burst Jobs. It has fewer limitations than [DODBT-Dots](../VadimBurym-DODBT-Dots/DOCUMENTATION-en.md): the context can hold regular C# references, you define the leaf state, and dependencies can be passed through DI.

> [!IMPORTANT]
> This guide assumes you already know how Behaviour Trees work. It does not cover the general theory of the algorithm.

> [!IMPORTANT]
> The current Light and Example2D integration was tested in **Unity 6000.3.10f1** and **Unity 6000.0.51f1**. The dependencies of [Example2D](Samples~/Example2D/README-RU.md) are needed by the sample, not by the plugin core.

> [!WARNING]
> Migrating from DODBT 0.4.0: simply replacing the old DODBT folder is not a migration. Update `using` statements, asmdef references, and serialized `SerializeReference` type names; some script GUIDs also differ. Work on a copy of your project and check the graphs and compiled assets. The included Example2D has already been migrated.

> [!TIP]
> DODBT-Light was originally built for easy integration with ECS architectures, but it is not tied to ECS: you can also use it in OOP projects and with your own C# game systems.

<h2 align="center">Contents</h2>

- [Context](#context)
- [Leaf state](#leaf-state)
- [Leaves](#leaves)
  - [Lifecycle](#leaf-lifecycle)
  - [Settings and state](#leaf-settings)
  - [A simple condition leaf](#condition-example)
  - [A more complex action leaf](#action-example)
- [Graph editor](#graph-editor)
  - [Creating a tree asset](#create-graph)
  - [Building a graph](#graph-editing)
  - [Decorators](#decorators)
  - [Quality of Life](#quality-of-life)
  - [Compilation and messages](#compilation)
- [Project integration](#integration)
  - [Creating a runtime tree](#create-tree)
  - [Dependency Injection](#dependency-injection)
  - [Agent setup](#agent-state)
  - [Running the tree](#tree-execution)
  - [Aborting and reusing state](#abort-and-pooling)
- [Debugging](#debugging)
  - [Connecting the debugger](#debug-integration)
  - [Debug and Observe mode](#debugger-window)
- [Example2D sample](#example)
- [Limitations and details](#limitations)
- [Tests and performance](#tests)
- [License](#license)
- [Public API](#public-api)
  - [BtContextAttribute](#api-bt-context)
  - [BtLeafStateAttribute](#api-bt-leaf-state)
  - [ILeaf&lt;TContext, TLeafState&gt;](#api-ileaf-generic)
  - [ILeaf](#api-ileaf)
  - [LeafIconAttribute](#api-leaf-icon)
  - [NodeStatus](#api-node-status)
  - [BehaviourTreeAsset](#api-behaviour-tree-asset)
  - [BehaviourTree&lt;TContext, TLeafState&gt;](#api-behaviour-tree)
  - [BtState&lt;TLeafState&gt;](#api-bt-state)
  - [BtMonoDebug](#api-bt-mono-debug)
  - [IBtDebugState](#api-debug-state)

<a id="context"></a>

<h2 align="center">Context</h2>

Open **Tools → VadimBurym → DODBT-Light** and click **New Context**. Enter a class name, namespace, and folder.

<p align="center">
  <a href="docs~/images/01-create-context.webp"><img src="docs~/images/01-create-context.webp" width="100%" alt="DODBT-Light: main window and context creation"></a>
</p>

**The context holds the data and dependencies the tree receives when running for a specific agent.**

It is a regular class: it can hold an agent ID, references to Unity components, ECS pools, and game services. Your project determines which fields it needs.

<a id="context-example"></a>

#### Context example from Example2D

[LeoEcsContext](Samples~/Example2D/BehaviourTree/LeoEcsContext.cs) only needs the ECS entity index. Leaves receive shared dependencies during setup.

```csharp
[BtContext]
public sealed class LeoEcsContext
{
    public int AgentIndex;
}
```

> [!NOTE]
> The `[BtContext]` attribute makes it easier to create and select types in the editor; Light has no separate code generation step.

<a id="leaf-state"></a>

<h2 align="center">Leaf state</h2>

Open **Tools → VadimBurym → DODBT-Light**, click **New LeafState**, and create your own struct.

**LeafState holds the mutable data of one leaf node for one agent.**

This could be a timer, counter, target, or the ID of a separate game action. Two nodes of the same class receive separate states. All of these elements for an agent are stored in `BtState<TLeafState>.LeafStates`.

<a id="leaf-state-example"></a>

#### State example from Example2D

[LeoEcsLeafState](Samples~/Example2D/BehaviourTree/LeoEcsLeafState.cs) stores the index of an action entity in the `BT_STATES` world:

```csharp
[Serializable, BtLeafState]
public struct LeoEcsLeafState
{
    public int StateIndex;

    public void Reset()
    {
        StateIndex = -1;
    }
}
```

A separate system processes the action entity. The leaf uses `StateIndex` to check the result and remove the action when it ends or is aborted. This serves the same purpose as `StateEntity` in Dots, but in Light you define the state format yourself.

> [!IMPORTANT]
> `Reset()` in this example is your own method, not a plugin contract. The runner does not call it automatically. After `FillInitialState`, set initial values for your custom fields; when reusing state from a pool, end the previous actions first.

> [!NOTE]
> The `[BtLeafState]` attribute makes it easier to create and select types in the editor; Light has no separate code generation step.

<a id="leaves"></a>

<h2 align="center">Leaves</h2>

Click **New Leaf**, then select **Context**, **Leaf state**, a namespace, and a folder. The template creates a serializable class with the `ILeaf<TContext, TLeafState>` interface and four lifecycle methods.

<p align="center">
  <a href="docs~/images/02-create-leaf.webp"><img src="docs~/images/02-create-leaf.webp" width="100%" alt="DODBT-Light: creating a leaf with Context and LeafState selection and the generated code"></a>
</p>

Leaves are the end nodes of a tree. They can check conditions, perform specific actions, or represent states. They run in the agent's context and have their own execution state. The graph editor finds them in your project and offers them when you add a leaf.

A leaf belongs to a specific pair of types. For example, `ILeaf<LeoEcsContext, LeoEcsLeafState>` only works with a graph that uses this pair;

<a id="leaf-lifecycle"></a>

### Lifecycle

<table width="100%">
<thead>
<tr>
<th width="32%" align="left">Method</th>
<th width="68%" align="left">When it is called</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>OnEnter(context, ref state)</code></td>
<td>Enters an inactive leaf before its first <code>OnTick</code>. This is where an action usually starts.</td>
</tr>
<tr>
<td><code>OnTick(context, ref state)</code></td>
<td>Checks a condition or an action result. Returns <code>Running</code>, <code>Success</code>, or <code>Failure</code>.</td>
</tr>
<tr>
<td><code>OnExit(context, ref state, exitStatus)</code></td>
<td>Ends the leaf after <code>Success</code> or <code>Failure</code>; the result is passed in <code>exitStatus</code>.</td>
</tr>
<tr>
<td><code>OnAbort(context, ref state)</code></td>
<td>Aborts an active leaf. No additional <code>OnExit</code> is called when aborting.</td>
</tr>
</tbody>
</table>

While a leaf returns `Running`, `OnEnter` is not called again. `OnEnter` and the first `OnTick` run in the same tree call.

> [!WARNING]
> When switching branches, the new Enter/Tick calls happen **before Abort on the previous Running branch**. Do not make actions mutually exclusive by assuming the old action always ends first. For separate action entities, an explicit owner and independent cleanup for each operation are useful.

<a id="leaf-settings"></a>

### Settings and state

<table width="100%">
<thead>
<tr>
<th width="32%" align="left">Where to store it</th>
<th width="68%" align="left">What to store</th>
</tr>
</thead>
<tbody>
<tr>
<td>Leaf class fields</td>
<td>Shared leaf settings and prepared dependencies: configurable fields, ECS pools, and services.</td>
</tr>
<tr>
<td><code>TContext</code></td>
<td>Data and dependencies for a specific agent, available to all of its leaves.</td>
</tr>
<tr>
<td><code>TLeafState</code></td>
<td>Mutable state of a specific node for a specific agent.</td>
</tr>
</tbody>
</table>

Add `[Serializable]` to the leaf class, `[SerializeField]` to private settings, and `[SerializeReference]` to nested polymorphic data. Settings are edited inside the node and in the graph editor. You do not need to pack them as you do in Dots.

> [!WARNING]
> Leaf instances are shared by all agents using the tree. Do not store an individual agent's current target, timer, or state in leaf fields: the next agent will overwrite that data. Use the context and LeafState for this.

<a id="condition-example"></a>

### A simple condition leaf

This example uses an `IsPlayerRayCastLeaf` (the agent checks whether it can see the player) with the `LeoEcsLite` framework. The leaf receives its dependencies through DI `[Inject]`: an ECS pool with the agents' sensor data. In `OnTick`, it uses the current agent's index to get its sensor from the shared pool and checks its value. A separate ECS system handles the RayCast and sensor logic and writes data to the sensor.
```csharp
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

> [!NOTE]
> DI setup is described in [Dependency Injection](#dependency-injection). You can also use ServiceLocator or pass dependencies through Context.

<a id="action-example"></a>

### A more complex action leaf

This example uses a `ShootPlayerLeaf` (the agent shoots at the player) with the `LeoEcsLite` framework. The leaf receives its dependencies through DI `[Inject]`: an ECS world and ECS pools for agent sensors and states. In `OnEnter`, the leaf accesses the current agent's sensor through `Context`, gets information about the player entity in sight, and creates a new action entity in the world. Another ECS system responsible for shooting processes this entity. In `OnTick`, the leaf checks the status of the entity it created. If the action stops for any reason (for example, the agent runs out of ammo or can no longer see the player), another ECS system changes the action's status. The leaf reads this and returns Failure/Success instead of Running. In `OnExit` and `OnAbort`, the leaf forcibly removes the action entity it created.

> [!NOTE]
> Shooting itself is handled by your separate ECS system that processes the `ShootEntityState` component. The leaf only creates/removes an entity with this component in the world for the current agent and watches its state.
```csharp
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
> [!NOTE]
> DI setup is described in [Dependency Injection](#dependency-injection). You can also use ServiceLocator or pass dependencies through Context.

> [!TIP]
> **The recommended role of a leaf is to connect tree decisions to game systems.** A condition reads a sensor, an action starts an operation, `Running` waits for the result, and `Exit/Abort` end it. Short calculations can run directly in a leaf; ongoing movement, shooting, and animation are easier to update in separate systems.

<a id="graph-editor"></a>

<h2 align="center">Graph editor</h2>

Open **Tools → VadimBurym → DODBT-Light** and click **BT Editor**.

<p align="center">
  <a href="docs~/images/03-editor-overview.webp"><img src="docs~/images/03-editor-overview.webp" width="100%" alt="BT Editor overview: graph, minimap, Details, and Output"></a>
</p>

<a id="create-graph"></a>

### Creating a tree asset

<p align="center">
  <a href="docs~/images/08-create-graph.webp"><img src="docs~/images/08-create-graph.webp" width="100%" alt="Creating a graph and building a tree in the Dots editor"></a>
</p>

Click **New**. Enter a name and folder, then choose **Context** and **LeafState**. The editor creates a `BtGraphAsset` graph and its linked `BehaviourTreeAsset` compiled asset.

The **Context + LeafState** pair determines which leaves are available. Both settings can be changed in the top toolbar. Existing nodes that become incompatible keep their settings and receive an orange border and a warning icon; replace them with compatible leaves before compiling.

**Copy** creates a separate tree with its own compiled asset. **Rename** renames the pair. **Delete** removes the selected graph and its compiled asset after confirmation.

<a id="graph-editing"></a>

### Building a graph

Right-click an empty area and select **Add → node type**. Connect the parent's output to the child's input. Root has one child; every other node has one parent. Children execute **from left to right**, based on their positions.

For a Leaf, choose an implementation in the field with the leaf icon.

<p align="center">
  <a href="docs~/images/07-select-leaf.webp"><img src="docs~/images/07-select-leaf.webp" width="100%" alt="Selecting a leaf implementation directly in the node"></a>
</p>

<table width="100%">
<thead>
<tr>
<th width="32%" align="left">Node</th>
<th width="68%" align="left">Behaviour</th>
</tr>
</thead>
<tbody>
<tr>
<td>Root</td>
<td>Defines the entry point of the graph.</td>
</tr>
<tr>
<td>Leaf</td>
<td>A user-defined condition or action.</td>
</tr>
<tr>
<td>Sequence</td>
<td>Checks children from left to right until Failure or Running. Each new Tick starts checking from the beginning.</td>
</tr>
<tr>
<td>Selector</td>
<td>Finds the first child that returns Success or Running. Each new Tick checks children from the left again.</td>
</tr>
<tr>
<td>MemorySequence</td>
<td>Continues from the saved child. <code>Reset On Failure</code> controls reset on Failure, and <code>Reset On Abort</code> controls reset on abort. Starts over after an overall Success.</td>
</tr>
<tr>
<td>MemorySelector</td>
<td>Saves its execution position. <code>Pick Random</code> changes the selection order; <code>Reset On Abort</code> controls reset on abort.</td>
</tr>
<tr>
<td>Parallel</td>
<td>Visits branches and compares their results against Success/Failure thresholds. <code>Cache Child Status</code> avoids ticking children that have already finished during the current attempt.</td>
</tr>
</tbody>
</table>

> [!TIP]
> You can find a detailed description of each node in the editor hints in the **Details** panel.

<p align="center">
  <a href="docs~/images/05-parallel-details.webp"><img src="docs~/images/05-parallel-details.webp" width="100%" alt="Parallel settings in the node and Details panel"></a>
</p>

> [!IMPORTANT]
> Parallel does not create threads: branches of one tree run sequentially within the current call. Light does not turn regular C# leaves into Burst Jobs.

> [!TIP]
> Nodes that are not connected to Root can be kept as drafts. They are not included in the compiled tree.

<a id="decorators"></a>

### Decorators

To use Timeout, Cooldown, and Inverter, add `DODBT_DECORATORS` to **Scripting Define Symbols** for the selected platform. Decorators will appear in the Add menu; each must have exactly one child.

<table width="100%">
<thead>
<tr>
<th width="32%" align="left">Decorator</th>
<th width="68%" align="left">Behaviour</th>
</tr>
</thead>
<tbody>
<tr>
<td>Timeout</td>
<td>Limits the duration of an attempt. When time runs out, it aborts the active child and returns Failure. After an abort, a new attempt receives a new time interval.</td>
</tr>
<tr>
<td>Cooldown</td>
<td>The first attempt is available immediately. After the child returns Success or Failure, blocks new attempts for the specified time by returning Failure. Aborting an active attempt does not start a cooldown by itself; an existing cooldown is preserved.</td>
</tr>
<tr>
<td>Inverter</td>
<td>Turns Success into Failure and vice versa; Running remains Running.</td>
</tr>
</tbody>
</table>

For Timeout and Cooldown, pass time in seconds:

```csharp
#if DODBT_DECORATORS
NodeStatus result = tree.TickWithStatus(context, state, Time.timeAsDouble);
#endif
```

> [!IMPORTANT]
> `now` must be finite and must not decrease during the lifetime of an initialized agent state. Use the same clock throughout that lifetime. Timers are checked when the branch is visited: infrequent ticks do not cause background execution between tree calls.

If you remove the directive, decorator data stays in the graph, but compilation and runtime tree creation with decorators will be rejected. Inverter without timed decorators allows a regular Tick without a time argument.

<a id="quality-of-life"></a>

### Quality of Life

The plugin keeps growing with new improvements to ease of use. These changes affect only **authoring — creating, configuring, and editing trees** — and do not affect runtime execution. If something is missing or a step could be simpler, share your ideas and feedback. Your suggestions help decide what comes in future updates. You can suggest workflow improvements in [DODBT discussions](https://github.com/vadimburym/DODBT/discussions).

Already available:

- The **Details** panel lets you edit settings, rename nodes, check child execution order, and read hints about how each node works.
- The **Output** panel shows logs, errors, and reasons for compilation failures.
- **Search** by node name, type, and namespace; **Previous / Next** move the selection between results.
- **Copy / Paste / Duplicate** copy nodes or a selected block with internal connections and independent settings. Root is not copied.
- **Linked groups:** select **Create Linked Group** on a branch root, then **Duplicate Group**. Changes to structure, parameters, and relative positions are synced. **Unlink Group** makes an instance independent; the list is on the **Groups** tab.
- **Creation windows** remember the folder and last settings. Context, state, and leaf pickers support search.
- **Use node icons** in the launcher enables compact composite nodes with icons. You can also add LeafIconAttribute to set a custom leaf icon using a texture GUID or a path in Assets/ or Packages/.
- **Undo / Redo** work for supported graph editing operations.

> [!NOTE]
> Linked groups exist only in the editor. During compilation, each instance becomes regular nodes with their own states. Groups cannot overlap or be nested.

<p align="center">
  <a href="docs~/images/06-linked-groups.webp"><img src="docs~/images/06-linked-groups.webp" width="100%" alt="Syncing changes between linked group instances"></a>
</p>

<a id="compilation"></a>

### Compilation and messages

Click **Compile** to write the result to `BehaviourTreeAsset`. Use this asset in the game; the graph with node positions and groups is for the editor.

<p align="center">
  <a href="docs~/images/04-editor-toolbar.webp"><img src="docs~/images/04-editor-toolbar.webp" width="100%" alt="Editor toolbar: graph, context, Compile, and search; Light also has LeafState"></a>
</p>

<table width="100%">
<thead>
<tr>
<th width="32%" align="left">Error cause</th>
<th width="68%" align="left">What to check</th>
</tr>
</thead>
<tbody>
<tr>
<td>Root has no branch</td>
<td>Connect the entry point to the tree.</td>
</tr>
<tr>
<td>Leaf is empty or its class was deleted</td>
<td>Assign an implementation or restore the script and serialized data.</td>
</tr>
<tr>
<td>Context or LeafState does not match</td>
<td>Select the correct pair for the graph and compatible leaves.</td>
</tr>
<tr>
<td>A cycle, multiple parents, or an invalid connection</td>
<td>Fix the connections.</td>
</tr>
<tr>
<td>Invalid child count or thresholds</td>
<td>Check composite nodes and the decorator's single child.</td>
</tr>
<tr>
<td>Decorators are disabled</td>
<td>Enable <code>DODBT_DECORATORS</code> for the target platform or remove decorators from the tree.</td>
</tr>
</tbody>
</table>


<a id="integration"></a>

<h2 align="center">Project integration</h2>

<a id="create-tree"></a>

### Creating a runtime tree

Create one `BehaviourTree<TContext, TLeafState>` for the selected compiled asset before starting the agents:

```csharp
// compiledAsset is the BehaviourTreeAsset created by Compile in the graph editor.
var tree = new BehaviourTree<LeoEcsContext, LeoEcsLeafState>();
tree.Construct(compiledAsset);
```

The types must match the graph's pair. In Example2D, trees are created in [BehaviourTreeWarmUpSystem](Samples~/Example2D/BehaviourTree/BehaviourTreeWarmUpSystem.cs) and stored in [BehaviourTreeService](Samples~/Example2D/BehaviourTree/BehaviourTreeService.cs) for access at any time during gameplay.

> [!TIP]
> Use one prepared tree for all agents with the same behaviour and shared dependencies. Do not create a separate copy of the same `BehaviourTree` for each agent.

<a id="dependency-injection"></a>

### Dependency Injection

**DI means passing an object the dependencies it uses.** DODBT does not require a specific container: shared services and ECS pools can be passed to leaves once, while agent-specific data is passed through the context.

Set up dependencies after `tree.Construct(compiledAsset)` and before the first tick. `tree.Leafs` gives you access to leaf instances in the compiled tree.

<a id="di-container"></a>

#### Through a DI container

Pass dependencies to each leaf through your chosen container:

```csharp
foreach (var leaf in tree.Leafs)
    DiContainer.Inject(leaf);
```

`DiContainer.Inject` here represents the API of your chosen DI container, not a DODBT method. The `[Inject]` attribute and dependency registration also belong to the container. It must support injecting dependencies into existing instances; you do not need a separate container for each agent if dependencies are shared. Do this once when building the tree before the game starts.

<a id="service-locator"></a>

#### Through ServiceLocator

Example2D uses its own `IConstruct` contract. [BehaviourTreeLeafsConstructSystem](Samples~/Example2D/BehaviourTree/BehaviourTreeLeafsConstructSystem.cs) prepares each leaf:

```csharp
foreach (var leaf in tree.Leafs)
{
    if (leaf is IConstruct constructable)
        constructable.Construct();
}
```

This is Example2D integration code, not a required plugin service. To implement it, add your own contract (such as `IConstruct`) to the leaves and get all required dependencies from `ServiceLocator` inside its method. As with a DI container, prepare the leaves once when building the tree before the game starts.

<a id="context-dependencies"></a>

#### Through the context

For a small project, context fields are enough. For example, a reference to the agent's `Transform` or a prepared `EcsPool<PlayerVisibility>` is passed when creating its context; the leaf accesses them through the `context` argument.

> [!WARNING]
> Shared leaves should receive shared dependencies. If two game worlds use different DI containers, do not reconfigure the same leaf instances for the second world: `Construct` does not clone the compiled asset's leaves. Independent environments need independent instances of this data, such as a separate runtime copy of the asset, or dependencies passed through the context.

<a id="agent-state"></a>

### Agent setup

Create a separate context and `BtState<TLeafState>` when setting up each agent. Initialize the state using the tree that will run it:

```csharp
// agent is the index of an existing LeoEcsLite entity.
var context = new LeoEcsContext { AgentIndex = agent };
var state = new BtState<LeoEcsLeafState>();
tree.FillInitialState(state);

for (int i = 0; i < state.LeafStates.Length; i++)
    state.LeafStates[i].Reset();
```

`FillInitialState` prepares the internal traversal state and allocates arrays if their capacity is too small. In the loop above, `Reset()` sets the custom initial value `StateIndex = -1`. See [EnemyFactory](Samples~/Example2D/Gameplay/Enemies/EnemyFactory.cs) for enemy creation.

> [!IMPORTANT]
> `BtState<TLeafState>` is a class. Do not share one state instance between agents. The length of `LeafStates` may include spare capacity and exceed the actual number of leaves in the tree.

<a id="tree-execution"></a>

### Running the tree

Run the tree for each agent, passing its `context` and `state` at the rate your game needs:

```csharp
tree.Tick(context, state);
```

If the calling code needs the result, use this instead:

```csharp
NodeStatus result = tree.TickWithStatus(context, state);
```

These are two forms of the same call: use one of them. `TickWithStatus` traverses the tree and returns `Running`, `Success`, or `Failure`; `Tick` discards the result.

In [AiBrainTickSystem](Samples~/Example2D/BehaviourTree/AiBrainTickSystem.cs), each agent has its own tick interval. The tree can update less often than movement, sensors, and shooting. For trees with timed decorators, use the overload with `now`, as shown in [Decorators](#decorators).

<a id="abort-and-pooling"></a>

### Aborting and reusing state

Before an agent dies, changes trees, or returns to a pool, abort its active actions:

```csharp
tree.Abort(context, state);

#if UNITY_EDITOR
if (owner.TryGetComponent<BtMonoDebug>(out var debug))
    debug.Detach();
#endif
```

When reusing the agent, configure its context, call `FillInitialState`, reset your LeafState fields, and reconnect debugging. You can implement Context and BtState pools in your integration.

> [!WARNING]
> `FillInitialState` does not call `OnAbort`, remove action entities, or reset existing user-defined LeafState values. End the behaviour properly first. `Abort` itself does not disable AI permanently: the next Tick can start the tree again.

<a id="debugging"></a>

<h2 align="center">Debugging</h2>

<p align="center">
  <a href="docs~/images/09-example2d-observe.webp"><img src="docs~/images/09-example2d-observe.webp" width="100%" alt="DODBT-Light: debugging enemy behaviour in Example2D with active tree branches highlighted"></a>
</p>

<a id="debug-integration"></a>

### Connecting the debugger

After `FillInitialState`, link the scene object, compiled asset, and agent state:

```csharp
#if UNITY_EDITOR
BtMonoDebug.Attach(owner, compiledAsset, state);
#endif
```

`owner` is the agent's GameObject. The component is created or reused automatically. Call `Attach` again when changing the tree or state; call `Detach` when returning the state to a pool.

> [!IMPORTANT]
> Connect once during agent setup. Debugging does not need an extra Tick: the regular `Tick` or `TickWithStatus` already records statuses in the Editor. Do not call `FillInitialState` every frame — this resets execution.

If `BtMonoDebug` is already on the object, you can call `debug.Construct(compiledAsset, state)`. Its `RunningLeafs` contains active leaf names and can be used for your own editor labels.

<a id="debugger-window"></a>

### Debug and Observe mode

Open **Tools → VadimBurym → DODBT-Light** and click **BT Debugger**. The window shows connected agents, their trees, and active leaves, with search and pages. Click **Observe** to open a specific agent's tree.

To open it from the scene, enable **Settings → Show scene markers** in the launcher. Select the agent or one of its child objects in Scene View and click **Observe** above it.

Node statuses: blue — Running, green — Success, red — Failure, gray — None. Observe supports search and viewing settings. If the asset or its revision no longer matches the agent's state, the editor stops observing instead of showing data from a different tree.

> [!NOTE]
> Visual debugging works in the Unity Editor during Play Mode. Connection methods and tracing data are available under `UNITY_EDITOR`; this is not a debugger for a built player. The tree core does not require MonoBehaviour — it is only needed to connect the agent to the scene and editor.

If the agent does not appear, check the `Construct → FillInitialState → Attach` order, the compiled asset, and whether Tick is running. After changing the graph, recompile it and recreate the runtime state.

<a id="example"></a>

<h2 align="center">Example2D sample</h2>

[Example2D](Samples~/Example2D/README-RU.md) is a small 2D game built with LeoEcsLite and ServiceLocator. The enemy patrols, detects the player, chases, retreats, and shoots. The game scene is [Example2D.unity](Samples~/Example2D/Example2D.unity), and the tree is [ExampleBrain](Samples~/Example2D/BehaviourTree/Assets/ExampleBrain.asset).

With UPM, import **Samples → LeoECS 2D Example** after installing the dependencies listed in the sample README. If the sample is already in `Assets` with the plugin, open the scene directly. Controls: **WASD** or arrow keys.

> [!NOTE]
> The sample shows how to integrate the plugin. It is not a recommended game architecture. LeoECS, navigation, input, and rendering belong to the sample; you do not need to add them to your project to use DODBT-Light itself.

<a id="limitations"></a>

<h2 align="center">Limitations and details</h2>

**Light has far fewer limitations than Dots.** There is no fixed LeafData format, unmanaged requirement, or requirement for static leaf methods. You can use regular C# objects, Unity references, and DI. The main rules concern serialization and keeping shared data separate from agent state.

<a id="type-limits"></a>

### Types and serialization

- `TContext` must be a class and `TLeafState` a struct. You define their contents.
- One graph is tied to one pair of types; leaves must match this pair.
- Settings fields follow Unity's serialization rules. An arbitrary property or unsupported collection does not become serializable automatically.
- Shared game dependencies are injected after creating the runtime tree; editor windows and serialization do not perform DI for you.

<a id="shared-tree-limits"></a>

### Shared leaves and execution order

One tree instance is called sequentially for different agents. It contains shared working buffers and leaf instances, so concurrent or nested reentrant calls to the same instance are not supported. Use Dots for Burst Jobs.

Each agent has its own BtState. Do not change shared leaf settings during execution or store agent-specific data in them. Allocations inside your leaf methods and services remain under your control.

<a id="size-limits"></a>

### Tree size and directives

- Up to **65,535 executable nodes**, or **255** with `DODBT_SMALL_SIZE`. The editor Root is not included in this count.
- Up to **255 children** per composite; a decorator has exactly one child.
- `DODBT_DECORATORS` enables Timeout, Cooldown, and Inverter. Timed decorators require a monotonic clock.

> [!IMPORTANT]
> After changing directives, recompile graphs and recreate runtime trees and states. If assemblies use different sets of directives, make them consistent. Editing a graph does not automatically update agents that are already running.

<a id="tests"></a>

<h2 align="center">Tests and performance</h2>

Tests are included in the package: [Tests](Tests). They check nodes, lifecycle, independent agent states, compilation and serialization, Context/LeafState pairs, decorators, linked groups, and debugging.

For UPM, add the package to `testables` in the root `Packages/manifest.json`, keeping the other entries:

```json
"testables": [
  "com.vadimburym.dodbt.light"
]
```

The **Unity Test Framework** package is required to run them. Open **Window → General → Test Runner** and run the EditMode tests. Decorator checks depend on `DODBT_DECORATORS`; tests are in a separate editor assembly and are not needed in a regular player.

> [!TIP]
> `Construct` and the initial `FillInitialState` allocate memory in advance. Once prepared, regular tree traversal does not need new objects on every Tick; the tests include allocation checks. Your system chooses the update rate. Measure your own leaves and game workload separately, after warmup and with Observe closed.

Dots results with Burst and Jobs are not Light measurements: the execution model is different. Light gains efficiency from a shared tree instance, separate compact agent state, and a controlled tick rate.

<a id="license"></a>

<h2 align="center">License</h2>

The plugin and its documentation use the [DODBT Custom Game-Use and Redistribution License](LICENSE.md), shared by DODBT-Light and DODBT-Dots. The full text is also in the [common LICENSE](../LICENSE.md).

Example2D code has a separate [MIT license](Samples~/Example2D/LICENSE.md). External libraries keep their authors' licenses.

<a id="public-api"></a>

<h2 align="center">Public API</h2>

Types for creating behaviour, preparing and running the tree, and connecting debugging. The namespace is `VadimBurym.DodBehaviourTree.Light`. Below, `TContext` means your context class and `TLeafState` means your leaf state struct.

---

<a id="api-bt-context"></a>

### **`BtContextAttribute`**

**Type:** attribute for a context class.

**Parameters:** no arguments, or `Type leafStateType` — the default state type for the editor.

**Properties:**

- **`LeafStateType`** — `Type`, read-only. A hint for selecting LeafState; `null` when no argument is passed.

**Description:** makes the context available in creation windows and the graph editor before the first leaf exists. The specified LeafState is a hint, not a restriction on other type pairs. The attribute does not inject dependencies or generate a runner.

---

<a id="api-bt-leaf-state"></a>

### **`BtLeafStateAttribute`**

**Type:** attribute for a leaf state struct.

**Parameters:** none.

**Description:** adds the type to LeafState lists in the editor. You define the struct's fields and their initial values. Contexts and states without attributes are also discovered through `ILeaf<TContext, TLeafState>` implementations.

---

<a id="api-ileaf-generic"></a>

### **`ILeaf<TContext, TLeafState>`**

**Type:** interface for a leaf class; inherits `ILeaf`.

**Type parameters:**

- **`TContext`** — the agent's context class.
- **`TLeafState`** — the state struct for one leaf node of one agent.

**Members:** `OnEnter`, `OnTick`, `OnExit`, `OnAbort`.

**Description:** connects a leaf to a Context + LeafState pair and defines its lifecycle. Methods are called on the leaf instance. Changes to the `ref state` argument are stored in the agent's state; the leaf's own fields remain shared settings and dependencies.

#### **`OnEnter(context, ref state)`**

**Parameters:**

- **`context`** — `TContext`. The current agent's context.
- **`state`** — `ref TLeafState`. The current node's state for this agent.

**Returns:** nothing (`void`).

**Description:** starts behaviour before the first `OnTick` of an inactive leaf. Usually creates a game action and stores its ID in the state. While the leaf returns `Running`, it is not entered again.

#### **`OnTick(context, ref state)`**

**Parameters:** `TContext context` and `ref TLeafState state` — the agent's context and the current node's state.

**Returns:** `NodeStatus` — `Running`, `Success`, or `Failure`.

**Description:** checks a condition or the result of an active action. `Running` keeps the leaf active; `Success` and `Failure` finish it and lead to an `OnExit` call.

#### **`OnExit(context, ref state, exitStatus)`**

**Parameters:**

- **`context`** — `TContext`. The agent's context.
- **`state`** — `ref TLeafState`. The state of the node that is finishing.
- **`exitStatus`** — `NodeStatus`. The `Success` or `Failure` result returned by the leaf.

**Returns:** nothing (`void`).

**Description:** ends the action after a normal leaf exit. Used to remove the action entity, release resources, and reset user data.

#### **`OnAbort(context, ref state)`**

**Parameters:** `TContext context` and `ref TLeafState state` — the agent's context and the state of the node being aborted.

**Returns:** nothing (`void`).

**Description:** stops active behaviour when a branch or the whole tree is aborted. Must release its resources. There is no additional `OnExit` call on abort.

---

<a id="api-ileaf"></a>

### **`ILeaf`**

**Type:** base marker interface with no methods or fields.

**Description:** lets different leaf implementations be stored in one array and accessed through `BehaviourTree.Leafs` for dependency setup. For an executable leaf, implement the generic `ILeaf<TContext, TLeafState>`.

---

<a id="api-leaf-icon"></a>

### **`LeafIconAttribute`**

**Type:** optional leaf class attribute; used only in the editor.

**Parameters:** `string asset` — a texture GUID or a path inside `Assets/` or `Packages/`.

**Properties:**

- **`Asset`** — `string`, read-only. The texture reference passed to the attribute.

**Description:** sets the node icon when **Use node icons** is enabled. A GUID is preferred: it keeps the reference when the file moves. If the texture is missing, the leaf keeps its regular appearance. The attribute does not affect tree execution.

---

<a id="api-node-status"></a>

### **`NodeStatus`**

**Type:** enum with `byte` as its underlying type.

**Values:**

- **`None = 0`** — internal value: no result has been set yet. Do not return it from `OnTick`.
- **`Running = 1`** — execution continues.
- **`Success = 2`** — execution completed successfully.
- **`Failure = 3`** — the condition was not met or the action failed.

**Description:** the common result format for leaves and nodes. Returned by `OnTick` and `TickWithStatus`, and displayed in Observe.

---

<a id="api-behaviour-tree-asset"></a>

### **`BehaviourTreeAsset`**

**Type:** `ScriptableObject` with compiled tree data.

**Properties:**

- **`GUID`** — `string`, read-only. The source graph ID saved during compilation. The sample uses it as a key in the tree service.

**Description:** the result of **Compile**. Contains executable nodes, connections, and serialized leaf instances. Passed to `BehaviourTree.Construct` and used to connect debugging. Integration does not require filling its internal arrays manually.

---

<a id="api-behaviour-tree"></a>

### **`BehaviourTree<TContext, TLeafState>`**

**Type:** tree execution class; `TContext : class`, `TLeafState : struct`.

**Properties:**

- **`Leafs`** — `ILeaf[]`, a read-only property. Leaf instances from the compiled asset for dependency setup. Do not replace array elements or change the leaf order.

**Description:** one prepared instance serves many agents sequentially. Their mutable data is stored in separate `BtState` instances. The tree contains shared working buffers and does not support concurrent or nested reentrant calls to the same instance.

#### **`Construct(asset)`**

**Parameters:** `BehaviourTreeAsset asset` — a compiled asset with leaves for the selected type pair.

**Returns:** nothing (`void`).

**Description:** prepares the tree and working buffers before use. Rejects null references, incompatible data, and leaves with incompatible types. Does not clone leaf instances from the asset. After this call, prepare their dependencies, then the agent states.

#### **`FillInitialState(btState)`**

**Parameters:** `BtState<TLeafState> btState` — an individual agent's state.

**Returns:** nothing (`void`).

**Description:** prepares arrays with enough capacity and resets internal traversal state; when decorators are enabled, also resets their timing state. Called before the first tick or before reuse. Does not call `OnAbort` or reset existing custom `LeafStates` elements: ending actions and setting their initial field values remain the integration's responsibility.

#### **`Tick(context, state)`**

**Parameters:**

- **`context`** — `TContext`. The current agent's context.
- **`state`** — `BtState<TLeafState>`. Its state, prepared by this tree.

**Returns:** nothing (`void`).

**Description:** performs one tree traversal. The game controls how often it is called. In the editor, it also records diagnostic statuses; no extra call is needed for Observe. A tree with Timeout or Cooldown requires the overload with `now`.

#### **`TickWithStatus(context, state)`**

**Parameters:** `TContext context` and `BtState<TLeafState> state` — the agent's context and prepared state.

**Returns:** `NodeStatus` — the result of the current tree traversal.

**Description:** performs the same traversal as `Tick`, but returns its result to the caller. Use it instead of `Tick`, not after it. Timed decorators require the overload with `now`.

#### **`Tick(context, state, now)`**

**Parameters:**

- **`context`** — `TContext`. The agent's context.
- **`state`** — `BtState<TLeafState>`. Its prepared state.
- **`now`** — `double`. Current time in seconds: a finite value no smaller than the previous value for this state.

**Returns:** nothing (`void`).

**Description:** available with `DODBT_DECORATORS`. Runs the tree with timing for Timeout and Cooldown. Use the same clock; equal time values on consecutive ticks are allowed. Time is checked when the relevant branch is visited.

#### **`TickWithStatus(context, state, now)`**

**Parameters:** `TContext context`, `BtState<TLeafState> state`, and `double now` — the same arguments and timing rules as the timed `Tick` overload.

**Returns:** `NodeStatus` — the result of the current tree traversal.

**Description:** available with `DODBT_DECORATORS`. Runs a tree with timed decorators and returns the result. When these decorators are present, calls without `now` are rejected so timers do not use implicit time.

#### **`Abort(context, state)`**

**Parameters:** `TContext context` and `BtState<TLeafState> state` — the agent's context and current state.

**Returns:** nothing (`void`).

**Description:** aborts active branches and calls `OnAbort` on running leaves. Used before removing an agent, changing trees, or returning it to a pool. Respects Memory node reset settings; does not replace full state initialization. The next Tick can start execution again.

---

<a id="api-bt-state"></a>

### **`BtState<TLeafState>`**

**Type:** serializable class; `TLeafState : struct`.

**Fields:**

- **`LeafStates`** — `TLeafState[]`. User-defined leaf node states for one agent. `FillInitialState` prepares the array; its length may include spare capacity.

**Description:** holds mutable data for one tree execution: leaf states and internal traversal, node memory, and enabled decorator data. Create a separate instance for each agent. There is no common `Reset()` for user data: your code sets the required initial values. In the Editor, the class also implements `IBtDebugState`.

---

<a id="api-bt-mono-debug"></a>

### **`BtMonoDebug`**

**Type:** `MonoBehaviour` that connects an agent to editor debugging.

**Properties:**

- **`RunningLeafs`** — `IReadOnlyList<string>`, read-only. Active leaf names from the linked state; may be `null` without a connection.

**Description:** links a GameObject, compiled asset, and execution state for BT Debugger, Observe, and the scene marker. The methods and property listed below are available under `UNITY_EDITOR`; the component itself does not schedule ticks.

#### **`Attach(owner, behaviourTreeAsset, targetState)`**

**Parameters:**

- **`owner`** — `GameObject`. The agent's scene object.
- **`behaviourTreeAsset`** — `BehaviourTreeAsset`. The asset used to prepare its runtime tree.
- **`targetState`** — `IBtDebugState`. The agent's state after `FillInitialState`; usually `BtState<TLeafState>`.

**Returns:** `BtMonoDebug` — the created or reused component.

**Description:** a static method that connects debugging in one line. Adds the component if needed and calls its `Construct`. All three arguments are required. Reconnect when the agent's tree or state changes.

#### **`Construct(behaviourTreeAsset, targetState)`**

**Parameters:** `BehaviourTreeAsset behaviourTreeAsset` and `IBtDebugState targetState` — the compiled asset and prepared agent state.

**Returns:** nothing (`void`).

**Description:** configures an existing component for observation. Stores references to the state's diagnostic data and the asset revision; does not create or run a tree.

#### **`Detach()`**

**Parameters:** none.

**Returns:** nothing (`void`).

**Description:** removes the link to the asset and state, for example before returning an agent to a pool. Does not remove the component or call `Abort`: stop the behaviour separately.

---

<a id="api-debug-state"></a>

### **`IBtDebugState`**

**Type:** debug data interface, available only under `UNITY_EDITOR`.

**Properties:**

- **`DebugStatus`** — `NodeStatus[]`, a read-only property. Node statuses for Observe.
- **`DebugRunningLeafs`** — `List<string>`, a read-only property. Active leaf names.

**Description:** lets `BtMonoDebug` work with different LeafState types. `BtState<TLeafState>` already implements this interface explicitly; regular integration does not require your own implementation. The runner fills the data, and the editor uses it for display.
