using DODBT.Light.Example.Services;
using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Gameplay.Settings;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace DODBT.Light.Example.Gameplay.Systems
{
    public sealed class InputMoveSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<InputMoveTag>> _filter;
        private readonly EcsPoolInject<Movement> _movementPool;
        
        private IInputService _inputService;
        
        public void Init(IEcsSystems systems)
        {
            _inputService = ServiceLocator.Resolve<IInputService>();
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var movement = ref _movementPool.Value.Get(entity);
                movement.Direction = _inputService.MoveDirection;
            }
        }
    }
}
