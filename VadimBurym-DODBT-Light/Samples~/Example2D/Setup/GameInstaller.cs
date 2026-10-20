using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Gameplay.Systems;
using DODBT.Light.Example.Gameplay.Enemies;
using DODBT.Light.Example.Gameplay.Player;
using DODBT.Light.Example.Gameplay.Projectiles;
using DODBT.Light.Example.Services;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Setup;
using DODBT.Light.Example.Services.Pooling;
using Leopotam.EcsLite;
using UnityEngine;

namespace DODBT.Light.Example.Setup
{
    [CreateAssetMenu(fileName = "GameInstaller", menuName = "DODBT/Light Example/New GameInstaller")]
    public sealed class GameInstaller : ScriptableObject
    {
        [SerializeField] private StaticDataService _staticDataService;
        
        public void InstallBindings()
        {
            BindInfrastructure();
            BindAI();
            BindEcsCore();
            BindEnemies();
            BindPlayer();
            BindProjectile();
        }
        
        private void BindInfrastructure()
        {
            ServiceLocator.Bind<StaticDataService>(_staticDataService);
            ServiceLocator.Bind<IMemoryPoolService, System.IDisposable>(new MemoryPoolService());
            ServiceLocator.Bind<IInputService, ITick>(new InputService());
            ServiceLocator.Bind<IConstruct, IWarmUp>(new MemoryPoolWarmUpSystem());
        }
        
        private void BindAI()
        {
            ServiceLocator.Bind<IBehaviourTreeService>(new BehaviourTreeService());
            ServiceLocator.Bind<IConstruct, IWarmUp>(new BehaviourTreeWarmUpSystem());
            ServiceLocator.Bind<IConstruct, IInit>(new BehaviourTreeLeafsConstructSystem());
        }
        
        private void BindEcsCore()
        {
            ServiceLocator.Bind<IEcsSystem>(new InputMoveSystem());
            ServiceLocator.Bind<IEcsSystem>(new AiBrainTickSystem());
            ServiceLocator.Bind<IEcsSystem>(new PatrolStateSystem());
            ServiceLocator.Bind<IEcsSystem>(new ChaseEntityStateSystem());
            ServiceLocator.Bind<IEcsSystem>(new RetreatEntityStateSystem());
            ServiceLocator.Bind<IEcsSystem>(new StrafeEntityStateSystem());
            ServiceLocator.Bind<IEcsSystem>(new MovementSystem());
            ServiceLocator.Bind<IEcsSystem>(new NavigationStateSystem());
            ServiceLocator.Bind<IEcsSystem>(new ShootEntityStateSystem());
            ServiceLocator.Bind<IEcsSystem>(new WeaponReloadSystem());
            ServiceLocator.Bind<IEcsSystem>(new WeaponShootSystem());
            ServiceLocator.Bind<IEcsSystem>(new ProjectileCollisionSystem());
            ServiceLocator.Bind<IEcsSystem>(new EventWorldCleanUpSystem());
            ServiceLocator.Bind<ILateTick, IInit>(new CameraFollowSystem());
        }
        
        private void BindEnemies()
        {
            ServiceLocator.Bind<IEnemyFactory, IConstruct>(new EnemyFactory());
            ServiceLocator.Bind<IConstruct, IInit>(new EnemyInitSystem());
            ServiceLocator.Bind<IEcsSystem>(new EnemyDeathSystem());
        }

        private void BindPlayer()
        {
            ServiceLocator.Bind<IPlayerFactory, IConstruct>(new PlayerFactory());
            ServiceLocator.Bind<IConstruct, IInit>(new PlayerInitSystem());
            ServiceLocator.Bind<IEcsSystem>(new PlayerVisibilitySensorSystem());
            ServiceLocator.Bind<IEcsSystem>(new PlayerDeathSystem());
        }
        
        private void BindProjectile()
        {
            ServiceLocator.Bind<IProjectileFactory, IConstruct>(new ProjectileFactory());
            ServiceLocator.Bind<IEcsSystem>(new ProjectileDeathSystem());
        }
    }
}
