// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using NUnit.Framework;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class NativeAuthoringTests
    {
        [Test]
        public void NodeMenu_OnlyContainsCoreDotsNodes()
        {
            Assert.That(System.Enum.GetNames(typeof(BtNodeKind)), Is.EqualTo(new[]
            { "None", "Root", "Leaf", "Sequence", "Selector", "MemorySequence", "MemorySelector", "Parallel" }));
        }
    }
}
