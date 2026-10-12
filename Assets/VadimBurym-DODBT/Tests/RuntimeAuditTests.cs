using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public sealed class RuntimeAuditTests
    {
        private readonly List<BehaviourTreeAsset> _assets = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        private BehaviourTreeAsset Asset(Node[] nodes, ILeaf[] leaves)
        {
            var asset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            _assets.Add(asset);
            asset.Nodes = nodes;
            asset.Leafs = leaves;
            asset.SelectorNodes = Array.Empty<SelectorNode>();
            asset.SequenceNodes = Array.Empty<SequenceNode>();
            asset.MemorySelectorNodes = Array.Empty<MemorySelectorNode>();
            asset.MemorySequenceNodes = Array.Empty<MemorySequenceNode>();
            asset.ParallelNodes = Array.Empty<ParallelNode>();
            asset.ChildBufferSize = 3;
            return asset;
        }

        private BehaviourTreeAsset NestedSelectors(ILeaf[] leaves, bool outerRandom, bool innerRandom)
        {
            var asset = Asset(new[]
            {
                new Node { Id = NodeId.MemorySelector, DataIndex = 0 },
                new Node { Id = NodeId.MemorySelector, DataIndex = 1 },
                new Node { Id = NodeId.Leaf, DataIndex = 0 },
                new Node { Id = NodeId.Leaf, DataIndex = 1 },
                new Node { Id = NodeId.Leaf, DataIndex = 2 },
                new Node { Id = NodeId.Leaf, DataIndex = 3 },
                new Node { Id = NodeId.Leaf, DataIndex = 4 }
            }, leaves);
            asset.MemorySelectorNodes = new[]
            {
                new MemorySelectorNode { FirstChild = 1, ChildCount = 3, PickRandom = outerRandom, ResetOnAbort = true },
                new MemorySelectorNode { FirstChild = 4, ChildCount = 3, PickRandom = innerRandom, ResetOnAbort = true }
            };
            return asset;
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void NestedMemorySelectors_VisitEveryFailingChildExactlyOnce(bool outerRandom, bool innerRandom)
        {
            var savedRandom = Random.state;
            try
            {
                for (int seed = 0; seed < 32; seed++)
                {
                    Random.InitState(seed);
                    var leaves = new[]
                    {
                        new RecordingLeaf("TailA", NodeStatus.Failure),
                        new RecordingLeaf("TailB", NodeStatus.Failure),
                        new RecordingLeaf("InnerA", NodeStatus.Failure),
                        new RecordingLeaf("InnerB", NodeStatus.Failure),
                        new RecordingLeaf("InnerC", NodeStatus.Failure)
                    };
                    var asset = NestedSelectors(leaves, outerRandom, innerRandom);
                    var tree = TestTreeFactory.CreateTree(asset);
                    var state = TestTreeFactory.CreateInitializedState(tree);
                    var context = new TestContext();
                    tree.Tick(context, state);
                    Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Failure));
                    foreach (var leaf in leaves)
                        Assert.That(leaf.TickCount, Is.EqualTo(1), "Each leaf must be visited once, seed " + seed);
                    if (!outerRandom)
                        Assert.That(context.Events[context.Events.Count - 1], Is.EqualTo("exit:TailB:Failure"));
                }
            }
            finally { Random.state = savedRandom; }
        }

        [Test]
        public void SharedTree_NestedRandomSelectorsKeepAgentStateIndependentWithoutTickAllocations()
        {
            var savedRandom = Random.state;
            try
            {
                var asset = NestedSelectors(new ILeaf[]
                {
                    new CountingLeaf(), new CountingLeaf(), new CountingLeaf(), new CountingLeaf(), new CountingLeaf()
                }, true, true);
                var tree = TestTreeFactory.CreateTree(asset);
                var first = TestTreeFactory.CreateInitializedState(tree);
                var second = TestTreeFactory.CreateInitializedState(tree);
                var context = new TestContext();
                for (int i = 0; i < 32; i++) { tree.Tick(context, first); tree.Tick(context, second); }
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++)
                {
                    tree.Tick(context, first);
                    if ((i & 1) == 0) tree.Tick(context, second);
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(allocated, Is.Zero, "Steady-state nested selector ticks must not allocate managed memory.");
                for (int i = 0; i < asset.Leafs.Length; i++)
                {
                    Assert.That(first.LeafStates[i].TickCount, Is.EqualTo(1032));
                    Assert.That(second.LeafStates[i].TickCount, Is.EqualTo(532));
                }
            }
            finally { Random.state = savedRandom; }
        }

        [Serializable]
        private sealed class CountingLeaf : ILeaf<TestContext, TestLeafState>
        {
            public NodeStatus OnTick(TestContext context, ref TestLeafState state) { state.TickCount++; return NodeStatus.Failure; }
            public void OnEnter(TestContext context, ref TestLeafState state) { }
            public void OnExit(TestContext context, ref TestLeafState state, NodeStatus status) { }
            public void OnAbort(TestContext context, ref TestLeafState state) { }
        }

        [Test]
        public void ParallelWithoutCache_DoesNotAbortAnAlreadyFailedMemorySequence()
        {
            var sibling = new RecordingLeaf("Sibling", NodeStatus.Running);
            var prefix = new RecordingLeaf("Prefix", NodeStatus.Success);
            var action = new RecordingLeaf("Action", NodeStatus.Running, NodeStatus.Failure, NodeStatus.Success);
            var asset = Asset(new[]
            {
                new Node { Id = NodeId.Parallel, DataIndex = 0 },
                new Node { Id = NodeId.MemorySequence, DataIndex = 0 },
                new Node { Id = NodeId.Leaf, DataIndex = 0 },
                new Node { Id = NodeId.Leaf, DataIndex = 1 },
                new Node { Id = NodeId.Leaf, DataIndex = 2 }
            }, new ILeaf[] { sibling, prefix, action });
            asset.ParallelNodes = new[] { new ParallelNode { FirstChild = 1, ChildCount = 2, SuccessThreshold = 2, FailsThreshold = 2, CacheChildStatus = false } };
            asset.MemorySequenceNodes = new[] { new MemorySequenceNode { FirstChild = 3, ChildCount = 2, ResetOnFailure = false, ResetOnAbort = true } };
            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state);
            tree.Tick(context, state);
            tree.Abort(context, state);
            tree.Tick(context, state);
            Assert.That(prefix.TickCount, Is.EqualTo(1), "Aborting the running sibling must not reset the completed sequence's failure cursor.");
            Assert.That(action.AbortCount, Is.Zero);
            Assert.That(sibling.AbortCount, Is.EqualTo(1));
        }

        [Test]
        public void ParallelCompletion_RemovesAbortedLeavesFromDebugRunningList()
        {
            var winner = new RecordingLeaf("Winner", NodeStatus.Success);
            var running = new RecordingLeaf("Running", NodeStatus.Running);
            var asset = TestTreeFactory.CreateParallelAsset(1, 2, true, winner, running);
            _assets.Add(asset);
            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);
            tree.Tick(new TestContext(), state);
            Assert.That(running.AbortCount, Is.EqualTo(1));
            Assert.That(state.DebugRunningLeafs, Is.Empty);
            Assert.That(state.DebugStatus[2], Is.EqualTo(NodeStatus.None));
        }

        [Test]
        public void ParallelCachedSuccess_RemainsVisibleInDebugStatus()
        {
            var asset = TestTreeFactory.CreateParallelAsset(2, 2, true,
                new RecordingLeaf("Done", NodeStatus.Success), new RecordingLeaf("Active", NodeStatus.Running));
            _assets.Add(asset);
            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state);
            tree.Tick(context, state);
            Assert.That(state.DebugStatus[1], Is.EqualTo(NodeStatus.Success));
        }

        [Test]
        public void DebugRunningLeaves_FollowTraversalOrderAcrossDifferentDepths()
        {
            var asset = Asset(new[]
            {
                new Node { Id = NodeId.Parallel, DataIndex = 0 },
                new Node { Id = NodeId.Sequence, DataIndex = 0 },
                new Node { Id = NodeId.Leaf, DataIndex = 0 },
                new Node { Id = NodeId.Leaf, DataIndex = 1 }
            }, new ILeaf[] { new LateRunningLeaf(), new EarlyRunningLeaf() });
            asset.ParallelNodes = new[] { new ParallelNode { FirstChild = 1, ChildCount = 2, SuccessThreshold = 2, FailsThreshold = 2 } };
            asset.SequenceNodes = new[] { new SequenceNode { FirstChild = 3, ChildCount = 1 } };
            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state);
            CollectionAssert.AreEqual(new[] { "EarlyRunning", "LateRunning" }, state.DebugRunningLeafs);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) tree.Tick(context, state);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero, "Steady-state active-leaf debug tracking must not allocate.");
        }

        [Serializable]
        private abstract class RunningLeaf : ILeaf<TestContext, TestLeafState>
        {
            public NodeStatus OnTick(TestContext context, ref TestLeafState state) => NodeStatus.Running;
            public void OnEnter(TestContext context, ref TestLeafState state) { }
            public void OnExit(TestContext context, ref TestLeafState state, NodeStatus status) { }
            public void OnAbort(TestContext context, ref TestLeafState state) { }
        }

        [Serializable] private sealed class EarlyRunningLeaf : RunningLeaf { }
        [Serializable] private sealed class LateRunningLeaf : RunningLeaf { }

        [Test]
        public void Abort_ClearsDebugRunningStateImmediately()
        {
            var asset = TestTreeFactory.CreateLeafAsset(new RecordingLeaf("Active", NodeStatus.Running));
            _assets.Add(asset);
            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state);
            tree.Abort(context, state);
            Assert.That(state.DebugRunningLeafs, Is.Empty);
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.None));
        }

        [Test]
        public void ReinitializedState_HasNoPreviousDebugRunningLeaves()
        {
            var asset = TestTreeFactory.CreateLeafAsset(new RecordingLeaf("Active", NodeStatus.Running));
            _assets.Add(asset);
            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);
            tree.Tick(new TestContext(), state);
            tree.FillInitialState(state);
            Assert.That(state.DebugRunningLeafs, Is.Empty);
        }
    }
}
