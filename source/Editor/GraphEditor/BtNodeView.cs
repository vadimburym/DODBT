// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VadimBurym.DodBehaviourTree;
using Node = UnityEditor.Experimental.GraphView.Node;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

internal sealed class BtNodeView : Node
{
    public static readonly Vector2 DefaultSize = new Vector2(320, 50);
    private const float PortPillWidth = 56f;
    private const float PortPillHeight = 15f;
    
    public string Guid { get; }
    public string Title;
    public BtNodeKind Kind { get; }

    public Port InputPort { get; private set; }
    public Port OutputPort { get; private set; }

    private BtGraphAsset _graphAsset;
    private Action<string> _onDataChanged;
    private Action _onPortsColorChanged;
    
    private IntegerField _integerFieldOne;
    private IntegerField _integerFieldTwo;
    private Toggle _boolToggleOne;
    private Toggle _boolToggleTwo;

    private VisualElement _outputVisual;
    private VisualElement _inputVisual;
    
    private IMGUIContainer _leafOdinContainer;
    private PropertyTree _leafNodeTree;
    
    private bool _isReadOnly;
    private Color _currentDebugColor = new Color(0.10f, 0.70f, 1.00f, 1f);
    
    internal BtNodeView(string guid, BtNodeKind kind, string title, IEdgeConnectorListener edgeConnectorListener)
    {
        Guid = guid;
        Kind = kind;
        this.title = title;
        Title = title;
        
        titleContainer.style.justifyContent = Justify.Center;
        titleContainer.style.alignItems = Align.Center;

        var titleLabel = titleContainer.Q<Label>();
        if (titleLabel != null)
        {
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.style.fontSize = 17;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        
        style.width = 320;
        style.minHeight = 50;
        style.overflow = Overflow.Visible;
        mainContainer.style.overflow = Overflow.Visible;
        titleContainer.style.overflow = Overflow.Visible;
        extensionContainer.style.overflow = Overflow.Visible;
        //style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
        //mainContainer.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
        contentContainer.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
        extensionContainer.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
        extensionContainer.style.borderBottomLeftRadius = 11;
        extensionContainer.style.borderBottomRightRadius = 11;
  
        style.borderTopLeftRadius = 12;
        style.borderTopRightRadius = 12;
        style.borderBottomLeftRadius = 12;
        style.borderBottomRightRadius = 12;

        mainContainer.style.borderTopLeftRadius = 12;
        mainContainer.style.borderTopRightRadius = 12;
        mainContainer.style.borderBottomLeftRadius = 12;
        mainContainer.style.borderBottomRightRadius = 12;

        contentContainer.style.borderBottomLeftRadius = 12;
        contentContainer.style.borderBottomRightRadius = 12;

        titleContainer.style.backgroundColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        titleContainer.style.borderTopLeftRadius = 11;
        titleContainer.style.borderTopRightRadius = 11;
        titleContainer.style.height = 50;
        
        mainContainer.style.borderTopWidth = 4;
        mainContainer.style.borderTopColor = kind switch
        {
            BtNodeKind.Leaf => new Color(0.95f, 0.35f, 0.35f),
            _ => new Color(0.75f, 0.55f, 0.95f)
        };

        inputContainer.Clear();
        outputContainer.Clear();

        if (kind == BtNodeKind.Root)
        {
            capabilities &= ~Capabilities.Deletable;
        }
        if (kind != BtNodeKind.Leaf)
        {
            OutputPort = CreateHiddenPort(Direction.Output,
                kind == BtNodeKind.Root ? Port.Capacity.Single : Port.Capacity.Multi,
                edgeConnectorListener, false);
            _outputVisual = CreatePortVisual(isInput: false);
            OutputPort.RegisterCallback<MouseEnterEvent>(_ => _outputVisual.AddToClassList("is-hover"));
            OutputPort.RegisterCallback<MouseLeaveEvent>(_ => _outputVisual.RemoveFromClassList("is-hover"));
            VisualElement outputHost = CreatePortHost(isTop: false);
            outputHost.Add(OutputPort);
            outputHost.Add(_outputVisual);
            Add(outputHost);
        }
        if (kind != BtNodeKind.Root)
        {
            InputPort = CreateHiddenPort(Direction.Input, Port.Capacity.Single, edgeConnectorListener, isTop: true);
            _inputVisual = CreatePortVisual(isInput: true);
            InputPort.RegisterCallback<MouseEnterEvent>(_ => _inputVisual.AddToClassList("is-hover"));
            InputPort.RegisterCallback<MouseLeaveEvent>(_ => _inputVisual.RemoveFromClassList("is-hover"));
            VisualElement inputHost = CreatePortHost(isTop: true);
            inputHost.Add(InputPort);
            inputHost.Add(_inputVisual);
            Add(inputHost);
        }
        
        RefreshPorts();
        RefreshExpandedState();
        RegisterCallback<DetachFromPanelEvent>(_ => DisposeNodeTrees());
    }

    public void SetDebugStatus(NodeStatus status)
    {
        switch (status)
        {
            case NodeStatus.Success:
                _currentDebugColor = new Color(0.2f, 0.85f, 0.3f); // green
                break;
            case NodeStatus.Failure:
                _currentDebugColor = new Color(1.0f, 0.1f, 0.1f); // red
                break;
            case NodeStatus.Running:
                _currentDebugColor = new Color(0.2f, 0.3f, 1.0f); // yellow
                break;
            case NodeStatus.None:
            default:
                _currentDebugColor = new Color(0.1f, 0.1f, 0.1f); // blue
                break;
        }

        if (InputPort != null) InputPort.portColor = _currentDebugColor;
        if (OutputPort != null) OutputPort.portColor = _currentDebugColor;

        InputPort?.MarkDirtyRepaint();
        OutputPort?.MarkDirtyRepaint();
        
        style.borderLeftWidth = 6;
        style.borderRightWidth = 6;
        style.borderTopWidth = 6;
        style.borderBottomWidth = 6;

        style.borderLeftColor = _currentDebugColor;
        style.borderRightColor = _currentDebugColor;
        style.borderTopColor = _currentDebugColor;
        style.borderBottomColor = _currentDebugColor;
        
        MarkDirtyRepaint();
        RefreshPorts();
        _onPortsColorChanged?.Invoke();
    }
    
    private static void ForceRepaintEdges(Port port)
    {
        if (port == null)
            return;
        foreach (var c in port.connections)
        {
            if (c is not Edge edge)
                continue;
            edge.MarkDirtyRepaint();
            edge.edgeControl?.MarkDirtyRepaint();
            edge.parent?.MarkDirtyRepaint();
        }
        port.MarkDirtyRepaint();
        port.parent?.MarkDirtyRepaint();
    }
    
    public void Bind(BtGraphAsset graphAsset, Action<string> onDataChanged, bool isReadOnly, Action onPortsColorChanged)
    {
        _isReadOnly = isReadOnly;
        _graphAsset = graphAsset;
        _onDataChanged = onDataChanged;
        _onPortsColorChanged = onPortsColorChanged;

        if (_isReadOnly)
        {
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;
            if (_inputVisual != null)
                _inputVisual.style.display = DisplayStyle.None;
            if (_outputVisual != null)
                _outputVisual.style.display = DisplayStyle.None;
        }
        
        BuildInlineInspectorUI();
        RefreshFromAsset();
    }

    public void RefreshFromAsset()
    {
        if (_graphAsset == null)
            return;
        
        int headerIndex = _graphAsset.FindHeaderIndex(Guid);
        if (headerIndex >= 0)
        {
            BtNodeHeader header = _graphAsset.Nodes[headerIndex];
            if (!string.IsNullOrEmpty(header.Title) && title != header.Title)
                SetTitle(header.Title);
        }

        if (Kind == BtNodeKind.MemorySequence)
        {
            BtMemorySequenceNodeData data = _graphAsset.FindMemorySequenceData(Guid);
            if (data != null)
            {
                _boolToggleOne?.SetValueWithoutNotify(data.ResetOnFailure);
                _boolToggleTwo?.SetValueWithoutNotify(data.ResetOnAbort);
            }
        }
        else if (Kind == BtNodeKind.MemorySelector)
        {
            BtMemorySelectorNodeData data = _graphAsset.FindMemorySelectorData(Guid);
            if (data != null)
            {
                _boolToggleOne?.SetValueWithoutNotify(data.PickRandom);
                _boolToggleTwo?.SetValueWithoutNotify(data.ResetOnAbort);
            }
        }
        else if (Kind == BtNodeKind.Parallel)
        {
            BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
            if (data != null)
            {
                _boolToggleOne?.SetValueWithoutNotify(data.CacheChildStatus);
                _integerFieldOne?.SetValueWithoutNotify(data.FailureThreshold);
                _integerFieldTwo?.SetValueWithoutNotify(data.SuccessThreshold);
            }
        }
        if (Kind == BtNodeKind.Leaf)
        {
            _leafOdinContainer?.MarkDirtyRepaint();
        }
    }

    public void SetTitle(string newTitle)
    {
        title = newTitle;
    }

    private void BuildInlineInspectorUI()
    {
        extensionContainer.Clear();
        extensionContainer.style.paddingLeft = 8;
        extensionContainer.style.paddingRight = 8;
        extensionContainer.style.paddingTop = 6;
        extensionContainer.style.paddingBottom = 6;

        bool allowEdit = !_isReadOnly;
        if (Kind == BtNodeKind.MemorySequence)
        {
            _boolToggleOne = new Toggle("Reset On Failure");
            _boolToggleTwo = new Toggle("Reset On Abort");
            _boolToggleOne.SetEnabled(allowEdit);
            _boolToggleTwo.SetEnabled(allowEdit);
            _boolToggleOne.RegisterValueChangedCallback(eventData =>
            {
                if (_graphAsset == null) return;
                BtMemorySequenceNodeData data = _graphAsset.FindMemorySequenceData(Guid);
                if (data == null) return;
                Undo.RecordObject(_graphAsset, "BT MemorySequence Change");
                data.ResetOnFailure = eventData.newValue;
                EditorUtility.SetDirty(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });
            _boolToggleTwo.RegisterValueChangedCallback(eventData =>
            {
                if (_graphAsset == null) return;
                BtMemorySequenceNodeData data = _graphAsset.FindMemorySequenceData(Guid);
                if (data == null) return;
                Undo.RecordObject(_graphAsset, "BT MemorySequence Change");
                data.ResetOnAbort = eventData.newValue;
                EditorUtility.SetDirty(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });
            extensionContainer.Add(_boolToggleOne);
            extensionContainer.Add(_boolToggleTwo);
            RefreshExpandedState();
            RefreshPorts();
            return;
        }
        if (Kind == BtNodeKind.MemorySelector)
        {
            _boolToggleOne = new Toggle("Pick Random");
            _boolToggleTwo = new Toggle("Reset On Abort");
            _boolToggleOne.SetEnabled(allowEdit);
            _boolToggleTwo.SetEnabled(allowEdit);
            _boolToggleOne.RegisterValueChangedCallback(eventData =>
            {
                if (_graphAsset == null) return;
                BtMemorySelectorNodeData data = _graphAsset.FindMemorySelectorData(Guid);
                if (data == null) return;
                Undo.RecordObject(_graphAsset, "BT MemorySelector Change");
                data.PickRandom = eventData.newValue;
                EditorUtility.SetDirty(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });

            _boolToggleTwo.RegisterValueChangedCallback(eventData =>
            {
                if (_graphAsset == null) return;
                BtMemorySelectorNodeData data = _graphAsset.FindMemorySelectorData(Guid);
                if (data == null) return;
                Undo.RecordObject(_graphAsset, "BT MemorySelector Change");
                data.ResetOnAbort = eventData.newValue;
                EditorUtility.SetDirty(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });
            extensionContainer.Add(_boolToggleOne);
            extensionContainer.Add(_boolToggleTwo);
            RefreshExpandedState();
            RefreshPorts();
            return;
        }
        if (Kind == BtNodeKind.Parallel)
        {
            _boolToggleOne = new Toggle("Cache Child Status");
            _boolToggleOne.SetEnabled(allowEdit);
            _boolToggleOne.RegisterValueChangedCallback(eventData =>
            {
                if (_graphAsset == null) return;
                BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
                if (data == null) return;
                Undo.RecordObject(_graphAsset, "BT Parallel Change");
                data.CacheChildStatus = eventData.newValue;
                EditorUtility.SetDirty(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });
            _integerFieldOne = new IntegerField("Failure Threshold");
            _integerFieldOne.SetEnabled(allowEdit);
            _integerFieldOne.RegisterValueChangedCallback(eventData =>
            {
                if (_graphAsset == null) return;
                BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
                if (data == null) return;
                Undo.RecordObject(_graphAsset, "BT Parallel Change");
                data.FailureThreshold = eventData.newValue;
                EditorUtility.SetDirty(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });
            _integerFieldTwo = new IntegerField("Success Threshold");
            _integerFieldTwo.SetEnabled(allowEdit);
            _integerFieldTwo.RegisterValueChangedCallback(eventData =>
            {
                if (_graphAsset == null) return;
                BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
                if (data == null) return;
                Undo.RecordObject(_graphAsset, "BT Parallel Change");
                data.SuccessThreshold = eventData.newValue;
                EditorUtility.SetDirty(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });
            extensionContainer.Add(_boolToggleOne);
            extensionContainer.Add(_integerFieldOne);
            extensionContainer.Add(_integerFieldTwo);
            RefreshExpandedState();
            RefreshPorts();
            return;
        }
        if (Kind == BtNodeKind.Leaf)
        {
            _leafOdinContainer = new IMGUIContainer(DrawLeafOdinInNode);
            _leafOdinContainer.style.marginTop = 2;
            _leafOdinContainer.SetEnabled(allowEdit);
            extensionContainer.Add(_leafOdinContainer);
            RefreshExpandedState();
            RefreshPorts();
            return;
        }
        if (Kind == BtNodeKind.Sequence)
        {
            RefreshExpandedState();
            RefreshPorts();
        }
        if (Kind == BtNodeKind.Selector)
        {
            RefreshExpandedState();
            RefreshPorts();
        }
    }
    
    private void EnsureLeafTree()
    {
        if (_leafNodeTree != null)
            return;
        if (_graphAsset == null)
            return;
        BtLeafNodeData leafData = _graphAsset.FindLeafData(Guid);
        if (leafData == null)
            return;
        _leafNodeTree = PropertyTree.Create(leafData);
    }

    private void DrawLeafOdinInNode()
    {
        if (Kind != BtNodeKind.Leaf || _graphAsset == null)
            return;

        bool prevGuiEnabled = GUI.enabled;
        if (_isReadOnly)
            GUI.enabled = false;
        
        EnsureLeafTree();
        if (_leafNodeTree == null)
            return;
        if (_isReadOnly)
        {
            _leafNodeTree.BeginDraw(false);
            var leafProp = _leafNodeTree.RootProperty.Children["Leaf"];
            leafProp?.Draw();
            _leafNodeTree.EndDraw();
            GUI.enabled = prevGuiEnabled;
            return;
        }
        
        Undo.RecordObject(_graphAsset, "BT Leaf Change");
        
        BtLeafNodeData leafData = _graphAsset.FindLeafData(Guid);
        string typeBefore = leafData?.Leaf == null ? null : leafData.Leaf.GetType().FullName;

        _leafNodeTree.BeginDraw(false);
        var leafProperty = _leafNodeTree.RootProperty.Children["Leaf"];
        leafProperty?.Draw();
        _leafNodeTree.EndDraw();
        
        string typeAfter = leafData?.Leaf == null ? null : leafData.Leaf.GetType().FullName;
        bool typeChanged = typeBefore != typeAfter;

        if (GUI.changed || typeChanged)
        {
            EditorUtility.SetDirty(_graphAsset);
            if (typeChanged && leafData?.Leaf != null)
                TryAutoRenameLeafNodeTitle(_graphAsset);
            _onDataChanged?.Invoke(Guid);
        }

        if (Event.current.type == EventType.Repaint)
        {
            RefreshExpandedState();
            RefreshPorts();
        }
        GUI.enabled = prevGuiEnabled;
    }

    private void TryAutoRenameLeafNodeTitle(BtGraphAsset asset)
    {
        BtLeafNodeData leafData = asset.FindLeafData(Guid);
        if (leafData == null || leafData.Leaf == null)
            return;

        string typeName = leafData.Leaf.GetType().Name;
        if (typeName.EndsWith("Leaf", StringComparison.Ordinal))
            typeName = typeName.Substring(0, typeName.Length - "Leaf".Length);

        int headerIndex = asset.FindHeaderIndex(Guid);
        if (headerIndex < 0)
            return;

        BtNodeHeader header = asset.Nodes[headerIndex];
        if (header.Title == typeName)
            return;

        header.Title = typeName;
        asset.Nodes[headerIndex] = header;

        SetTitle(typeName);
    }

    private void DisposeNodeTrees()
    {
        _leafNodeTree?.Dispose();
        _leafNodeTree = null;
    }

    private VisualElement CreatePortHost(bool isTop)
    {
        var host = new VisualElement();
        host.style.position = Position.Absolute;
        host.style.width = PortPillWidth;
        host.style.height = PortPillHeight;

        host.style.left = new Length(50, LengthUnit.Percent);
        host.style.marginLeft = -PortPillWidth * 0.5f;

        if (isTop)
            host.style.top = -PortPillHeight;
        else
            host.style.bottom = -PortPillHeight;

        host.style.justifyContent = Justify.Center;
        host.style.alignItems = Align.Center;
        host.pickingMode = PickingMode.Ignore;

        return host;
    }

    private Port CreateHiddenPort(Direction direction, Port.Capacity capacity, IEdgeConnectorListener edgeConnectorListener, bool isTop)
    {
        var port = InstantiatePort(Orientation.Vertical, direction, capacity, typeof(bool));

        port.AddToClassList("bt-port");
        port.AddToClassList(direction == Direction.Input ? "bt-port-input" : "bt-port-output");

        port.portName = "";
        port.style.width = PortPillWidth;
        port.style.height = PortPillHeight;

        port.style.position = Position.Absolute;
        if (isTop)
            port.style.right = -4;
        else
            port.style.left = -4;
        
        port.style.top = 0;
        port.style.opacity = 0;
        port.pickingMode = PickingMode.Position;
        
        port.portColor = new Color(0.10f, 0.70f, 1.00f, 1f);
        //port.RegisterCallback<AttachToPanelEvent>(_ => port.MarkDirtyRepaint());
        
        port.AddManipulator(new EdgeConnector<Edge>(edgeConnectorListener));
        return port;
    }

    private VisualElement CreatePortVisual(bool isInput)
    {
        var pill = new VisualElement();
        pill.AddToClassList("bt-port-pill");
        pill.AddToClassList(isInput ? "bt-port-in" : "bt-port-out");

        pill.pickingMode = PickingMode.Ignore;
        pill.style.position = Position.Absolute;
        pill.style.left = 0;
        pill.style.top = 0;
        pill.style.width = PortPillWidth;
        pill.style.height = PortPillHeight;

        var dot = new VisualElement();
        dot.AddToClassList("bt-port-dot");
        dot.pickingMode = PickingMode.Ignore;
        pill.Add(dot);

        return pill;
    }
}
#endif