// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.Collections.Generic;

#if UNITY_EDITOR
namespace VadimBurym.DodBehaviourTree.Dots
{
    internal interface IBtDebugState
    {
        NodeStatus[] DebugStatus { get; }
        List<string> DebugRunningLeafs { get; }
    }
}
#endif
