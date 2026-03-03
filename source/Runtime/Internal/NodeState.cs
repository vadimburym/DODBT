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
        [SerializeField] internal int Cursor;
        [SerializeField] internal sbyte CachedStatus;
        [SerializeField] internal int LeafStateIndex;
        
        internal void Reset()
        {
            IsEntered = false;
            Cursor = -1;
            CachedStatus = 0;
            LeafStateIndex = -1;
        }
    }
}