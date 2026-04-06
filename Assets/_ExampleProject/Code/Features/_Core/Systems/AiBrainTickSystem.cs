using _ExampleProject.Code.Features.AI.Service;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
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
                    bt.Tick(aiBrain.ContextRef, aiBrain.StateRef);
                }
            }
        }
    }
}