using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Projectile.Factory;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class WeaponShootSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<WeaponShootRequest>> _filter;
        private readonly EcsPoolInject<WeaponShootRequest> _requestPool;
        private readonly EcsPoolInject<Weapon> _weaponPool;
        
        private WeaponsStaticData _weaponsStaticData;
        private IProjectileFactory _projectileFactory;
        
        public void Init(IEcsSystems systems)
        {
            _weaponsStaticData = ServiceLocator.Resolve<StaticDataService>().WeaponsStaticData;
            _projectileFactory = ServiceLocator.Resolve<IProjectileFactory>();
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var requestData = ref _requestPool.Value.Get(entity);
                ref var weapon = ref _weaponPool.Value.Get(entity);
                var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);
                
                requestData.BurstTickTime += Time.deltaTime;
                if (requestData.BurstTickTime >= weaponData.BurstInterval)
                {
                    requestData.BurstTickTime -= weaponData.BurstInterval;

                    Vector2 position = weapon.FirePointRef.position;
                    var direction = (requestData.ShootPosition - position).normalized;
                    _projectileFactory.Create(weaponData.ProjectileId, position, direction);
                    
                    requestData.BurstCount += 1;
                    if (requestData.BurstCount >= weaponData.BurstsPerShot)
                        _requestPool.Value.Del(entity);
                }
            }
        }
    }
}