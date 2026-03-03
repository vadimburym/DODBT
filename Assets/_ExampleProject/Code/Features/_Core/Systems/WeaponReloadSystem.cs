using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class WeaponReloadSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<WeaponReloadRequest>> _filter;
        private readonly EcsPoolInject<WeaponReloadRequest> _requestPool;
        private readonly EcsPoolInject<Weapon> _weaponPool;
        
        private WeaponsStaticData _weaponsStaticData;
        
        public void Init(IEcsSystems systems)
        {
            _weaponsStaticData = ServiceLocator.Resolve<StaticDataService>().WeaponsStaticData;
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var requestData = ref _requestPool.Value.Get(entity);
                ref var weapon = ref _weaponPool.Value.Get(entity);
                var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);

                requestData.TickTime += Time.deltaTime;
                if (requestData.TickTime >= weaponData.ReloadTime)
                {
                    var maxMagazine = weaponData.MaxMagazineSize;
                    if (weapon.TotalAmmo < maxMagazine)
                    {
                        weapon.MagazineAmmo = weapon.TotalAmmo;
                        weapon.TotalAmmo = 0;
                    }
                    else
                    {
                        weapon.MagazineAmmo = maxMagazine;
                        weapon.TotalAmmo -= maxMagazine;
                    }
                    _requestPool.Value.Del(entity);
                }
            }
        }
    }
}