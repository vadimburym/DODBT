using DODBT.Light.Example.BehaviourTree;
using System;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree.Leaves
{
    [Serializable]
    public sealed class ShootPlayerLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        [SerializeField] private int _targetShots;
        
        private EcsWorld _world;
        private EcsPool<AgentEntity> _agentPool;
        private EcsPool<ShootEntityState> _shootStatePool;
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.BT_STATES);
            _agentPool = EcsWorlds.GetPool<AgentEntity>(EcsWorlds.BT_STATES);
            _shootStatePool = EcsWorlds.GetPool<ShootEntityState>(EcsWorlds.BT_STATES);
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            var status = _shootStatePool.Get(state.StateIndex).StateStatus;
            return status == NodeStatus.None ? NodeStatus.Running : status;
        }

        public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            var playerIndex = _playerVisibilityPool.Get(context.AgentIndex).DetectedPlayer;
            state.StateIndex = _world.NewEntity();
            _agentPool.Add(state.StateIndex).AgentIndex = context.AgentIndex;
            _shootStatePool.Add(state.StateIndex).Setup(
                targetShots: _targetShots,
                entityIndex: playerIndex);
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
