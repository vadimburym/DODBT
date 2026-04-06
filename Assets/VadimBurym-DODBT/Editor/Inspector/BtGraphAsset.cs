// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if ODIN_INSPECTOR
[HideMonoScript]
#endif
[Icon("Packages/com.vadimburym.dodbt/Editor/Icons/dodbt-icon.png")]
internal sealed class BtGraphAsset : ScriptableObject
{
    [HideInInspector] public BehaviourTreeAsset CompiledAsset;
    [HideInInspector] public string[] GuidsByCompiledId;
    
#if ODIN_INSPECTOR
    [ReadOnly] public string CreationDate;
    [ReadOnly] public string LastModifiedDate;
    [ReadOnly] public string CompiledVersion;
    [ReadOnly] public int NodesCount;
    [ReadOnly] public int LeafNodesCount;
#else
    [HideInInspector] public string CreationDate;
    [HideInInspector] public string LastModifiedDate;
    [HideInInspector] public string CompiledVersion;
    [HideInInspector] public int NodesCount;
    [HideInInspector] public int LeafNodesCount;
#endif
    
    [HideInInspector] public bool NotActualCompiledVersion;
    [HideInInspector] public BtRootNodeData RootNode;
    [HideInInspector] public List<BtNodeHeader> Nodes = new();
    [HideInInspector] public List<BtLeafNodeData> LeafNodes = new();
    [HideInInspector] public List<BtSequenceNodeData> SequenceNodes = new();
    [HideInInspector] public List<BtMemorySequenceNodeData> MemorySequenceNodes = new();
    [HideInInspector] public List<BtSelectorNodeData> SelectorNodes = new();
    [HideInInspector] public List<BtMemorySelectorNodeData> MemorySelectorNodes = new();
    [HideInInspector] public List<BtParallelNodeData> ParallelNodes = new();

#if ODIN_INSPECTOR
    [InfoBox(
        "The compiled version is different from the current version of the graph. Open in Editor and compile the asset to the current version.",
        InfoMessageType.Warning,
        "NotActualCompiled")]
    [Button("Open In Editor", ButtonSizes.Large)] //[GUIColor(0.45f, 0.55f, 0.95f)]
    internal void OpenAsset() => BtEditorWindow.OpenWithAsset(this);
    internal bool NotActualCompiled() => NotActualCompiledVersion;
    
    internal int FindHeaderIndex(string guid)
    {
        for (int i = 0; i < Nodes.Count; i++)
            if (Nodes[i].Guid == guid) return i;
        return -1;
    }

    internal BtNodeHeader FindHeader(string guid)
    {
        int index = FindHeaderIndex(guid);
        return index < 0 ? null : Nodes[index];
    }

    internal BtLeafNodeData FindLeafData(string guid)
    {
        for (int i = 0; i < LeafNodes.Count; i++)
            if (LeafNodes[i].Guid == guid) return LeafNodes[i];
        return null;
    }
    
    internal BtSequenceNodeData FindSequenceData(string guid)
    {
        for (int i = 0; i < SequenceNodes.Count; i++)
            if (SequenceNodes[i].Guid == guid) return SequenceNodes[i];
        return null;
    }

    internal BtMemorySequenceNodeData FindMemorySequenceData(string guid)
    {
        for (int i = 0; i < MemorySequenceNodes.Count; i++)
            if (MemorySequenceNodes[i].Guid == guid) return MemorySequenceNodes[i];
        return null;
    }
    
    internal BtSelectorNodeData FindSelectorData(string guid)
    {
        for (int i = 0; i < SelectorNodes.Count; i++)
            if (SelectorNodes[i].Guid == guid) return SelectorNodes[i];
        return null;
    }

    internal BtMemorySelectorNodeData FindMemorySelectorData(string guid)
    {
        for (int i = 0; i < MemorySelectorNodes.Count; i++)
            if (MemorySelectorNodes[i].Guid == guid) return MemorySelectorNodes[i];
        return null;
    }
    
    internal BtParallelNodeData FindParallelData(string guid)
    {
        for (int i = 0; i < ParallelNodes.Count; i++)
            if (ParallelNodes[i].Guid == guid) return ParallelNodes[i];
        return null;
    }

    internal void RemoveTypedDataByGuid(string guid)
    {
        LeafNodes.RemoveAll(data => data.Guid == guid);
        SequenceNodes.RemoveAll(data => data.Guid == guid);
        MemorySequenceNodes.RemoveAll(data => data.Guid == guid);
        SelectorNodes.RemoveAll(x => x.Guid == guid);
        MemorySelectorNodes.RemoveAll(x => x.Guid == guid);
        ParallelNodes.RemoveAll(x => x.Guid == guid);
        ReloadNodeCounts();
    }

    internal List<string> GetChildrenList(string parentGuid, BtNodeKind kind)
    {
        switch (kind)
        {
            case BtNodeKind.Sequence:
                return FindSequenceData(parentGuid)?.ChildrenGuids;
            case BtNodeKind.MemorySequence:
                return FindMemorySequenceData(parentGuid)?.ChildrenGuids;
            case BtNodeKind.Selector:
                return FindSelectorData(parentGuid)?.ChildrenGuids;
            case BtNodeKind.MemorySelector:
                return FindMemorySelectorData(parentGuid)?.ChildrenGuids;
            case BtNodeKind.Parallel:
                return FindParallelData(parentGuid)?.ChildrenGuids;
            default:
                return null;
        }
    }

    internal void ReloadNodeCounts()
    {
        NodesCount = Nodes.Count;
        LeafNodesCount = LeafNodes.Count;
    }

    internal void SetupNewCompiledVersion()
    {
        CompiledVersion = LastModifiedDate;
        NotActualCompiledVersion = false;
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(CompiledAsset);
    }
    
    internal void MarkModified()
    {
        LastModifiedDate = System.DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture);
        NotActualCompiledVersion = true;
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(CompiledAsset);
    }
#endif
}


