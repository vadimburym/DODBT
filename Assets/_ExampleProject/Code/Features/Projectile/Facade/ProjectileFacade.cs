using _ExampleProject.Code.Features._Core.Behaviours;
using UnityEngine;

namespace _ExampleProject.Code.Features.Projectile.Facade
{
    public sealed class ProjectileFacade : MonoBehaviour
    {
        public EcsEntity EcsEntity;
        public EcsCollider EcsCollider;
        public Rigidbody2D Rigidbody;
    }
}