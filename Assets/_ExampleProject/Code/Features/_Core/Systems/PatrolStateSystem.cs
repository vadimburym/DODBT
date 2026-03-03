using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Enemy.Behaviours;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;
using Utils;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class PatrolStateSystem : IEcsRunSystem, IEcsInitSystem
    {
        private const float EPS = 0.5f;
        private const float NEAREST_RADIUS = 2f;
        private const float NEAREST_TIME_OUT = 4f;
        
        private readonly EcsFilterInject<Inc<PatrolState>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<PatrolState> _statePool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<UnityNavMeshAgent> _navAgentPool;
        private readonly EcsPoolInject<UnityRigidbody> _rigidbodyPool;

        private PatrolPointsProvider _patrolPointsProvider;
        
        public void Init(IEcsSystems systems)
        {
            _patrolPointsProvider = ServiceLocator.Resolve<PatrolPointsProvider>();
        } 
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;
                ref var patrolState = ref _statePool.Value.Get(entity);
                ref var navAgent = ref _navAgentPool.Value.Get(agentIndex).Ref;
                if (!patrolState.IsReached)
                {
                    if (navAgent.remainingDistance <= NEAREST_RADIUS)
                    {
                        patrolState.NearestTimer += Time.deltaTime;
                        if (patrolState.NearestTimer > NEAREST_TIME_OUT)
                        {
                            patrolState.NearestTimer = 0f;
                            patrolState.IsReached = true;
                        }
                    }
                    if (!navAgent.pathPending && navAgent.remainingDistance <= EPS)
                        patrolState.IsReached = true;
                    else
                        continue;
                }
                ref var rigidbody = ref _rigidbodyPool.Value.Get(agentIndex).Ref;

                int targetPositionIndex = -1;
                var patrolPoints = _patrolPointsProvider.PatrolPositions;
                if (patrolState.TargetPositionIndex == -1)
                {
                    var position = rigidbody.position;
                    var minValue = float.MaxValue;
                    for (int i = 0; i < patrolPoints.Length; i++)
                    {
                        var point = patrolPoints[i];
                        if (NavMeshUtils.TryGetNavMeshDistance(position, point.Position, out var distance)
                            && distance < minValue)
                        {
                            minValue = distance;
                            targetPositionIndex = i;
                        }
                    }
                }
                else
                {
                    var neighbours = patrolPoints[patrolState.TargetPositionIndex].NeighbourPoints;
                    if (neighbours.Length > 0)
                        targetPositionIndex = neighbours[Random.Range(0, neighbours.Length)];
                }

                if (targetPositionIndex == -1)
                    continue;
                navAgent.SetDestination(patrolPoints[targetPositionIndex].Position);
                patrolState.TargetPositionIndex = targetPositionIndex;
                patrolState.NearestTimer = 0f;
                patrolState.IsReached = false;
            }
        }
    }
}