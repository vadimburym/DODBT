// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{

    using System;
    using UnityEngine;
    using VadimBurym.DodBehaviourTree.Dots;


    [Serializable]
    internal sealed class BtLeafNodeData
    {
        public string Guid;

        [SerializeReference] public ILeaf Leaf;
    }
}
