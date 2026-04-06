// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree
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