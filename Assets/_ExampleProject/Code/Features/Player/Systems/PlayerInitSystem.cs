using _ExampleProject.Code.Features.Player.Factory;
using _ExampleProject.Code.Scene.SpawnPoints;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Infrastructure;

namespace _ExampleProject.Code.Features.Player.Systems
{
    public sealed class PlayerInitSystem : IConstruct, IInit
    {
        private PlayerSpawnPosition _playerSpawnPosition;
        private IPlayerFactory _playerFactory;
        
        public void Construct()
        {
            _playerSpawnPosition = ServiceLocator.Resolve<PlayerSpawnPosition>();
            _playerFactory = ServiceLocator.Resolve<IPlayerFactory>();
        }

        public void Init()
        {
            var player = _playerFactory.Create(_playerSpawnPosition.SpawnPosition);
        }
    }
}