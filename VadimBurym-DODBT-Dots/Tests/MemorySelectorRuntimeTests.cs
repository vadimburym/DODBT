// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class MemorySelectorRuntimeTests
    {
        [Test]
        public void MemorySelector_WhenAllChildrenFail_TickAllChildren_AndReturnsFailure()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.MemorySelector(
                    false,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Failure)));

            var status = runner.Tick();

            Assert.That(status, Is.EqualTo(NodeStatus.Failure));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(1));
        }

        [Test]
        public void MemorySelector_WhenChildSucceeds_DoesNotTickRemainingChildren_AndReturnsSuccess()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.MemorySelector(
                    false,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            var status = runner.Tick();

            Assert.That(status, Is.EqualTo(NodeStatus.Success));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(0));
        }

        [Test]
        public void MemorySelector_WhenChildIsRunning_DoesNotTickRemainingChildren_AndReturnsRunning()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.MemorySelector(
                    false,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Running),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            var status = runner.Tick();

            Assert.That(status, Is.EqualTo(NodeStatus.Running));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(0));
        }

        [Test]
        public void MemorySelector_Abort_WhenLeafIsRunning_AbortsRunningChild()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.MemorySelector(
                    false,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Running),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            runner.Tick();
            runner.Abort();

            Assert.That(runner.Recording("A").AbortCount, Is.EqualTo(0));
            Assert.That(runner.Recording("B").AbortCount, Is.EqualTo(1));
            Assert.That(runner.Recording("C").AbortCount, Is.EqualTo(0));
        }

        [Test]
        public void MemorySelector_WhenChildRunning_ResumesFromRememberedLeafOnNextTick()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.MemorySelector(
                    false,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Running, NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)));

            var firstTick = runner.Tick();
            var secondTick = runner.Tick();

            Assert.That(firstTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(secondTick, Is.EqualTo(NodeStatus.Success));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").EnterCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("C").TickCount, Is.EqualTo(0));
        }

        [Test]
        public void MemorySelector_WithPickRandomTrue_UsesDeterministicSeededChoice()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.MemorySelector(
                    true,
                    true,
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Success)),
                12345);

            var status = runner.Tick();
            var totalTicks = runner.Recording("A").TickCount + runner.Recording("B").TickCount + runner.Recording("C").TickCount;

            Assert.That(status, Is.EqualTo(NodeStatus.Success));
            Assert.That(totalTicks, Is.EqualTo(1));
        }

        [Test]
        public void MemorySelector_WithResetOnAbortFalse_ResumesRememberedLeafAfterAbort()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Selector(
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Failure, NodeStatus.Success, NodeStatus.Failure),
                    TestNodeSpec.MemorySelector(
                        pickRandom: false,
                        resetOnAbort: false,
                        TestNodeSpec.RecordingLeaf("B0", NodeStatus.Failure),
                        TestNodeSpec.RecordingLeaf("B1", NodeStatus.Running, NodeStatus.Running))));

            var firstTick = runner.Tick();
            var secondTick = runner.Tick();
            var thirdTick = runner.Tick();

            Assert.That(firstTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(secondTick, Is.EqualTo(NodeStatus.Success));
            Assert.That(thirdTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(runner.Recording("B0").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("B1").EnterCount, Is.EqualTo(2));
            Assert.That(runner.Recording("B1").AbortCount, Is.EqualTo(1));
        }

        [Test]
        public void MemorySelector_WithResetOnAbortTrue_RestartsFromFirstChildAfterAbort()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.Selector(
                    TestNodeSpec.RecordingLeaf("A",
                        NodeStatus.Failure,
                        NodeStatus.Failure,
                        NodeStatus.Success,
                        NodeStatus.Failure),
                    TestNodeSpec.MemorySelector(
                        pickRandom: false,
                        resetOnAbort: true,
                        TestNodeSpec.RecordingLeaf("B0",
                            NodeStatus.Failure,
                            NodeStatus.Failure),
                        TestNodeSpec.RecordingLeaf("B1",
                            NodeStatus.Running,
                            NodeStatus.Running,
                            NodeStatus.Running))));

            var firstTick = runner.Tick();
            var secondTick = runner.Tick();
            var thirdTick = runner.Tick();
            var fourthTick = runner.Tick();

            Assert.That(firstTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(secondTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(thirdTick, Is.EqualTo(NodeStatus.Success));
            Assert.That(fourthTick, Is.EqualTo(NodeStatus.Running));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(4));
            Assert.That(runner.Recording("B0").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("B1").EnterCount, Is.EqualTo(2));
            Assert.That(runner.Recording("B1").TickCount, Is.EqualTo(3));
            Assert.That(runner.Recording("B1").AbortCount, Is.EqualTo(1));
        }
    }
}
