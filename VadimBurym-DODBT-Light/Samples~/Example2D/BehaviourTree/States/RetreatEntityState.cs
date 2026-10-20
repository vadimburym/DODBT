using System;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree.States
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
