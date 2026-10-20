using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace DODBT.Light.Example.Gameplay.Systems
{
    public sealed class MovementSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<Movement>, Exc<NavigationState>> _movementFilter;
        private readonly EcsPoolInject<Movement> _movementPool;
        private readonly EcsPoolInject<UnityRigidbody> _rigidbodyPool;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _movementFilter.Value)
            {
                ref var movement = ref _movementPool.Value.Get(entity);
                ref var rigidbody = ref _rigidbodyPool.Value.Get(entity);
                var velocity = movement.Direction * movement.MoveSpeed;
                rigidbody.Ref.linearVelocity = velocity;
            }
        }
    }
}
