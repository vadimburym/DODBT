using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Features.Test;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features.Player.Systems
{
    public sealed class PlayerVisibilitySensorSystem : IEcsRunSystem
    {
        private const float BODY_WIDTH = 2.5f;
        private const float CIRCLE_CAST_R = 0.4f;
        private const string WALLS_MASK = "Walls";
        
        private EcsFilterInject<Inc<PlayerVisibility>> _filter;
        private EcsFilterInject<Inc<PlayerTag>> _playerFilter;
        private EcsPoolInject<PlayerVisibility> _playerVisibilityPool;
        private EcsPoolInject<UnityTransform> _transformPool;
        
        public void Run(IEcsSystems systems)
        {
            bool isPlayerAlive = _playerFilter.Value.GetEntitiesCount() != 0;
            
            foreach (var entity in _filter.Value)
            {
                ref var sensorData = ref _playerVisibilityPool.Value.Get(entity);
                
                sensorData.TickTime += Time.deltaTime;
                if (sensorData.TickTime >= sensorData.TickInterval)
                {
                    sensorData.TickTime -= sensorData.TickInterval;
                    if (!isPlayerAlive)
                    {
                        sensorData.Reset();
                        continue;
                    }
                    
                    Vector2 selfPosition = _transformPool.Value.Get(entity).Ref.position;
                    foreach (var player in _playerFilter.Value) 
                    {
                        Vector2 playerPosition = _transformPool.Value.Get(player).Ref.position;
                        var distance = (playerPosition - selfPosition).sqrMagnitude;
                        sensorData.SqrDistanceToPlayer = distance;
                        if (sensorData.IsPlayerDetected)
                        {
                            if (distance > sensorData.HuntingSqrDistance)
                            {
                                sensorData.IsPlayerDetected = false;
                                sensorData.IsPlayerRaycast = false;
                                sensorData.DetectedPlayer = -1;
                                return;
                            }
                            sensorData.IsPlayerRaycast = RaycastPlayer(selfPosition, playerPosition, distance);
                        }
                        else if (distance <= sensorData.DetectSqrDistance)
                        {
                            if (RaycastPlayer(selfPosition, playerPosition, distance))
                            {
                                sensorData.IsPlayerDetected = true;
                                sensorData.IsPlayerRaycast = true;
                                sensorData.DetectedPlayer = player;
                            }
                        }
                        break; 
                    }
                }
            }
        }

        private bool RaycastPlayer(Vector2 origin, Vector2 target, float distance)
        {
            if (distance <= BODY_WIDTH * BODY_WIDTH) return true;
            var direction = (target - origin).normalized;
            var playerHit = Physics2D.CircleCast(origin,CIRCLE_CAST_R, direction, Mathf.Sqrt(distance), LayerMask.GetMask(WALLS_MASK));
            return playerHit.collider == null;
        }
    }
}