// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Dots
{
    [Serializable]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct Node
    {
        public NodeId Id;
#if DODBT_SMALL_SIZE
        public byte DataIndex;
        public byte ParentIndex;
#else
        public ushort DataIndex;
        public ushort ParentIndex;
#endif
    }
}
