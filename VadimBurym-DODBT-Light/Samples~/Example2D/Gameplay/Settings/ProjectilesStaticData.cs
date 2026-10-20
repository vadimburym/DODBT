using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(ProjectilesStaticData), menuName ="DODBT/Light Example/StaticData/New ProjectilesStaticData")]
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
