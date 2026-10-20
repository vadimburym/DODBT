// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light
{
    public enum NodeStatus : byte
    {
        None = 0,
        Running = 1,
        Success = 2,
        Failure = 3,
    }
}