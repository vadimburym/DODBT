<p align="right">
  🌐 <a href="README-ru.md">Русский</a> &nbsp;|&nbsp; <strong>English</strong>
</p>

<p align="center">
  <a href="VadimBurym-DODBT-Light/DOCUMENTATION-en.md">DODBT-Light</a> &nbsp;·&nbsp;
  <a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md">DODBT-Dots</a> &nbsp;·&nbsp;
  <a href="VadimBurym-DODBT-Light/Samples~/Example2D/README-RU.md">Example2D</a> &nbsp;·&nbsp;
  <a href="VadimBurym-DODBT-Dots/Samples~/BattleExample/README-RU.md">Battle Example</a> &nbsp;·&nbsp;
  <a href="#gallery">Gallery</a> &nbsp;·&nbsp;
  <a href="LICENSE.md">License</a>
</p>

<h1 align="center">DODBT</h1>

<p align="center"><strong>Behaviour trees in C# for Unity, ECS architectures, and Unity DOTS — a complete workflow from building trees visually to debugging individual agents.</strong></p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6-59616E?style=for-the-badge&amp;logo=unity&amp;logoColor=white&amp;labelColor=343B45" alt="Unity 6">
  <img src="https://img.shields.io/badge/Light-C%23-397E94?style=for-the-badge&amp;labelColor=343B45" alt="Light: C#">
  <img src="https://img.shields.io/badge/Dots-Burst_%2B_Jobs-658A5A?style=for-the-badge&amp;labelColor=343B45" alt="Dots: Burst + Jobs">
  <img src="https://img.shields.io/badge/Editor-Observe-756790?style=for-the-badge&amp;labelColor=343B45" alt="Editor: Observe">
</p>

<a id="benefits"></a>

<h2 align="center">⭐ 10 reasons to choose DODBT</h2>

**⭐ 0 MonoBehaviour**<br>
Tree execution is independent of Unity's lifecycle.

**⭐ 0 GC allocations**<br>
Predictable tree traversal without GC.

**⭐ One shared BT for N agents**<br>
A memory-efficient architecture.

**⭐ Debug during gameplay**<br>
Inspect the selected agent's behaviour in Play Mode.

**⭐ A full BT graph editor**<br>
Logs, hints, search, and clear visual feedback.

**⭐ Separate graph and runtime**<br>
The graph compiles into compact data. Editor tools are not needed in the build.

**⭐ BT is a decision layer**<br>
The tree chooses behaviour; game systems carry it out. You decide how often to Tick.

**⭐ No Blackboard**<br>
Dependencies are passed directly. Light supports DI and ServiceLocator.

**⭐ ECS-friendly**<br>
Light fits into regular C# systems; Dots fits into Unity Entities with Burst and Jobs.

**⭐ Production quality**<br>
Core features are covered by tests, graphs are checked during compilation, and agent behaviour can be debugged visually.

<p align="center">
  <img src="docs~/images/dodbt.png" width="100%" alt="DODBT: visual behaviour tree editor and debugging">
</p>

<a id="versions"></a>

<h2 align="center">Which version to choose</h2>

<table width="100%">
<thead>
<tr>
<th width="24%" align="left"></th>
<th width="38%" align="left">DODBT-Light</th>
<th width="38%" align="left">DODBT-Dots</th>
</tr>
</thead>
<tbody>
<tr>
<td>Execution</td>
<td>Regular C#</td>
<td>Unity Entities, Burst, and Jobs</td>
</tr>
<tr>
<td>When to choose it</td>
<td>Projects that do not need to run BT inside Burst Jobs</td>
<td>DOTS projects with many agents and AI processing in Jobs</td>
</tr>
<tr>
<td>Context</td>
<td>A user-defined class, including managed references and services</td>
<td>An unmanaged struct with data and access to ECS</td>
</tr>
<tr>
<td>Leaf state</td>
<td>A user-defined LeafState struct</td>
<td>A fixed state format; extra data lives in ECS components</td>
</tr>
<tr>
<td>Integration</td>
<td>Simpler; supports DI and works well as a managed layer for ECS</td>
<td>Requires knowledge of Entities, Job dependencies, and the lifetime of native data</td>
</tr>
<tr>
<td>Core dependencies</td>
<td>—</td>
<td>Entities, Burst, Collections, and Mathematics</td>
</tr>
<tr>
<td>Sample</td>
<td>Example2D with LeoEcsLite and ServiceLocator</td>
<td>Battle Example: a battle with 10,000 agents</td>
</tr>
</tbody>
</table>

