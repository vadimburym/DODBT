// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{

    using System;
    using System.Collections.Generic;

    [Serializable]
    internal sealed class BtParallelNodeData
    {
        public string Guid;
        public int FailureThreshold;
        public int SuccessThreshold;
        public bool CacheChildStatus;
        public List<string> ChildrenGuids = new();
    }
}
