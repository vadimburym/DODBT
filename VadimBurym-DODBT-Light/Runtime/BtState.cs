// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light
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
#if DODBT_DECORATORS
        internal DecoratorState[] DecoratorStates = Array.Empty<DecoratorState>();
        internal double TickTime;
        internal bool HasTickTime;
#endif
#if UNITY_EDITOR
        NodeStatus[] IBtDebugState.DebugStatus => DebugStatus;
        List<string> IBtDebugState.DebugRunningLeafs => DebugRunningLeafs;
        internal NodeStatus[] DebugStatus = Array.Empty<NodeStatus>();
        internal List<string> DebugRunningLeafs = new();
        internal List<int> DebugRunningLeafIndices = new();
#endif
    }
}
