// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;

namespace VadimBurym.DodBehaviourTree
{
#if ODIN_INSPECTOR
    [HideMonoScript]
#endif
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
            int childBufferSize)
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
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}