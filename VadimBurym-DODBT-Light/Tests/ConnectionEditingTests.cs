// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Light.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Light.Tests
{
    internal sealed class ConnectionEditingTests
    {
        private BtGraphAsset _graph;
        private BtGraphView _view;
        private readonly List<Regex> _expectedWarnings = new();
        private readonly List<string> _graphWarnings = new();
        private readonly List<string> _unityWarnings = new();

        [SetUp]
        public void Setup()
        {
            _expectedWarnings.Clear();
            _graphWarnings.Clear();
            _unityWarnings.Clear();
            Application.logMessageReceived += CaptureUnityWarning;
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "first" };
            Add("first", BtNodeKind.Sequence);
            Add("second", BtNodeKind.Sequence);
            Add("leaf", BtNodeKind.Leaf);
            _graph.FindHeader("first").ParentGuid = "root";
            _graph.FindHeader("leaf").ParentGuid = "first";
            _graph.FindSequenceData("first").ChildrenGuids.Add("leaf");
        }

        private void Add(string id, BtNodeKind kind)
        {
            _graph.Nodes.Add(new BtNodeHeader { Guid = id, Title = id, Kind = kind });
            if (kind == BtNodeKind.Leaf) _graph.LeafNodes.Add(new BtLeafNodeData { Guid = id, Leaf = new SnapshotTestLeaf() });
            else if (kind == BtNodeKind.Sequence) _graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = id });
            else _graph.DecoratorNodes.Add(new BtDecoratorNodeData { Guid = id });
        }

        private void Bind(bool readOnly = false)
        {
            _view = new BtGraphView(readOnly);
            _view.Bind(_graph);
            _view.LogRequested += (message, level, _) =>
            {
                if (level == LogLevel.Warning) _graphWarnings.Add(message);
            };
            Undo.IncrementCurrentGroup();
        }

        private void Connect(string parent, string child)
        {
            var nodes = _view.nodes.OfType<BtNodeView>().ToDictionary(n => n.Guid);
            new BtEdgeConnectorListener(_view).OnDrop(_view, new Edge
            {
                input = nodes[child].InputPort, output = nodes[parent].OutputPort
            });
        }

        private void ExpectLimit(string limit) =>
            _expectedWarnings.Add(new Regex("Connection replaced: .* accepts only one " + limit + "\\."));

        private void CaptureUnityWarning(string message, string stack, LogType type)
        {
            if (type == LogType.Warning) _unityWarnings.Add(message);
        }

        [TearDown]
        public void Cleanup()
        {
            Application.logMessageReceived -= CaptureUnityWarning;
            _view?.DisposeInspectors();
            Undo.ClearUndo(_graph);
            Object.DestroyImmediate(_graph);
            Assert.That(_unityWarnings, Is.Empty, "Graph warnings must not reach the Unity Console.");
            Assert.That(_graphWarnings.Count, Is.EqualTo(_expectedWarnings.Count));
            for (int i = 0; i < _expectedWarnings.Count; i++)
                Assert.That(_expectedWarnings[i].IsMatch(_graphWarnings[i]), Is.True, _graphWarnings[i]);
        }

        [Test]
        public void ReparentLeaf_WarnsAndUpdatesBothEnds_UndoRedoTogether()
        {
            Bind();
            ExpectLimit("parent");
            Connect("second", "leaf");
            Assert.That(_graph.FindSequenceData("first").ChildrenGuids, Is.Empty);
            Assert.That(_graph.FindSequenceData("second").ChildrenGuids, Is.EqualTo(new[] { "leaf" }));
            Assert.That(_graph.FindHeader("leaf").ParentGuid, Is.EqualTo("second"));
            Assert.That(_view.edges.Count(), Is.EqualTo(2));
            Undo.PerformUndo();
            Assert.That(_graph.FindHeader("leaf").ParentGuid, Is.EqualTo("first"));
            Assert.That(_graph.FindSequenceData("first").ChildrenGuids, Is.EqualTo(new[] { "leaf" }));
            Assert.That(_graph.FindSequenceData("second").ChildrenGuids, Is.Empty);
            Undo.PerformRedo();
            Assert.That(_graph.FindHeader("leaf").ParentGuid, Is.EqualTo("second"));
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(BtNodeKind.Root)]
#if DODBT_DECORATORS
        [TestCase(BtNodeKind.Inverter)]
        [TestCase(BtNodeKind.Timeout)]
        [TestCase(BtNodeKind.Cooldown)]
