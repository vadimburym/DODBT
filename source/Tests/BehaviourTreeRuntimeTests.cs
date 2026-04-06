// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public sealed class BehaviourTreeRuntimeTests
    {
        [Test]
        public void Construct_WhenLeafGenericTypeDoesNotMatch_ThrowsInvalidOperationException()
        {
            var asset = TestTreeFactory.CreateLeafAsset(new WrongContextLeaf(NodeStatus.Success));
            var tree = new BehaviourTree<TestContext, TestLeafState>();

            Assert.Throws<InvalidOperationException>(() => tree.Construct(asset));
        }

        [Test]
        public void FillInitialState_AllocatesPowerOfTwoBuffers_AndAssignsLeafStateIndices()
        {
            var asset = TestTreeFactory.CreateSelectorAsset(
                new RecordingLeaf("A", NodeStatus.Failure),
                new RecordingLeaf("B", NodeStatus.Failure),
                new RecordingLeaf("C", NodeStatus.Success));

            var tree = TestTreeFactory.CreateTree(asset);
            var state = TestTreeFactory.CreateInitializedState(tree);

            Assert.That(state.NodeStates.Length, Is.EqualTo(4));
            Assert.That(state.LeafStates.Length, Is.EqualTo(4));
            Assert.That((int)state.NodeStates[1].LeafStateIndex, Is.EqualTo(0));
            Assert.That((int)state.NodeStates[2].LeafStateIndex, Is.EqualTo(1));
            Assert.That((int)state.NodeStates[3].LeafStateIndex, Is.EqualTo(2));
        }

        [Test]
        public void Leaf_WhenReturningSuccess_EntersTicksAndExitsOnEachTick()
        {
            var leaf = new RecordingLeaf("Leaf", NodeStatus.Success, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateLeafAsset(leaf));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Tick(context, state);

            Assert.That(leaf.EnterCount, Is.EqualTo(2));
            Assert.That(leaf.TickCount, Is.EqualTo(2));
            Assert.That(leaf.ExitCount, Is.EqualTo(2));
            Assert.That(leaf.AbortCount, Is.EqualTo(0));
            Assert.That(state.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
        }

        [Test]
        public void Abort_WhenLeafIsRunning_CallsOnAbort_AndNextTickStartsFromScratch()
        {
            var leaf = new RecordingLeaf("Leaf", NodeStatus.Running, NodeStatus.Success);
            var tree = TestTreeFactory.CreateTree(TestTreeFactory.CreateLeafAsset(leaf));
            var context = new TestContext();
            var state = TestTreeFactory.CreateInitializedState(tree);

            tree.Tick(context, state);
            tree.Abort(context, state);
            tree.Tick(context, state);

            Assert.That(leaf.EnterCount, Is.EqualTo(2));
            Assert.That(leaf.TickCount, Is.EqualTo(2));
            Assert.That(leaf.AbortCount, Is.EqualTo(1));
            Assert.That(leaf.ExitCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(
                new[]
                {
                    "enter:Leaf",
                    "tick:Leaf:Running",
                    "abort:Leaf",
                    "enter:Leaf",
                    "tick:Leaf:Success",
                    "exit:Leaf:Success"
                },
                context.Events);
        }
    }
}
