// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
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
        Timeout = 8,
        Cooldown = 9,
        Inverter = 10
    }

    internal static class BtNodeKinds
    {
        internal static bool IsDecorator(BtNodeKind kind) => kind >= BtNodeKind.Timeout && kind <= BtNodeKind.Inverter;
    }
}
