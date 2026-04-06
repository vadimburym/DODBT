using _ExampleProject.Code.Infrastructure.InputService;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features._Core.Systems
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