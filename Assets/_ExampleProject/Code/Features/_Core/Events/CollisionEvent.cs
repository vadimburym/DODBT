using System;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Requests
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