> [!NOTE]
> DODBT-Light was originally built for easy integration with ECS architectures, but it is not tied to ECS: you can also use it in OOP projects and with your own C# game systems.

> [!TIP]
> If you need a Behaviour Tree for **LeoEcsLite, LeoEcsProto, Entitas, Morpeh**, or a similar ECS framework and plan to run the tree as regular C# code, choose **DODBT-Light**. It fits into an ECS architecture as a managed decision layer: the context can hold references to your ECS data and services, while game systems carry out the chosen behaviour.
>
> **DODBT-Dots** is designed for **Unity Entities**, with tree execution through **Burst and Jobs** and data stored in **unmanaged memory**. It uses a different approach to data layout and agent state, designed for high performance when processing large numbers of AI agents.

> [!IMPORTANT]
> Upgrading from 0.4.0? Read the [upgrade guide](UPGRADING.md) before replacing the old package. Version 1.0.0 changes package names, namespaces, and asset identities.

<a id="installation"></a>

<h2 align="center">Installation</h2>

### With Package Manager and Git

Open **Window → Package Management → Package Manager**, select **+ → Install package from git URL**, and enter the URL of the package you need.

- **Light:** `https://github.com/vadimburym/DODBT.git?path=/VadimBurym-DODBT-Light`
- **Dots:** `https://github.com/vadimburym/DODBT.git?path=/VadimBurym-DODBT-Dots`

### From a ZIP without Git

1. Extract the plugin ZIP to a permanent folder on disk.
2. In Package Manager, select **+ → Install package from disk**.
3. Select `package.json` inside `VadimBurym-DODBT-Light` or `VadimBurym-DODBT-Dots`.

Unity uses this folder as a local package, so do not delete or move it after adding it. The ZIP itself is not a UPM package. [Installing a local package](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html).

### From a `.unitypackage`

