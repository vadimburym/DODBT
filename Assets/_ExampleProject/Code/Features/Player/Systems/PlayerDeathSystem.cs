using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Core.Keys;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features.Player.Systems
{
    public sealed class PlayerDeathSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<PlayerTag, DeathRequest>> _filter;
        private readonly EcsPoolInject<PlayerTag> _playerTagPool;
        private readonly EcsWorldInject _world;
        
        private IMemoryPoolService _memoryPoolService;
        
        public void Init(IEcsSystems systems)
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var playerTag = ref _playerTagPool.Value.Get(entity);
                _memoryPoolService.UnspawnGameObject(MemoryPoolId.Player, playerTag.GameObjectRef);
                _world.Value.DelEntity(entity);
            }
        }
    }
}