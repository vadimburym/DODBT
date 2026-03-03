// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VadimBurym.DodBehaviourTree;

internal sealed class BtGraphView : GraphView
{
    public BtGraphAsset GraphAsset { get; private set; }
    public event Action<BtNodeView> NodeSelected;
    public event Action<string, LogLevel, bool> LogRequested;
    public event Action<string> NodeDataChanged;
    public event Action GraphStructureChanged;
    public bool IsReadOnly { get; private set; }
    
    private IVisualElementScheduledItem _edgeColorUpdateItem;
    private bool _edgeColorsDirty;
    private readonly IEdgeConnectorListener _edgeConnectorListener;
    private readonly Dictionary<string, BtNodeView> _nodeViewsByGuid = new();

    private readonly HashSet<string> _visited = new();
    private bool _isBinding;
    private bool _suppressBindLog;
    
    internal BtGraphView(bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
        style.flexGrow = 1;

        _edgeConnectorListener = new BtEdgeConnectorListener(this);
        _edgeColorUpdateItem = schedule.Execute(UpdateEdgeColorsIfDirty).Every(16);
        
        var path = BtEditorPaths.GetEditorWindowFolderPath();
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            path + "/BtGraphView.uss");
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        AddToClassList("bt-graph-view");

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        
        var grid = new GridBackground();
        grid.pickingMode = PickingMode.Ignore;
        Insert(0, grid);
        grid.StretchToParentSize();
        grid.SendToBack();

