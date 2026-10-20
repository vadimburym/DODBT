// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Dots.Editor;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal sealed class DebugUiPerformanceTests
    {
        [Test]
        public void MissingSnapshotClearsHighlightAndStaleMappingsDoNotThrow()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            var view = new BtGraphView(true);
            try
            {
                graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "selector" };
                graph.Nodes.Add(new BtNodeHeader { Guid = "selector", Kind = BtNodeKind.Selector, Title = "Selector", ParentGuid = "root" });
                graph.GuidsByCompiledId = new[] { "selector" };
                view.Bind(graph);
                var node = view.nodes.OfType<BtNodeView>().Single(n => n.title == "Selector");
                view.SetDebugStatus(new[] { NodeStatus.Running });
                Assert.That(node.InputPort.portColor, Is.EqualTo(new Color(.2f, .3f, 1f)));
                Assert.DoesNotThrow(() => view.SetDebugStatus(null));
                Assert.That(node.InputPort.portColor, Is.EqualTo(new Color(.1f, .1f, .1f)));
                view.SetDebugStatus(new[] { NodeStatus.Running });
                graph.GuidsByCompiledId = new[] { "removed", "selector", null };
                Assert.DoesNotThrow(() => view.SetDebugStatus(new[] { NodeStatus.Success }));
                Assert.That(node.InputPort.portColor, Is.EqualTo(new Color(.1f, .1f, .1f)));
            }
            finally { view.DisposeInspectors(); Object.DestroyImmediate(graph); }
        }

        [Test]
        public void RepeatedStatusDoesNotInvalidatePortsAndRealTransitionsStillUpdateColors()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            var node = new BtNodeView("node", BtNodeKind.Selector, "Node", null);
            int updates = 0;
            try
            {
                node.Bind(graph, null, true, () => updates++);
                node.SetDebugStatus(NodeStatus.None);
                for (int i = 0; i < 100; i++) node.SetDebugStatus(NodeStatus.None);
                Assert.That(updates, Is.EqualTo(1));
                node.SetDebugStatus(NodeStatus.Running);
                Assert.That(updates, Is.EqualTo(2));
                Assert.That(node.InputPort.portColor, Is.EqualTo(new Color(.2f, .3f, 1f)));
                for (int i = 0; i < 100; i++) node.SetDebugStatus(NodeStatus.Running);
                Assert.That(updates, Is.EqualTo(2));
                node.SetDebugStatus(NodeStatus.Success);
                Assert.That(node.OutputPort.portColor, Is.EqualTo(new Color(.2f, .85f, .3f)));
                node.SetDebugStatus(NodeStatus.Failure);
                Assert.That(node.InputPort.portColor, Is.EqualTo(new Color(1f, .1f, .1f)));
                node.SetDebugStatus(NodeStatus.None);
                Assert.That(node.InputPort.portColor, Is.EqualTo(new Color(.1f, .1f, .1f)));
                Assert.That(updates, Is.EqualTo(5));
            }
            finally { node.DisposeInspector(); Object.DestroyImmediate(graph); }
        }

        [Test]
        public void CachedInspectorPathFollowsReorderingAndRejectsDeletedLeaf()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "a" });
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "b" });
            try
            {
                using var inspector = new BtNodeInspector(graph, "b", BtNodeKind.Leaf, null);
                var serialized = (SerializedObject)typeof(BtNodeInspector).GetField("_serialized", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inspector);
                Assert.That(inspector.RootPath(), Is.EqualTo("LeafNodes.Array.data[1]"));
                graph.LeafNodes.Reverse();
                serialized.Update();
                Assert.That(inspector.RootPath(), Is.EqualTo("LeafNodes.Array.data[0]"));
                graph.LeafNodes.RemoveAt(0);
                serialized.Update();
                Assert.That(inspector.RootPath(), Is.Null);
            }
            finally { Object.DestroyImmediate(graph); }
        }
    }
}
