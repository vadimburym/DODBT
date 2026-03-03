using _ExampleProject.Code.Features.Enemy.Behaviours;
using _ExampleProject.Code.Scene;
using _ExampleProject.Code.Scene.SpawnPoints;
using _Project.Code.Infrastructure.Scene;
using _Project.Infrastructure;
using UnityEngine;

namespace _Project.Code._Bootstrap
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