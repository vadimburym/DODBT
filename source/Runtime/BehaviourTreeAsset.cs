// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.


using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree
{

    [Icon("Packages/com.vadimburym.dodbt/Editor/Icons/dodbt-icon.png")]
    public sealed class BehaviourTreeAsset : ScriptableObject
    {
        public string GUID => InternalGUID;
        
        [SerializeField, HideInInspector] internal string InternalGUID;
        [SerializeField, HideInInspector] internal int RootIndex;
        [SerializeField, HideInInspector] internal Node[] Nodes;
        [SerializeField, HideInInspector] internal SelectorNode[] SelectorNodes;
        [SerializeField, HideInInspector] internal SequenceNode[] SequenceNodes;
        [SerializeField, HideInInspector] internal MemorySelectorNode[] MemorySelectorNodes;
        [SerializeField, HideInInspector] internal MemorySequenceNode[] MemorySequenceNodes;
        [SerializeField, HideInInspector] internal ParallelNode[] ParallelNodes;
        [SerializeReference, HideInInspector] internal ILeaf[] Leafs;
        [SerializeField, HideInInspector] internal int ChildBufferSize;
        [SerializeField, HideInInspector] internal int RequiredFeatures;
        [SerializeField, HideInInspector] internal DecoratorNode[] DecoratorNodes = Array.Empty<DecoratorNode>();

        internal string CompatibilityError(bool decoratorsEnabled)
        {
            if ((RequiredFeatures & ~1) != 0) return "The tree requires an unsupported DODBT extension.";
            if (!decoratorsEnabled && (RequiredFeatures & 1) != 0) return "This tree requires DODBT_DECORATORS.";
            if (Nodes == null || Nodes.Length == 0 || RootIndex < 0 || RootIndex >= Nodes.Length)
                return "The tree has no valid compiled root.";
            for (int i = 0; i < Nodes.Length; i++)
            {
                var node = Nodes[i];
                if (node.Id > NodeId.Inverter) return "The tree contains an unsupported node type.";
                if (node.Id < NodeId.Timeout) continue;
                if (!decoratorsEnabled) return "This tree requires DODBT_DECORATORS.";
                if (DecoratorNodes == null || node.DataIndex >= DecoratorNodes.Length)
                    return "Missing compiled decorator data.";
                var data = DecoratorNodes[node.DataIndex];
                if (data.Child >= Nodes.Length || data.Child <= i) return "Invalid compiled decorator child.";
                if (node.Id != NodeId.Inverter && (double.IsNaN(data.Duration) || double.IsInfinity(data.Duration) || data.Duration < 0))
                    return "Decorator duration must be finite and nonnegative.";
            }
            return string.Empty;
        }
        
#if UNITY_EDITOR
        internal void SetupCompiledTree(
            Node[] nodes,
            int rootIndex,
            SelectorNode[] selectorNodes,
            SequenceNode[] sequenceNodes,
            MemorySelectorNode[] memorySelectorNodes,
            MemorySequenceNode[] memorySequenceNodes,
            ParallelNode[] parallelNodes,
            ILeaf[] leafs,
            int childBufferSize,
            DecoratorNode[] decoratorNodes = null)
        {
            Nodes = nodes;
            RootIndex = rootIndex;
            SelectorNodes = selectorNodes;
            SequenceNodes = sequenceNodes;
            MemorySelectorNodes = memorySelectorNodes;
            MemorySequenceNodes = memorySequenceNodes;
            ParallelNodes = parallelNodes;
            Leafs = leafs;
            ChildBufferSize = childBufferSize;
            DecoratorNodes = decoratorNodes ?? Array.Empty<DecoratorNode>();
            RequiredFeatures = DecoratorNodes.Length > 0 ? 1 : 0;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
