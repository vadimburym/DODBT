// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public sealed class MemorySequenceRuntimeTests
    {
        [Test]
        public void MemorySequence_WhenAllChildrenSucceed_ReturnsSuccess()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Success);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateMemorySequenceAsset(resetOnFailure: true, resetOnAbort: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(1));
        }
        
        [Test]
        public void MemorySequence_WhenChildFails_ReturnsFailure_AndDoesNotTickRemainingChildren()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Failure);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateMemorySequenceAsset(resetOnFailure: true, resetOnAbort: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Failure));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
        
        [Test]
        public void MemorySequence_WhenChildIsRunning_ReturnsRunning_AndDoesNotTickFollowingChildren()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateMemorySequenceAsset(resetOnFailure: true, resetOnAbort: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Running));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
        
        [Test]
        public void MemorySequence_WhenRunningChildSucceeds_ContinuesToNextChildOnSameTick()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Success);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateMemorySequenceAsset(resetOnFailure: true, resetOnAbort: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(2));
            Assert.That(third.TickCount, Is.EqualTo(1));
        }
        
        [Test]
        public void MemorySequence_WhenResetOnFailureIsDisabled_RetriesFailedChildWithoutRetickingPreviousSuccesses()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Failure, NodeStatus.Success);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateMemorySequenceAsset(resetOnFailure: false, resetOnAbort: true, first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(2));
            Assert.That(third.TickCount, Is.EqualTo(1));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }
        
        [Test]
        public void MemorySequence_WhenResetOnAbortIsEnabled_RestartsFromFirstChildAfterAbort()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success, NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(
                TestTreeFactory.CreateMemorySequenceAsset(resetOnFailure: true, resetOnAbort: true, first, second));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Abort(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(2));
            Assert.That(second.EnterCount, Is.EqualTo(2));
            Assert.That(second.AbortCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(2));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }
        
        [Test]
        public void MemorySequence_WhenChildIsRunning_ResumesFromCursorWithoutRetickingPreviousChildren()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateMemorySequenceAsset(
                resetOnFailure: true,
                resetOnAbort: true,
                first, second));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.EnterCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(2));
            Assert.That(second.ExitCount, Is.EqualTo(1));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }

        [Test]
        public void MemorySequence_WhenResetOnFailureIsEnabled_RestartsFromFirstChildOnNextTick()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success, NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Failure, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateMemorySequenceAsset(
                resetOnFailure: true,
                resetOnAbort: true,
                first, second));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(2));
            Assert.That(second.EnterCount, Is.EqualTo(2));
            Assert.That(second.ExitCount, Is.EqualTo(2));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }

        [Test]
        public void MemorySequence_WhenResetOnAbortIsDisabled_ResumesCurrentChildAfterAbort()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateMemorySequenceAsset(
                resetOnFailure: true,
                resetOnAbort: false,
                first, second));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Abort(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.EnterCount, Is.EqualTo(2));
            Assert.That(second.AbortCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(2));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }
    }
}