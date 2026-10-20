// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{

    internal enum BtNodeKind
    {
        None = 0,
        Root = 1,
        Leaf = 2,
        Sequence = 3,
        Selector = 4,
        MemorySequence = 5,
        MemorySelector = 6,
        Parallel = 7,
    }
}
