using System;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.States
{
    [Serializable]
    public struct RetreatEntityState
    {
        public float RetreatDistance;
        public int EntityIndex;
        public bool IsReached;
        public NodeStatus StateStatus;

        public void Setup(int entityIndex, float retreatDistance)
        {
            EntityIndex = entityIndex;
            RetreatDistance = retreatDistance;
            IsReached = true;
        }
    }
}