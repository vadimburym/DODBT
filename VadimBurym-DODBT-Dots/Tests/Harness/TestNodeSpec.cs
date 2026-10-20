// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using System;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal abstract class TestNodeSpec
    {
        public static TestNodeSpec RecordingLeaf(string name, params NodeStatus[] statuses)
        {
            return new RecordingLeafSpec(name, statuses);
        }

        public static TestNodeSpec Selector(params TestNodeSpec[] children)
        {
            return new SelectorSpec(children);
        }

        public static TestNodeSpec Sequence(params TestNodeSpec[] children)
        {
            return new SequenceSpec(children);
        }

        public static TestNodeSpec MemorySelector(bool pickRandom, bool resetOnAbort, params TestNodeSpec[] children)
        {
            return new MemorySelectorSpec(children, pickRandom, resetOnAbort);
        }

        public static TestNodeSpec MemorySequence(bool resetOnFailure, bool resetOnAbort, params TestNodeSpec[] children)
        {
            return new MemorySequenceSpec(children, resetOnFailure, resetOnAbort);
        }

        public static TestNodeSpec Parallel(byte successThreshold, byte failsThreshold, bool cacheChildStatus, params TestNodeSpec[] children)
        {
            return new ParallelSpec(children, successThreshold, failsThreshold, cacheChildStatus);
        }
    }

    internal abstract class CompositeSpec : TestNodeSpec
    {
        protected CompositeSpec(TestNodeSpec[] children)
        {
            if (children == null || children.Length == 0)
                throw new ArgumentException("At least one child is required.", nameof(children));

            Children = children;
        }

        public TestNodeSpec[] Children { get; }
    }

    internal sealed class RecordingLeafSpec : TestNodeSpec
    {
        public RecordingLeafSpec(string name, NodeStatus[] statuses)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Leaf name is required.", nameof(name));
            if (statuses == null || statuses.Length == 0)
                throw new ArgumentException("At least one status is required.", nameof(statuses));

            Name = name;
            Statuses = statuses;
        }

        public string Name { get; }
        public NodeStatus[] Statuses { get; }
    }

    internal sealed class SelectorSpec : CompositeSpec
    {
        public SelectorSpec(TestNodeSpec[] children) : base(children) { }
    }

    internal sealed class SequenceSpec : CompositeSpec
    {
        public SequenceSpec(TestNodeSpec[] children) : base(children) { }
    }

    internal sealed class MemorySelectorSpec : CompositeSpec
    {
        public MemorySelectorSpec(TestNodeSpec[] children, bool pickRandom, bool resetOnAbort) : base(children)
        {
            PickRandom = pickRandom;
            ResetOnAbort = resetOnAbort;
        }

        public bool PickRandom { get; }
        public bool ResetOnAbort { get; }
    }

    internal sealed class MemorySequenceSpec : CompositeSpec
    {
        public MemorySequenceSpec(TestNodeSpec[] children, bool resetOnFailure, bool resetOnAbort) : base(children)
        {
            ResetOnFailure = resetOnFailure;
            ResetOnAbort = resetOnAbort;
        }

        public bool ResetOnFailure { get; }
        public bool ResetOnAbort { get; }
    }

    internal sealed class ParallelSpec : CompositeSpec
    {
        public ParallelSpec(TestNodeSpec[] children, byte successThreshold, byte failsThreshold, bool cacheChildStatus) : base(children)
        {
            SuccessThreshold = successThreshold;
            FailsThreshold = failsThreshold;
            CacheChildStatus = cacheChildStatus;
        }

        public byte SuccessThreshold { get; }
        public byte FailsThreshold { get; }
        public bool CacheChildStatus { get; }
    }
}
