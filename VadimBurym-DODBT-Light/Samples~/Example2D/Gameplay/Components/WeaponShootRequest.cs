using System;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Components
{
    [Serializable]
    public struct WeaponShootRequest
    {
        public float BurstTickTime;
        public int BurstCount;
        public Vector2 ShootPosition;

        public void Setup(float burstTickTime, Vector2 shootPosition)
        {
            BurstTickTime = burstTickTime;
            ShootPosition = shootPosition;
        }
    }
}
