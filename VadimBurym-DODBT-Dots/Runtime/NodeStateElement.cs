// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using Unity.Entities;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots
{
    [Serializable]
    [InternalBufferCapacity(0)]
    public struct NodeStateElement : IBufferElementData
    {
#if UNITY_EDITOR
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public NodeStatus DebugCachedStatus => (NodeStatus)CachedStatus;
#endif
        [SerializeField] internal byte Cursor;
        [SerializeField] internal byte MemoryCursor;
        [SerializeField] internal byte CachedStatus;
        [SerializeField] internal byte TmpA;
        [SerializeField] internal byte TmpB;
#if DODBT_SMALL_SIZE
        [SerializeField] public byte LeafStateIndex;
#else
        [SerializeField] public ushort LeafStateIndex;
#endif
    }
}
