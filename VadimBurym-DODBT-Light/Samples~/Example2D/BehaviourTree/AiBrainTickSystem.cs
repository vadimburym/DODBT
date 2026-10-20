using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace DODBT.Light.Example.BehaviourTree
{
    public sealed class AiBrainTickSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<AiBrain>> _filter;
        private readonly EcsPoolInject<AiBrain> _aiBrainPool;
        
        private IBehaviourTreeService _behaviourTreeService;
        
        public void Init(IEcsSystems systems)
        {
            _behaviourTreeService = ServiceLocator.Resolve<IBehaviourTreeService>();
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var aiBrain = ref _aiBrainPool.Value.Get(entity);
                aiBrain.TickTime += Time.deltaTime;
                if (aiBrain.TickTime >= aiBrain.TickInterval)
                {
                    aiBrain.TickTime -= aiBrain.TickInterval;
                    var bt = _behaviourTreeService.GetBehaviourTree(aiBrain.BehaviourTreeGUID);
#if DODBT_DECORATORS
                    bt.Tick(aiBrain.ContextRef, aiBrain.StateRef, Time.timeAsDouble);
#else
                    bt.Tick(aiBrain.ContextRef, aiBrain.StateRef);
#endif
                }
            }
        }
    }
}
