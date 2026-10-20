using DODBT.Light.Example.BehaviourTree;
using DODBT.Light.Example.Gameplay.Scene;
using UnityEngine;
using UnityEngine.AI;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.Gameplay.Enemies
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
