// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{

    using System;
    using System.Collections.Generic;

    [Serializable]
    internal sealed class BtMemorySequenceNodeData
    {
        public string Guid;
        public List<string> ChildrenGuids = new();
        public bool ResetOnFailure;
        public bool ResetOnAbort;
    }
}