        var minimap = new MiniMap { anchored = true };
        minimap.SetPosition(new Rect(0, 40, 210, 210));
        minimap.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        minimap.style.borderLeftWidth = 1;
        minimap.style.borderRightWidth = 1;
        minimap.style.borderTopWidth = 1;
        minimap.style.borderBottomWidth = 1;
        minimap.style.borderLeftColor = new Color(1f, 1f, 1f, 0.12f);
        minimap.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);
        minimap.style.borderTopColor = new Color(1f, 1f, 1f, 0.12f);
        minimap.style.borderBottomColor = new Color(1f, 1f, 1f, 0.12f);
        minimap.style.overflow = Overflow.Hidden;
        Add(minimap);

        this.AddManipulator(new ContentDragger());
        if (!isReadOnly)
        {
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContextualMenuManipulator(menuEvent =>
            {
                Vector2 mousePosition = contentViewContainer.WorldToLocal(menuEvent.localMousePosition);

                menuEvent.menu.AppendAction("Add/Selector", _ => AddNode(BtNodeKind.Selector, mousePosition));
                menuEvent.menu.AppendAction("Add/Sequence", _ => AddNode(BtNodeKind.Sequence, mousePosition));
                menuEvent.menu.AppendAction("Add/Memory Selector", _ => AddNode(BtNodeKind.MemorySelector, mousePosition));
                menuEvent.menu.AppendAction("Add/Memory Sequence", _ => AddNode(BtNodeKind.MemorySequence, mousePosition));
                menuEvent.menu.AppendAction("Add/Parallel", _ => AddNode(BtNodeKind.Parallel, mousePosition));
                menuEvent.menu.AppendAction("Add/Leaf", _ => AddNode(BtNodeKind.Leaf, mousePosition));
            }));
        }
        
        graphViewChanged = OnGraphViewChanged;
    }
    
    internal void SetDebugStatus(NodeStatus[] debugStatus)
    {
        if (GraphAsset == null)
            return;
        var guidsByCompiled = GraphAsset.GuidsByCompiledId;
        for (int index = 0; index < guidsByCompiled.Length; index++)
        {
            var status = debugStatus[index];
            var guid = guidsByCompiled[index];
            var view = _nodeViewsByGuid[guid];
            view.SetDebugStatus(status);
        }
    }

    private void UpdateEdgeColorsIfDirty()
    {
        if (!_edgeColorsDirty)
            return;
        _edgeColorsDirty = false;
        
        foreach (var edge in edges)
        {
            if (edge == null)
                continue;

            var ec = edge.edgeControl;
            if (ec == null)
                continue;

            var inPort = edge.input;
            var outPort = edge.output;
            if (inPort == null || outPort == null)
                continue;
            
            ec.inputColor = inPort.portColor;
            ec.outputColor = outPort.portColor;
            ec.MarkDirtyRepaint();
        }
        
        MarkDirtyRepaint();
    }
    
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        if (IsReadOnly)
            return new List<Port>();
        return ports.ToList()
            .Where(port =>
                port != startPort &&
                port.node != startPort.node &&
                port.direction != startPort.direction &&
                port.portType == startPort.portType)
            .ToList();
    }

    public override void AddToSelection(ISelectable selectable)
    {
        base.AddToSelection(selectable);
        NotifySelection();
    }

    public override void RemoveFromSelection(ISelectable selectable)
    {
        base.RemoveFromSelection(selectable);
        NotifySelection();
    }

    public override void ClearSelection()
    {
        base.ClearSelection();
        NotifySelection();
    }

    private void NotifySelection()
    {
        var picked = selection?.OfType<BtNodeView>().FirstOrDefault();
        NodeSelected?.Invoke(picked);
    }

    public void MarkEdgeColorsDirty()
    {
        _edgeColorsDirty = true;
    }
    
    public void Bind(BtGraphAsset graphAsset)
    {
        _isBinding = true;
        try
        {
            GraphAsset = graphAsset;
            _nodeViewsByGuid.Clear();
            DeleteElements(graphElements.ToList());
            if (GraphAsset == null)
                return;
            if (GraphAsset.RootNode != null && !string.IsNullOrEmpty(GraphAsset.RootNode.Guid))
            {
                var rootView = new BtNodeView(
                    GraphAsset.RootNode.Guid,
                    BtNodeKind.Root,
                    "Root",
                    _edgeConnectorListener);

                rootView.SetPosition(new Rect(GraphAsset.RootNode.Position, BtNodeView.DefaultSize));
                AddElement(rootView);

                _nodeViewsByGuid[GraphAsset.RootNode.Guid] = rootView;
                rootView.Bind(GraphAsset, NotifyNodeDataChanged, IsReadOnly, MarkEdgeColorsDirty);
            }
            else
            {
                var guid = System.Guid.NewGuid().ToString("N");
                var rootView = new BtNodeView(
                    guid,
                    BtNodeKind.Root,
                    "Root",
                    _edgeConnectorListener);
                rootView.SetPosition(new Rect(Vector2.zero, BtNodeView.DefaultSize));
                AddElement(rootView);
                _nodeViewsByGuid[guid] = rootView;
                GraphAsset.RootNode = new() { Guid =  guid, Position = Vector2.zero };
                rootView.Bind(GraphAsset, NotifyNodeDataChanged, IsReadOnly, MarkEdgeColorsDirty);
                LogRequested?.Invoke($"Graph does not have a Root node! Root node was created at (0,0)", LogLevel.Warning, false);
            }

            foreach (var header in GraphAsset.Nodes)
            {
                var nodeView = new BtNodeView(header.Guid, header.Kind, header.Title, _edgeConnectorListener);
                nodeView.SetPosition(new Rect(header.Position, BtNodeView.DefaultSize));
                AddElement(nodeView);

                _nodeViewsByGuid[header.Guid] = nodeView;
                nodeView.Bind(GraphAsset, NotifyNodeDataChanged, IsReadOnly, MarkEdgeColorsDirty);
            }

            CreateEdgesFromAsset();

            if (!_suppressBindLog)
            {
                string assetName = GraphAsset != null ? GraphAsset.name : "NULL";
                LogRequested?.Invoke($"GraphAsset Loaded: {assetName}", LogLevel.Info, true);
            }
        }
        finally
        {
            _isBinding = false;
        }
    }
    
    public void ReloadWithoutLog()
    {
        if (GraphAsset == null)
            return;

        _suppressBindLog = true;
        try
        {
            Bind(GraphAsset);
            GraphStructureChanged?.Invoke();
        }
        finally
        {
            _suppressBindLog = false;
        }
    }

    public void NotifyNodeDataChanged(string guid)
    {
        if (GraphAsset != null)
        {
            GraphAsset.MarkModified();
            //EditorUtility.SetDirty(GraphAsset);
        }

        if (_nodeViewsByGuid.TryGetValue(guid, out var nodeView))
        {
            nodeView.RefreshFromAsset();
        }

        NodeDataChanged?.Invoke(guid);
    }

    public void AddNode(BtNodeKind kind, Vector2 graphPosition)
    {
        if (GraphAsset == null)
        {
            LogRequested?.Invoke($"GraphAsset is null. Open or create new!", LogLevel.Error, false);
            return;
        }

        string guid = System.Guid.NewGuid().ToString("N");

        var header = new BtNodeHeader
        {
            Guid = guid,
            Kind = kind,
            Position = graphPosition,
            Title = kind.ToString(),
            ParentGuid = null
        };

        GraphAsset.Nodes.Add(header);
        AllocateTypedData(guid, kind);
        GraphAsset.ReloadNodeCounts();

        GraphAsset.MarkModified();
        //EditorUtility.SetDirty(GraphAsset);

        var nodeView = new BtNodeView(guid, kind, header.Title, _edgeConnectorListener);
        nodeView.SetPosition(new Rect(graphPosition, BtNodeView.DefaultSize));
        AddElement(nodeView);

        _nodeViewsByGuid[guid] = nodeView;
        nodeView.Bind(GraphAsset, NotifyNodeDataChanged, IsReadOnly, MarkEdgeColorsDirty);
        GraphStructureChanged?.Invoke();
    }

    private void AllocateTypedData(string guid, BtNodeKind kind)
    {
        switch (kind)
        {
            case BtNodeKind.Leaf:
                GraphAsset.LeafNodes.Add(new BtLeafNodeData { Guid = guid, Leaf = null });
                break;
            case BtNodeKind.Sequence:
                GraphAsset.SequenceNodes.Add(new BtSequenceNodeData { Guid = guid });
                break;
            case BtNodeKind.MemorySequence:
                GraphAsset.MemorySequenceNodes.Add(new BtMemorySequenceNodeData { Guid = guid });
                break;
            case BtNodeKind.Selector:
                GraphAsset.SelectorNodes.Add(new BtSelectorNodeData { Guid = guid });
                break;
            case BtNodeKind.MemorySelector:
                GraphAsset.MemorySelectorNodes.Add(new BtMemorySelectorNodeData { Guid = guid });
                break;
            case BtNodeKind.Parallel:
                GraphAsset.ParallelNodes.Add(new BtParallelNodeData { Guid = guid });
                break;
        }
    }

    private static bool IsComposite(BtNodeKind kind)
    {
        return kind != BtNodeKind.Leaf && kind != BtNodeKind.Root;
    }

    public void OnEdgeCreated(Edge edge)
    {
        if (GraphAsset == null)
            return;
        if (IsReadOnly)
            return;
        
        if (edge.output?.node is not BtNodeView parentNodeView)
            return;

        if (edge.input?.node is not BtNodeView childNodeView)
            return;
        
        if (parentNodeView.Kind == BtNodeKind.Root)
        {
            GraphAsset.RootNode.ChildrenGuid = childNodeView.Guid;
            GraphAsset.MarkModified();
            //EditorUtility.SetDirty(GraphAsset);
            int child = GraphAsset.FindHeaderIndex(childNodeView.Guid);
            if (child < 0) return;
            BtNodeHeader childHead = GraphAsset.Nodes[child];
            childHead.ParentGuid = GraphAsset.RootNode.Guid;
            return;
        }

        int parentHeaderIndex = GraphAsset.FindHeaderIndex(parentNodeView.Guid);
        int childHeaderIndex = GraphAsset.FindHeaderIndex(childNodeView.Guid);
        if (parentHeaderIndex < 0 || childHeaderIndex < 0)
            return;
        
        BtNodeHeader parentHeader = GraphAsset.Nodes[parentHeaderIndex];
        BtNodeHeader childHeader = GraphAsset.Nodes[childHeaderIndex];
        
        if (!IsComposite(parentHeader.Kind))
            return;

        List<string> parentChildren = GraphAsset.GetChildrenList(parentHeader.Guid, parentHeader.Kind);
        if (parentChildren == null)
            return;
        
        if (!string.IsNullOrEmpty(childHeader.ParentGuid) && childHeader.ParentGuid != parentHeader.Guid)
        {
            RemoveChildFromOldParent(childHeader.Guid, childHeader.ParentGuid);
            childHeader.ParentGuid = null;
        }

        bool isNewChild = false;
        if (!parentChildren.Contains(childHeader.Guid))
        {
            parentChildren.Add(childHeader.Guid);
            isNewChild = true;
        }

        if (childHeader.ParentGuid != parentHeader.Guid)
        {
            childHeader.ParentGuid = parentHeader.Guid;
            GraphAsset.Nodes[childHeaderIndex] = childHeader;
        }

        if (isNewChild)
        {
            //LogRequested?.Invoke($"");
        }

        bool orderChanged = SortChildrenByXAndLogIfChanged(parentHeader, parentChildren);
        if (orderChanged || isNewChild)
        {
            GraphAsset.MarkModified();
            //EditorUtility.SetDirty(GraphAsset);
            NotifyNodeDataChanged(parentHeader.Guid);
        }
    }

    private void RemoveChildFromOldParent(string childGuid, string oldParentGuid)
    {
        if (string.IsNullOrEmpty(oldParentGuid))
            return;

        int oldParentIndex = GraphAsset.FindHeaderIndex(oldParentGuid);
        if (oldParentIndex < 0)
            return;

        BtNodeHeader oldParentHeader = GraphAsset.Nodes[oldParentIndex];
        if (!IsComposite(oldParentHeader.Kind))
            return;

        List<string> oldParentChildren = GraphAsset.GetChildrenList(oldParentHeader.Guid, oldParentHeader.Kind);
        if (oldParentChildren == null)
            return;

        oldParentChildren.Remove(childGuid);
        SortChildrenByXAndLogIfChanged(oldParentHeader, oldParentChildren);
    }

    private bool SortChildrenByXAndLogIfChanged(BtNodeHeader parentHeader, List<string> childrenGuids)
    {
        if (childrenGuids == null || childrenGuids.Count <= 1)
            return false;

        var before = childrenGuids.ToArray();

        childrenGuids.Sort((leftGuid, rightGuid) =>
        {
            BtNodeHeader leftHeader = GraphAsset.FindHeader(leftGuid);
            BtNodeHeader rightHeader = GraphAsset.FindHeader(rightGuid);

            if (leftHeader == null && rightHeader == null) return 0;
            if (leftHeader == null) return 1;
            if (rightHeader == null) return -1;

            BtNodeHeader left = leftHeader;
            BtNodeHeader right = rightHeader;

            int xCompare = left.Position.x.CompareTo(right.Position.x);
            if (xCompare != 0) return xCompare;

            int yCompare = left.Position.y.CompareTo(right.Position.y);
            if (yCompare != 0) return yCompare;

            return string.CompareOrdinal(left.Guid, right.Guid);
        });

        bool changed = false;
        for (int i = 0; i < before.Length; i++)
        {
            if (before[i] != childrenGuids[i])
            {
                changed = true;
                break;
            }
        }

        if (!changed)
            return false;

        var builder = new System.Text.StringBuilder();
        builder.Append($"The order of children in the {SafeTitle(parentHeader)} node has changed: ");

        for (int i = 0; i < childrenGuids.Count; i++)
        {
            BtNodeHeader childHeader = GraphAsset.FindHeader(childrenGuids[i]);
            string childTitle = childHeader == null ? childrenGuids[i] : SafeTitle(childHeader);

            builder.Append(i);
            builder.Append(": ");
            builder.Append(childTitle);

            if (i + 1 < childrenGuids.Count)
                builder.Append(", ");
        }

        LogRequested?.Invoke(builder.ToString(), LogLevel.Info, false);
        return true;
    }

    private static string SafeTitle(BtNodeHeader header)
    {
        return string.IsNullOrEmpty(header.Title) ? header.Guid : header.Title;
    }

    private void CreateEdgesFromAsset()
    {
        if (GraphAsset == null)
            return;
        
        var views = graphElements
            .OfType<BtNodeView>()
            .ToDictionary(view => view.Guid, view => view);

        if (GraphAsset.RootNode != null && !string.IsNullOrEmpty(GraphAsset.RootNode.ChildrenGuid))
        {
            if (views.TryGetValue(GraphAsset.RootNode.Guid, out var rootView) &&
                views.TryGetValue(GraphAsset.RootNode.ChildrenGuid, out var childView))
            {
                if (rootView.OutputPort != null && childView.InputPort != null)
                {
                    var edge = new Edge { output = rootView.OutputPort, input = childView.InputPort };
                    edge.output.Connect(edge);
                    edge.input.Connect(edge);
                    AddElement(edge);
                }
            }
        }
        
        foreach (var parentHeader in GraphAsset.Nodes)
        {
            if (!IsComposite(parentHeader.Kind))
                continue;

            if (!views.TryGetValue(parentHeader.Guid, out var parentView))
                continue;

            if (parentView.OutputPort == null)
                continue;

            List<string> children = GraphAsset.GetChildrenList(parentHeader.Guid, parentHeader.Kind);
            if (children == null)
                continue;

            for (int i = 0; i < children.Count; i++)
            {
                if (!views.TryGetValue(children[i], out var childView))
                    continue;

                if (childView.InputPort == null)
                    continue;

                bool alreadyConnected = parentView.OutputPort.connections.Any(edge => edge.input?.node == childView);
                if (alreadyConnected)
                    continue;

                var edge = new Edge
                {
                    output = parentView.OutputPort,
                    input = childView.InputPort
                };

                edge.output.Connect(edge);
                edge.input.Connect(edge);
                AddElement(edge);
            }
        }
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (_isBinding)
            return change;
        if (GraphAsset == null)
            return change;
        if (IsReadOnly)
        {
            change.elementsToRemove?.Clear();
            change.movedElements?.Clear();
            change.edgesToCreate?.Clear();
            return change;
        }
        
        bool nodeWasDeleted = false;
        bool isAssetDirty = false;
        bool isAssetMovedDirty = false;

        if (change.elementsToRemove != null && GraphAsset.RootNode != null)
        {
            string rootGuid = GraphAsset.RootNode.Guid;
            change.elementsToRemove.RemoveAll(e => e is BtNodeView node && node.Guid == rootGuid);
        }
        
        if (change.elementsToRemove != null)
        {
            var guidsToDelete = change.elementsToRemove
                .OfType<BtNodeView>()
                .Select(nodeView => nodeView.Guid)
                .ToHashSet();

            if (guidsToDelete.Count > 0)
            {
                nodeWasDeleted = true;
                GraphAsset.Nodes.RemoveAll(header => guidsToDelete.Contains(header.Guid));
                foreach (string guid in guidsToDelete)
                {
                    if (GraphAsset.RootNode != null && GraphAsset.RootNode.ChildrenGuid == guid)
                        GraphAsset.RootNode.ChildrenGuid = null;
                    GraphAsset.RemoveTypedDataByGuid(guid);
                }
                
                foreach (var remainingHeader in GraphAsset.Nodes.ToArray())
                {
                    int headerIndex = GraphAsset.FindHeaderIndex(remainingHeader.Guid);
                    if (headerIndex < 0) continue;

                    BtNodeHeader fixedHeader = GraphAsset.Nodes[headerIndex];
                    
                    if (!string.IsNullOrEmpty(fixedHeader.ParentGuid) && guidsToDelete.Contains(fixedHeader.ParentGuid))
                    {
                        fixedHeader.ParentGuid = null;
                        GraphAsset.Nodes[headerIndex] = fixedHeader;
                        isAssetDirty = true;
                    }
                    
                    if (IsComposite(fixedHeader.Kind))
                    {
                        List<string> children = GraphAsset.GetChildrenList(fixedHeader.Guid, fixedHeader.Kind);
                        if (children != null)
                        {
                            int beforeCount = children.Count;
                            children.RemoveAll(childGuid => guidsToDelete.Contains(childGuid));
                            if (children.Count != beforeCount)
                                isAssetDirty = true;

                            SortChildrenByXAndLogIfChanged(fixedHeader, children);
                        }
                    }
                }

                foreach (string guid in guidsToDelete)
                    _nodeViewsByGuid.Remove(guid);

                isAssetDirty = true;
            }
        }
        
        GraphStructureChanged?.Invoke();
        if (change.elementsToRemove != null)
        {
            bool areNodesBeingDeleted = change.elementsToRemove.OfType<BtNodeView>().Any();
            if (!areNodesBeingDeleted)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is not Edge removedEdge)
                        continue;

                    if (removedEdge.output?.node is not BtNodeView parentNodeView)
                        continue;

                    if (removedEdge.input?.node is not BtNodeView childNodeView)
                        continue;
                    
                    if (GraphAsset.RootNode != null &&
                        parentNodeView.Guid == GraphAsset.RootNode.Guid)
                    {
                        GraphAsset.RootNode.ChildrenGuid = null;
                        int child = GraphAsset.FindHeaderIndex(childNodeView.Guid);
                        if (child < 0) continue;
                        BtNodeHeader childHead = GraphAsset.Nodes[child];
                        childHead.ParentGuid = null;
                        isAssetDirty = true;
                    }
                    
                    int parentIndex = GraphAsset.FindHeaderIndex(parentNodeView.Guid);
                    int childIndex = GraphAsset.FindHeaderIndex(childNodeView.Guid);
                    if (parentIndex < 0 || childIndex < 0)
                        continue;

                    BtNodeHeader parentHeader = GraphAsset.Nodes[parentIndex];
                    BtNodeHeader childHeader = GraphAsset.Nodes[childIndex];
                    
                    
                    if (!IsComposite(parentHeader.Kind))
                        continue;

                    List<string> children = GraphAsset.GetChildrenList(parentHeader.Guid, parentHeader.Kind);
                    if (children == null)
                        continue;

                    bool removed = children.Remove(childHeader.Guid);
                    if (removed)
                    {
                        if (childHeader.ParentGuid == parentHeader.Guid)
                        {
                            childHeader.ParentGuid = null;
                            GraphAsset.Nodes[childIndex] = childHeader;
                        }

                        SortChildrenByXAndLogIfChanged(parentHeader, children);
                        NotifyNodeDataChanged(parentHeader.Guid);

                        isAssetDirty = true;
                    }
                }
            }
        }
        if (change.movedElements != null)
        {
            foreach (var element in change.movedElements)
            {
                if (element is not BtNodeView nodeView)
                    continue;
                
                if (nodeView.Kind == BtNodeKind.Root)
                {
                    GraphAsset.RootNode.Position = nodeView.GetPosition().position;
                    isAssetMovedDirty = true;
                    continue;
                }
                
                int headerIndex = GraphAsset.FindHeaderIndex(nodeView.Guid);
                if (headerIndex < 0)
                    continue;

                BtNodeHeader header = GraphAsset.Nodes[headerIndex];

                float oldX = header.Position.x;
                Vector2 newPosition = nodeView.GetPosition().position;

                if (header.Position != newPosition)
                {
                    header.Position = newPosition;
                    GraphAsset.Nodes[headerIndex] = header;
                    isAssetMovedDirty = true;
                }

                if (!Mathf.Approximately(oldX, newPosition.x) && !string.IsNullOrEmpty(header.ParentGuid))
                {
                    int parentIndex = GraphAsset.FindHeaderIndex(header.ParentGuid);
                    if (parentIndex >= 0)
                    {
                        BtNodeHeader parentHeader = GraphAsset.Nodes[parentIndex];
                        if (IsComposite(parentHeader.Kind))
                        {
                            List<string> children = GraphAsset.GetChildrenList(parentHeader.Guid, parentHeader.Kind);
                            if (children != null)
                            {
                                bool orderChanged = SortChildrenByXAndLogIfChanged(parentHeader, children);
                                if (orderChanged)
                                {
                                    isAssetDirty = true;
                                    NotifyNodeDataChanged(parentHeader.Guid);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (isAssetMovedDirty && !isAssetDirty)
            EditorUtility.SetDirty(GraphAsset);
        
        if (isAssetDirty)
            GraphAsset.MarkModified();
            //EditorUtility.SetDirty(GraphAsset);

        if (nodeWasDeleted)
        {
            schedule.Execute(() =>
            {
                ReloadWithoutLog();
            });
        }
        
        return change;
    }
    
    public bool WouldCreateCycle(BtNodeView parentNode, BtNodeView childNode)
    {
        if (GraphAsset == null)
            return false;

        var parentGuid = parentNode.Guid;
        var childGuid = childNode.Guid;
        _visited.Clear();
        var hasPath = HasPath(childGuid, parentGuid, _visited);
        if (hasPath)
        {
            LogRequested?.Invoke(
                $"Connection blocked: would create cyclic dependency ({parentNode.title} → {childNode.title})",
                LogLevel.Warning,
                false);
        }
        return hasPath;
    }

    private bool HasPath(string currentGuid, string targetGuid, HashSet<string> visited)
    {
        if (currentGuid == targetGuid)
            return true;

        if (!visited.Add(currentGuid))
            return false;

        var node = GraphAsset.Nodes.FirstOrDefault(n => n.Guid == currentGuid);
        if (node == null)
            return false;

        if (!IsComposite(node.Kind))
            return false;

        var children = GraphAsset.GetChildrenList(node.Guid, node.Kind);
        if (children == null)
            return false;
        foreach (var child in children)
        {
            if (HasPath(child, targetGuid, visited))
                return true;
        }

        return false;
    }
    
    public void ResetViewToOrigin()
    {
        float width = layout.width;
        float height = layout.height;
        if (width <= 0 || height <= 0)
        {
            schedule.Execute(ResetViewToOrigin);
            return;
        }
        Vector3 position = new Vector3(width * 0.5f, height * 0.5f, 0f);
        UpdateViewTransform(position, Vector3.one);
    }
}
#endif