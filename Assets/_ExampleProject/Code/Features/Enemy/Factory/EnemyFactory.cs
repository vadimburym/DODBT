using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.AI.Service;
using _ExampleProject.Code.Features.Enemy.Facade;
using _ExampleProject.Code.Features.Player.Components;
using _ExampleProject.Code.Infrastructure.StaticData.Enemy;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Enemy.Factory
{
    public sealed class EnemyFactory : IEnemyFactory, IConstruct
    {
        private IMemoryPoolService _memoryPoolService;
        private IBehaviourTreeService _behaviourTreeService;
        private EnemyStaticData _enemyStaticData;
        private WeaponsStaticData _weaponsStaticData;
        private EcsWorld _world;
        
        public void Construct()
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
            _enemyStaticData = ServiceLocator.Resolve<StaticDataService>().EnemyStaticData;
            _weaponsStaticData = ServiceLocator.Resolve<StaticDataService>().WeaponsStaticData;
            _behaviourTreeService = ServiceLocator.Resolve<IBehaviourTreeService>();
            _world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
        }
        
        public int Create(EnemyId enemyId, Vector2 position)
        {
            var entity = _world.NewEntity();
            var enemyData = _enemyStaticData.GetEnemyConfig(enemyId);
            var enemyFacade = _memoryPoolService.SpawnGameObject<EnemyFacade>(MemoryPoolId.Enemy);

            var bt = _behaviourTreeService.GetBehaviourTree(enemyData.Brain.GUID);
            var btState = _memoryPoolService.DequeueObject<BtState<LeoEcsLeafState>>();
            bt.FillInitialState(btState);
            var leafStates = btState.LeafStates;
            for (int i = 0; i < leafStates.Length; i++)
                leafStates[i].Reset();
            var btContext = _memoryPoolService.DequeueObject<LeoEcsContext>();
            btContext.AgentIndex = entity;
            
            enemyFacade.EcsEntity.Construct(_world, entity);
            enemyFacade.Rigidbody.position = position;
            enemyFacade.BtMonoDebug.Construct(enemyData.Brain, btState);
            enemyFacade.NavMeshAgent.speed = enemyData.MoveSpeed;
            enemyFacade.NavMeshAgent.avoidancePriority = Random.Range(10, 90);
            
            _world.GetPool<EnemyTag>().Add(entity).Setup(
                enemyId: enemyData.Id,
                gameObjectRef: enemyFacade.gameObject);
            _world.GetPool<UnityRigidbody>().Add(entity).Ref
                = enemyFacade.Rigidbody;
            _world.GetPool<UnityNavMeshAgent>().Add(entity).Ref
                = enemyFacade.NavMeshAgent;
            _world.GetPool<UnityTransform>().Add(entity).Ref
                = enemyFacade.transform;
            _world.GetPool<DifficultyWeight>().Add(entity).Value
                = enemyData.DifficultyWeight;
            _world.GetPool<Movement>().Add(entity).MoveSpeed
                = enemyData.MoveSpeed;
            _world.GetPool<AiBrain>().Add(entity).Setup(
                behaviourTreeGuid: enemyData.Brain.GUID,
                btState: btState,
                btContext: btContext,
                tickInterval: enemyData.BrainTickInterval,
                tickTime: Random.Range(0, enemyData.BrainTickInterval));
            _world.GetPool<PlayerVisibility>().Add(entity).Setup(
                detectSqrDistance: enemyData.DetectDistance * enemyData.DetectDistance,
                huntingSqrDistance: enemyData.HuntingDistance * enemyData.HuntingDistance,
                tickInterval: enemyData.PlayerVisibilitySensorTickInterval,
                tickTime: Random.Range(0, enemyData.PlayerVisibilitySensorTickInterval));
            var weaponMagazineSize = _weaponsStaticData.GetWeaponData(enemyData.Weapon).MaxMagazineSize;
            _world.GetPool<Weapon>().Add(entity).Setup(
                firePointRef: enemyFacade.FirePoint,
                weaponId: enemyData.Weapon,
                totalAmmo: enemyData.InitAmmo - weaponMagazineSize,
                magazineAmmo: weaponMagazineSize);
            
            return entity;
        }
    }
}