Download a `.unitypackage` from the [1.0.0 release](https://github.com/vadimburym/DODBT/releases/tag/v1.0.0), then open **Assets → Import Package → Custom Package** and select the file. Keep the entire folder of the plugin you need, or both folders; you can exclude samples from the import.

This method places files in `Assets`. For Dots, install dependencies through Package Manager first: importing a `.unitypackage` does not automatically install dependencies from `package.json`. [Importing an asset package](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetPackagesImport.html).

> [!WARNING]
> Do not install the same version through both UPM and `Assets`: this creates duplicate types and assemblies. Select the entire plugin folder — partial imports of Runtime, Editor, or the generator DLL are not supported. Light and Dots can be used together.

<a id="samples"></a>

<h2 align="center">Installing samples</h2>

For a UPM installation, open the package in **Package Manager → Samples** and click **Import**:

- **DODBT-Light → LeoECS 2D Example** — a small 2D game with LeoEcsLite and ServiceLocator.
- **DODBT-Dots → Battle Example** — a battle with a shared tree blob, separate agent states, and Burst Jobs.

Unity copies the selected sample into your project:

```text
Assets/Samples/DODBT-Light/<package version>/LeoECS 2D Example/
Assets/Samples/DODBT-Dots/<package version>/Battle Example/
```

This is your copy: you can study and change it. Updating the package does not update a sample you have already imported. Before importing a new version, save your changes and keep only one copy of each sample to avoid duplicate assemblies and types.

> [!IMPORTANT]
> Install the sample's dependencies first. Battle Example requires **Entities Graphics 1.5.0, URP 17.3.0, Input System 1.18.0**, and a configured URP project. Example2D's dependencies are listed in its [README](VadimBurym-DODBT-Light/Samples~/Example2D/README-RU.md). These packages are not required by the Light core.

With a `.unitypackage` installation, there is no Samples tab: samples are selected in the import window or provided as separate asset packages. If a sample is already in `Assets`, open its scene directly. [How Unity samples work](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-samples.html).

> [!NOTE]
> The samples show how to use the plugin. They are not a recommended architecture for a game project.

<a id="documentation"></a>

<h2 align="center">Documentation</h2>

<table width="100%">
<thead>
<tr>
<th width="32%" align="left">Guide</th>
<th width="68%" align="left">Contents</th>
</tr>
</thead>
<tbody>
<tr>
<td><a href="VadimBurym-DODBT-Light/DOCUMENTATION-en.md">DODBT-Light</a></td>
<td>Context and LeafState, leaves, editor, DI, execution, debugging, and public API.</td>
</tr>
<tr>
<td><a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md">DODBT-Dots</a></td>
<td>Context, leaves, editor, blob, agent setup, Burst Jobs, debugging, and public API.</td>
</tr>
<tr>
<td><a href="VadimBurym-DODBT-Light/Samples~/Example2D/README-RU.md">Example2D</a></td>
<td>Running the Light 2D sample with LeoEcsLite and connecting it to game systems.</td>
</tr>
<tr>
<td><a href="VadimBurym-DODBT-Dots/Samples~/BattleExample/README-RU.md">Battle Example</a></td>
<td>Running the Dots sample, ECS integration, and tree execution in Jobs.</td>
</tr>
</tbody>
</table>

To get started, import the sample for your chosen version and open `ExampleBrain` in BT Editor. Run the scene, select an agent, and click **Observe** to see how the tree's decisions relate to behaviour in the game.

<a id="gallery"></a>

<h2 align="center">Gallery</h2>

<p align="center">
  <a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md#debugging"><img src="VadimBurym-DODBT-Dots/docs~/images/03-battle-observe.webp" width="100%" alt="Observe in Battle Example: the selected agent in Scene View and the active tree branches."></a>
  <a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md#create-graph"><img src="VadimBurym-DODBT-Dots/docs~/images/08-create-graph.webp" width="100%" alt="Creating a graph, adding nodes, and configuring leaves."></a>
  <a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md#graph-editing"><img src="VadimBurym-DODBT-Dots/docs~/images/07-select-leaf.webp" width="100%" alt="Changing a leaf implementation directly in the node."></a>
  <a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md#quality-of-life"><img src="VadimBurym-DODBT-Dots/docs~/images/06-linked-groups.webp" width="100%" alt="Editing one branch updates its linked instances."></a>
  <a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md#graph-editor"><img src="VadimBurym-DODBT-Dots/docs~/images/03-editor-overview.webp" width="100%" alt="Graph, minimap, selected node settings, and Output messages in one window."></a>
  <a href="VadimBurym-DODBT-Dots/DOCUMENTATION-en.md#quality-of-life"><img src="VadimBurym-DODBT-Dots/docs~/images/05-parallel-details.webp" width="100%" alt="Parallel settings are available directly in the node and in the Details panel."></a>
</p>

<a id="tests-and-performance"></a>

<h2 align="center">Tests and performance</h2>

**Behaviour tests.** The packages include [Light tests](VadimBurym-DODBT-Light/Tests) and [Dots tests](VadimBurym-DODBT-Dots/Tests) for lifecycle, node execution, compilation, serialization, and editor operations.

**Burst + Jobs performance gain.** In a Dots comparison test with **10,000 agents**, processing time dropped from **126 ms to 1 ms** when Burst and Jobs were enabled. This is the same Dots scenario with and without them; the data layout that enables this execution is tied to the limitations of this version. [More about the gain and limitations](VadimBurym-DODBT-Dots/DOCUMENTATION-en.md#performance-gain).

> [!NOTE]
> This is the result of one test, not a comparison of Light and Dots or a guarantee of frame time. Tree structure, leaves, tick rate, and hardware all affect the result. Frame/FPS in the sample measures the entire frame, including game systems and rendering.

**Integration check.** Battle Example runs 10,000 agents with an AI interval of 0.2 seconds and ticks spread across frames. Example2D shows the same separation of tree decisions and action execution in regular C#.

<a id="license"></a>

<h2 align="center">License</h2>

DODBT-Light and DODBT-Dots share the [DODBT Custom Game-Use and Redistribution License](LICENSE.md). The full text is also included in each package.

Sample code and third-party assets have separate terms listed in their README and LICENSE files.
