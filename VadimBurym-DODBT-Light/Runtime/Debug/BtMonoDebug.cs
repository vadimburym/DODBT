// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.Collections.Generic;
using UnityEngine;


namespace VadimBurym.DodBehaviourTree.Light
{

    public sealed class BtMonoDebug : MonoBehaviour
    {

        internal BehaviourTreeAsset BehaviourTreeAsset;
#if UNITY_EDITOR

        /// <summary>Connects a scene object to its initialized tree state. Call once after FillInitialState,
        /// and again when reusing the object with another tree or state.</summary>
        public static BtMonoDebug Attach(
            GameObject owner,
            BehaviourTreeAsset behaviourTreeAsset,
            IBtDebugState targetState)
        {
            if (owner == null) throw new System.ArgumentNullException(nameof(owner));
            if (behaviourTreeAsset == null) throw new System.ArgumentNullException(nameof(behaviourTreeAsset));
            if (targetState == null) throw new System.ArgumentNullException(nameof(targetState));

            var debug = owner.GetComponent<BtMonoDebug>();
            if (debug == null) debug = owner.AddComponent<BtMonoDebug>();
            debug.Construct(behaviourTreeAsset, targetState);
            return debug;
        }

        public IReadOnlyList<string> RunningLeafs { get; private set; }
        internal NodeStatus[] DebugStatus;
        internal string CompiledRevision;

        /// <summary>Disconnects an agent before returning its state to a pool.</summary>
        public void Detach()
        {
            BehaviourTreeAsset = null;
            CompiledRevision = null;
            DebugStatus = null;
            RunningLeafs = null;
        }
        
        public void Construct(
            BehaviourTreeAsset behaviourTreeAsset,
            IBtDebugState targetState)
        {
            if (behaviourTreeAsset == null) throw new System.ArgumentNullException(nameof(behaviourTreeAsset));
            if (targetState == null) throw new System.ArgumentNullException(nameof(targetState));
            BehaviourTreeAsset = behaviourTreeAsset;
            CompiledRevision = behaviourTreeAsset != null ? behaviourTreeAsset.CompiledRevision : null;
            DebugStatus = targetState.DebugStatus;
            RunningLeafs = targetState.DebugRunningLeafs;
        }
#endif
    }
}
