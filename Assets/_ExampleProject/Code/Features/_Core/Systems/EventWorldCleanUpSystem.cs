using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class EventWorldCleanUpSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<OneFrameTag>> _filter = EcsWorlds.EVENTS;
        private readonly EcsWorldInject _events = EcsWorlds.EVENTS;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                _events.Value.DelEntity(entity);
            }
        }
    }
}