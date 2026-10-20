using System;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree.States
{
    [Serializable]
    public struct ShootEntityState
    {
        public int TargetShots;
        public int CurrentShots;
        public float TickTime;
        public int EntityIndex;
        public NodeStatus StateStatus;

        public void Setup(int targetShots, int entityIndex)
        {
            TargetShots = targetShots;
            EntityIndex = entityIndex;
        }
    }
}
