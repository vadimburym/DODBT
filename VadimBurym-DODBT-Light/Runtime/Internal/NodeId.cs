// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Light
{
    [Serializable]
    internal enum NodeId : byte
    {
        Leaf = 0,
        Selector = 1,
        Sequence = 2,
        MemorySelector = 3,
        MemorySequence = 4,
        Parallel = 5,
        Timeout = 6,
        Cooldown = 7,
        Inverter = 8
    }
}
