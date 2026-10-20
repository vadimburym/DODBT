using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Projectiles
{
    public interface IProjectileFactory
    {
        int Create(ProjectileId id, Vector2 position, Vector2 direction, Rigidbody2D owner);
    }
}
