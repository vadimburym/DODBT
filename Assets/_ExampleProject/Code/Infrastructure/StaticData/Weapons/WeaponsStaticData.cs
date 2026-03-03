using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.StaticData.Weapons
{
    [CreateAssetMenu(fileName = nameof(WeaponsStaticData), menuName ="_Project/StaticData/New WeaponsStaticData")]
    public sealed class WeaponsStaticData : ScriptableObject
    {
        public WeaponData[] Weapons;

        public WeaponData GetWeaponData(WeaponId weaponId)
        {
            for (int i = 0; i < Weapons.Length; i++)
                if (Weapons[i].Id == weaponId) return Weapons[i];
            return null;
        }
    }
}