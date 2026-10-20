// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class ParallelRuntimeTests
    {
        [Test]
        public void Parallel_WhenSuccessThresholdIsReached_ReturnsSuccess()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Parallel(
                    successThreshold: 2,
                    failsThreshold: 2,
                    cacheChildStatus: true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            var status = runner.Tick();

            Assert.That(status, Is.EqualTo(NodeStatus.Success));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(1));
        }

        [Test]
        public void Parallel_WhenFailsThresholdIsReached_ReturnsFailure()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Parallel(
                    3,
                    1,
                    false,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            var status = runner.Tick();
            Assert.That(status, Is.EqualTo(NodeStatus.Failure));
        }

        [Test]
        public void Parallel_WhenResultBecomesSuccess_AbortsRunningChildren()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Parallel(
                    successThreshold: 2,
                    failsThreshold: 3,
                    cacheChildStatus: false,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Running),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            var status = runner.Tick();

            Assert.That(status, Is.EqualTo(NodeStatus.Success));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("A").AbortCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(1));
        }

        [Test]
        public void Parallel_WithCacheChildStatusTrue_DoesNotRetickCompletedChildrenOnNextTick()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Parallel(
                    3,
                    3,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Running, NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            var firstTick = runner.Tick();
            var secondTick = runner.Tick();

            Assert.That(firstTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(secondTick, Is.EqualTo(NodeStatus.Success));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(1));
        }

        [Test]
        public void Parallel_WithCacheChildStatusFalse_ReticksCompletedChildrenOnNextTick()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Parallel(
                    3,
                    3,
                    false,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success, NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Running, NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success, NodeStatus.Success)));

            var firstTick = runner.Tick();
            var secondTick = runner.Tick();

            Assert.That(firstTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(secondTick, Is.EqualTo(NodeStatus.Success));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(2));
        }

        [Test]
        public void Parallel_Abort_WhenChildIsRunning_AbortsRunningLeaf_AndClearsCachedStatus()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Parallel(
                    3,
                    3,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success, NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Running, NodeStatus.Running),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success, NodeStatus.Success)));

            var firstTick = runner.Tick();
            runner.Abort();
            var secondTick = runner.Tick();

            Assert.That(firstTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(secondTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("B").AbortCount, Is.EqualTo(1));
        }
    }
}
