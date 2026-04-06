// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.Linq;
using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public sealed class ParallelRuntimeTests
    {
        [Test]
        public void Parallel_WhenAllChildrenSucceed_ReturnsSuccess()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Success);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateParallelAsset(successThreshold: 3, failsThreshold: 3, cacheChildStatus: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Parallel_WhenAllChildrenFail_ReturnsFailure()
        {
            var first = new RecordingLeaf("First", NodeStatus.Failure);
            var second = new RecordingLeaf("Second", NodeStatus.Failure);
            var third = new RecordingLeaf("Third", NodeStatus.Failure);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateParallelAsset(successThreshold: 3, failsThreshold: 3, cacheChildStatus: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Failure));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Parallel_WhenNeitherThresholdIsReached_ReturnsRunning()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Failure);
            var third = new RecordingLeaf("Third", NodeStatus.Running);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateParallelAsset(successThreshold: 2, failsThreshold: 2, cacheChildStatus: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Running));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Parallel_WhenFailureThresholdIsReached_AbortsRunningChildren_AndReturnsFailure()
        {
            var first = new RecordingLeaf("First", NodeStatus.Failure);
            var second = new RecordingLeaf("Second", NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Failure);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateParallelAsset(successThreshold: 3, failsThreshold: 2, cacheChildStatus: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Failure));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(1));
            Assert.That(second.AbortCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Parallel_WhenCacheIsDisabled_ReticksCompletedSuccessChild()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success, NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateParallelAsset(successThreshold: 2, failsThreshold: 2, cacheChildStatus: false, first, second));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(2));
            Assert.That(second.TickCount, Is.EqualTo(2));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }
        
        [Test]
        public void Parallel_WhenCacheIsEnabled_DoesNotRetickCompletedSuccessChild()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateParallelAsset(
                successThreshold: 2,
                failsThreshold: 2,
                cacheChildStatus: true,
                first, second));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(2));
            Assert.That(second.EnterCount, Is.EqualTo(1));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }

        [Test]
        public void Parallel_WhenThresholdIsReached_AbortsRunningChildren_AndClearsCachedStatus()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success, NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Running, NodeStatus.Running);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateParallelAsset(
                successThreshold: 1,
                failsThreshold: 3,
                cacheChildStatus: true,
                first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(2), "Success child should tick again after cache cleanup.");
            Assert.That(second.AbortCount, Is.EqualTo(2));
            Assert.That(third.AbortCount, Is.EqualTo(2));
            Assert.That(second.EnterCount, Is.EqualTo(2));
            Assert.That(third.EnterCount, Is.EqualTo(2));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }

        [Test, Timeout(1500)]
        public void Parallel_With255Children_WhenThresholdReached_CompletesWithoutOverflowLoop()
        {
            var leafs = new RecordingLeaf[255];
            leafs[0] = new RecordingLeaf("Leaf_0", NodeStatus.Success);
            for (var i = 1; i < leafs.Length; i++)
                leafs[i] = new RecordingLeaf("Leaf_" + i, NodeStatus.Running);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateParallelAsset(
                successThreshold: 1,
                failsThreshold: 255,
                cacheChildStatus: true,
                leafs));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(leafs[0].TickCount, Is.EqualTo(1));
            Assert.That(leafs.Skip(1).All(x => x.AbortCount == 1), Is.True);
        }
    }
}