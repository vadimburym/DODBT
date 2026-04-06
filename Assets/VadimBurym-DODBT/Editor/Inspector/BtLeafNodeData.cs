// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

[Serializable]
internal sealed class BtLeafNodeData
{
    public string Guid;
#if ODIN_INSPECTOR
    [HideLabel]
#endif
    [SerializeReference] public ILeaf Leaf;
}