// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using NUnit.Framework;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal sealed class ActiveLeafSummaryTests
    {
        private BtGraphAsset _graph;
        private static TestNodeSpec Leaf(string name) => TestNodeSpec.RecordingLeaf(name, NodeStatus.Success);

        private BtActiveLeafMap Map(ProductionFixture fixture)
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.CompiledRevision = "test-revision";
            _graph.GuidsByCompiledId = new string[fixture.Nodes.Length];
            for (int i = 0; i < fixture.Nodes.Length; i++)
            {
                string guid = "node-" + i;
                _graph.GuidsByCompiledId[i] = guid;
                _graph.Nodes.Add(new BtNodeHeader { Guid = guid, Title = "Node " + i,
                    Kind = fixture.Blob.Value.Nodes[i].Id == NodeId.Leaf ? BtNodeKind.Leaf : BtNodeKind.Sequence });
            }
            return new BtActiveLeafMap(_graph);
        }

        private static BtActiveLeafMap.Summary Read(BtActiveLeafMap map, ProductionFixture fixture) =>
            map.Read(new BtDebugTree { CompiledRevision = "test-revision" }, fixture.Nodes, fixture.Leafs);

        [TearDown]
        public void Cleanup() { if (_graph != null) Object.DestroyImmediate(_graph); }

        [Test]
        public void Summary_FollowsRunningAndCompletionWithoutTraceBuffers()
        {
            using var fixture = new ProductionFixture(TestNodeSpec.Sequence(Leaf("A"), Leaf("B")));
            var map = Map(fixture);
            Assert.That(Read(map, fixture).Count, Is.Zero);
            fixture.Context.Outcomes[1] = NodeStatus.Running;
            fixture.Tick();
            Assert.That(Read(map, fixture).Text, Is.EqualTo("Node 2"));
            Assert.That(Read(map, fixture).IsValid, Is.True);
            fixture.Context.Outcomes[1] = NodeStatus.Success;
            fixture.Tick();
            Assert.That(Read(map, fixture).Text, Is.EqualTo("No running leaves"));
            Assert.That(fixture.World.EntityManager.HasBuffer<BtDebugNodeState>(fixture.Entity), Is.False);
        }

        [Test]
        public void Summary_ShowsAllParallelRunningLeavesAndDropsAbortedOnes()
        {
            using var fixture = new ProductionFixture(TestNodeSpec.Parallel(1, 2, false, Leaf("A"), Leaf("B")));
            var map = Map(fixture);
            fixture.Context.Outcomes[0] = fixture.Context.Outcomes[1] = NodeStatus.Running;
            fixture.Tick();
            Assert.That(Read(map, fixture).Text, Is.EqualTo("Node 1, Node 2"));
            Assert.That(Read(map, fixture).Count, Is.EqualTo(2));
            fixture.Context.Outcomes[1] = NodeStatus.Success;
            fixture.Tick();
            Assert.That(fixture.Context.Events[3], Is.EqualTo(1), "The first leaf was aborted.");
            Assert.That(Read(map, fixture).Count, Is.Zero);
        }

        [Test]
        public void Summary_UsesLeafStateIndexRatherThanNodeOrArrayOrder()
        {
            using var fixture = new ProductionFixture(TestNodeSpec.Sequence(Leaf("A"), Leaf("B")));
            var map = Map(fixture);
            fixture.Context.Outcomes[1] = NodeStatus.Running;
            fixture.Tick();
            var nodes = fixture.Nodes;
            var first = nodes[1];
            var second = nodes[2];
            (first.LeafStateIndex, second.LeafStateIndex) = (second.LeafStateIndex, first.LeafStateIndex);
            nodes[1] = first;
            nodes[2] = second;
            Assert.That(Read(map, fixture).Text, Is.EqualTo("Node 1"));
        }

        [Test]
        public void Summary_RejectsStaleRevisionDirtyGraphAndInvalidStateLayout()
        {
            using var fixture = new ProductionFixture(TestNodeSpec.Sequence(Leaf("A")));
            var map = Map(fixture);
            Assert.That(map.Read(new BtDebugTree { CompiledRevision = "old-revision" }, fixture.Nodes, fixture.Leafs).IsValid, Is.False);
            var nodes = fixture.Nodes;
            var node = nodes[1];
            node.LeafStateIndex = 255;
            nodes[1] = node;
            Assert.That(Read(map, fixture).IsValid, Is.False);
            _graph.NotActualCompiledVersion = true;
            Assert.That(Read(new BtActiveLeafMap(_graph), fixture).Text, Is.EqualTo("Graph has uncompiled edits"));
            Assert.That(Read(new BtActiveLeafMap(null), fixture).IsValid, Is.False);
        }
    }
}
