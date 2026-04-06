using System;
using _Project.Code.Core.Keys;

namespace _ExampleProject.Code.Infrastructure.StaticData.Projectile
{
    [Serializable]
    public sealed class ProjectileData
    {
        public ProjectileId Id;
        public MemoryPoolId PrefabId;
        public float MoveSpeed;
    }
}