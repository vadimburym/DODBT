using _ExampleProject.Code.Features._Core.Behaviours;
using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features.Projectile.Components;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class ProjectileCollisionSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<CollisionEvent>> _filter = EcsWorlds.EVENTS;
        private readonly EcsPoolInject<CollisionEvent> _eventPool= EcsWorlds.EVENTS;
        private readonly EcsPoolInject<ProjectileTag> _projPool;
        private readonly EcsPoolInject<DeathRequest> _deathRequestPool;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var eventData = ref _eventPool.Value.Get(entity);
                if (!_projPool.Value.Has(eventData.Entity))
                    continue;
                _deathRequestPool.Value.Add(eventData.Entity);
                if (eventData.CollisionRef.TryGetComponent<EcsEntity>(out var collider))
                {
                    //Take damage
                }
            }
        }
    }
}