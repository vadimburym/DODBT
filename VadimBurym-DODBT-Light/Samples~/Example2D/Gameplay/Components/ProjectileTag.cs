using System;
using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Components
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
