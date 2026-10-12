using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree
{
    // Keep the serialized descriptor available when the optional executor is disabled.
    [Serializable]
    internal struct DecoratorNode
    {
#if DODBT_SMALL_SIZE
        [SerializeField] internal byte Child;
#else
        [SerializeField] internal ushort Child;
#endif
        [SerializeField] internal double Duration;
    }

#if DODBT_DECORATORS
    internal struct DecoratorState
    {
        internal double Deadline;
        internal bool HasDeadline;
        internal bool Active;
    }
#endif
}
