using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using DODBT.Light.Example.Gameplay.Scene;
using DODBT.Light.Example.Services.Pooling;

namespace DODBT.Light.Example.Services.Pooling
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
