// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Light
{
    [Serializable]
    internal struct MemorySequenceNode
    {
#if DODBT_SMALL_SIZE
        public byte FirstChild;
#else
        public ushort FirstChild;
#endif
        public byte ChildCount;
        public bool ResetOnFailure;
        public bool ResetOnAbort;
    }
}