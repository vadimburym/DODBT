using System;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    public struct LeoEcsLeafState
    {
        public int StateIndex;

        public void Reset()
        {
            StateIndex = -1;
        }
    }
}