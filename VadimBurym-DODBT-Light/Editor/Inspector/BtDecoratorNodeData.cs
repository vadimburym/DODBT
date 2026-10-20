// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    // Preserve graph data when the optional extension is disabled.
    [Serializable]
    internal sealed class BtDecoratorNodeData
    {
        public string Guid;
        [Tooltip("Duration in seconds. Timed trees require an explicit monotonic timestamp in seconds.")]
        public double Duration = 1;
        public List<string> ChildrenGuids = new();
    }
}
