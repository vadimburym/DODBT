// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.Linq;
using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public sealed class SequenceRuntimeTests
    {
        [Test]
        public void Sequence_WhenAllChildrenSucceed_ReturnsSuccess()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Success);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSequenceAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Sequence_WhenChildFails_ReturnsFailure_AndDoesNotTickRemainingChildren()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Failure);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSequenceAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Failure));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
        
        [Test]
        public void Sequence_WhenChildIsRunning_ReturnsRunning_AndDoesNotTickFollowingChildren()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success);
            var second = new RecordingLeaf("Second", NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSequenceAsset(first, second, third));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Running));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(third.TickCount, Is.EqualTo(0));
        }
        
        [Test]
        public void Sequence_WhenEarlierChildBecomesRunningAfterLaterChildWasRunning_AbortsPreviouslyRunningChild()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success, NodeStatus.Running);
            var second = new RecordingLeaf("Second", NodeStatus.Running);
            var third = new RecordingLeaf("Third", NodeStatus.Success);

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSequenceAsset(first, second, third));
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
        public void Sequence_WhenEarlierChildFailsAfterLaterChildWasRunning_AbortsRunningChild()
        {
            var first = new RecordingLeaf("First", NodeStatus.Success, NodeStatus.Failure);
            var second = new RecordingLeaf("Second", NodeStatus.Running, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSequenceAsset(first, second));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(first.TickCount, Is.EqualTo(2));
            Assert.That(second.TickCount, Is.EqualTo(1));
            Assert.That(second.AbortCount, Is.EqualTo(1));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Failure));
            CollectionAssert.AreEqual(
                new[]
                {
                    "enter:First",
                    "tick:First:Success",
                    "exit:First:Success",
                    "enter:Second",
                    "tick:Second:Running",
                    "enter:First",
                    "tick:First:Failure",
                    "exit:First:Failure",
                    "abort:Second"
                },
                context.Events);
        }
        
        [Test]
        public void Sequence_With255Children_EvaluatesEveryChild_AndSupportsLargeCompositeWidth()
        {
            var leafs = Enumerable.Range(0, 255)
                .Select(i => new RecordingLeaf("Leaf_" + i, NodeStatus.Success))
                .ToArray();

            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateSequenceAsset(leafs));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);

            Assert.That(leafs.All(x => x.TickCount == 1), Is.True);
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }
    }
}