using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.States;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class StrafeEntityStateSystem : IEcsRunSystem
    {
        private const float EPS = 0.2f;
        private const string WALLS_MASK = "Walls";

        private readonly EcsFilterInject<Inc<StrafeEntityState>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<StrafeEntityState> _statePool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<UnityTransform> _transformPool;
        private readonly EcsPoolInject<UnityNavMeshAgent> _navAgentPool;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var stateData = ref _statePool.Value.Get(entity);
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;
                var navAgent = _navAgentPool.Value.Get(agentIndex).Ref;
                if (stateData.IsReached)
                {
                    stateData.TickTime += Time.deltaTime;
                    if (stateData.TickTime < stateData.StrafeCooldown)
                        continue;
                    stateData.TickTime = 0;
                    var selfPosition = _transformPool.Value.Get(agentIndex).Ref.position;
                    var entityPosition = _transformPool.Value.Get(stateData.EntityIndex).Ref.position;

                    if (TryGetStrafePoint(selfPosition, entityPosition, stateData.StrafeDistance, out Vector2 point))
                    {
                        navAgent.SetDestination(point);
                        stateData.IsReached = false;
                    }
                    else
                    {
                        navAgent.SetDestination(selfPosition);
                    }
                }
                else if (!navAgent.pathPending && navAgent.remainingDistance <= EPS)
                {
                    stateData.IsReached = true;
                }
            }
        }
        
        private bool TryGetStrafePoint(
            Vector2 selfPos,
            Vector2 targetPos,
            float distance,
            out Vector2 point)
        {
            point = default;

            Vector2 dir = selfPos - targetPos;
            float sq = dir.sqrMagnitude;
            dir *= 1f / Mathf.Sqrt(sq);
            Vector2 perpA = new Vector2(-dir.y, dir.x);
            Vector2 perpB = new Vector2(dir.y, -dir.x);
            
            bool firstA = Random.Range(0, 2) == 0;
            if (firstA)
            {
                if (TryRay(selfPos, selfPos + perpA * distance, out point)) return true;
                if (TryRay(selfPos, selfPos + perpB * distance, out point)) return true;
            }
            else
            {
                if (TryRay(selfPos, selfPos + perpB * distance, out point)) return true;
                if (TryRay(selfPos, selfPos + perpA * distance, out point)) return true;
            }
            return false;
        }

        private static bool TryRay(Vector2 from, Vector2 to, out Vector2 point)
        {
            point = default;
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            var hit = Physics2D.Raycast(from, delta / dist, dist, LayerMask.GetMask(WALLS_MASK));
            if (hit.collider != null) return false;
            point = to;
            return true;
        }
    }
}