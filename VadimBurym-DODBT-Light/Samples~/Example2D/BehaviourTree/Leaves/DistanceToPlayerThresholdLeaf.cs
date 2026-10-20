using DODBT.Light.Example.BehaviourTree;
using System;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using UnityEngine;
using DODBT.Light.Example.BehaviourTree.Comparisons;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree.Leaves
{
    [Serializable]
    public sealed class DistanceToPlayerThresholdLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        [SerializeField] private float _threshold;
        [SerializeReference] private IComparison _comparison;
        
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            return _comparison.CompareFloat(
                _playerVisibilityPool.Get(context.AgentIndex).SqrDistanceToPlayer,
                _threshold * _threshold) ? NodeStatus.Success : NodeStatus.Failure;
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
