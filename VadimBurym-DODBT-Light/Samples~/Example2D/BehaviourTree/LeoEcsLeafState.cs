using System;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree
{
    [Serializable, BtLeafState]
    public struct LeoEcsLeafState
    {
        public int StateIndex;

        public void Reset()
        {
            StateIndex = -1;
        }
    }
}
