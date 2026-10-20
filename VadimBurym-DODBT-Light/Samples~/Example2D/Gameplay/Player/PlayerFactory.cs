using DODBT.Light.Example.BehaviourTree.States;
using DODBT.Light.Example.Gameplay.Components;
using DODBT.Light.Example.Gameplay.Player;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Services;
using DODBT.Light.Example.Services.Pooling;
using Leopotam.EcsLite;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Player
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
            playerFacade.transform.position = new Vector3(position.x, position.y, playerFacade.transform.position.z);
            playerFacade.Rigidbody.position = position;
            playerFacade.Rigidbody.linearVelocity = Vector2.zero;
            
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
