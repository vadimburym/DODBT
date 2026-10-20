// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{

    using System;
    using UnityEngine;

    [Serializable]
    internal sealed class BtRootNodeData
    {
        public string Guid;
        public Vector2 Position;
        public string ChildrenGuid;
    }
}
