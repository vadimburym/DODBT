using _ExampleProject.Code.Features.Projectile.Components;
using _ExampleProject.Code.Features.Projectile.Facade;
using _ExampleProject.Code.Infrastructure.StaticData.Projectile;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features.Projectile.Factory
{
    public sealed class ProjectileFactory : IProjectileFactory, IConstruct
    {
        private ProjectilesStaticData _projectileStaticData;
        private IMemoryPoolService _memoryPoolService;
        private EcsWorld _world;
        
        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
            _projectileStaticData = ServiceLocator.Resolve<StaticDataService>().ProjectilesStaticData;
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
        }
        
        public int Create(ProjectileId id, Vector2 position, Vector2 direction)
        {
            var proj = _world.NewEntity();
            var projData = _projectileStaticData.GetProjectileData(id);
            var projFacade = _memoryPoolService.SpawnGameObject<ProjectileFacade>(projData.PrefabId);
            
            projFacade.EcsEntity.Construct(_world, proj);
            projFacade.EcsCollider.Construct(EcsWorlds.GetWorld(EcsWorlds.EVENTS));
            projFacade.transform.position = position;
            
            _world.GetPool<UnityRigidbody>().Add(proj).Ref
                = projFacade.Rigidbody;
            _world.GetPool<Movement>().Add(proj).Setup(
                direction: direction,
                moveSpeed: projData.MoveSpeed);
            _world.GetPool<ProjectileTag>().Add(proj).Setup(
                projectileId: id,
                gameObjectRef: projFacade.gameObject);
            
            return proj;
        }
    }
}