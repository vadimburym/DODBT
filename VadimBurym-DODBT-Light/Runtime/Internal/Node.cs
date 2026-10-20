// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Light
{
    [Serializable]
    internal struct Node
    {
        public NodeId Id;
#if DODBT_SMALL_SIZE
        public byte DataIndex;
#else
        public ushort DataIndex;
#endif
    }
}