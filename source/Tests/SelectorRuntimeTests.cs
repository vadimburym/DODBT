// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public sealed class SelectorRuntimeTests
    {
        [Test]
        public void Selector_WhenAllChildrenFail_ReturnsFailure()
        {
            var first = new RecordingLeaf("First", NodeStatus.Failure);
            var second = new RecordingLeaf("Second", NodeStatus.Failure);
            var third = new RecordingLeaf("Third", NodeStatus.Failure);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSelectorAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Failure));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Selector_WhenChildSucceeds_ReturnsSuccess_AndDoesNotTickRemainingChildren()
        {
            var first = new RecordingLeaf("First", NodeStatus.Failure);
            var second = new RecordingLeaf("Second", NodeStatus.Success);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSelectorAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
        
        [Test]
        public void Selector_WhenChildIsRunning_ReturnsRunning_AndDoesNotTickFollowingChildren()
        {
            var first = new RecordingLeaf("First", NodeStatus.Failure);
            var second = new RecordingLeaf("Second", NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSelectorAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Running));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
        
        [Test]
        public void Selector_WhenEarlierChildBecomesRunningAfterLaterChildWasRunning_AbortsPreviouslyRunningChild()
        {
            var first = new RecordingLeaf("First", NodeStatus.Failure, NodeStatus.Running);
            var second = new RecordingLeaf("Second", NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSelectorAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Running));
            Assert.That(first.TickCount, Is.EqualTo(2));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(second.AbortCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
        
        [Test]
        public void Selector_WhenEarlierChildSucceedsAfterLaterChildWasRunning_AbortsPreviouslyRunningChild()
        {
            var first = new RecordingLeaf("First", NodeStatus.Failure, NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSelectorAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(first.TickCount, Is.EqualTo(2));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(second.AbortCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
    }
}