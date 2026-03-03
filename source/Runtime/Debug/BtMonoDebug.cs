// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace VadimBurym.DodBehaviourTree
{
#if ODIN_INSPECTOR
    [HideMonoScript]
#endif
    public sealed class BtMonoDebug : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly] 
#endif
        internal BehaviourTreeAsset BehaviourTreeAsset;
#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly] 
#endif
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
