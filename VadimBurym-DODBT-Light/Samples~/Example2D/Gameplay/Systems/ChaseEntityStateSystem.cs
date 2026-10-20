using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Systems
{
    public sealed class ChaseEntityStateSystem : IEcsRunSystem
    {
        private const float INTERVAL = 0.2f;
        
        private readonly EcsFilterInject<Inc<ChaseEntityState>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<ChaseEntityState> _chasePlayerStatePool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<UnityNavMeshAgent> _navAgentPool;
        private readonly EcsPoolInject<UnityTransform> _transformPool;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;
                ref var chaseData = ref _chasePlayerStatePool.Value.Get(entity);
                chaseData.TickTime += Time.deltaTime;
                if (chaseData.TickTime >= INTERVAL)
                {
                    chaseData.TickTime -= INTERVAL;
                    if (chaseData.EntityIndex == -1)
                        return;
                    if (!_transformPool.Value.Has(chaseData.EntityIndex))
                        return;
                    var playerPosition = _transformPool.Value.Get(chaseData.EntityIndex).Ref.position;
                    var navAgent = _navAgentPool.Value.Get(agentIndex).Ref;
                    navAgent.SetDestination(playerPosition);
                }
            }
        }
    }
}