#endif
        public void ReplaceSingleChild_WarnsAndDetachesPreviousChild(BtNodeKind kind)
        {
            string parent = "root";
            if (kind != BtNodeKind.Root)
            {
                parent = "decorator";
                Add(parent, kind);
                _graph.FindDecoratorData(parent).ChildrenGuids.Add("first");
                _graph.FindHeader("first").ParentGuid = parent;
                _graph.RootNode.ChildrenGuid = parent;
                _graph.FindHeader(parent).ParentGuid = "root";
            }
            Bind();
            ExpectLimit("child");
            Connect(parent, "second");
            Assert.That(_graph.FindHeader("first").ParentGuid, Is.Null.Or.Empty);
            Assert.That(_graph.FindHeader("second").ParentGuid, Is.EqualTo(parent));
            if (kind == BtNodeKind.Root) Assert.That(_graph.RootNode.ChildrenGuid, Is.EqualTo("second"));
            else Assert.That(_graph.FindDecoratorData(parent).ChildrenGuids, Is.EqualTo(new[] { "second" }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BothOccupiedPorts_WarnForEachLimit_AndPreserveUndo()
        {
            Bind();
            ExpectLimit("parent");
            ExpectLimit("child");
            Connect("root", "leaf");
            Assert.That(_graph.RootNode.ChildrenGuid, Is.EqualTo("leaf"));
            Assert.That(_graph.FindHeader("leaf").ParentGuid, Is.EqualTo("root"));
            Assert.That(_graph.FindHeader("first").ParentGuid, Is.Null.Or.Empty);
            Assert.That(_graph.FindSequenceData("first").ChildrenGuids, Is.Empty);
            Assert.That(_view.edges.Count(), Is.EqualTo(1));
            Undo.PerformUndo();
            Assert.That(_graph.RootNode.ChildrenGuid, Is.EqualTo("first"));
            Assert.That(_graph.FindHeader("leaf").ParentGuid, Is.EqualTo("first"));
            Assert.That(_graph.FindSequenceData("first").ChildrenGuids, Is.EqualTo(new[] { "leaf" }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FreePortsAndSameConnection_DoNotWarnOrDuplicateEdges()
        {
            Add("free", BtNodeKind.Leaf);
            Bind();
            Connect("second", "free");
            Connect("second", "free");
            Assert.That(_graph.FindSequenceData("second").ChildrenGuids, Is.EqualTo(new[] { "free" }));
            Assert.That(_view.edges.Count(), Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CycleIsRejectedWithGraphWarning_BeforeRemovingConnections()
        {
            _graph.FindSequenceData("first").ChildrenGuids.Add("second");
            _graph.FindHeader("second").ParentGuid = "first";
            Bind();
            string before = EditorJsonUtility.ToJson(_graph);
            _expectedWarnings.Add(new Regex("Connection blocked: would create cyclic dependency"));
            Connect("second", "first");
            Assert.That(EditorJsonUtility.ToJson(_graph), Is.EqualTo(before));
            Assert.That(_view.edges.Count(), Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LinkedGroupConflict_WarnsOnlyInGraphConsole()
        {
            BtLinkedGroups.Create(_graph, "first");
            Bind();
            string before = EditorJsonUtility.ToJson(_graph);
            _expectedWarnings.Add(new Regex("Connection blocked: linked groups cannot overlap"));
            Connect("second", "leaf");
            Assert.That(EditorJsonUtility.ToJson(_graph), Is.EqualTo(before));
            Assert.That(_view.edges.Count(), Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ReadOnlyGraph_RejectsDropWithoutChangingDataOrEdges()
        {
            Bind(true);
            string before = EditorJsonUtility.ToJson(_graph);
            Connect("second", "leaf");
            Assert.That(EditorJsonUtility.ToJson(_graph), Is.EqualTo(before));
            Assert.That(_view.edges.Count(), Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
