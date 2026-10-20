// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using System.Collections.Generic;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal sealed partial class BtGraphView
    {
        private sealed class GroupSelectionDragger : SelectionDragger
        {
            internal void FinishDrag()
            {
                using var mouseUp = MouseUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0 });
                // Finish through the native path so its edge-panning schedule is stopped too.
                base.OnMouseUp(mouseUp);
            }
        }

        private readonly GroupSelectionDragger _selectionDragger = new();
        private bool _cancelingGroupDrag;
        private sealed class GroupDragPreview
        {
            internal Vector2 Origin;
            internal readonly List<(BtNodeView View, Vector2 Origin)> Members = new();
        }

        private readonly Dictionary<string, GroupDragPreview> _groupDragPreviews = new();
        private bool _applyingGroupPositions;

        private void ConfigureGroupDragging()
        {
            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && _groupDragPreviews.Count > 0) _cancelingGroupDrag = true;
            }, TrickleDown.TrickleDown);
            RegisterCallback<MouseCaptureOutEvent>(evt =>
            {
                if (evt.target != this) return;
                try
                {
                    // This callback runs before SelectionDragger clears its active drag state.
                    if (!_cancelingGroupDrag && _groupDragPreviews.Count > 0) _selectionDragger.FinishDrag();
                }
                finally
                {
                    EndGroupDragPreview(restore: true);
                    _cancelingGroupDrag = false;
                }
            });
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                if (evt.target == this) EndGroupDragPreview(restore: false);
            });
        }

        private void PreviewGroupRootPosition(BtNodeView root, Vector2 position)
        {
            if (_applyingGroupPositions || _isBinding || IsReadOnly || GraphAsset == null ||
                !root.selected || !this.HasMouseCapture()) return;

            if (!_groupDragPreviews.TryGetValue(root.Guid, out var preview))
            {
                var instance = BtLinkedGroups.Find(GraphAsset, root.Guid);
                if (instance == null || instance.RootGuid != root.Guid) return;
                preview = new GroupDragPreview { Origin = GraphAsset.FindHeader(root.Guid).Position };
                foreach (var member in instance.Members)
                    if (_nodeViewsByGuid.TryGetValue(member.Guid, out var view))
                        preview.Members.Add((view, GraphAsset.FindHeader(member.Guid).Position));
                _groupDragPreviews.Add(root.Guid, preview);
            }

            // Resolved layout can still describe the previous frame inside SetPosition.
            Vector2 delta = position - preview.Origin;
            _applyingGroupPositions = true;
            try
            {
                foreach (var member in preview.Members)
                {
                    // SelectionDragger already moves explicitly selected members, including the root.
                    if (member.View.selected) continue;
                    member.View.SetPosition(new Rect(member.Origin + delta, member.View.GetPosition().size));
                }
            }
            finally { _applyingGroupPositions = false; }
        }

        private void EndGroupDragPreview(bool restore)
        {
            if (restore)
            {
                _applyingGroupPositions = true;
                try
                {
                    foreach (var preview in _groupDragPreviews.Values)
                    foreach (var member in preview.Members)
                        member.View.SetPosition(new Rect(member.Origin, member.View.GetPosition().size));
                }
                finally { _applyingGroupPositions = false; }
            }
            _groupDragPreviews.Clear();
        }
    }
}
