// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using NUnit.Framework;
using System.Linq;
using VadimBurym.DodBehaviourTree.Dots.Generated;
using Random = Unity.Mathematics.Random;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class RuntimeFixTests
    {
        private static TestNodeSpec Leaf() => TestNodeSpec.RecordingLeaf("leaf", NodeStatus.Success);

        [Test]
        public void BenchmarkLayout_ExecutesAllThirtyLeavesAndUsesContiguousChildren()
        {
            var branches = Enumerable.Range(0, 3).Select(_ => TestNodeSpec.MemorySequence(true, true,
                Enumerable.Range(0, 10).Select(i => Leaf()).ToArray())).ToArray();
            using var f = new ProductionFixture(TestNodeSpec.Parallel(3, 1, false, branches));
            Assert.That(f.Tick(), Is.EqualTo(NodeStatus.Success));
            for (int i = 0; i < 30; i++) Assert.That(f.Context.Events[i * 4 + 1], Is.EqualTo(1), "Leaf " + i);
            for (int i = 1; i <= 3; i++) Assert.That(f.Blob.Value.Nodes[i].ParentIndex, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RandomSelector_AllFailuresVisitEveryChildOnce(bool debug)
        {
            for (uint seed = 1; seed <= 16; seed++)
            {
                using var f = new ProductionFixture(TestNodeSpec.MemorySelector(true, true, Leaf(), Leaf(), Leaf()), debug);
                f.Random = new Random(seed * 12345);
                for (int i = 0; i < 3; i++) f.Context.Outcomes[i] = NodeStatus.Failure;
                Assert.That(f.Tick(debug), Is.EqualTo(NodeStatus.Failure));
                for (int i = 0; i < 3; i++) Assert.That(f.Context.Events[i * 4 + 1], Is.EqualTo(1));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RandomSelector_RunningAndAbortPreserveOrResetTheTraversal(bool resetOnAbort)
        {
            using var f = new ProductionFixture(TestNodeSpec.MemorySelector(true, resetOnAbort, Leaf(), Leaf(), Leaf()));
            var expected = f.Random;
            int start = expected.NextInt(0, 3);
            for (int i = 0; i < 3; i++) f.Context.Outcomes[i] = NodeStatus.Failure;
            f.Context.Outcomes[start] = NodeStatus.Running;
            Assert.That(f.Tick(), Is.EqualTo(NodeStatus.Running));
            Assert.That(f.Nodes[0].TmpA, Is.EqualTo(start));
            Assert.That(f.Random.state, Is.EqualTo(expected.state));
            new BTRunner_AuditContext().Abort(f.Entity, ref f.Blob.Value, ref f.Random, f.Nodes, f.Leafs, f.Context, 0);
            Assert.That(f.Nodes[0].MemoryCursor, Is.EqualTo(resetOnAbort ? 255 : start));
            f.Context.Outcomes[start] = NodeStatus.Failure;
            if (resetOnAbort) expected.NextInt(0, 3);
            Assert.That(f.Tick(), Is.EqualTo(NodeStatus.Failure));
            Assert.That(f.Random.state, Is.EqualTo(expected.state));
            for (int i = 0; i < 3; i++) Assert.That(f.Context.Events[i * 4 + 1], Is.EqualTo(i == start ? 2 : 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NestedCachedParallel_ReturnsRunningWithoutLooping(bool debug)
        {
            using var f = new ProductionFixture(TestNodeSpec.Sequence(
                TestNodeSpec.Parallel(2, 2, true, Leaf(), Leaf()), Leaf()), debug);
            // Breadth-first packing assigns the final sequence leaf index 0.
            f.Context.Outcomes[1] = NodeStatus.Success;
            f.Context.Outcomes[2] = NodeStatus.Failure;
            Assert.That(f.Tick(debug), Is.EqualTo(NodeStatus.Running));
            Assert.That(f.Tick(debug), Is.EqualTo(NodeStatus.Running));
            Assert.That(f.Context.Events[1], Is.Zero);
            Assert.That(f.Context.Events[5], Is.EqualTo(1));
            Assert.That(f.Context.Events[9], Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompletedParallel_KeepsTheStateResetByItsAbort(bool debug)
        {
            using var f = new ProductionFixture(TestNodeSpec.Parallel(1, 2, true, Leaf(), Leaf()), debug);
            f.Context.Outcomes[0] = NodeStatus.Running;
            Assert.That(f.Tick(debug), Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Nodes[0].Cursor, Is.EqualTo(byte.MaxValue));
            Assert.That(f.Nodes[0].TmpA, Is.Zero);
            Assert.That(f.Nodes[0].TmpB, Is.Zero);
            Assert.That(f.Context.Events[3], Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RandomState_FlowsThroughEveryLifecycleCallback(bool debug)
        {
            using var f = new ProductionFixture(Leaf(), debug);
            f.Context.UseRandom = 15;
            var expected = f.Random;
            expected.NextUInt(); expected.NextUInt(); expected.NextUInt();
            Assert.That(f.Tick(debug), Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Random.state, Is.EqualTo(expected.state));
            f.Context.Outcomes[0] = NodeStatus.Running;
            expected.NextUInt(); expected.NextUInt();
            Assert.That(f.Tick(debug), Is.EqualTo(NodeStatus.Running));
            uint abortValue = expected.NextUInt();
            new BTRunner_AuditContext().Abort(f.Entity, ref f.Blob.Value, ref f.Random, f.Nodes, f.Leafs, f.Context, 0);
            Assert.That(f.Random.state, Is.EqualTo(expected.state));
            Assert.That(f.Context.RandomValues[0], Is.EqualTo(abortValue));
        }
    }
}
