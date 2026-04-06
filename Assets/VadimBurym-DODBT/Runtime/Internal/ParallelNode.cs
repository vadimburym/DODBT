// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    internal struct ParallelNode
    {
#if DODBT_SMALL_SIZE
        public byte FirstChild;
#else
        public ushort FirstChild;
#endif
        public byte ChildCount;
        public byte FailsThreshold;
        public byte SuccessThreshold;
        public bool CacheChildStatus;
    }
}