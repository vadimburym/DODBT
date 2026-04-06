using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.StaticData.Projectile
{
    [CreateAssetMenu(fileName = nameof(ProjectilesStaticData), menuName ="_Project/StaticData/New ProjectilesStaticData")]
    public sealed class ProjectilesStaticData : ScriptableObject
    {
        public ProjectileData[] Projectiles;

        public ProjectileData GetProjectileData(ProjectileId projectileId)
        {
            for (int i = 0; i < Projectiles.Length; i++)
                if (Projectiles[i].Id == projectileId) return Projectiles[i];
            return null;
        }
    }
}