using System;
using _ExampleProject.Code.Features._Core.Components;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.AiLeafs
{
    [Serializable]
    public sealed class PatrolLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        private EcsWorld _world;
        private EcsPool<AgentEntity> _agentPool;
        private EcsPool<PatrolState> _patrolStatePool;
        private EcsPool<NavigationState> _navStatePool;
        
        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.BT_STATES);
            _patrolStatePool = EcsWorlds.GetPool<PatrolState>(EcsWorlds.BT_STATES);
            _navStatePool = EcsWorlds.GetPool<NavigationState>(EcsWorlds.BT_STATES);
            _agentPool = EcsWorlds.GetPool<AgentEntity>(EcsWorlds.BT_STATES);
        }

        public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            return NodeStatus.Running;
        }

        public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            state.StateIndex = _world.NewEntity();
            _agentPool.Add(state.StateIndex).AgentIndex = context.AgentIndex;
            _navStatePool.Add(state.StateIndex);
            _patrolStatePool.Add(state.StateIndex).Reset();
        }

        public void OnExit(LeoEcsContext context, ref LeoEcsLeafState state, NodeStatus exitStatus)
        {
            _world.DelEntity(state.StateIndex);
        }

        public void OnAbort(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            _world.DelEntity(state.StateIndex);
        }
    }
}