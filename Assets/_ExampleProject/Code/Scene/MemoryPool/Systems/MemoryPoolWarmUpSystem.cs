using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Infrastructure;
using _Project.Code.Infrastructure.Scene;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Domain;
using Infrastructure.MemoryPool.Service;

namespace _Project.Code.Features.Locale.MemoryPool.Systems
{
    public sealed class MemoryPoolWarmUpSystem : IConstruct, IWarmUp
    {
        private TransformProvider _transformProvider;
        private StaticDataService _staticDataService;
        private IMemoryPoolService _memoryPoolService;
        
        public void Construct()
        {
            _transformProvider = ServiceLocator.Resolve<TransformProvider>();
            _staticDataService = ServiceLocator.Resolve<StaticDataService>();
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
        }

        public void WarmUp()
        {
            var pipeline = _staticDataService.MemoryPoolPipeline.GameObjectMemoryPools;
            for (int i = 0; i < pipeline.Length; i++)
            {
                var pool = pipeline[i];
                var transform = _transformProvider.GetTransform(pool.TransformId);
                var gameObjectPool = new GameObjectMemoryPool(pool.Asset, transform, pool.InitialCount);
                _memoryPoolService.AddGameObjectMemoryPool(pool.PoolId, gameObjectPool);
                gameObjectPool.Init();
            }
        }
    }
}