using DODBT.Light.Example.BehaviourTree;
using System;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree.Leaves
{
    [Serializable]
    public sealed class IsPlayerRayCastLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        [SerializeField] private bool _isNot;
        
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            if (!_isNot)
                return _playerVisibilityPool.Get(context.AgentIndex).IsPlayerRaycast ? NodeStatus.Success : NodeStatus.Failure;
            else
                return _playerVisibilityPool.Get(context.AgentIndex).IsPlayerRaycast ? NodeStatus.Failure : NodeStatus.Success;
        }

        public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state)
        {
        }

        public void OnExit(LeoEcsContext context, ref LeoEcsLeafState state, NodeStatus exitStatus)
        {
        }

        public void OnAbort(LeoEcsContext context, ref LeoEcsLeafState state)
        {
        }
    }
}
