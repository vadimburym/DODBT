// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Light.Editor;

namespace VadimBurym.DodBehaviourTree.Light.Tests
{
    internal sealed class DebugUiPerformanceTests
    {
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
