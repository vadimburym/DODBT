// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{

    using System;
    using UnityEngine;

    [Serializable]
    internal sealed class BtNodeHeader
    {
        public string Guid;
        public BtNodeKind Kind;
        public Vector2 Position;
        public string Title;
        public string ParentGuid;
    }
}
