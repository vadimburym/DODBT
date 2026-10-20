// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light
{
    [Serializable]
    internal struct NodeState
    {
        [SerializeField] internal bool IsEntered;
        [SerializeField] internal byte CachedStatus;
        [SerializeField] internal byte Cursor;
#if DODBT_SMALL_SIZE
        [SerializeField] internal byte LeafStateIndex;
#else
        [SerializeField] internal ushort LeafStateIndex;
#endif
        
        internal void Reset()
        {
            IsEntered = false;
            Cursor = 0xFF;
            CachedStatus = 0;
        }
    }
}