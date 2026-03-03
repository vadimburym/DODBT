using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features.Projectile.Components;
using _ExampleProject.Code.Infrastructure.StaticData.Projectile;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features.Projectile.Systems
{
    public sealed class ProjectileDeathSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<ProjectileTag, DeathRequest>> _filter;
        private readonly EcsPoolInject<ProjectileTag> _tagPool;
        private readonly EcsWorldInject _world;
        
        private IMemoryPoolService _memoryPoolService;
        private ProjectilesStaticData _projectilesStaticData;
        
        public void Init(IEcsSystems systems)
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
            _projectilesStaticData = ServiceLocator.Resolve<StaticDataService>().ProjectilesStaticData;
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var proj = ref _tagPool.Value.Get(entity);
                var projData = _projectilesStaticData.GetProjectileData(proj.ProjectileId);
                _memoryPoolService.UnspawnGameObject(projData.PrefabId, proj.GameObjectRef);
                _world.Value.DelEntity(entity);
            }
        }
    }
}