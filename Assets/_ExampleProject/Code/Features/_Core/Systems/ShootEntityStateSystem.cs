using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features._Core.States;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class ShootEntityStateSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<ShootEntityState>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<ShootEntityState> _shootStatePool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<WeaponShootRequest> _shootRequestPool;
        private readonly EcsPoolInject<WeaponReloadRequest> _reloadRequestPool;
        private readonly EcsPoolInject<Weapon> _weaponPool;
        private readonly EcsPoolInject<UnityTransform> _transformPool;
        
        private WeaponsStaticData _weaponsStaticData;
        
        public void Init(IEcsSystems systems)
        {
            _weaponsStaticData = ServiceLocator.Resolve<StaticDataService>().WeaponsStaticData;
        }
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var stateData = ref _shootStatePool.Value.Get(entity);
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;
                ref var weapon = ref _weaponPool.Value.Get(agentIndex);
                var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);
                
                stateData.TickTime += Time.deltaTime;
                if (stateData.TickTime >= weaponData.ShootSpeed)
                {
                    if (stateData.StateStatus == NodeStatus.Success)
                        continue;
                    if (weapon.MagazineAmmo < weaponData.AmmoPerShot)
                    {
                        if (weapon.TotalAmmo < weaponData.AmmoPerShot)
                            stateData.StateStatus = NodeStatus.Failure;
                        else
                            if (!_reloadRequestPool.Value.Has(agentIndex))
                                _reloadRequestPool.Value.Add(agentIndex);
                        continue;
                    }
                    stateData.TickTime = 0;
                    if (!_shootRequestPool.Value.Has(agentIndex))
                    {
                        weapon.MagazineAmmo -= weaponData.AmmoPerShot;
                        _shootRequestPool.Value.Add(agentIndex).Setup(
                            burstTickTime: weaponData.BurstInterval,
                            shootPosition: _transformPool.Value.Get(stateData.EntityIndex).Ref.position);
                        stateData.CurrentShots += 1;
                        if (stateData.CurrentShots >= stateData.TargetShots && stateData.TargetShots != -1)
                            stateData.StateStatus = NodeStatus.Success;
                    }
                }
            }
        }
    }
}