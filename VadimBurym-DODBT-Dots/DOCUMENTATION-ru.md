<p align="right">
  🌐 <strong>Русский</strong> &nbsp;|&nbsp; <a href="DOCUMENTATION-en.md">English</a>
</p>

<p align="center">
  <a href="../README.md#installation">Установка</a> &nbsp;·&nbsp;
  <a href="../VadimBurym-DODBT-Light/DOCUMENTATION-ru.md">DODBT-Light</a> &nbsp;·&nbsp;
  <a href="Samples~/BattleExample/README-RU.md">Battle Example</a> &nbsp;·&nbsp;
  <a href="LICENSE.md">Лицензия</a>
</p>

<h1 align="center">DODBT-Dots</h1>

<p align="center">Деревья поведения для Unity DOTS.</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.3-59616E?style=for-the-badge&amp;logo=unity&amp;logoColor=white&amp;labelColor=343B45" alt="Unity 6.3">
  <img src="https://img.shields.io/badge/Entities-1.5-397E94?style=for-the-badge&amp;labelColor=343B45" alt="Entities 1.5">
  <img src="https://img.shields.io/badge/Burst-1.8-658A5A?style=for-the-badge&amp;labelColor=343B45" alt="Burst 1.8">
  <img src="https://img.shields.io/badge/Execution-Jobs-756790?style=for-the-badge&amp;labelColor=343B45" alt="Execution: Jobs">
</p>

