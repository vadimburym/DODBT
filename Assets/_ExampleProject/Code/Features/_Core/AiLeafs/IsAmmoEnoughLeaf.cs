using System;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.AiLeafs
{
    [Serializable]
    public sealed class IsAmmoEnoughLeaf : ILeaf<LeoEcsContext, LeoEcsLeafState>, IConstruct
    {
        [SerializeField] private int _shotCount;

        private WeaponsStaticData _weaponsStaticData;
        private EcsPool<Weapon> _weaponPool;
        
        public void Construct()
        {
            _weaponsStaticData = ServiceLocator.Resolve<StaticDataService>().WeaponsStaticData;
            _weaponPool = EcsWorlds.GetPool<Weapon>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(LeoEcsContext context, ref LeoEcsLeafState state)
        {
            ref var weapon = ref _weaponPool.Get(context.AgentIndex);
            var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);
            var targetAmmo = _shotCount * weaponData.AmmoPerShot;
            return weapon.TotalAmmo + weapon.MagazineAmmo >= targetAmmo ? NodeStatus.Success : NodeStatus.Failure;
        }

        public void OnEnter(LeoEcsContext context, ref LeoEcsLeafState state)
        {
        }

        public void OnExit(LeoEcsContext context, ref LeoEcsLeafState state, NodeStatus exitStatus)
        {
        }

        public void OnAbort(LeoEcsContext context, ref LeoEcsLeafState state)
        {
        }
    }
}