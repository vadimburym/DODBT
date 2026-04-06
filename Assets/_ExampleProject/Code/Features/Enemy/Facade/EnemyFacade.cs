using _ExampleProject.Code.Features._Core.Behaviours;
using UnityEngine;
using UnityEngine.AI;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Enemy.Facade
{
    public sealed class EnemyFacade : MonoBehaviour
    {
        public EcsEntity EcsEntity;
        public Rigidbody2D Rigidbody;
        public BtMonoDebug BtMonoDebug;
        public NavMeshAgent NavMeshAgent;
        public Transform FirePoint;
        
        private void Awake()
        {
            NavMeshAgent.updatePosition = false;
            NavMeshAgent.updateRotation = false;
            NavMeshAgent.updateUpAxis = false;
        }
    }
}