using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Services;
using DODBT.Light.Example.Services.Pooling;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.Gameplay.Systems
{
    public sealed class EnemyDeathSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<DeathRequest, EnemyTag>> _filter;
        private readonly EcsPoolInject<EnemyTag> _enemyPool;
        private readonly EcsPoolInject<AiBrain> _brainPool;
        private readonly EcsWorldInject _world;
        
        private IMemoryPoolService _memoryPoolService;
        private IBehaviourTreeService _behaviourTreeService;
        
        public void Init(IEcsSystems systems)
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
            _behaviourTreeService = ServiceLocator.Resolve<IBehaviourTreeService>();
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var enemy = ref _enemyPool.Value.Get(entity);
                ref var brain = ref _brainPool.Value.Get(entity);
                _behaviourTreeService.GetBehaviourTree(brain.BehaviourTreeGUID).Abort(brain.ContextRef, brain.StateRef);
#if UNITY_EDITOR
                if (enemy.GameObjectRef.TryGetComponent<BtMonoDebug>(out var debug)) debug.Detach();
#endif
                _memoryPoolService.UnspawnGameObject(MemoryPoolId.Enemy, enemy.GameObjectRef);
                _memoryPoolService.EnqueueObject<BtState<LeoEcsLeafState>>(brain.StateRef);
                _memoryPoolService.EnqueueObject<LeoEcsContext>(brain.ContextRef);
                _world.Value.DelEntity(entity);
            }
        }
    }
}
