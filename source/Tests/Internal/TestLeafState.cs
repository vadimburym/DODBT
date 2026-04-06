// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;

namespace VadimBurym.DodBehaviourTree.Tests
{
    [Serializable]
    internal struct TestLeafState
    {
        public int EnterCount;
        public int TickCount;
        public int ExitCount;
        public int AbortCount;
        public NodeStatus LastExitStatus;
    }
}