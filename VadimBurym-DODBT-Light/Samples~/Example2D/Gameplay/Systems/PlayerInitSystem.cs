using DODBT.Light.Example.Gameplay.Player;
using DODBT.Light.Example.Gameplay.Scene;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;

namespace DODBT.Light.Example.Gameplay.Systems
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
