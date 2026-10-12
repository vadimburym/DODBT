using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VadimBurym.DodBehaviourTree.PlayerSmoke;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Tests
{
    internal sealed class DecoratorTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (var obj in _objects) Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        private BehaviourTreeAsset Wrap(NodeId kind, double duration, ILeaf leaf, NodeId? outer = null)
        {
            var asset = TestTreeFactory.CreateLeafAsset(leaf);
            _objects.Add(asset);
            asset.RequiredFeatures = 1;
            asset.Nodes = outer.HasValue
                ? new[] { new Node { Id = outer.Value, DataIndex = 0 }, new Node { Id = kind, DataIndex = 1 }, new Node { Id = NodeId.Leaf } }
                : new[] { new Node { Id = kind, DataIndex = 0 }, new Node { Id = NodeId.Leaf } };
            asset.DecoratorNodes = outer.HasValue
                ? new[] { new DecoratorNode { Child = 1, Duration = duration }, new DecoratorNode { Child = 2, Duration = duration } }
                : new[] { new DecoratorNode { Child = 1, Duration = duration } };
            return asset;
        }

        [TestCase(NodeId.Timeout)]
        [TestCase(NodeId.Cooldown)]
        [TestCase(NodeId.Inverter)]
        public void Compatibility_RejectsUnavailableExtensionEvenWithoutFeatureMarker(NodeId kind)
        {
            var asset = Wrap(kind, 1, new RecordingLeaf("Action", NodeStatus.Running));
            asset.RequiredFeatures = 0;
            StringAssert.Contains("DODBT_DECORATORS", asset.CompatibilityError(false));
#if !DODBT_DECORATORS
            Assert.Throws<InvalidOperationException>(() => TestTreeFactory.CreateTree(asset));
#endif
        }

        private BtGraphAsset Graph(BtNodeKind kind, double duration)
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _objects.Add(graph);
            var compiled = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            _objects.Add(compiled);
            graph.CompiledAsset = compiled;
            graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "decorator" };
            graph.Nodes.Add(new BtNodeHeader { Guid = "decorator", Kind = kind, Title = kind.ToString() });
            graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, Title = "Action", ParentGuid = "decorator" });
            graph.DecoratorNodes.Add(new BtDecoratorNodeData { Guid = "decorator", Duration = duration, ChildrenGuids = new() { "leaf" } });
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SmokeTraversalLeaf { First = NodeStatus.Running, Second = NodeStatus.Success } });
            return graph;
        }

        [Test]
        public void GraphData_PreservesDecoratorSettingsInNativeSnapshot()
        {
            var graph = Graph(BtNodeKind.Timeout, 2.5);
            var clone = Object.Instantiate(graph);
            _objects.Add(clone);
            Assert.That(clone.DecoratorNodes[0].Duration, Is.EqualTo(2.5));
            Assert.That(clone.Nodes[0].Kind, Is.EqualTo(BtNodeKind.Timeout));
            Assert.That(clone.GetChildrenList("decorator", BtNodeKind.Timeout), Is.EqualTo(new[] { "leaf" }));
#if !DODBT_DECORATORS
            StringAssert.Contains("DODBT_DECORATORS", BtCompiler.TryCompileAsset(clone, clone.CompiledAsset));
#endif
        }

