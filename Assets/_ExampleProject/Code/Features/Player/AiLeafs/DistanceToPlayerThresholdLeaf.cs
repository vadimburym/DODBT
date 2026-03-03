using System;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;
using Utils;
using VadimBurym.DodBehaviourTree;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace _ExampleProject.Code.Features.Player.AiLeafs
{
    [Serializable]
    public sealed class DistanceToPlayerThresholdLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        [SerializeField] private float _threshold;
#if ODIN_INSPECTOR
[HideLabel]
#endif
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