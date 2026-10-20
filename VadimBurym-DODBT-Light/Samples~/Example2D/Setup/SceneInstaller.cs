using DODBT.Light.Example.Gameplay.Scene;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using UnityEngine;

namespace DODBT.Light.Example.Setup
{
    public sealed class SceneInstaller : MonoBehaviour
    {
        [SerializeField] private CameraProvider _cameraProvider;
        [SerializeField] private TransformProvider _transformProvider;
        [SerializeField] private PatrolPointsProvider _patrolPointsProvider;
        [SerializeField] private PlayerSpawnPosition _playerSpawnPosition;
        
        public void InstallBindings()
        {
            ServiceLocator.Bind<CameraProvider>(_cameraProvider);
            ServiceLocator.Bind<TransformProvider>(_transformProvider);
            ServiceLocator.Bind<PatrolPointsProvider>(_patrolPointsProvider);
            ServiceLocator.Bind<PlayerSpawnPosition>(_playerSpawnPosition);
        }
    }
}
