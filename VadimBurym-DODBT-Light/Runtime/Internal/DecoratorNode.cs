// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light
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
