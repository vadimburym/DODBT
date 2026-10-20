using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(WeaponsStaticData), menuName ="DODBT/Light Example/StaticData/New WeaponsStaticData")]
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
