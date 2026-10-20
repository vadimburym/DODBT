using System;
using DODBT.Light.Example.Gameplay.Settings;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [Serializable]
    public sealed class WeaponData
    {
        public WeaponId Id;
        public int MaxTotalAmmo;
        public int MaxMagazineSize;
        public ProjectileId ProjectileId;
        public int BurstsPerShot;
        public float BurstInterval;
        public int ProjectilesPerBurst;
        public int AmmoPerShot;
        public float ReloadTime;
        public float ShootSpeed;
    }
}
