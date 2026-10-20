// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal sealed partial class BtGraphView
    {
        internal event Action GroupsChanged;
        private readonly HashSet<BtGroupInstance> _pendingGroups = new();
        private int _groupEditDepth;
        private bool _groupReloadScheduled;
        private string _hoverGroup;
        private string _pinnedGroup;
        private readonly HashSet<string> _highlightedGroups = new();
        private static readonly Color GroupHighlight = new(1f, .8f, .15f);

        internal bool CanConnectGroups(string parent, string child)
        {
            string error;
            try { error = BtLinkedGroups.ConnectionError(GraphAsset, parent, child); }
            catch (InvalidOperationException exception) { error = exception.Message; }
            if (error == null) return true;
            WarnGraph(error);
            return false;
        }

        internal void WarnGraph(string message)
        {
            LogRequested?.Invoke(message, LogLevel.Warning, false);
        }

        internal void BeginGroupEdit() => _groupEditDepth++;

        internal void EndGroupEdit()
        {
            if (--_groupEditDepth > 0) return;
            _groupEditDepth = 0;
            bool changed = _pendingGroups.Count > 0;
            foreach (var instance in _pendingGroups.ToArray()) BtLinkedGroups.Synchronize(GraphAsset, instance);
            _pendingGroups.Clear();
            if (changed) ScheduleGroupReload();
        }

        private void QueueGroupSynchronization(string guid)
        {
            if (IsReadOnly || _isBinding || GraphAsset == null) return;
            var instance = BtLinkedGroups.Find(GraphAsset, guid);
            if (instance == null) return;
            if (_groupEditDepth > 0) { _pendingGroups.Add(instance); return; }
            Undo.RecordObject(GraphAsset, "BT Edit Linked Group");
            BtLinkedGroups.Synchronize(GraphAsset, instance);
            // Preserve active IMGUI controls while refreshing the other instances' values.
            foreach (var view in _nodeViewsByGuid.Values) view.RefreshFromAsset();
            GroupsChanged?.Invoke();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isBinding || IsReadOnly || GraphAsset == null) return OnGraphViewChangedCore(change);
            BeginGroupEdit();
            var moved = change.movedElements;
            if (moved != null)
            {
                EndGroupDragPreview(restore: false);
                _applyingGroupPositions = true;
            }
            try
            {
                if (change.elementsToRemove != null)
                {
                    var ids = change.elementsToRemove.OfType<BtNodeView>().Select(n => n.Guid).ToHashSet();
                    // Combine member deletions from different instances before synchronizing any of them.
                    // Otherwise the first instance's snapshot can resurrect a node deleted in another.
                    var selectedIds = ids.ToHashSet();
                    foreach (var group in GraphAsset.LinkedGroups)
                    {
                        var deletedKeys = group.Instances.Where(i => !selectedIds.Contains(i.RootGuid))
                            .SelectMany(i => i.Members).Where(m => selectedIds.Contains(m.Guid)).Select(m => m.Key).ToHashSet();
                        foreach (var instance in group.Instances)
                            ids.UnionWith(instance.Members.Where(m => deletedKeys.Contains(m.Key)).Select(m => m.Guid));
                    }
                    foreach (var group in GraphAsset.LinkedGroups)
                    foreach (var instance in group.Instances)
                        if (ids.Contains(instance.RootGuid)) ids.UnionWith(instance.Members.Select(m => m.Guid));
                    // Direct multi-edge removal combines edits across instances as well. A reconnect
                    // has an outer transaction and must finish attaching its new edge before syncing.
                    if (_groupEditDepth == 1)
                    {
                        var edgeMap = edges.ToList().Where(e => e.output?.node is BtNodeView && e.input?.node is BtNodeView)
                            .ToLookup(e => (((BtNodeView)e.output.node).Guid, ((BtNodeView)e.input.node).Guid));
                        foreach (var edge in change.elementsToRemove.OfType<Edge>().ToArray())
                        {
                            if (edge.output?.node is not BtNodeView parent || edge.input?.node is not BtNodeView child) continue;
                            var source = BtLinkedGroups.Find(GraphAsset, parent.Guid);
                            if (source == null || ids.Contains(source.RootGuid) || BtLinkedGroups.Find(GraphAsset, child.Guid) != source) continue;
                            string parentKey = source.Members.Single(m => m.Guid == parent.Guid).Key;
                            string childKey = source.Members.Single(m => m.Guid == child.Guid).Key;
                            foreach (var target in BtLinkedGroups.Family(GraphAsset, source).Instances)
                            {
                                string parentId = target.Members.Single(m => m.Key == parentKey).Guid;
                                string childId = target.Members.Single(m => m.Key == childKey).Guid;
                                foreach (var peer in edgeMap[(parentId, childId)])
                                    if (!change.elementsToRemove.Contains(peer)) change.elementsToRemove.Add(peer);
                            }
                        }
                    }
                    foreach (string id in ids)
                        if (_nodeViewsByGuid.TryGetValue(id, out var view) && !change.elementsToRemove.Contains(view)) change.elementsToRemove.Add(view);
                    foreach (var edge in edges.ToList())
                        if ((edge.input?.node is BtNodeView child && ids.Contains(child.Guid)) ||
                            (edge.output?.node is BtNodeView parent && ids.Contains(parent.Guid)))
                            if (!change.elementsToRemove.Contains(edge)) change.elementsToRemove.Add(edge);
                    foreach (var element in change.elementsToRemove)
                    {
                        string id = element is BtNodeView node ? node.Guid : (element as Edge)?.output?.node is BtNodeView parent ? parent.Guid : null;
                        var instance = BtLinkedGroups.Find(GraphAsset, id);
                        if (instance != null) _pendingGroups.Add(instance);
                    }
                }
                // Movement is handled as a single transaction, including translated descendants.
                change.movedElements = null;
                change = OnGraphViewChangedCore(change);
                if (moved != null)
                {
                    var positions = moved.OfType<BtNodeView>().Where(n => n.Kind != BtNodeKind.Root)
                        .ToDictionary(n => n.Guid, n => n.GetPosition().position);
                    foreach (var root in moved.OfType<BtNodeView>().Where(n => n.Kind == BtNodeKind.Root))
                        GraphAsset.RootNode.Position = root.GetPosition().position;
                    BtLinkedGroups.Move(GraphAsset, positions);
                    GraphAsset.MarkModified();
                    foreach (var node in GraphAsset.Nodes)
                        if (_nodeViewsByGuid.TryGetValue(node.Guid, out var view)) view.SetPosition(new Rect(node.Position, view.GetPosition().size));
                    GraphStructureChanged?.Invoke();
                }
            }
            finally
            {
                if (moved != null) _applyingGroupPositions = false;
                change.movedElements = moved;
                EndGroupEdit();
            }
            return change;
        }

        private void ScheduleGroupReload()
        {
            if (_groupReloadScheduled) return;
            _groupReloadScheduled = true;
            schedule.Execute(() =>
            {
                _groupReloadScheduled = false;
                var selected = selection.OfType<BtNodeView>().Select(n => n.Guid).ToArray();
                ReloadWithoutLog();
                foreach (string id in selected)
                    if (_nodeViewsByGuid.TryGetValue(id, out var view)) AddToSelection(view);
            });
        }

        internal void RunGroupCommand(string guid, string command)
        {
            if (IsReadOnly || GraphAsset == null) return;
            try
            {
                Undo.IncrementCurrentGroup();
                var instance = BtLinkedGroups.Find(GraphAsset, guid);
                if (command == "create") BtLinkedGroups.Create(GraphAsset, guid);
                else
                {
                    if (instance == null || instance.RootGuid != guid) throw new InvalidOperationException("Select the root of a linked group.");
                    if (command == "unlink") BtLinkedGroups.Unlink(GraphAsset, instance);
                    else if (command == "duplicate")
                    {
                        var members = instance.Members.Select(m => GraphAsset.FindHeader(m.Guid));
                        float width = members.Max(n => n.Position.x) - members.Min(n => n.Position.x) + BtNodeView.DefaultSize.x;
                        var copy = BtLinkedGroups.Duplicate(GraphAsset, instance, new Vector2(width + 80, 0));
                        guid = copy.RootGuid;
                    }
                }
                ReloadWithoutLog();
                ClearSelection();
                if (_nodeViewsByGuid.TryGetValue(guid, out var root)) AddToSelection(root);
            }
            catch (InvalidOperationException exception) { WarnGraph(exception.Message); }
        }

        private void ConfigureGroupViews()
        {
            _hoverGroup = null;
            if (GraphAsset == null) { GroupsChanged?.Invoke(); return; }
            foreach (var pair in _nodeViewsByGuid)
            {
                ConfigureGroupView(pair.Key, pair.Value);
            }
            RefreshGroupHighlights();
            GroupsChanged?.Invoke();
        }

        private void ConfigureGroupView(string id, BtNodeView view)
        {
            var instance = BtLinkedGroups.Find(GraphAsset, id);
            var family = instance == null ? null : BtLinkedGroups.Family(GraphAsset, instance);
            view.SetGroupRoot(instance?.RootGuid == id, family?.Name);
            if (instance?.RootGuid == id) view.PositionChanged += PreviewGroupRootPosition;
            view.RegisterCallback<MouseEnterEvent>(_ => { _hoverGroup = family?.Id; RefreshGroupHighlights(); });
            view.RegisterCallback<MouseLeaveEvent>(_ => { _hoverGroup = null; RefreshGroupHighlights(); });
            if (!IsReadOnly && view.Kind != BtNodeKind.Root)
                view.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendSeparator();
                    var current = BtLinkedGroups.Find(GraphAsset, id);
                    if (current == null) evt.menu.AppendAction("Create Linked Group", _ => RunGroupCommand(id, "create"));
                    else if (current.RootGuid == id)
                    {
                        evt.menu.AppendAction("Duplicate Group", _ => RunGroupCommand(id, "duplicate"));
                        evt.menu.AppendAction("Unlink Group", _ => RunGroupCommand(id, "unlink"));
                    }
                }));
        }

        internal void PingGroup(string id)
        {
            _pinnedGroup = id;
            RefreshGroupHighlights();
        }

        private void RefreshGroupHighlights()
        {
            _highlightedGroups.Clear();
            if (GraphAsset == null) return;
            if (_hoverGroup != null) _highlightedGroups.Add(_hoverGroup);
            if (_pinnedGroup != null) _highlightedGroups.Add(_pinnedGroup);
            foreach (var node in selection.OfType<BtNodeView>())
            {
                var instance = BtLinkedGroups.Find(GraphAsset, node.Guid);
                if (instance != null) _highlightedGroups.Add(BtLinkedGroups.Family(GraphAsset, instance).Id);
            }
            foreach (var pair in _nodeViewsByGuid)
            {
                var instance = BtLinkedGroups.Find(GraphAsset, pair.Key);
                pair.Value.SetGroupHighlight(instance != null && _highlightedGroups.Contains(BtLinkedGroups.Family(GraphAsset, instance).Id));
            }
            MarkEdgeColorsDirty();
        }

        private bool IsHighlightedGroupEdge(Edge edge)
        {
            if (GraphAsset == null || edge.input?.node is not BtNodeView child || edge.output?.node is not BtNodeView parent) return false;
            var instance = BtLinkedGroups.Find(GraphAsset, parent.Guid);
            return instance != null && instance == BtLinkedGroups.Find(GraphAsset, child.Guid) &&
                _highlightedGroups.Contains(BtLinkedGroups.Family(GraphAsset, instance).Id);
        }
    }
}
