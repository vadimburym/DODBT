using System;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.States;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Player.AiLeafs
{
    [Serializable]
    public sealed class RetreatPlayerLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        [SerializeField] private float _retreatDistance;
        
        private EcsWorld _world;
        private EcsPool<AgentEntity> _agentPool;
        private EcsPool<RetreatEntityState> _retreatStatePool;
        private EcsPool<NavigationState> _navStatePool;
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.BT_STATES);
            _agentPool = EcsWorlds.GetPool<AgentEntity>(EcsWorlds.BT_STATES);
            _retreatStatePool = EcsWorlds.GetPool<RetreatEntityState>(EcsWorlds.BT_STATES);
            _navStatePool = EcsWorlds.GetPool<NavigationState>(EcsWorlds.BT_STATES);
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            var status = _retreatStatePool.Get(state.StateIndex).StateStatus;
            return status == NodeStatus.None ? NodeStatus.Running : status;
        }

        public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            var playerIndex = _playerVisibilityPool.Get(context.AgentIndex).DetectedPlayer;
            state.StateIndex = _world.NewEntity();
            _agentPool.Add(state.StateIndex).AgentIndex = context.AgentIndex;
            _navStatePool.Add(state.StateIndex);
            _retreatStatePool.Add(state.StateIndex).Setup(
                entityIndex: playerIndex,
                retreatDistance: _retreatDistance);
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