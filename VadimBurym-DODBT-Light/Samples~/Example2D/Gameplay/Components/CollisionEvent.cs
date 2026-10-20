using System;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Components
{
    [Serializable]
    public struct CollisionEvent
    {
        public int Entity;
        public GameObject CollisionRef;

        public void Setup(int entity, GameObject collisionRef)
        {
            Entity = entity;
            CollisionRef = collisionRef;
        }
    }
}
