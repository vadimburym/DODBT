using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace DODBT.Light.Example.Gameplay.Systems
{
    public sealed class NavigationStateSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<NavigationState>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<UnityNavMeshAgent> _navAgentPool;
        private readonly EcsPoolInject<UnityRigidbody> _rigidbodyPool;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;
                ref var navAgent = ref _navAgentPool.Value.Get(agentIndex).Ref;
                ref var rigidbody = ref _rigidbodyPool.Value.Get(agentIndex).Ref;
                rigidbody.linearVelocity = navAgent.desiredVelocity;
                navAgent.nextPosition = rigidbody.position;
            }
        }
    }
}
