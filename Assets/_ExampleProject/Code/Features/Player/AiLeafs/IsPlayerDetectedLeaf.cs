using System;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Player.AiLeafs
{
    [Serializable]
    public sealed class IsPlayerDetectedLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            return _playerVisibilityPool.Get(context.AgentIndex).IsPlayerDetected ? NodeStatus.Success : NodeStatus.Failure;
        }

        public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state)
        {
        }

        public void OnExit(LeoEcsContext context, ref LeoEcsLeafState state, NodeStatus exitStatus)
        {
        }

        public void OnAbort(LeoEcsContext context,ref LeoEcsLeafState state)
        {
        }
    }
}