// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class LeafsRuntimeTests
    {
        [Test]
        public void Leaf_WhenStatusIsRunning_EnterIsCalledOnce_AndDoesNotCallExitBeforeCompletion()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.RecordingLeaf("A",
                    NodeStatus.Running,
                    NodeStatus.Running,
                    NodeStatus.Success));

            runner.Tick();
            runner.Tick();
            runner.Tick();

            Assert.That(runner.Recording("A").EnterCount, Is.EqualTo(1));
            Assert.That(runner.Recording("A").ExitCount, Is.EqualTo(1));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(3));

            CollectionAssert.AreEqual(new[] {
                    "enter:A",
                    "tick:A:Running",
                    "tick:A:Running",
                    "tick:A:Success",
                    "exit:A" },
                runner.Events);
        }

        [Test]
        public void Leaf_WhenStatusIsNotRunning_CallsEnterAndExitEveryTick()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.RecordingLeaf("A",
                    NodeStatus.Success,
                    NodeStatus.Failure,
                    NodeStatus.Success));

            runner.Tick();
            runner.Tick();
            runner.Tick();

            Assert.That(runner.Recording("A").EnterCount, Is.EqualTo(3));
            Assert.That(runner.Recording("A").ExitCount, Is.EqualTo(3));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(3));
        }

        [Test]
        public void Leaf_WhenAbortedWhileRunning_CallsAbort_AndDoesNotCallExit()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.RecordingLeaf("A",
                    NodeStatus.Running));

            runner.Tick();
            runner.Abort();

            Assert.That(runner.Recording("A").EnterCount, Is.EqualTo(1));
            Assert.That(runner.Recording("A").ExitCount, Is.EqualTo(0));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(1));
            Assert.That(runner.Recording("A").AbortCount, Is.EqualTo(1));

            CollectionAssert.AreEqual(new[] {
                    "enter:A",
                    "tick:A:Running",
                    "abort:A" },
                runner.Events);
        }

        [Test]
        public void Leaf_WhenTickedAfterAbort_Reenters()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.RecordingLeaf("A",
                    NodeStatus.Running,
                    NodeStatus.Running));

            runner.Tick();
            runner.Abort();
            runner.Tick();

            Assert.That(runner.Recording("A").EnterCount, Is.EqualTo(2));
            Assert.That(runner.Recording("A").ExitCount, Is.EqualTo(0));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("A").AbortCount, Is.EqualTo(1));

            CollectionAssert.AreEqual(new[] {
                    "enter:A",
                    "tick:A:Running",
                    "abort:A",
                    "enter:A",
                    "tick:A:Running" },
                runner.Events);
        }

        [Test]
        public void Leaf_WhenAbortedAfterCompletion_DoesNotCallAbort()
        {
            using var runner = TestTreeFactory.CreateRunner(
                TestNodeSpec.RecordingLeaf("A",
                    NodeStatus.Running,
                    NodeStatus.Failure));

            runner.Tick();
            runner.Tick();
            runner.Abort();

            Assert.That(runner.Recording("A").EnterCount, Is.EqualTo(1));
            Assert.That(runner.Recording("A").ExitCount, Is.EqualTo(1));
            Assert.That(runner.Recording("A").TickCount, Is.EqualTo(2));
            Assert.That(runner.Recording("A").AbortCount, Is.EqualTo(0));

            CollectionAssert.AreEqual(new[] {
                    "enter:A",
                    "tick:A:Running",
                    "tick:A:Failure",
                    "exit:A" },
                runner.Events);
        }
    }
}
