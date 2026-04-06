// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

namespace VadimBurym.DodBehaviourTree
{
    public enum NodeStatus : byte
    {
        None = 0,
        Running = 1,
        Success = 2,
        Failure = 3,
    }
}