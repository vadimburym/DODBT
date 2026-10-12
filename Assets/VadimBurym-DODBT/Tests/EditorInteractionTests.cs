using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Tests
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
        public void LeafSelector_DisclosureAndTypeSelectionHaveSeparateClickTargets()
        {
            bool expanded = false;
            int opened = 0, draws = 0;
            _window.Draw = () =>
            {
                draws++;
                if (BtLeafSelector.Draw(new Rect(10, 10, 320, 26), typeof(SnapshotTestLeaf), true, ref expanded, "selector")) opened++;
            };
            Click(20);
            Assert.That(draws, Is.GreaterThan(0));
            Assert.That(expanded, Is.True);
            Assert.That(opened, Is.Zero);
            Click(100);
            Assert.That(expanded, Is.True);
            Assert.That(opened, Is.EqualTo(1));
            Click(20);
            Assert.That(expanded, Is.False);
            Assert.That(opened, Is.EqualTo(1));
        }

        [Test]
        public void LeafSelector_EmptyValueUsesTheWholeRowForSelection()
        {
            bool expanded = false;
            int opened = 0;
            _window.Draw = () =>
            {
                if (BtLeafSelector.Draw(new Rect(10, 10, 320, 26), null, false, ref expanded, "selector")) opened++;
            };
            Click(20);
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(expanded, Is.False);
        }

        [Test]
        public void LeafSelector_KeyboardOpensOnce_AndDisabledControlIgnoresInput()
        {
            bool expanded = false, enabled = true;
            int opened = 0;
            _window.Draw = () =>
            {
                using (new EditorGUI.DisabledScope(!enabled))
                    if (BtLeafSelector.Draw(new Rect(10, 10, 320, 26), typeof(SnapshotTestLeaf), true, ref expanded, "selector")) opened++;
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
            Assert.That(expanded, Is.False);
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
            _window.Draw = () =>
            {
                serialized.Update();
                fields.DrawLeafLayout(serialized.FindProperty(path));
                serialized.ApplyModifiedProperties();
                draws++;
            };
            Send(EventType.Layout);
            Send(EventType.Repaint);
            Assert.That(serialized.FindProperty(path + "._privateNumber").intValue, Is.EqualTo(11));
            Assert.That(draws, Is.GreaterThanOrEqualTo(2));
            serialized.FindProperty(path).isExpanded = false;
            Send(EventType.Layout);
            Send(EventType.Repaint);
            serialized.FindProperty(path).managedReferenceValue = null;
            serialized.ApplyModifiedProperties();
            Send(EventType.Layout);
            Send(EventType.Repaint);
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
