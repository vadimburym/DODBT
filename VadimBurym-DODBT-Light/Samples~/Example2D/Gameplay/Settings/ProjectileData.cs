using System;
using DODBT.Light.Example.Gameplay.Settings;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [Serializable]
    public sealed class ProjectileData
    {
        public ProjectileId Id;
        public MemoryPoolId PrefabId;
        public float MoveSpeed;
    }
}
