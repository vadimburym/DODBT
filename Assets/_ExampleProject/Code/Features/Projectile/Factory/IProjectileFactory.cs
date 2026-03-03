using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Features.Projectile.Factory
{
    public interface IProjectileFactory
    {
        int Create(ProjectileId id, Vector2 position, Vector2 direction);
    }
}