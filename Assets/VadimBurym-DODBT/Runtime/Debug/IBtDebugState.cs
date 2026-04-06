// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

#if UNITY_EDITOR
namespace VadimBurym.DodBehaviourTree
{
    public interface IBtDebugState
    {
        NodeStatus[] DebugStatus { get; }
        List<string> DebugRunningLeafs { get; }
    }
}
#endif