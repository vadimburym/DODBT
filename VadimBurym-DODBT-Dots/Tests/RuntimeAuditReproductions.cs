// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using NUnit.Framework;
using VadimBurym.DodBehaviourTree.Dots.Generated;
using Unity.Mathematics;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    // Regression cases reproduced against the original simulator runtime.
    public sealed class RuntimeAuditReproductions
    {
        [Test]
        public void RandomSelector_MustNotSkipTheOnlySuccessfulChild()
        {
            for (int seed = 1; seed < 64; seed++)
            {
                using var runner = TestTreeFactory.CreateRunner(TestNodeSpec.MemorySelector(true, true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Failure)), seed * 12345);
                Assert.That(runner.Tick(), Is.EqualTo(NodeStatus.Success), "Seed " + seed + " skipped the successful child.");
            }
        }

        [Test]
        public void LeafRandomState_MustBeReturnedToTheCaller()
        {
            using var fixture = new ProductionFixture(TestNodeSpec.RecordingLeaf("A", NodeStatus.Success));
            fixture.Context.UseRandom = 1;
            uint before = fixture.Random.state;
            fixture.Tick();
            Assert.That(fixture.Random.state, Is.Not.EqualTo(before));
        }

        [Test]
        public void CachedParallel_MustReturnToParentWhenEveryChildIsCached()
        {
            using var fixture = new ProductionFixture(TestNodeSpec.Parallel(2, 2, true,
                TestNodeSpec.RecordingLeaf("A", NodeStatus.Success), TestNodeSpec.RecordingLeaf("B", NodeStatus.Failure)));
            fixture.Context.Outcomes[1] = NodeStatus.Failure;
            Assert.That(fixture.Tick(), Is.EqualTo(NodeStatus.Running));
            int pc = 0;
            var status = NodeStatus.None;
            bool returning = false;
            ushort abort = ushort.MaxValue;
            var state = fixture.Nodes[0];
            var node = fixture.Blob.Value.Nodes[0];
            var nodes = fixture.Nodes;
            NodeExecutor.ExecuteParallel(ref pc, ref status, ref returning, ref abort, ref state, ref node, ref fixture.Blob.Value, ref nodes);
            Assert.That(returning, Is.True, "A nested parent would otherwise enter the cached parallel repeatedly in the same tick.");
        }
    }
}
