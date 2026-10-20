using DODBT.Light.Example.BehaviourTree;
using System;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree
{
    [Serializable]
    public struct AiBrain
    {
        public string BehaviourTreeGUID;
        public BtState<LeoEcsLeafState> StateRef;
        public LeoEcsContext ContextRef;
        public float TickInterval;
        public float TickTime;

        public void Setup(string behaviourTreeGuid, BtState<LeoEcsLeafState> btState, LeoEcsContext btContext, float tickInterval, float tickTime)
        {
            BehaviourTreeGUID = behaviourTreeGuid;
            StateRef = btState;
            ContextRef = btContext;
            TickInterval = tickInterval;
            TickTime = tickTime;
        }
    }
}
