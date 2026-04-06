using _ExampleProject.Code.Features.Player.Components;
using _ExampleProject.Code.Features.Player.Facade;
using _ExampleProject.Code.Infrastructure.StaticData.Player;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features.Player.Factory
{
    public sealed class PlayerFactory : IPlayerFactory, IConstruct
    {
        private PlayerStaticData _playerData;
        private IMemoryPoolService _memoryPoolService;
        private EcsWorld _world;
        
        public void Construct()
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
            _playerData = ServiceLocator.Resolve<StaticDataService>().PlayerStaticData;
            _world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
        }
        
        public int Create(Vector2 position)
        {
            var entity = _world.NewEntity();
            var playerFacade = _memoryPoolService.SpawnGameObject<PlayerFacade>(MemoryPoolId.Player);

            playerFacade.EcsEntity.Construct(_world, entity);
            playerFacade.Rigidbody.position = position;
            
            _world.GetPool<Movement>().Add(entity).MoveSpeed
                = _playerData.MoveSpeed;
            _world.GetPool<UnityRigidbody>().Add(entity).Ref
                = playerFacade.Rigidbody;
            _world.GetPool<UnityTransform>().Add(entity).Ref
                = playerFacade.transform;
            _world.GetPool<CameraFollowTag>().Add(entity);
            _world.GetPool<InputMoveTag>().Add(entity);
            _world.GetPool<PlayerTag>().Add(entity).GameObjectRef
                = playerFacade.gameObject;
            
            return entity;
        }
    }
}