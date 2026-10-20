// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Light.Editor;

using System;

namespace VadimBurym.DodBehaviourTree.Light.Tests
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