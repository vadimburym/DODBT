// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using UnityEngine;


namespace VadimBurym.DodBehaviourTree
{

    public sealed class BtMonoDebug : MonoBehaviour
    {

        internal BehaviourTreeAsset BehaviourTreeAsset;
#if UNITY_EDITOR

        public IReadOnlyList<string> RunningLeafs { get; private set; }
        internal NodeStatus[] DebugStatus;
        
        public void Construct(
            BehaviourTreeAsset behaviourTreeAsset,
            IBtDebugState targetState)
        {
            BehaviourTreeAsset = behaviourTreeAsset;
            DebugStatus = targetState.DebugStatus;
            RunningLeafs = targetState.DebugRunningLeafs;
        }
#endif
    }
}
