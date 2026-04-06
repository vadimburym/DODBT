// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    internal enum NodeId : byte
    {
        Leaf = 0,
        Selector = 1,
        Sequence = 2,
        MemorySelector = 3,
        MemorySequence = 4,
        Parallel = 5
    }
}