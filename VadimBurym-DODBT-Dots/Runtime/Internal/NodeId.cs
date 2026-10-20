// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Dots
{
    [Serializable]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum NodeId : byte
    {
        Leaf = 0,
        Selector = 1,
        Sequence = 2,
        MemorySelector = 3,
        MemorySequence = 4,
        Parallel = 5
    }
}