#if DODBT_DECORATORS
        [Test]
        public void Timeout_SparseTickAbortsBeforeChildCanReturnSuccess()
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Running, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(Wrap(NodeId.Timeout, .5, leaf));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            Assert.That(tree.TickWithStatus(context, state, 10), Is.EqualTo(NodeStatus.Running));
            Assert.That(tree.TickWithStatus(context, state, 12), Is.EqualTo(NodeStatus.Failure));
            Assert.That(leaf.TickCount, Is.EqualTo(1));
            Assert.That(leaf.AbortCount, Is.EqualTo(1));
            Assert.That(leaf.ExitCount, Is.Zero);
            Assert.That(state.DebugRunningLeafs, Is.Empty);
            tree.Abort(context, state);
            Assert.That(leaf.AbortCount, Is.EqualTo(1));
            Assert.That(tree.TickWithStatus(context, state, 12), Is.EqualTo(NodeStatus.Success));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Timeout_ExactDeadlineWinsAndZeroNeverEnters(double duration)
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Running);
            var tree = TestTreeFactory.CreateTree(Wrap(NodeId.Timeout, duration, leaf));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state, 5);
            Assert.That(tree.TickWithStatus(context, state, 5 + duration), Is.EqualTo(NodeStatus.Failure));
            Assert.That(leaf.EnterCount, Is.EqualTo(duration == 0 ? 0 : 1));
            Assert.That(leaf.AbortCount, Is.EqualTo(duration == 0 ? 0 : 1));
        }

        [Test]
        public void Timeout_ExternalAbortRestartsBudget()
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Running);
            var tree = TestTreeFactory.CreateTree(Wrap(NodeId.Timeout, 2, leaf));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state, 0);
            tree.Abort(context, state);
            Assert.That(tree.TickWithStatus(context, state, 10), Is.EqualTo(NodeStatus.Running));
            Assert.That(tree.TickWithStatus(context, state, 11), Is.EqualTo(NodeStatus.Running));
            Assert.That(leaf.EnterCount, Is.EqualTo(2));
            Assert.That(leaf.AbortCount, Is.EqualTo(1));
        }

        [TestCase(NodeStatus.Success)]
        [TestCase(NodeStatus.Failure)]
        public void Cooldown_StartsAtCompletionSurvivesAbortAndAllowsExactDeadline(NodeStatus terminal)
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Running, terminal);
            var tree = TestTreeFactory.CreateTree(Wrap(NodeId.Cooldown, 3, leaf));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            Assert.That(tree.TickWithStatus(context, state, 0), Is.EqualTo(NodeStatus.Running));
            Assert.That(tree.TickWithStatus(context, state, 10), Is.EqualTo(terminal));
            tree.Abort(context, state);
            Assert.That(tree.TickWithStatus(context, state, 12.99), Is.EqualTo(NodeStatus.Failure));
            Assert.That(leaf.TickCount, Is.EqualTo(2));
            Assert.That(leaf.AbortCount, Is.Zero);
            Assert.That(tree.TickWithStatus(context, state, 13), Is.EqualTo(terminal));
            Assert.That(leaf.TickCount, Is.EqualTo(3));
            tree.Tick(context, state, 100);
            Assert.That(leaf.TickCount, Is.EqualTo(4));
        }

        [Test]
        public void Cooldown_AbortOfRunningAttemptDoesNotStartDelay()
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Running);
            var tree = TestTreeFactory.CreateTree(Wrap(NodeId.Cooldown, 100, leaf));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state, 0);
            tree.Abort(context, state);
            tree.Abort(context, state);
            Assert.That(tree.TickWithStatus(context, state, 0), Is.EqualTo(NodeStatus.Running));
            Assert.That(leaf.AbortCount, Is.EqualTo(1));
            Assert.That(leaf.EnterCount, Is.EqualTo(2));
        }

        [Test]
        public void Cooldown_ZeroPassesThroughAndResetClearsPreviousAgentDeadline()
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Success);
            var asset = Wrap(NodeId.Cooldown, 0, leaf);
            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            tree.Tick(context, state, 0);
            Assert.That(tree.TickWithStatus(context, state, 0), Is.EqualTo(NodeStatus.Success));
            asset.DecoratorNodes[0].Duration = 10;
            tree.Construct(asset);
            tree.Tick(context, state, 20);
            tree.FillInitialState(state);
            Assert.That(tree.TickWithStatus(context, state, 0), Is.EqualTo(NodeStatus.Success));
        }

        [TestCase(NodeStatus.Success, NodeStatus.Failure)]
        [TestCase(NodeStatus.Failure, NodeStatus.Success)]
        [TestCase(NodeStatus.Running, NodeStatus.Running)]
        public void Inverter_MapsResultAndOnlyAbortsActiveChild(NodeStatus child, NodeStatus expected)
        {
            var leaf = new RecordingLeaf("Action", child);
            var tree = TestTreeFactory.CreateTree(Wrap(NodeId.Inverter, 0, leaf));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            Assert.That(tree.TickWithStatus(context, state), Is.EqualTo(expected));
            tree.Abort(context, state);
            tree.Abort(context, state);
            Assert.That(leaf.AbortCount, Is.EqualTo(child == NodeStatus.Running ? 1 : 0));
            Assert.That(leaf.ExitCount, Is.EqualTo(child == NodeStatus.Running ? 0 : 1));
        }

        [TestCase(NodeId.Timeout, NodeId.Inverter, NodeStatus.Success)]
        [TestCase(NodeId.Inverter, NodeId.Timeout, NodeStatus.Failure)]
        [TestCase(NodeId.Timeout, NodeId.Cooldown, NodeStatus.Failure)]
        public void NestedWrappers_PreserveAbortAndResultOrder(NodeId inner, NodeId outer, NodeStatus expected)
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Running);
            var tree = TestTreeFactory.CreateTree(Wrap(inner, 1, leaf, outer));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            Assert.That(tree.TickWithStatus(context, state, 0), Is.EqualTo(NodeStatus.Running));
            Assert.That(tree.TickWithStatus(context, state, 1), Is.EqualTo(expected));
            tree.Abort(context, state);
            Assert.That(leaf.AbortCount, Is.EqualTo(1));
            Assert.That(leaf.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void Timer_RequiresFiniteMonotonicExplicitTimeBeforeInvokingChild()
        {
            var leaf = new RecordingLeaf("Action", NodeStatus.Running);
            var tree = TestTreeFactory.CreateTree(Wrap(NodeId.Timeout, 1, leaf));
            var state = TestTreeFactory.CreateInitializedState(tree);
            var context = new TestContext();
            Assert.Throws<InvalidOperationException>(() => tree.Tick(context, state));
            foreach (double time in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => tree.Tick(context, state, time));
            Assert.That(leaf.TickCount, Is.Zero);
            tree.Tick(context, state, 10);
            Assert.Throws<ArgumentOutOfRangeException>(() => tree.Tick(context, state, 9));
            Assert.That(tree.TickWithStatus(context, state, 10), Is.EqualTo(NodeStatus.Running));
            Assert.That(leaf.TickCount, Is.EqualTo(2));
        }

        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Compiler_RejectsInvalidDurationWithoutReplacingCompiledTree(double duration)
        {
            var graph = Graph(BtNodeKind.Timeout, 1);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            var before = graph.CompiledAsset.Nodes;
            graph.DecoratorNodes[0].Duration = duration;
            StringAssert.Contains("duration", BtCompiler.TryCompileAsset(graph, graph.CompiledAsset));
            Assert.That(graph.CompiledAsset.Nodes, Is.SameAs(before));
        }

        [TestCase(BtNodeKind.Timeout)]
        [TestCase(BtNodeKind.Cooldown)]
        [TestCase(BtNodeKind.Inverter)]
        public void Compiler_RequiresOneChildAndProducesExecutableNativeSnapshot(BtNodeKind kind)
        {
            var graph = Graph(kind, 2);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            var compiled = Object.Instantiate(graph.CompiledAsset);
            _objects.Add(compiled);
            var tree = new BehaviourTree<SmokeContext, SmokeState>();
            tree.Construct(compiled);
            var state = new BtState<SmokeState>();
            tree.FillInitialState(state);
            Assert.That(tree.TickWithStatus(new SmokeContext(), state, 0), Is.EqualTo(NodeStatus.Running));
            graph.DecoratorNodes[0].ChildrenGuids.Clear();
            StringAssert.Contains("exactly one", BtCompiler.TryCompileAsset(graph, graph.CompiledAsset));
            graph.DecoratorNodes[0].ChildrenGuids.AddRange(new[] { "leaf", "leaf" });
            StringAssert.Contains("exactly one", BtCompiler.TryCompileAsset(graph, graph.CompiledAsset));
        }

        [Test]
        public void Timers_AreIndependentAcrossAgentsAndAllocateNothingDuringStableTicks()
        {
            var asset = Wrap(NodeId.Timeout, 10, new SmokeTraversalLeaf { First = NodeStatus.Running, Second = NodeStatus.Running, Later = NodeStatus.Running });
            var tree = new BehaviourTree<SmokeContext, SmokeState>();
            tree.Construct(asset);
            var a = new BtState<SmokeState>();
            var b = new BtState<SmokeState>();
            tree.FillInitialState(a);
            tree.FillInitialState(b);
            var context = new SmokeContext();
            tree.Tick(context, a, 0);
            tree.Tick(context, b, 5);
            Assert.That(tree.TickWithStatus(context, a, 10), Is.EqualTo(NodeStatus.Failure));
            Assert.That(tree.TickWithStatus(context, b, 10), Is.EqualTo(NodeStatus.Running));
            for (int i = 0; i < 32; i++) tree.Tick(context, b, 10);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) tree.Tick(context, b, 10);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(b.LeafStates[0].Aborts, Is.Zero);
            Assert.That(a.LeafStates[0].Aborts, Is.EqualTo(1));
        }
#endif
    }
}
