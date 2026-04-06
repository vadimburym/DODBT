// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

internal enum BtNodeKind
{
    None = 0,
    Root = 1,
    Leaf = 2,
    Sequence = 3,
    Selector = 4,
    MemorySequence = 5,
    MemorySelector = 6,
    Parallel = 7
}