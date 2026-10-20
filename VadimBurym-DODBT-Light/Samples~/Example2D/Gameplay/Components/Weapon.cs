using System;
using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Components
{
    [Serializable]
    public struct Weapon
    {
        public Transform FirePointRef;
        public WeaponId WeaponId;
        public int TotalAmmo;
        public int MagazineAmmo;

        public void Setup(Transform firePointRef, WeaponId weaponId, int totalAmmo, int magazineAmmo)
        {
            FirePointRef = firePointRef;
            WeaponId = weaponId;
            TotalAmmo = totalAmmo;
            MagazineAmmo = magazineAmmo;
        }
    }
}
