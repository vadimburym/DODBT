// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    public sealed class BtState<TLeafState> 
#if UNITY_EDITOR
        : IBtDebugState
#endif
        where TLeafState : struct
    {
        [SerializeField] internal NodeState[] NodeStates = Array.Empty<NodeState>();
        public TLeafState[] LeafStates = Array.Empty<TLeafState>();
#if UNITY_EDITOR
        NodeStatus[] IBtDebugState.DebugStatus => DebugStatus;
        List<string> IBtDebugState.DebugRunningLeafs => DebugRunningLeafs;
        internal NodeStatus[] DebugStatus = Array.Empty<NodeStatus>();
        internal List<string> DebugRunningLeafs = new();
#endif
    }
}