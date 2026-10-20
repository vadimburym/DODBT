// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal sealed class InspectorInteractionWindow : EditorWindow
    {
        internal Action Draw;
        private void OnGUI() => Draw?.Invoke();
    }

    internal sealed class EditorInteractionTests
    {
        private InspectorInteractionWindow _window;
        private BtGraphAsset _graph;

        [SetUp]
        public void Setup()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Editor interaction tests require a graphics device; run without -nographics.");
            _window = ScriptableObject.CreateInstance<InspectorInteractionWindow>();
            _window.position = new Rect(100, 100, 400, 500);
            _window.ShowUtility();
        }

        [TearDown]
        public void Cleanup()
        {
            if (_window != null)
            {
                _window.Draw = null;
                _window.Close();
            }
            if (_graph == null) return;
            Undo.ClearUndo(_graph);
            Object.DestroyImmediate(_graph);
        }

        private void Send(EventType type, Vector2 position = default, KeyCode key = KeyCode.None)
            => _window.SendEvent(new Event { type = type, mousePosition = position, button = 0, keyCode = key });

        private void Click(float x)
        {
            Send(EventType.Layout);
            Send(EventType.MouseDown, new Vector2(x, 20));
            Send(EventType.MouseUp, new Vector2(x, 20));
        }

        [Test]
        public void GraphContext_ReportsMismatchOnlyInGraphConsoleWithoutRepeatingIt()
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
            _graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Title = "Kept leaf", Kind = BtNodeKind.Leaf, ParentGuid = "root" });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf { Number = 37 } });
            var editor = ScriptableObject.CreateInstance<BtEditorWindow>();
            try
            {
                editor.ShowUtility();
                editor.CreateGUI();
                editor.SetCurrentGraphAsset(_graph);
                BtGraphContext.Set(_graph, typeof(BenchmarkContext));
                var output = (Label)typeof(BtEditorWindow).GetField("_outputLabel",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(editor);
                Assert.That(output.text, Does.Contain("[WARNING]").And.Contain("Kept leaf"));
                string previous = output.text;
                editor.RefreshCounters();
                editor.RefreshCounters();
                Assert.That(output.text, Is.EqualTo(previous));
                editor.SendEvent(new Event { type = EventType.Layout });
                editor.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(((SnapshotTestLeaf)_graph.LeafNodes[0].Leaf).Number, Is.EqualTo(37));
                LogAssert.NoUnexpectedReceived();
            }
            finally { editor.Close(); }
        }

        [Test]
        public void EntityDebugger_DrawsGameWorldsAndRecoversAfterWorldDisposal()
        {
            using var editorWorld = new World("Debugger editor-only world", WorldFlags.Editor);
            using var simulationWorld = new World("Debugger simulation world", WorldFlags.Simulation);
            using var clientWorld = new World("Debugger client world", WorldFlags.GameClient);
            var gameWorld = new World("Debugger game world", WorldFlags.Game);
            var debugger = ScriptableObject.CreateInstance<BtEntityDebuggerWindow>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var selectedField = typeof(BtEntityDebuggerWindow).GetField("_world", flags);
            var worldsField = typeof(BtEntityDebuggerWindow).GetField("_worlds", flags);
            try
            {
                selectedField.SetValue(debugger, gameWorld);
                _window.Draw = debugger.DrawDebugger;
                Send(EventType.Layout);
                Send(EventType.Repaint);
                var worlds = (System.Collections.Generic.List<World>)worldsField.GetValue(debugger);
                Assert.That(worlds, Does.Contain(gameWorld));
                Assert.That(worlds, Does.Contain(simulationWorld));
                Assert.That(worlds, Does.Contain(clientWorld));
                Assert.That(worlds.Contains(editorWorld), Is.False);
                Assert.That(selectedField.GetValue(debugger), Is.SameAs(gameWorld));
                gameWorld.Dispose();
                Send(EventType.Layout);
                Send(EventType.Repaint);
                Assert.That(worlds.Contains(gameWorld), Is.False);
                Assert.That(selectedField.GetValue(debugger), Is.Not.SameAs(gameWorld));
                using var replacement = new World("Debugger replacement world", WorldFlags.Game);
                Send(EventType.Layout);
                Send(EventType.Repaint);
                Assert.That(worlds, Does.Contain(replacement));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                _window.Draw = null;
                Object.DestroyImmediate(debugger);
                if (gameWorld.IsCreated) gameWorld.Dispose();
            }
        }

        [Test]
        public void LeafSelector_UsesWholeRowForTypeSelection()
        {
            int opened = 0, draws = 0;
            _window.Draw = () =>
            {
                draws++;
                if (BtLeafSelector.Draw(new Rect(10, 10, 320, 26), typeof(SnapshotTestLeaf), "selector")) opened++;
            };
            Click(20);
            Assert.That(draws, Is.GreaterThan(0));
            Assert.That(opened, Is.EqualTo(1));
            Click(100);
            Assert.That(opened, Is.EqualTo(2));
            Click(20);
            Assert.That(opened, Is.EqualTo(3));
        }

        [Test]
        public void LeafSelector_EmptyValueUsesTheWholeRowForSelection()
        {
            int opened = 0;
            _window.Draw = () =>
            {
                if (BtLeafSelector.Draw(new Rect(10, 10, 320, 26), null, "selector")) opened++;
            };
            Click(20);
            Assert.That(opened, Is.EqualTo(1));
        }

        [Test]
        public void LeafSelector_KeyboardOpensOnce_AndDisabledControlIgnoresInput()
        {
            bool enabled = true;
            int opened = 0;
            _window.Draw = () =>
            {
                using (new EditorGUI.DisabledScope(!enabled))
                    if (BtLeafSelector.Draw(new Rect(10, 10, 320, 26), typeof(SnapshotTestLeaf), "selector")) opened++;
                GUI.FocusControl("selector");
            };
            Send(EventType.Layout);
            foreach (var key in new[] { KeyCode.Return, KeyCode.DownArrow, KeyCode.KeypadEnter, KeyCode.Space })
            {
                int before = opened;
                Send(EventType.KeyDown, key: key);
                Send(EventType.KeyUp, key: key);
                Assert.That(opened, Is.EqualTo(before + 1), key.ToString());
            }
            enabled = false;
            int previous = opened;
            Click(20);
            Click(100);
            Send(EventType.KeyDown, key: KeyCode.Return);
            Assert.That(opened, Is.EqualTo(previous));
        }

        [Test]
        public void LeafInspector_DrawsExpandedPrivateFieldsAndCyclicCollectionsAfterReplacement()
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            var value = new SnapshotTestValue { Number = 9 };
            value.Next = value;
            _graph.LeafNodes.Add(new BtLeafNodeData
            {
                Guid = "leaf",
                Leaf = new SnapshotTestLeaf { Value = value, Alias = value, Values = new() { value } }
            });
            using var serialized = new SerializedObject(_graph);
            using var fields = new BtSerializedFields(_graph, path => path, null);
            const string path = "LeafNodes.Array.data[0].Leaf";
            foreach (string suffix in new[] { "", ".Value", ".Value.Next", ".Alias", ".Values", ".Values.Array.data[0]" })
                serialized.FindProperty(path + suffix).isExpanded = true;
            int draws = 0;
            float drawnHeight = 0;
            _window.Draw = () =>
            {
                serialized.Update();
                fields.DrawLeafLayout(serialized.FindProperty(path));
                if (Event.current.type == EventType.Repaint) drawnHeight = GUILayoutUtility.GetLastRect().height;
                serialized.ApplyModifiedProperties();
                draws++;
            };
            Send(EventType.Layout);
            Send(EventType.Repaint);
            Assert.That(serialized.FindProperty(path + "._privateNumber").intValue, Is.EqualTo(11));
            Assert.That(draws, Is.GreaterThanOrEqualTo(2));
            float expandedHeight = drawnHeight;
            Assert.That(expandedHeight, Is.GreaterThan(BtLeafSelector.Height));
            serialized.FindProperty(path).isExpanded = false;
            Send(EventType.Layout);
            Send(EventType.Repaint);
            Assert.That(drawnHeight, Is.EqualTo(expandedHeight), "Root foldout state must not hide Leaf fields.");
            serialized.FindProperty(path + ".Value").isExpanded = false;
            Send(EventType.Layout);
            Send(EventType.Repaint);
            Assert.That(serialized.FindProperty(path + ".Value").isExpanded, Is.False);
            Assert.That(drawnHeight, Is.LessThan(expandedHeight), "Nested managed references must still collapse.");
            serialized.FindProperty(path).managedReferenceValue = null;
            serialized.ApplyModifiedProperties();
            Send(EventType.Layout);
            Send(EventType.Repaint);
            Assert.That(drawnHeight, Is.EqualTo(BtLeafSelector.Height));
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator FlushPanel(BtGraphView view)
        {
            bool completed = false;
            view.schedule.Execute(() => completed = true).StartingIn(100);
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (!completed && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(completed, Is.True, "The attached panel must execute scheduled UI work.");
        }

        [UnityTest]
        public IEnumerator EdgeHover_KeepsOrdinaryEdgesStableAndPreservesLinkedGroupHighlight()
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "branch" };
            _graph.Nodes.Add(new BtNodeHeader { Guid = "branch", Kind = BtNodeKind.Sequence });
            _graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "branch", ChildrenGuids = new() { "leaf" } });
            _graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, ParentGuid = "branch" });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf() });
            BtLinkedGroups.Create(_graph, "branch");
            var view = new BtGraphView(false);
            _window.rootVisualElement.Add(view);
            try
            {
                view.Bind(_graph);
                yield return FlushPanel(view);
                var root = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "root");
                var branch = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "branch");
                var ordinary = view.edges.Single(e => e.output.node == root);
                var grouped = view.edges.Single(e => e.output.node == branch);
                Color normal = ordinary.output.portColor;
                using (var enter = MouseEnterEvent.GetPooled()) { enter.target = root; root.SendEvent(enter); }
                yield return FlushPanel(view);
                ordinary.ContainsPoint(Vector2.zero);
                Assert.That(ordinary.edgeControl.edgeWidth, Is.EqualTo(3));
                Assert.That(ordinary.edgeControl.outputColor, Is.EqualTo(normal));
                Assert.That(grouped.edgeControl.edgeWidth, Is.EqualTo(3));

                using (var enter = MouseEnterEvent.GetPooled()) { enter.target = branch; branch.SendEvent(enter); }
                yield return FlushPanel(view);
                Color gold = new Color(1f, .8f, .15f);
                Assert.That(grouped.edgeControl.outputColor, Is.EqualTo(gold));
                grouped.ContainsPoint(Vector2.zero);
                Assert.That(grouped.edgeControl.edgeWidth, Is.EqualTo(5));
                Assert.That(grouped.edgeControl.outputColor, Is.EqualTo(gold));
                ordinary.ContainsPoint(Vector2.zero);
                Assert.That(ordinary.edgeControl.edgeWidth, Is.EqualTo(3));
                Assert.That(ordinary.edgeControl.outputColor, Is.EqualTo(normal));

                using (var leave = MouseLeaveEvent.GetPooled()) { leave.target = branch; branch.SendEvent(leave); }
                yield return FlushPanel(view);
                grouped.ContainsPoint(Vector2.zero);
                Assert.That(grouped.edgeControl.edgeWidth, Is.EqualTo(3));
                Assert.That(grouped.edgeControl.outputColor, Is.EqualTo(normal));
                grouped.selected = true;
                grouped.OnSelected();
                Assert.That(grouped.edgeControl.outputColor, Is.EqualTo(grouped.selectedColor));
                grouped.selected = false;
                grouped.OnUnselected();
                Assert.That(grouped.edgeControl.outputColor, Is.EqualTo(normal));
            }
            finally { view.DisposeInspectors(); view.RemoveFromHierarchy(); }
        }

        private BtGraphView CreateDragGraph(out string copyRoot)
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "branch" };
            _graph.Nodes.Add(new BtNodeHeader { Guid = "branch", Kind = BtNodeKind.Sequence, Position = new Vector2(60, 60) });
            _graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "branch", ChildrenGuids = new() { "leaf" } });
            _graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, ParentGuid = "branch", Position = new Vector2(80, 180) });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf() });
            var family = BtLinkedGroups.Create(_graph, "branch");
            copyRoot = BtLinkedGroups.Duplicate(_graph, family.Instances[0], Vector2.right * 400).RootGuid;
            var view = new BtGraphView(false);
            _window.rootVisualElement.Add(view);
            view.Bind(_graph);
            return view;
        }

        private static void GraphMouse(VisualElement target, EventType type, Vector2 point)
        {
            var systemEvent = new Event { type = type, button = 0, mousePosition = point };
            if (type == EventType.MouseDown)
            {
                using var evt = MouseDownEvent.GetPooled(systemEvent);
                evt.target = target; target.SendEvent(evt);
            }
            else if (type == EventType.MouseUp)
            {
                using var evt = MouseUpEvent.GetPooled(systemEvent);
                evt.target = target; target.SendEvent(evt);
            }
            else
            {
                using var evt = MouseMoveEvent.GetPooled(systemEvent);
                evt.target = target; target.SendEvent(evt);
            }
        }

        [UnityTest]
        public IEnumerator LinkedRootDrag_PreviewsBeforeRelease_CommitsOnceAndSupportsUndo()
        {
            var view = CreateDragGraph(out string copyRoot);
            try
            {
                yield return FlushPanel(view);
                var root = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "branch");
                var leaf = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "leaf");
                var peer = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == copyRoot);
                Vector2 rootOrigin = root.GetPosition().position, leafOrigin = leaf.GetPosition().position;
                Vector2 peerOrigin = peer.GetPosition().position;
                object leafData = _graph.FindLeafData("leaf").Leaf;
                view.AddToSelection(root);
                Undo.IncrementCurrentGroup();
                Vector2 pointer = root.titleContainer.LocalToWorld(new Vector2(160, 25));
                GraphMouse(root.titleContainer, EventType.MouseDown, pointer);
                Assert.That(view.HasMouseCapture(), Is.True);
                foreach (float distance in new[] { 25f, 50f, 75f })
                {
                    GraphMouse(view, EventType.MouseDrag, pointer + new Vector2(distance, 20));
                    Vector2 delta = root.GetPosition().position - rootOrigin;
                    Assert.That(delta.sqrMagnitude, Is.GreaterThan(0));
                    Assert.That(leaf.GetPosition().position, Is.EqualTo(leafOrigin + delta));
                    Assert.That(peer.GetPosition().position, Is.EqualTo(peerOrigin));
                    Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(leafOrigin));
                    Assert.That(_graph.FindHeader("branch").Position, Is.EqualTo(rootOrigin));
                    Assert.That(_graph.FindLeafData("leaf").Leaf, Is.SameAs(leafData));
                }
                Vector2 finalLeaf = leaf.GetPosition().position;
                GraphMouse(view, EventType.MouseUp, pointer + new Vector2(75, 20));
                Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(finalLeaf));
                Assert.That(leaf.GetPosition().position, Is.EqualTo(finalLeaf));
                Undo.PerformUndo();
                Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(leafOrigin));
                Assert.That(_graph.FindHeader("branch").Position, Is.EqualTo(rootOrigin));
                Undo.PerformRedo();
                Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(finalLeaf));
            }
            finally { view.ReleaseMouse(); view.DisposeInspectors(); view.RemoveFromHierarchy(); }
        }

        [UnityTest]
        public IEnumerator LinkedRootDrag_MultipleSelectionAtZoom_CancelsOnEscapeAndFinishesOnCaptureLoss()
        {
            var view = CreateDragGraph(out string copyRoot);
            try
            {
                view.UpdateViewTransform(Vector3.zero, Vector3.one * .75f);
                yield return FlushPanel(view);
                var root = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "branch");
                var leaf = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "leaf");
                Vector2 rootOrigin = root.GetPosition().position, leafOrigin = leaf.GetPosition().position;
                view.AddToSelection(root);
                view.AddToSelection(leaf);
                Vector2 pointer = root.titleContainer.LocalToWorld(new Vector2(160, 25));
                GraphMouse(root.titleContainer, EventType.MouseDown, pointer);
                GraphMouse(view, EventType.MouseDrag, pointer + new Vector2(50, 20));
                Vector2 delta = root.GetPosition().position - rootOrigin;
                Assert.That(delta.sqrMagnitude, Is.GreaterThan(0));
                Assert.That(leaf.GetPosition().position, Is.EqualTo(leafOrigin + delta));
                using (var escape = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                { escape.target = view; view.SendEvent(escape); }
                yield return FlushPanel(view);
                Assert.That(root.GetPosition().position, Is.EqualTo(rootOrigin));
                Assert.That(leaf.GetPosition().position, Is.EqualTo(leafOrigin));
                Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(leafOrigin));

                var peer = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == copyRoot);
                string peerLeafId = _graph.FindSequenceData(copyRoot).ChildrenGuids.Single();
                var peerLeaf = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == peerLeafId);
                Vector2 peerLeafOrigin = peerLeaf.GetPosition().position;
                view.AddToSelection(peer);
                GraphMouse(root.titleContainer, EventType.MouseDown, pointer);
                GraphMouse(view, EventType.MouseDrag, pointer + new Vector2(50, 20));
                Assert.That(leaf.GetPosition().position, Is.Not.EqualTo(leafOrigin));
                Vector2 finalRoot = root.GetPosition().position, finalLeaf = leaf.GetPosition().position;
                Assert.That(finalLeaf, Is.EqualTo(leafOrigin + finalRoot - rootOrigin));
                Vector2 finalPeerLeaf = peerLeafOrigin + finalRoot - rootOrigin;
                Assert.That(peerLeaf.GetPosition().position, Is.EqualTo(finalPeerLeaf));
                view.ReleaseMouse();
                yield return FlushPanel(view);
                Assert.That(root.GetPosition().position, Is.EqualTo(finalRoot));
                Assert.That(leaf.GetPosition().position, Is.EqualTo(finalLeaf));
                Assert.That(_graph.FindHeader("branch").Position, Is.EqualTo(finalRoot));
                Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(finalLeaf));
                Assert.That(_graph.FindHeader(peerLeafId).Position, Is.EqualTo(finalPeerLeaf));
            }
            finally { view.ReleaseMouse(); view.DisposeInspectors(); view.RemoveFromHierarchy(); }
        }

        [UnityTest]
        public IEnumerator GraphDeletion_PreservesSurvivingSelectionAfterScheduledReload()
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "branch" };
            _graph.Nodes.Add(new BtNodeHeader { Guid = "branch", Kind = BtNodeKind.Sequence });
            _graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "branch", ChildrenGuids = new() { "leaf" } });
            _graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, ParentGuid = "branch" });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf() });
            var family = BtLinkedGroups.Create(_graph, "branch");
            BtLinkedGroups.Duplicate(_graph, family.Instances[0], Vector2.right * 400);
            var view = new BtGraphView(false);
            _window.rootVisualElement.Add(view);
            try
            {
                view.Bind(_graph);
                view.AddToSelection(view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "branch"));
                view.DeleteElements(new[] { view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "leaf") });
                bool scheduled = false;
                view.schedule.Execute(() => scheduled = true).StartingIn(100);
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (!scheduled && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.That(scheduled, Is.True, "The attached panel must execute scheduled UI work.");
                Assert.That(view.selection.OfType<BtNodeView>().Select(n => n.Guid), Is.EquivalentTo(new[] { "branch" }));
                Assert.That(view.nodes.OfType<BtNodeView>().Count(), Is.EqualTo(3));
            }
            finally { view.DisposeInspectors(); view.RemoveFromHierarchy(); }
        }
    }
}
