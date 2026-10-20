using DODBT.Light.Example.BehaviourTree;
using System;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree.Leaves
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
