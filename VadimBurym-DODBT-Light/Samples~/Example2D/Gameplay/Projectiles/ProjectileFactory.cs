using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Gameplay.Projectiles;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Services;
using DODBT.Light.Example.Services.Pooling;
using Leopotam.EcsLite;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Projectiles
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
        
        public int Create(ProjectileId id, Vector2 position, Vector2 direction, Rigidbody2D owner)
        {
            var proj = _world.NewEntity();
            var projData = _projectileStaticData.GetProjectileData(id);
            var projFacade = _memoryPoolService.SpawnGameObject<ProjectileFacade>(projData.PrefabId);
            
            projFacade.EcsEntity.Construct(_world, proj);
            projFacade.EcsCollider.Construct(EcsWorlds.GetWorld(EcsWorlds.EVENTS));
            projFacade.Launch(position, direction * projData.MoveSpeed, owner);
            
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
