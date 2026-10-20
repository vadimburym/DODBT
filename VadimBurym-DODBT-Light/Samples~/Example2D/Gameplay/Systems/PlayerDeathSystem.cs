using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using DODBT.Light.Example.Services.Pooling;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace DODBT.Light.Example.Gameplay.Systems
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
