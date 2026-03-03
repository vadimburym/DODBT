using System;
using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Features.Projectile.Components
{
    [Serializable]
    public struct ProjectileTag
    {
        public ProjectileId ProjectileId;
        public GameObject GameObjectRef;

        public void Setup(ProjectileId projectileId, GameObject gameObjectRef)
        {
            ProjectileId = projectileId;
            GameObjectRef = gameObjectRef;
        }
    }
}