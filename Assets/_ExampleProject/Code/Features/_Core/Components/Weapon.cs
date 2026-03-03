using System;
using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Components
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