> [!IMPORTANT]
> DODBT-Dots рассчитан на проекты со строгим бюджетом времени игрового AI. Для его использования требуется понимание Unity Entities, Burst, Jobs и управления нативной памятью, что влечет за собой ряд ограничений (подробнее в разделе [Ограничения](#limitations)). Если ваш проект не нуждается в данных инструментах, то рекомендую [DODBT-Light](../VadimBurym-DODBT-Light/DOCUMENTATION-ru.md). Dots-версия заметно сложнее в интеграции и для несоответствующего проекта его ограничения могут создать больше работы, чем сэкономить времени выполнения.

> [!IMPORTANT]
> Руководство предполагает, что вы уже знакомы с Behaviour Tree. Общая теория алгоритма здесь не рассматривается.

> [!IMPORTANT]
> **Проверенная конфигурация:** Unity **6000.3.10f1**, Entities **1.5.0**, Burst **1.8.29**, Collections **2.7.1**, Mathematics **1.3.2**.

<h2 align="center">Содержание</h2>

- [Контекст](#context)
  - [Пример контекста из Battle Example](#context-example)
  - [Кодогенерация](#code-generation)
- [Листья в DOTS](#leaves)
  - [Жизненный цикл](#leaf-lifecycle)
  - [RunnerState_&lt;ВашКонтекст&gt;: передаваемые данные](#runner-state)
  - [LeafData, общие настройки для агентов](#leaf-data)
  - [LeafState, состояние отдельного агента](#leaf-state)
  - [Пример простого листа-условия из BattleExample](#condition-example)
  - [Пример сложного листа-поведения из BattleExample](#action-example)
- [Редактор графов](#graph-editor)
  - [Создание ассета дерева](#create-graph)
  - [Построение графа](#graph-editing)
  - [Quality of Life](#quality-of-life)
- [Интеграция в проект](#integration)
  - [Создание деревьев в runtime](#create-trees)
  - [Создание агентов и инициализация буферов](#agent-state-buffers)
  - [Исполнение дерева в runtime](#tree-execution)
- [Отладка](#debugging)
  - [Что подключить для отладки](#debug-integration)
  - [Визуальный отладчик](#debugger-window)
  - [Observe над выбранным юнитом](#scene-observe)
- [Ограничения и компромиссы](#limitations)
  - [LeafData и настройки листьев](#leaf-data-limits)
  - [Контекст и состояние](#context-state-limits)
  - [Поведение и интеграция](#integration-limits)
  - [Размеры дерева и директивы](#size-limits)
  - [Выигрыш в производительности](#performance-gain)
- [Автоматические проверки](#automated-tests)
- [Лицензия](#license)
- [Публичный API](#public-api)
  - [GenerateBtRunnerAttribute](#api-generate-bt-runner)
  - [LeafCodeGenAttribute](#api-leaf-code-gen)
  - [ILeaf](#api-ileaf)
  - [LeafIconAttribute](#api-leaf-icon)
  - [RunnerState_&lt;Контекст&gt;](#api-runner-state)
  - [LeafData](#api-leaf-data)
  - [LeafStateElement](#api-leaf-state-element)
  - [NodeStatus](#api-node-status)
  - [BehaviourTreeAsset](#api-behaviour-tree-asset)
  - [BehaviourTreeBlob](#api-behaviour-tree-blob)
  - [NodeStateElement](#api-node-state-element)
  - [BTRunner_&lt;Контекст&gt;](#api-bt-runner)
  - [BtDebugTree](#api-bt-debug-tree)
  - [BtDebugTarget](#api-bt-debug-target)
  - [BtDebugNodeState](#api-bt-debug-node-state)
  - [BtDebugSnapshot](#api-bt-debug-snapshot)

<a id="getting-started"></a>

<a id="context"></a>

<h2 align="center">Контекст</h2>

Откройте **Tools → VadimBurym → DODBT-Dots** и нажмите **New Context**.

<p align="center">
  <a href="docs~/images/01-create-context.webp"><img src="docs~/images/01-create-context.webp" width="100%" alt="Окно DODBT-Dots и создание нового контекста"></a>
</p>

**Контекст — типизированный набор данных и доступов, которые система тика передает листьям.** Он описывает, что дерево может узнать об игре и какие команды может отправить.

В контексте могут находиться:

- `ComponentLookup<T>` и `BufferLookup<T>` для обращения к ECS-данным;
- `EntityStorageInfoLookup` для проверки существования сущностей;
- `EntityCommandBuffer.ParallelWriter` для записи команд из параллельной Job;
- числа, идентификаторы, время и другие unmanaged-значения;
- совместимые с Jobs native-контейнеры и ссылки на blob при правильно настроенных доступах и времени жизни.

> [!IMPORTANT]
> В него нельзя поместить `GameObject`, `MonoBehaviour`, обычный C#-сервис, `string`, `List<T>` или другую managed-ссылку. Переданные native-контейнеры тоже не становятся автоматически безопасными для конкурентной записи: правила Jobs продолжают действовать.

<a id="context-example"></a>

### Пример контекста из Battle Example:

```csharp
[GenerateBtRunner]
public struct ExampleContext
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public EntityStorageInfoLookup EntityInfoLookup;

    [ReadOnly] public ComponentLookup<EyeSensor> EyeSensorLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
    [ReadOnly] public ComponentLookup<AttackStats> AttackStatsLookup;
    [ReadOnly] public ComponentLookup<GridNavigationState> GridNavigationStateLookup;
    [ReadOnly] public ComponentLookup<IsMovingTag> IsMovingTagLookup;
    [ReadOnly] public ComponentLookup<RendererEntityRef> RenderEntityLookup;
    [ReadOnly] public ComponentLookup<AttackState> AttackStateLookup;
}
```

<a id="code-generation"></a>

### Кодогенерация

После компиляции C# для созданного вами контекста (далее `<ВашКонтекст>`) вам понадобятся два сгенерированных документа:

<table width="100%">
<thead>
<tr>
<th width="25%" align="left">Документ</th>
<th width="75%" align="left">Назначение</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>BTRunner_&lt;ВашКонтекст&gt;.g.cs</code></td>
<td>Обход дерева: Tick, Abort и редакторский TickWithDebug.</td>
</tr>
<tr>
<td><code>RunnerState_&lt;ВашКонтекст&gt;.g.cs</code></td>
<td>Данные, передаваемые выполняемому листу.</td>
</tr>
</tbody>
</table>

> [!NOTE]
> Типы находятся в `VadimBurym.DodBehaviourTree.Dots.Generated`, а компилируются в сборке вашего контекста. Это результат автоматической Roslyn-генерации, а не файлы, которые надо создавать или редактировать в `Assets`.

<a id="leaves"></a>

<h2 align="center">Листья в DOTS</h2>

Откройте **Tools → VadimBurym → DODBT-Dots** и нажмите **New Leaf**. Лист создается для **одного конкретного контекста**.

<p align="center">
  <a href="docs~/images/02-create-leaf.webp"><img src="docs~/images/02-create-leaf.webp" width="100%" alt="Создание листа для ExampleContext и сгенерированный шаблон кода"></a>
</p>

**Роль листа — связать решение дерева с игровыми системами.** Условие читает готовый сенсор, действие запускает операцию, Running ожидает результат, Exit/Abort завершают ее. Движение, навигация, атака и анимация остаются в написанных вами системах. Прямое короткое вычисление в листе допустимо; разделение особенно полезно для длительного поведения и разной частоты обновления AI и игры.

> [!NOTE]
> Класс и интерфейс листа нужен для настройки в редакторе. Его поля сериализуются в графе, а `GetCompiledData()` упаковывает необходимые значения в `LeafData`. Во время Burst-выполнения экземпляры этого класса не создаются: runner эффективно и безопасно вызывает его статические методы.

<a id="leaf-lifecycle"></a>

### Жизненный цикл

<table width="100%">
<thead>
<tr>
<th width="25%" align="left">Метод</th>
<th width="75%" align="left">Когда вызывается</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>OnEnter</code></td>
<td>Перед первым Tick неактивного листа. Подходит для запуска действия.</td>
</tr>
<tr>
<td><code>OnTick</code></td>
<td>Возвращает <code>Running</code>, <code>Success</code> или <code>Failure</code>. <code>None</code> не является результатом игрового листа.</td>
</tr>
<tr>
<td><code>OnExit</code></td>
<td>После <code>OnTick</code>, вернувшего Success или Failure.</td>
</tr>
<tr>
<td><code>OnAbort</code></td>
<td>При отмене активного листа, например после переключения ветки.</td>
</tr>
</tbody>
</table>

> [!IMPORTANT]
> `OnEnter` и первый `OnTick` происходят в одном вызове runner. Пока результат Running, следующий Tick не вызывает OnEnter повторно. После завершения новый вход снова вызывает OnEnter.

> [!WARNING]
> При переключении веток новые Enter/Tick происходят **до Abort прежней Running-ветки**. Не стройте взаимное исключение действий на предположении, что старое действие всегда будет снято первым. Для отдельных сущностей действий полезны явный владелец и независимое завершение каждой операции.

<a id="runner-state"></a>

### `RunnerState_<ВашКонтекст>`: передаваемые данные

Каждый метод листа принимает `ref RunnerState_<ВашКонтекст> state` - с ним вы будете работать в жизненном цикле листа для настройки вашего поведения.

<table width="100%">
<thead>
<tr>
<th width="25%" align="left">Поле</th>
<th width="75%" align="left">Что содержит</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>Agent</code></td>
<td>Entity, конкретный агент чье дерево выполняется.</td>
</tr>
<tr>
<td><code>Context</code></td>
<td><code>&lt;ВашКонтекст&gt;</code>, общий ранее созданный вами контекст.</td>
</tr>
<tr>
<td><code>LeafData</code></td>
<td>Скомпилированные, общие и неизменяемые настройки листа.</td>
</tr>
<tr>
<td><code>LeafState</code></td>
<td>Изменяемое состояние листа у конкретного агента.</td>
</tr>
<tr>
<td><code>Random</code></td>
<td>Состояние <code>Unity.Mathematics.Random</code> агента. Изменения runner возвращает вызывающему коду.</td>
</tr>
<tr>
<td><code>SortKey</code></td>
<td>Ключ сортировки команд ECB; например индекс чанка.</td>
</tr>
</tbody>
</table>

<a id="leaf-data"></a>

### LeafData, общие настройки для агентов

У каждого листа есть настройки, общие для всех агентов, которые хранятся в LeafData. Автоматически туда уже идет системный ID листа, который не рекомендуется изменять. Все поля описаны в [справочнике LeafData](#api-leaf-data). Его структура фиксирована:

```csharp
public struct LeafData
{
    public int LeafId;
    public int Int0;
    public float Float0;
    public byte Byte0;
    public FixedList32Bytes<byte> Bytes;
}
```

Также вы можете настроить лист под кастомизацию своих значений из редактора. Для этого добавьте сериализуемое поле прямо внутри класса, например `[SerializeField] private float _distance;` его можно записать в `Float0` внутри `GetCompiledData()`, а далее прочитать через `state.LeafData.Float0`. Назначение слотов вы определяете сами под нужды конкретного листа. Фиксированная структура и доступ к LeafData это одно из существенных ограничений dots-версии.

<a id="leaf-state"></a>

### LeafState, состояние отдельного агента

[`LeafStateElement`](#api-leaf-state-element) имеет фиксированные поля:

<table width="100%">
<thead>
<tr>
<th width="25%" align="left">Поле</th>
<th width="75%" align="left">Назначение</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>IsEntered</code></td>
<td>Признак активного входа; им управляет runner.</td>
</tr>
<tr>
<td><code>BufferIndex</code></td>
<td>Индекс состояния этой ноды в буфере агента; задается при инициализации.</td>
</tr>
<tr>
<td><code>StateEntity</code></td>
<td>Ссылка на сущность с дополнительным состоянием действия. Создает и управляет ей пользователь в реализации сложного поведения.</td>
</tr>
</tbody>
</table>

> [!NOTE]
> У каждого агента свой буфер данных состояний. Два появления одного класса листа в графе получают разные состояния.

**StateEntity - мост между интеграцией поведенческого дерева в игровые механики.** В такой схеме дерево управляет запуском и остановкой поведения, а его выполнение выносится в отдельную систему.

При запуске поведения лист должен создать сущность действия с компонентом, содержащим ссылку на агента и необходимые параметры. Например лист создает AttackState, который хранит атакующего агента и его цель. Ваша игровая система находит сущности с AttackState и обрабатывает их независимо от тиков дерева: отсчитывает время, выполняет атаку или управляет движением. Наличие компонента включает действие в обработку; удаление компонента или всей сущности прекращает ее. А ссылка на сущность с AttackState сохраняется в StateEntity, чтобы лист мог ей управлять или удалять при отмене действия.

В простой схеме цикл можно описать так:

1. Лист создает для агента действие.
2. Лист сохраняет ссылку на это действие в `StateEntity`.
3. Отдельная система обрабатывает действие.
4. Лист наблюдает за действием и, когда приходит время, удаляет его.

> [!IMPORTANT]
> В Dots-версии пользователь не объявляет произвольный универсальный тип LeafState, как в Light. Таймеры, цели и результаты долгих действий можно хранить в компонентах агента либо в отдельной сущности, на которую указывает `StateEntity`. Листу-условию отдельная сущность обычно не нужна.

> [!WARNING]
> При создании сущности действия вам нужно сохранить ее в StateEntity. Если вы создаете действие через ECB, то возвращенную Entity нельзя напрямую записывать в LeafState.StateEntity. Вам потребуется также вести запись в LeafState через ECB. Такая реализация показана в BattleExample через [LeafStateWriteRequest](Samples~/BattleExample/BehaviourTree/Integration/LeafStateWriteRequest.cs) и [LeafStateWriteSystem](Samples~/BattleExample/BehaviourTree/Integration/LeafStateWriteSystem.cs). Также важно понимать, что время первого `OnTick` такая сущность действия еще может быть не создана, поэтому если вам нужно к ней обратиться в OnTick вы должны предварительно проверять ее наличие.

<a id="condition-example"></a>

### Пример простого листа-условия из BattleExample

```csharp
[Serializable]
[LeafCodeGen(1, typeof(ExampleContext))]
public sealed class IsEnemyDetectedLeaf : ILeaf
{
    public LeafData GetCompiledData() => new LeafData
    {
        LeafId = 1
    };

    public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
    {
        var sensor = state.Context.EyeSensorLookup[state.Agent];
        bool exists = state.Context.EntityInfoLookup.Exists(sensor.DetectedEntity);
        return sensor.IsDetected == 1 && exists ? NodeStatus.Success : NodeStatus.Failure;
    }

    public static void OnEnter(ref RunnerState_ExampleContext state) { }
    public static void OnExit(ref RunnerState_ExampleContext state) { }
    public static void OnAbort(ref RunnerState_ExampleContext state) { }
}
```

Полный исходник: [IsEnemyDetectedLeaf](Samples~/BattleExample/BehaviourTree/Leaves/IsEnemyDetectedLeaf.cs).

Сенсор обновляет отдельная игровая система. Лист только читает готовый результат. Здесь предполагается, что у каждого агента есть `EyeSensor`; для необязательного компонента сначала проверяйте `HasComponent`.

<a id="action-example"></a>

### Пример сложного листа-поведения из BattleExample

Сокращенный пример из [AttackEnemyLeaf](Samples~/BattleExample/BehaviourTree/Leaves/AttackEnemyLeaf.cs). Выполнение атаки находится в [AttackStateSystem](Samples~/BattleExample/BehaviourTree/Systems/AttackStateSystem.cs).

```csharp
[Serializable]
[LeafCodeGen(6, typeof(DODBT.Example.BehaviourTree.ExampleContext))]
public sealed class AttackEnemyLeaf : ILeaf
{
    public LeafData GetCompiledData() => new LeafData
    {
        LeafId = 6
    };

    public static NodeStatus OnTick(ref RunnerState_ExampleContext state)
    {
        var lookUp = state.Context.AttackStateLookup;
        if (!lookUp.HasComponent(state.LeafState.StateEntity))
            return NodeStatus.Running;
        if (!state.Context.EntityInfoLookup.Exists(lookUp[state.LeafState.StateEntity].Target))
            return NodeStatus.Failure;
        if (state.Context.EyeSensorLookup[state.Agent].DetectedEntity != lookUp[state.LeafState.StateEntity].Target)
            return NodeStatus.Failure;

        return NodeStatus.Running;
    }

    public static void OnEnter(ref RunnerState_ExampleContext state)
    {
        var ecb = state.Context.Ecb;
        var entity = ecb.CreateEntity(state.SortKey);
        ecb.AddComponent(state.SortKey, entity, new AttackState {
            Owner = state.Agent,
            Target = state.Context.EyeSensorLookup[state.Agent].DetectedEntity
        });
        ecb.AddComponent(state.SortKey, ecb.CreateEntity(state.SortKey), new LeafStateWriteRequest {
            Entity = state.Agent,
            Index = state.LeafState.BufferIndex,
            Value = entity
        });
        ecb.SetComponentEnabled<SeeToDetectedTag>(state.SortKey, state.Agent, true);
    }

    public static void OnExit(ref RunnerState_ExampleContext state)
    {
        var ecb = state.Context.Ecb;
        ecb.DestroyEntity(state.SortKey, state.LeafState.StateEntity);
        ecb.SetComponentEnabled<SeeToDetectedTag>(state.SortKey, state.Agent, false);
    }

    public static void OnAbort(ref RunnerState_ExampleContext state)
    {
        var ecb = state.Context.Ecb;
        ecb.DestroyEntity(state.SortKey, state.LeafState.StateEntity);
        ecb.SetComponentEnabled<SeeToDetectedTag>(state.SortKey, state.Agent, false);
    }
}
```

<a id="graph-editor"></a>

<h2 align="center">Редактор графов</h2>

Откройте **Tools → VadimBurym → DODBT-Dots** и нажмите **BT Editor**.

<p align="center">
  <a href="docs~/images/03-editor-overview.webp"><img src="docs~/images/03-editor-overview.webp" width="100%" alt="BT Editor: граф дерева, панель Details и сообщения Output"></a>
</p>

<a id="create-graph"></a>

### Создание ассета дерева

Создайте новый ассет поведенческого дерева нажав на кнопку `New` в шапке редактора. Редактор создаст сразу 2 ассета: один для редактора и _Compiled версию для исполнения.

<p align="center">
  <a href="docs~/images/08-create-graph.webp"><img src="docs~/images/08-create-graph.webp" width="100%" alt="Создание нового BT Graph и построение дерева в редакторе DODBT-Dots"></a>
</p>

<p align="center"><em>Создание графа, добавление нод и настройка листьев.</em></p>

Дерево создается для **одного конкретного контекста**. Выбор листьев внутри дерева ограничен выбранным контекстом. В будущем контекст можно поменять в верхней панели редактора. Уже настроенные листья сохранятся, несовместимые ноды получат `Context mismatch` предупреждение пока их не заменят подходящими листьями.

**Copy** создает отдельное дерево со своим compiled-ассетом. **Rename** в BT Editor переименовывает пару, **Delete** удаляет выбранный граф и принадлежащий ему compiled-ассет после подтверждения.

<a id="graph-editing"></a>

### Построение графа

Нажмите правой кнопкой по свободному месту и выберите **Add → тип ноды**.

Соедините выход родителя со входом ребенка. У Root должен быть один выход; у остальных нод — один родитель. Порядок детей определяется их расположением **слева направо**. Перестановка нод по горизонтали меняет приоритет выполнения.

Для листьев выберите одну из вами созданных ранее реализаций.

<p align="center">
  <a href="docs~/images/07-select-leaf.webp"><img src="docs~/images/07-select-leaf.webp" width="100%" alt="Выбор реализации листа в графе через окно Select Leaf"></a>
</p>

<p align="center"><em>Выбор реализации листа прямо в ноде.</em></p>

> [!TIP]
> Узлы, не связанные с Root, можно оставить как черновики — в скомпилированное дерево они не входят.

<a id="node-types"></a>

#### Доступные ноды

<table width="100%">
<thead>
<tr>
<th width="25%" align="left">Нода</th>
<th width="75%" align="left">Поведение</th>
</tr>
</thead>
<tbody>
<tr>
<td>Root</td>
<td>Указатель на точку входа в граф</td>
</tr>
<tr>
<td>Leaf</td>
<td>Пользовательское условие или действие.</td>
</tr>
<tr>
<td>Sequence</td>
<td>Проверяет детей слева направо; останавливается на Failure или Running. Новый Tick начинает проверку сначала.</td>
</tr>
<tr>
<td>Selector</td>
<td>Ищет первого ребенка с Success или Running. Новый Tick снова начинает с приоритетных детей слева.</td>
</tr>
<tr>
<td>MemorySequence</td>
<td>Продолжает с сохраненного ребенка. <code>Reset On Failure</code> определяет сброс при Failure, <code>Reset On Abort</code> — при отмене. После общего Success начинает заново.</td>
</tr>
<tr>
<td>MemorySelector</td>
<td>Сохраняет Running-ребенка. <code>Pick Random</code> выбирает случайную начальную позицию; после Failure перебор идет по кругу, без повторного посещения в одной попытке. <code>Reset On Abort</code> управляет сбросом при отмене.</td>
</tr>
<tr>
<td>Parallel</td>
<td>Обходит несколько веток за Tick и считает их результаты по порогам Success и Failure. <code>Cache Child Status</code> позволяет не вызывать уже завершившихся детей повторно в текущей попытке.</td>
</tr>
</tbody>
</table>

> [!IMPORTANT]
> **Parallel не создает потоки или Jobs.** Это семантика дерева; внутри одного агента runner обходит ветки последовательно. Параллельное выполнение разных агентов обеспечивает ваша Job.

> [!IMPORTANT]
> **Декораторов в Dots сейчас нет.** Формат данных, компилятор и сгенерированный runner их не реализуют. Это граница текущей версии, а не ограничение Burst как технологии. Условия можно выражать листом перед действием в Sequence, а задержки и перезарядку — состоянием действия или игровой системой.

<a id="quality-of-life"></a>

### Quality of Life

Плагин постоянно развивается и получает новые улучшения удобства работы. Эти улучшения относятся только к **authoring — созданию, настройке и редактированию деревьев** — и не затрагивают их выполнение в рантайме. Если вам чего-то не хватает или какой-то шаг можно упростить — делитесь идеями и обратной связью. Ваши предложения помогают определять, что появится в следующих обновлениях.

В редакторе есть панель **Details**, в которой для выбранной ноды всегда можно:

- изменить имя
- посмотреть порядок выполнения детей
- изменить настройки
- посмотреть подсказку о поведении

<p align="center">
  <a href="docs~/images/05-parallel-details.webp"><img src="docs~/images/05-parallel-details.webp" width="100%" alt="Настройки Parallel в ноде и панели Details"></a>
</p>

**Связанные группы** позволяют повторять ветку внутри графа и синхронизировать ее структуру и настройки между экземплярами:

1. На корне нужной ветки выберите **Create Linked Group**.
2. Для связанной копии используйте **Duplicate Group**.
3. На вкладке **Groups** можно найти все семейство группы и его экземпляры.
4. **Unlink Group** превращает выбранный экземпляр в независимую ветку.

<p align="center">
  <a href="docs~/images/06-linked-groups.webp"><img src="docs~/images/06-linked-groups.webp" width="100%" alt="Редактирование связанных экземпляров ветки в BT Editor"></a>
</p>

<p align="center"><em>Связанные группы: изменения ветки синхронизируются между ее экземплярами.</em></p>

> [!IMPORTANT]
> Группы не должны пересекаться или включать другие связанные группы. При компиляции каждый экземпляр становится обычными нодами со своим состоянием. Это редакторская связь, а не общий изменяемый runtime-объект и не отдельное поддерево, разделяемое между агентами.

Панель **Output** показывает результат загрузки и компиляции, предупреждения и ошибки. Наиболее частые причины неудачного Compile:

<table width="100%">
<thead>
<tr>
<th width="40%" align="left">Причина</th>
<th width="60%" align="left">Что исправить</th>
</tr>
</thead>
<tbody>
<tr>
<td>Root не соединен с веткой</td>
<td>Соедините точку входа с деревом.</td>
</tr>
<tr>
<td>В Leaf не назначен класс или удален его скрипт</td>
<td>Выберите реализацию либо восстановите тип и его сериализованные данные.</td>
</tr>
<tr>
<td>Контекст не выбран, отсутствует или не подходит листу</td>
<td>Восстановите контекст или замените несовместимые листья.</td>
</tr>
<tr>
<td>Цикл, несколько родителей, ссылка на отсутствующую ноду</td>
<td>Исправьте соединения.</td>
</tr>
<tr>
<td>Композитная нода без детей, недопустимые пороги Parallel</td>
<td>Добавьте детей или поправьте настройки.</td>
</tr>
<tr>
<td>LeafId отличается от атрибута</td>
<td>Верните одинаковый ID в декларации и GetCompiledData.</td>
</tr>
</tbody>
</table>

В одной строке в шапке находятся **New / Delete / Copy / Rename**, выбор графа, **Context**, **Compile**, счетчики нод и листьев, поиск и переходы **Previous / Next**. Поиск помогает найти ноду, но не меняет порядок ее выполнения. Undo/Redo доступны для поддерживаемых операций редактирования графа.

<p align="center">
  <a href="docs~/images/04-editor-toolbar.webp"><img src="docs~/images/04-editor-toolbar.webp" width="100%" alt="Верхняя панель BT Editor: ассеты, контекст, компиляция и поиск"></a>
</p>

<a id="integration"></a>

<h2 align="center">Интеграция в проект</h2>

После создания графа в редакторе, нажмите кнопку `Compile`, чтобы получить скомпилированный ассет графа для исполнения.

<a id="create-trees"></a>

### Создание деревьев в runtime

В проекте участвуют три разных представления:

<table width="100%">
<thead>
<tr>
<th width="40%" align="left">Представление</th>
<th width="60%" align="left">Назначение</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>BtGraphAsset</code></td>
<td>Редакторский граф: ноды, связи, позиции, параметры и группы.</td>
</tr>
<tr>
<td><code>BehaviourTreeAsset</code></td>
<td>Результат Compile: данные, необходимые для создания runtime-дерева. Его назначают в настройки спавна или другие runtime-конфигурации.</td>
</tr>
<tr>
<td><code>BlobAssetReference&lt;BehaviourTreeBlob&gt;</code></td>
<td>Неизменяемые native-данные, которые читает Burst Job. Создаются из compiled-ассета 1 раз перед стартом игры.</td>
</tr>
</tbody>
</table>

Создайте для каждого уникального скомпилированного ассета дерева его `Blob` представление с помощью метода [CreateBlob()](#api-create-blob) у `BehaviourTreeAsset` и положите их в ECS для общего доступа.

> [!TIP]
> **Создавайте один blob на одно уникальное скомпилированное дерево и используйте его у всех подходящих агентов.** Не вызывайте `CreateBlob()` при каждом Tick или отдельно для каждого одинакового юнита.

**Пример из BattleExample:**

В примере все blob деревья хранятся в одном Singleton компоненте, к которому можно получить доступ из рантайма:

```csharp
public struct BehaviourTreeSingleton : IComponentData
{
    public NativeArray<BlobAssetReference<BehaviourTreeBlob>> Blobs;
}
```

Для одного дерева инициализация на основном потоке выглядит так:

```csharp
// compiled — BehaviourTreeAsset после успешного Compile.
// manager — EntityManager вашего World.

var blob = compiled.CreateBlob();
var trees = new NativeArray<BlobAssetReference<BehaviourTreeBlob>>(1, Allocator.Persistent);
trees[0] = blob;

var singleton = manager.CreateEntity();
manager.AddComponentData(singleton, new BehaviourTreeSingleton
{
    Blobs = trees
});
```

Пример владельца — [BattleSession](Samples~/BattleExample/Setup/BattleSession.cs).

<a id="agent-state-buffers"></a>

### Создание агентов и инициализация буферов

Помимо игровых компонентов агенту нужны два изменяемых буфера:

- `NodeStateElement` — состояние обхода и композитных нод;
- `LeafStateElement` — состояние каждого появления Leaf в дереве.

Создайте на агенте эти 2 буфера и оставьте их **пустыми**. Потом возьмите скомпилированный ассет `BehaviourTreeAsset` и с помощью метода [`FillAgentStateBuffers()`](#api-fill-agent-state-buffers) задайте служебные начальные значения.

> [!WARNING]
> Метод не очищает ранее заполненный буфер и не создает сами буферы вместо вас.

**Пример создания агента из BattleExample:**

```csharp
using var commands = new EntityCommandBuffer(Allocator.Temp);

var agent = commands.CreateEntity();
commands.AddBuffer<NodeStateElement>(agent);
commands.AddBuffer<LeafStateElement>(agent);
compiled.FillAgentStateBuffers(agent, commands); //compiled - скомпилированный BehaviourTreeAsset

commands.AddComponent(agent, new AiBrain
{
    BlobId = 0,
    UpdateTime = 0,
    RandomState = 12345u
});

// Здесь же добавьте компоненты, которые читают листья вашего контекста.
commands.Playback(manager);
```

> [!TIP]
> `AiBrain` тоже относится к примеру: он хранит индекс дерева, время следующего Tick и состояние Random. В реальном спавне задайте каждому агенту собственный ненулевой seed и распределите время первого обновления по интервалу. Иначе одинаковые агенты могут принимать одинаковые случайные решения и одновременно создавать пик нагрузки.

> [!NOTE]
> Такой «прогрев» — инициализация состояния и выделение памяти **до первого Tick**, а не компиляция Burst. Буферы объявлены с `InternalBufferCapacity(0)`: их содержимое не занимает фиксированный большой запас внутри чанка. Первичная инициализация имеет стоимость и не должна повторяться в горячем цикле.

> [!WARNING]
> При работе с пулом не добавляйте элементы поверх прежнего состояния. Если агент был активен, сначала корректно отмените его действия; затем подготовьте чистые буферы и заполните их для нужного дерева. Простая очистка буферов не вызывает `OnAbort` и не удаляет ваши сущности действий. Не допускайте Tick до завершения playback, который подготовил агента.

<a id="tree-execution"></a>

### Исполнение дерева в runtime

Частоту выполнения определяет ваш код. В Battle Example дерево каждого агента обновляется раз в **0,2 секунды**, а таймеры агентов сдвинуты. Системы движения и анимации могут обновляться чаще.

Система тика выполняет следующие действия:

1. Получает общую таблицу blob.
2. Обновляет lookups и type handles.
3. Создает ECB у выбранной ECB-системы и заполняет `ExampleContext`.
4. Планирует Burst Job с входящей `state.Dependency` и сохраняет возвращенный handle обратно.
5. Job выбирает дерево агента, восстанавливает Random, вызывает runner и сохраняет Random и время следующего обновления.

Ключевая часть Job из примера BattleExample (см. [BehaviourTreeTickSystem](Samples~/BattleExample/BehaviourTree/Integration/BehaviourTreeTickSystem.cs)):

```csharp
var aiBrain = aiBrains[i];
if (aiBrain.UpdateTime > CurrentTime)
    continue;

var entity = entities[i];
var nodeStates = nodeBuffers[i];
var leafStates = leafBuffers[i];
var treeBlob = Trees[aiBrain.BlobId];
var random = new Unity.Mathematics.Random { state = aiBrain.RandomState };

Runner.Tick(
    entity,
    ref treeBlob.Value,
    ref random,
    nodeStates,
    leafStates,
    Context,
    unfilteredChunkIndex);

aiBrain.RandomState = random.state;
aiBrain.UpdateTime += UpdateInterval;
aiBrains[i] = aiBrain;
```

Здесь `Runner` - автоматически сгенерированный скрипт и имеет тип `BTRunner_<ВашКонтекст>`. Вы должны предварительно создать его в своей системе в методе OnCreate и закешировать для дальнейшего использования. В примере это выглядит так: `_runner = new BTRunner_ExampleContext()`

Для явной отмены используйте [`Runner.Abort`](#api-runner-abort) с `ref random`, теми же буферами и подходящим контекстом. Сохраните получившийся Random обратно агенту. Применение команд отмены, удаление сущностей действий и очистку игровых состояний организует проект.

> [!WARNING]
> `state.Dependency = job.ScheduleParallel(query, state.Dependency)` сохраняет связь с другими системами. Не вызывайте `Complete()` после каждого планирования без необходимости. При ручном создании ECB и нестандартном планировании сами обеспечьте завершение его производителей перед playback и освобождением.

> [!IMPORTANT]
> Изменение графа и Compile не обновляют уже созданный blob. В обычном процессе работы остановите Play Mode, скомпилируйте граф и запустите сцену снова. Runtime-замена дерева требует остановить его Jobs, отменить старые действия, обновить привязку к blob и заново инициализировать буферы. Автоматической горячей замены плагин не делает.

<a id="debugging"></a>

<h2 align="center">Отладка</h2>

<p align="center">
  <a href="docs~/images/03-battle-observe.webp"><img src="docs~/images/03-battle-observe.webp" width="100%" alt="Battle Example: наблюдение за Orc 50 в Scene View и подсветка выполняемых веток дерева"></a>
</p>

<p align="center"><em>Observe в Battle Example: поведение выбранного агента и выполняемые ветки дерева в реальном времени.</em></p>

> [!IMPORTANT]
> Отладка дерева работает **в Unity Editor во время Play Mode**. Код редактора и компоненты трассировки исключены из player через `UNITY_EDITOR`. Это не удаленный отладчик готовой сборки.

<a id="debug-integration"></a>

### Что подключить для отладки

`FillAgentStateBuffers()` в редакторе автоматически добавляет агенту `BtDebugTree`: GUID исходного графа и ревизию компиляции. Он позволяет найти дерево агента. Ничего вручную прикреплять ко всем сущностям через инспектор не требуется.

> [!IMPORTANT]
> Для полной подсветки Observe ваша система тика должна поддержать запись статусов **одного выбранного агента**. Плагин не может автоматически подменить произвольную пользовательскую Job и определить ее зависимости. В Battle Example это подключение уже сделано.

<table width="100%">
<thead>
<tr>
<th width="25%" align="left">Тип</th>
<th width="75%" align="left">Кто создает и использует</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>BtDebugTree</code></td>
<td>Добавляется при FillAgentStateBuffers в Editor. Связывает агента с графом и ревизией.</td>
</tr>
<tr>
<td><code>BtDebugTarget</code></td>
<td>Окно Observe создает singleton-запрос в нужном World. Система тика читает выбранную сущность.</td>
</tr>
<tr>
<td><code>BtDebugNodeState</code></td>
<td>Observe создает буфер статусов у выбранного агента. Job записывает результат TickWithDebug.</td>
</tr>
<tr>
<td><code>BtDebugSnapshot</code></td>
<td>Observe создает компонент снимка; Job обновляет Tick, Time и итоговый Status.</td>
</tr>
</tbody>
</table>

> [!IMPORTANT]
> Все ссылки на эти четыре типа в вашей runtime-сборке заключайте в `#if UNITY_EDITOR`, включая поля, инициализацию и вызовы.

**Поля системы и создание lookups в OnCreate:**

```csharp
#if UNITY_EDITOR
private BufferLookup<BtDebugNodeState> _debugStates;
private ComponentLookup<BtDebugSnapshot> _debugSnapshots;
#endif

// Внутри OnCreate:
#if UNITY_EDITOR
_debugStates = state.GetBufferLookup<BtDebugNodeState>();
_debugSnapshots = state.GetComponentLookup<BtDebugSnapshot>();
#endif
```

**Перед ScheduleParallel:**

```csharp
#if UNITY_EDITOR
_debugStates.Update(ref state);
_debugSnapshots.Update(ref state);

job.DebugEntity = SystemAPI.TryGetSingleton<BtDebugTarget>(out var target)
    ? target.Entity
    : Entity.Null;
job.DebugStates = _debugStates;
job.DebugSnapshots = _debugSnapshots;
#endif

state.Dependency = job.ScheduleParallel(_query, state.Dependency);
```

**Поля Job:**

```csharp
#if UNITY_EDITOR
public Entity DebugEntity;
[NativeDisableParallelForRestriction]
public BufferLookup<BtDebugNodeState> DebugStates;
[NativeDisableParallelForRestriction]
public ComponentLookup<BtDebugSnapshot> DebugSnapshots;
#endif
```

> [!WARNING]
> Ограничение параллельного доступа снимается только для этих lookup: Job пишет лишь в текущего выбранного агента, а каждый агент встречается в запросе один раз. Этот атрибут не дает общей защиты от гонок; не переносите его без проверки своей схемы записи.

**Вместо единственного вызова Tick в цикле агентов:**

```csharp
bool captured = false;
#if UNITY_EDITOR
if (entity == DebugEntity && DebugStates.HasBuffer(entity) &&
    DebugSnapshots.HasComponent(entity))
{
    var statuses = DebugStates[entity];
    if (statuses.Length == nodeStates.Length)
    {
        var result = Runner.TickWithDebug(
            entity, ref treeBlob.Value, ref random,
            nodeStates, leafStates, Context, unfilteredChunkIndex, statuses);

        var snapshot = DebugSnapshots[entity];
        snapshot.Tick++;
        snapshot.Time = CurrentTime;
        snapshot.Status = result;
        DebugSnapshots[entity] = snapshot;
        captured = true;
    }
}
#endif

if (!captured)
{
    Runner.Tick(entity, ref treeBlob.Value, ref random,
        nodeStates, leafStates, Context, unfilteredChunkIndex);
}
```

> [!WARNING]
> Агент должен исполниться **ровно один раз**: либо обычным Tick, либо TickWithDebug. После этой ветки сохраните Random и таймер так же, как без отладки. Не запускайте дополнительный Tick ради снимка и не ускоряйте частоту обновления наблюдаемого агента. Burst работает и для отладочной Job; цена записи добавляется выбранному агенту. Полный вариант подключения находится в [системе тика примера](Samples~/BattleExample/BehaviourTree/Integration/BehaviourTreeTickSystem.cs).

<a id="debugger-window"></a>

### Визуальный отладчик

Откройте **Tools → VadimBurym → DODBT-Dots** и нажмите **BT Debugger**, выберите нужный World и найдите агента. Окно показывает агентов с привязкой BT и их активные листья, поддерживает поиск и постраничный вывод. Нажмите **Observe**, чтобы открыть дерево выбранной сущности.

Список активных листьев обновляется до 2 раз в секунду; полная визуализация Observe — до 10 раз в секунду. Если само дерево тикает раз в 0,2 секунды, новые результаты появляются с частотой его Tick. Это ожидаемое поведение, а не потеря кадров отладки.

Статусы: синий — Running, зеленый — Success, красный — Failure, серый — отсутствие результата. В заголовке видны World, сущность, номер Tick, время и итоговый статус. В одном World допускается один активный запрос Observe. Закрытие наблюдения освобождает его временные компоненты.

<a id="scene-observe"></a>

### Observe над выбранным юнитом

В окне **Tools → VadimBurym → DODBT-Dots** включите **Settings → Show scene markers** и выберите сущность в Scene View. Для агента с BT появляется маркер с кнопкой **Observe**. Положение берется из `LocalToWorld`; при выборе дочерней render-сущности плагин ищет владельца BT по цепочке `Parent`.

Это часть плагина: специальный MonoBehaviour маркера в свой проект добавлять не надо. Встроенный адаптер читает выбор сущности из редактора Entities. Он зависит от совместимости API этой версии пакета; при несовместимости короткий путь через выбор сцены может отключиться, но список **BT Debugger** остается альтернативой.

Если подсветка не появляется, проверьте:

- выбран правильный World, сущность существует и имеет оба заполненных буфера;
- создание агента прошло через FillAgentStateBuffers и есть BtDebugTree;
- система тика читает BtDebugTarget, вызывает TickWithDebug и обновляет BtDebugSnapshot;
- после ветки отладки не выполняется второй обычный Tick;
- граф скомпилирован, а агент использует тот же compiled-ассет и его ревизию;
- исходный граф доступен в проекте.

> [!NOTE]
> Если изменить граф, перекомпилировать его или перепривязать агента к другому дереву во время Observe, наблюдение остановится вместо показа неподходящих данных. Скомпилируйте граф, пересоздайте runtime-сессию и откройте Observe снова.

<a id="limitations"></a>

<h2 align="center">Ограничения и компромиссы</h2>

<a id="leaf-data-limits"></a>

### LeafData и настройки листьев

В рантайм попадает только результат `GetCompiledData()`. Поле, добавленное в класс листа, нужно самостоятельно записать в фиксированные слоты [LeafData](#api-leaf-data): `Int0`, `Float0`, `Byte0` или `Bytes`.

Ссылки на объекты Unity, строки и обычные C#-коллекции туда не помещаются. Передавайте числовые параметры, индексы или храните дополнительные данные в ECS-компонентах и blob, доступных через контекст.

<a id="context-state-limits"></a>

### Контекст и состояние

Контекст должен быть `unmanaged`: без `GameObject`, C#-сервисов и других managed-ссылок. Получать такие сервисы через DI или ServiceLocator внутри Burst-листа нельзя. Подготовьте зависимости в своей системе и передайте листьям данные, lookups и ECB.

Листья и их контекст должны находиться в одной сборке. Имена контекстов в ней должны различаться, а ID реализаций листьев — быть уникальными внутри каждого контекста.

[LeafStateElement](#api-leaf-state-element) имеет фиксированную структуру. Таймеры, цели и другие данные действия храните в игровых компонентах; `StateEntity` позволяет связать лист с отдельной сущностью действия. При работе через ECB учитывайте playback, а при смене дерева заново подготовьте буферы и отмените устаревшие запросы записи.

<a id="integration-limits"></a>

### Поведение и интеграция

В Dots нет встроенных декораторов. Условия можно выразить отдельными листьями, а таймеры и перезарядку — состоянием действия. Директива `DODBT_DECORATORS` относится только к Light.

Дерево выбирает поведение; движение, атаки, сенсоры, частоту тиков и планирование Jobs реализует ваш проект. Вы также отвечаете за завершение действий, зависимости Jobs и освобождение native-памяти. Battle Example показывает один из вариантов подключения.

<a id="size-limits"></a>

### Размеры дерева и директивы

- До **65 535 исполняемых узлов** в обычном формате или **255** с `DODBT_SMALL_SIZE`. Редакторский Root в это число не входит.
- От **1 до 255 детей** у композитного узла. Допустимость вложенных Parallel проверяет компилятор с учетом стека отмены.
- ID реализации листа — неотрицательный `int`; `DODBT_SMALL_SIZE` не меняет его тип.
- Один контекст на граф. Отладка доступна в Unity Editor, с одним активным Observe на World.

> [!IMPORTANT]
> Обычный формат подходит по умолчанию. При включении `DODBT_SMALL_SIZE` задайте директиву и плагину, и сборке контекста, затем перекомпилируйте графы и пересоздайте blob и состояния агентов.

<a id="performance-gain"></a>

### Выигрыш в производительности

**Главный выигрыш — оптимизация выполнения дерева на большом количестве агентов.** Фиксированные unmanaged-данные и статические методы листьев позволяют использовать Burst и обрабатывать разных агентов параллельно в Jobs. Само дерево хранится в общем blob, а у каждого агента остается только его изменяемое состояние.

В сравнительном тесте на **10 000 агентах** одна и та же Dots-версия без Burst и Jobs затрачивала около **126 мс**, а с ними — около **1 мс**: ускорение примерно **в 126 раз**. И это без учета распределенного обновления BT раз в 0,2 секунды (например), что также допускается в рамках данной архитектуры. Ограничения формата данных как раз позволяют использовать эти инструменты. Это результат конкретного теста; время в вашей игре зависит от дерева, листьев и оборудования.

Если Burst и Jobs вашему проекту не нужны, [DODBT-Light](../VadimBurym-DODBT-Light/DOCUMENTATION-ru.md) будет проще в использовании.

<a id="performance"></a>

<a id="automated-tests"></a>

<h2 align="center">Автоматические проверки</h2>

Тесты ядра и редактора находятся в [Tests](Tests). Для UPM-установки добавьте идентификатор пакета в массив `testables` корневого `Packages/manifest.json`, сохранив остальные записи, и откройте **Window → General → Test Runner**:

```json
"testables": [
  "com.vadimburym.dodbt.dots"
]
```

Проверяются поведение нод, жизненный цикл, компиляция и сериализация, совместимость ID, работа с Burst Jobs и совпадение результатов обычного и отладочного исполнения. Тесты объявлены отдельной редакторской сборкой и не должны попадать в обычный player. Проверяйте производительность своей игры отдельно: без открытого Observe, после прогрева и с реальными листьями и частотой обновления.

<a id="license"></a>

<h2 align="center">Лицензия</h2>

Плагин и его документация распространяются по [DODBT Custom Game-Use and Redistribution License](LICENSE.md), общей для DODBT-Light и DODBT-Dots.

Код Battle Example имеет отдельную [MIT-лицензию](Samples~/BattleExample/LICENSE.md). Условия сторонних материалов перечислены в [THIRD_PARTY.md](Samples~/BattleExample/THIRD_PARTY.md).

<a id="public-api"></a>

<h2 align="center">Публичный API</h2>

Типы для создания поведения, подготовки дерева, выполнения и подключения отладки. Основное пространство имен — `VadimBurym.DodBehaviourTree.Dots`; сгенерированные типы находятся в `VadimBurym.DodBehaviourTree.Dots.Generated`.

В названиях и аргументах ниже `<Контекст>` обозначает тип вашего контекста. Генератор подставляет его имя в названия `BTRunner_<Контекст>` и `RunnerState_<Контекст>`.

---

<a id="api-generate-bt-runner"></a>

### **`GenerateBtRunnerAttribute`**

**Тип:** атрибут структуры контекста.

**Принимает:** нет аргументов.

**Описание:** включает генерацию раннера и передаваемого листьям `RunnerState` для указанного контекста. Контекст должен быть доступной генератору структурой без параметров типа, содержащей только unmanaged-данные. Простые имена контекстов внутри одной сборки должны различаться: по ним формируются имена сгенерированных типов.

---

<a id="api-leaf-code-gen"></a>

### **`LeafCodeGenAttribute`**

**Тип:** атрибут класса листа.

**Принимает:**

- **`id`** — `int`. Неотрицательный идентификатор реализации листа, уникальный в пределах контекста.
- **`contextType`** — `Type`. Тип контекста с атрибутом `GenerateBtRunner`.

**Описание:** связывает реализацию листа с контекстом и включает ее в сгенерированный раннер. Значение `id` должно совпадать с `LeafData.LeafId`, возвращаемым из `GetCompiledData()`. Несколько нод одного класса используют один ID реализации, но имеют собственные настройки и состояния. Лист и контекст должны находиться в одной сборке. Окно **New Leaf** выбирает свободный ID автоматически.

---

<a id="api-ileaf"></a>

### **`ILeaf`**

**Тип:** интерфейс класса листа.

**Состав:** метод `GetCompiledData()`.

**Описание:** позволяет редактору получить настройки узла для компиляции. Экземпляр класса используется при настройке графа; во время выполнения раннер вызывает статические методы жизненного цикла. Эти методы объявляются в классе листа и необходимы генератору, но не входят в сам интерфейс `ILeaf`. Окно **New Leaf** создает все нужные сигнатуры.

#### **`GetCompiledData()`**

**Принимает:** нет аргументов.

**Возвращает:** `LeafData` — скомпилированные параметры узла.

**Описание:** упаковывает настройки экземпляра листа в фиксированную структуру, которую компилятор сохраняет в дереве. Вызывается при компиляции графа, а не при каждом тике агента.

#### **`OnEnter(state)`**

**Принимает:** `ref RunnerState_<Контекст> state` — данные агента, контекст и состояние текущего листа.

**Возвращает:** ничего (`void`).

**Описание:** статический метод входа в неактивный лист. Обычно запускает игровое действие. Вызывается перед первым `OnTick` в том же вызове раннера; пока лист возвращает `Running`, повторного входа нет.

#### **`OnTick(state)`**

**Принимает:** `ref RunnerState_<Контекст> state` — данные агента, контекст и состояние текущего листа.

**Возвращает:** `NodeStatus` — `Running`, `Success` или `Failure`.

**Описание:** статический метод проверки или обновления поведения. `Running` сохраняет лист активным до следующего тика, а `Success` и `Failure` завершают его выполнение.

#### **`OnExit(state)`**

**Принимает:** `ref RunnerState_<Контекст> state` — данные агента, контекст и состояние текущего листа.

**Возвращает:** ничего (`void`).

**Описание:** статический метод завершения после `OnTick`, вернувшего `Success` или `Failure`. Используется для освобождения ресурсов и завершения связанных игровых действий.

#### **`OnAbort(state)`**

**Принимает:** `ref RunnerState_<Контекст> state` — данные агента, контекст и состояние текущего листа.

**Возвращает:** ничего (`void`).

**Описание:** статический метод принудительной отмены активного листа. Должен остановить незавершенное действие и освободить его ресурсы. При отмене дополнительный `OnExit` не вызывается.

---

<a id="api-leaf-icon"></a>

### **`LeafIconAttribute`**

**Тип:** необязательный атрибут класса листа; используется только в редакторе.

**Принимает:** `string asset` — GUID текстуры или путь внутри `Assets/` либо `Packages/`.

**Свойства:**

- **`Asset`** — `string`, только чтение. Ссылка на текстуру, переданная атрибуту.

**Описание:** задает иконку узла листа при включенной настройке **Use node icons**. Предпочтителен GUID: он сохраняет связь при перемещении и переименовании файла. Если текстура не найдена, лист сохраняет обычный вид. На выполнение дерева атрибут не влияет.

---

<a id="api-runner-state"></a>

### **`RunnerState_<Контекст>`**

**Тип:** сгенерированная `ref struct`.

**Поля:**

- **`Agent`** — `Entity`. Агент, для которого выполняется дерево.
- **`LeafData`** — `LeafData`. Скомпилированные параметры текущего узла. Предназначены для чтения; изменение этой копии не обновляет blob.
- **`LeafState`** — `LeafStateElement`. Изменяемое состояние текущего листа. Раннер записывает изменения обратно в буфер агента после выполнения методов листа.
- **`Context`** — `<Контекст>`. Контекст, переданный вашей системой: доступ к игровым данным и запись команд в ECB.
- **`Random`** — `Unity.Mathematics.Random`. Генератор случайных чисел агента. Изменения его состояния передаются обратно вызывающему коду.
- **`SortKey`** — `int`. Ключ сортировки команд `EntityCommandBuffer.ParallelWriter`, переданный вашей системой.

**Описание:** объединяет все данные, доступные листу в текущем вызове. Данные, которые должны сохраняться между тиками, храните в [LeafStateElement](#api-leaf-state-element) или игровых ECS-компонентах.

---

<a id="api-leaf-data"></a>

### **`LeafData`**

**Тип:** структура данных.

**Поля:**

- **`LeafId`** — `int`. Идентификатор реализации листа; совпадает с `id` в `LeafCodeGen`.
- **`Int0`** — `int`. Целочисленный параметр.
- **`Float0`** — `float`. Параметр с плавающей точкой.
- **`Byte0`** — `byte`. Однобайтовый параметр, например флаг или небольшое перечисление.
- **`Bytes`** — `FixedList32Bytes<byte>`. Список байтов фиксированной емкости для дополнительных упакованных параметров.

**Описание:** хранит компактные настройки конкретного узла в скомпилированном дереве. Они общие для всех агентов, использующих это дерево. Назначение пользовательских слотов и формат `Bytes` определяет реализация листа. Индивидуальное изменяемое состояние агента хранится отдельно.

---

<a id="api-leaf-state-element"></a>

### **`LeafStateElement`**

**Тип:** структура, реализующая `IBufferElementData`.

**Поля:**

- **`IsEntered`** — `byte`. Признак активного входа: `0` — лист не активен, `1` — вход выполнен и лист еще не завершен. Полем управляет раннер.
- **`BufferIndex`** — `ushort`. Индекс элемента в буфере состояния листьев. Позволяет адресно записать состояние из другой системы.
- **`StateEntity`** — `Entity`. Ссылка на сущность текущего действия; изначально равна `Entity.Null`.

**Описание:** хранит индивидуальное состояние одного узла листа у конкретного агента в `DynamicBuffer<LeafStateElement>`. Через `StateEntity` лист проверяет действие, меняет его данные или останавливает его. Создание и удаление сущности реализует ваш код. Временную сущность из `ecb.CreateEntity()` нельзя напрямую записывать в живой буфер: сначала ссылка должна быть разрешена при playback. Вариант записи через запрос показан в [LeafStateWriteRequest](Samples~/BattleExample/BehaviourTree/Integration/LeafStateWriteRequest.cs) и [LeafStateWriteSystem](Samples~/BattleExample/BehaviourTree/Integration/LeafStateWriteSystem.cs).

---

<a id="api-node-status"></a>

### **`NodeStatus`**

**Тип:** перечисление на основе `byte`.

**Значения:**

- **`None`** — `0`. Служебное отсутствие результата, в том числе для отладки.
- **`Running`** — `1`. Выполнение продолжается.
- **`Success`** — `2`. Выполнение завершено успешно.
- **`Failure`** — `3`. Условие не выполнено или действие завершилось неудачей.

**Описание:** обозначает результат выполнения узла или всего дерева. Из `OnTick` возвращайте `Running`, `Success` или `Failure`.

---

<a id="api-behaviour-tree-asset"></a>

### **`BehaviourTreeAsset`**

**Тип:** класс, наследующий `ScriptableObject`.

**Свойства:**

- **`GUID`** — `string`, только чтение. GUID исходного графа, связывающий скомпилированное дерево с редакторским представлением.

**Описание:** скомпилированный ассет дерева. Используется для создания blob и подготовки состояния агентов.

<a id="api-create-blob"></a>

#### **`CreateBlob()`**

**Принимает:** нет аргументов.

**Возвращает:** `BlobAssetReference<BehaviourTreeBlob>` — ссылку на созданные данные дерева.

**Описание:** создает blob в памяти `Allocator.Persistent`. Один blob можно использовать для всех агентов с этим деревом. Владелец освобождает его через `Dispose()` после завершения всех использующих его Jobs. При несовместимом формате ассета или привязках листьев метод выбрасывает `InvalidOperationException` с описанием причины.

<a id="api-fill-agent-state-buffers"></a>

#### **`FillAgentStateBuffers(entity, ecb)`**

**Принимает:**

- **`entity`** — `Entity`. Сущность агента.
- **`ecb`** — `EntityCommandBuffer`. Буфер команд для записи начальных состояний.

**Возвращает:** ничего (`void`).

**Описание:** записывает в ECB команды заполнения состояния агента для этого дерева. На агенте уже должны быть подготовлены пустые буферы `NodeStateElement` и `LeafStateElement`; их можно добавить предыдущими командами того же ECB. Метод добавляет элементы, не очищает существующие данные и не создает сами буферы. В редакторе также добавляется `BtDebugTree`. Первый тик выполняйте после playback команд подготовки.

---

<a id="api-behaviour-tree-blob"></a>

### **`BehaviourTreeBlob`**

**Тип:** структура данных.

**Поля для интеграции:**

- **`RootIndex`** — `int`. Индекс корневого исполняемого узла.
- **`Nodes`** — `BlobArray<Node>`. Массив узлов. `Nodes.Length` определяет размер буфера состояния узлов.
- **`Leafs`** — `BlobArray<LeafData>`. Скомпилированные параметры узлов листьев.

**Описание:** хранит данные дерева внутри `BlobAssetReference<BehaviourTreeBlob>`. После создания данные доступны для чтения и могут совместно использоваться разными агентами. При обычной интеграции создавайте blob через [CreateBlob](#api-create-blob) и передавайте его раннеру.

---

<a id="api-node-state-element"></a>

### **`NodeStateElement`**

**Тип:** структура, реализующая `IBufferElementData`.

**Поля для интеграции:**

- **`LeafStateIndex`** — `ushort`, либо `byte` при `DODBT_SMALL_SIZE`. Индекс соответствующего элемента в буфере `LeafStateElement`, если узел является листом. Для остальных узлов содержит служебное значение отсутствия индекса.

**Описание:** хранит индивидуальное состояние обхода одного узла у конкретного агента. Буфер `DynamicBuffer<NodeStateElement>` подготавливается через [FillAgentStateBuffers](#api-fill-agent-state-buffers) и передается раннеру. Остальным состоянием обхода управляет раннер; вручную менять его при обычной интеграции не требуется.

---

<a id="api-bt-runner"></a>

### **`BTRunner_<Контекст>`**

**Тип:** сгенерированная структура.

**Состав:** методы `Tick`, `Abort` и редакторский `TickWithDebug`. Пользовательских полей состояния нет.

**Описание:** выполняет деревья выбранного контекста. Не хранит состояние конкретного агента: оно передается через буферы, контекст и генератор случайных чисел. Частоту вызовов и планирование Jobs определяет ваша система.

<a id="api-runner-tick"></a>

#### **`Tick(...)`**

**Принимает:**

- **`entity`** — `Entity`. Агент, для которого выполняется дерево.
- **`blob`** — `ref BehaviourTreeBlob`. Данные этого дерева.
- **`rng`** — `ref Unity.Mathematics.Random`. Генератор случайных чисел агента; после вызова сохраните измененное состояние.
- **`nodeStates`** — `DynamicBuffer<NodeStateElement>`. Буфер состояния узлов агента.
- **`leafStates`** — `DynamicBuffer<LeafStateElement>`. Буфер состояния листьев агента.
- **`leafContext`** — `in <Контекст>`. Актуальный игровой контекст.
- **`sortKey`** — `int`. Ключ сортировки команд ECB, доступный листьям через `RunnerState.SortKey`.

**Возвращает:** `NodeStatus` — результат дерева после текущего тика.

**Описание:** выполняет один тик дерева, вызывает методы листьев и обновляет состояние обхода. Используется для обычного выполнения без записи отладочного снимка.

<a id="api-runner-abort"></a>

#### **`Abort(...)`**

**Принимает:**

- **`entity`** — `Entity`. Агент, выполнение которого нужно отменить.
- **`blob`** — `ref BehaviourTreeBlob`. Данные дерева агента.
- **`rng`** — `ref Unity.Mathematics.Random`. Генератор случайных чисел агента; после вызова сохраните измененное состояние.
- **`nodeStates`** — `DynamicBuffer<NodeStateElement>`. Буфер состояния узлов агента.
- **`leafStates`** — `DynamicBuffer<LeafStateElement>`. Буфер состояния листьев агента.
- **`leafContext`** — `in <Контекст>`. Контекст для завершения активных действий.
- **`sortKey`** — `int`. Ключ сортировки команд ECB.

**Возвращает:** ничего (`void`).

**Описание:** отменяет активное выполнение и вызывает `OnAbort` у активных листьев. Используется перед отключением AI, заменой дерева или возвратом агента в пул. Очистку игровых действий реализуют обработчики `OnAbort`; записанные ими команды ECB применяются при playback. Последующий `Tick` снова запускает выполнение, а сохранение позиции Memory-узлов зависит от `Reset On Abort`. Перегрузка без `ref rng` устарела.

<a id="api-runner-tick-with-debug"></a>

#### **`TickWithDebug(...)`**

**Принимает:**

- **`entity`** — `Entity`. Наблюдаемый агент.
- **`blob`** — `ref BehaviourTreeBlob`. Данные дерева агента.
- **`rng`** — `ref Unity.Mathematics.Random`. Генератор случайных чисел агента; после вызова сохраните измененное состояние.
- **`nodeStates`** — `DynamicBuffer<NodeStateElement>`. Буфер состояния узлов агента.
- **`leafStates`** — `DynamicBuffer<LeafStateElement>`. Буфер состояния листьев агента.
- **`leafContext`** — `in <Контекст>`. Актуальный игровой контекст.
- **`sortKey`** — `int`. Ключ сортировки команд ECB.
- **`debugStates`** — `DynamicBuffer<BtDebugNodeState>`. Буфер для записи статусов; его длина должна совпадать с количеством узлов дерева.

**Возвращает:** `NodeStatus` — результат дерева после текущего тика.

**Описание:** выполняет один тик с записью статусов узлов для визуальной отладки. Вызывается вместо `Tick`, чтобы агент не обновлялся дважды. Поля `BtDebugSnapshot` после вызова обновляет ваша система. Метод доступен только под `UNITY_EDITOR`.

---

<a id="api-bt-debug-tree"></a>

### **`BtDebugTree`**

**Тип:** структура, реализующая `ISharedComponentData`; доступна только под `UNITY_EDITOR`.

**Поля:**

- **`GraphGuid`** — `FixedString64Bytes`. GUID исходного графа.
- **`CompiledRevision`** — `FixedString64Bytes`. Идентификатор версии компиляции, используемой агентом.

**Описание:** связывает агента с графом и его скомпилированной версией. Добавляется через `FillAgentStateBuffers`. Редактор использует компонент для поиска графа и проверки соответствия данным агента.

---

<a id="api-bt-debug-target"></a>

### **`BtDebugTarget`**

**Тип:** структура, реализующая `IComponentData`; доступна только под `UNITY_EDITOR`.

**Поля:**

- **`Entity`** — `Entity`. Сущность агента, выбранного для отладки.

**Описание:** singleton-запрос наблюдения, который редактор создает при запуске Observe. Ваша система читает его и вызывает `TickWithDebug` для указанного агента, сохраняя обычный `Tick` для остальных.

---

<a id="api-bt-debug-node-state"></a>

### **`BtDebugNodeState`**

**Тип:** структура, реализующая `IBufferElementData`; доступна только под `UNITY_EDITOR`.

**Поля:**

- **`Status`** — `NodeStatus`. Статус узла, записанный отладочным раннером.

**Описание:** элемент буфера статусов для визуальной отладки. Индекс элемента соответствует индексу узла в скомпилированном дереве. Буфер создает редактор при запуске Observe; ваша система передает его в `TickWithDebug`.

---

<a id="api-bt-debug-snapshot"></a>

### **`BtDebugSnapshot`**

**Тип:** структура, реализующая `IComponentData`; доступна только под `UNITY_EDITOR`.

**Поля:**

- **`Tick`** — `uint`. Счетчик записанных отладочных тиков.
- **`Time`** — `double`. Время последнего отладочного тика, обычно `SystemAPI.Time.ElapsedTime`.
- **`Status`** — `NodeStatus`. Результат дерева, возвращенный `TickWithDebug`.

**Описание:** хранит результат последнего записанного отладочного тика агента. Компонент создает редактор при запуске Observe. Его поля обновляет ваша система тика; сам `TickWithDebug` этого не делает. Полная схема описана в разделе [подключения отладки](#debug-integration).
