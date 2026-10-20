// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

#if DODBT_SMALL_SIZE
using NodeIndex = System.Byte;
#else
using NodeIndex = System.UInt16;
#endif


using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots
{
    public sealed class BehaviourTreeAsset : ScriptableObject
    {
        public string GUID => InternalGUID;

        [SerializeField, HideInInspector] internal string InternalGUID;
#if UNITY_EDITOR
        [SerializeField, HideInInspector] internal string InternalCompiledRevision;
#endif
        [SerializeField, HideInInspector] internal int RootIndex;
        [SerializeField, HideInInspector] internal Node[] Nodes;
        [SerializeField, HideInInspector] internal SelectorNode[] SelectorNodes;
        [SerializeField, HideInInspector] internal SequenceNode[] SequenceNodes;
        [SerializeField, HideInInspector] internal MemorySelectorNode[] MemorySelectorNodes;
        [SerializeField, HideInInspector] internal MemorySequenceNode[] MemorySequenceNodes;
        [SerializeField, HideInInspector] internal ParallelNode[] ParallelNodes;
        [SerializeField, HideInInspector] internal LeafData[] Leafs;
        [SerializeField, HideInInspector] internal int InternalFormatVersion;
        [SerializeField, HideInInspector] internal string InternalContextIdentity;
        [SerializeField, HideInInspector] internal LeafBinding[] InternalLeafBindings;

#if DODBT_SMALL_SIZE
        internal const int CurrentFormatVersion = 2;
#else
        internal const int CurrentFormatVersion = 1;
#endif

        internal string GetCompatibilityError()
        {
            string error = InternalFormatVersion != CurrentFormatVersion
                ? "The compiled tree uses an older or different data format."
                : LeafBindingRegistry.Validate(InternalContextIdentity, InternalLeafBindings, Leafs);
            return error == null ? null : name + ": " + error + " Recompile the source BT graph before creating its blob.";
        }

        public void FillAgentStateBuffers(
            //DynamicBuffer<NodeStateElement> nodesBuffer,
            //DynamicBuffer<LeafStateElement> leafsBuffer,
            Entity entity,
            EntityCommandBuffer ecb)
        {
            //nodesBuffer.Clear();
            //leafsBuffer.Clear();
#if UNITY_EDITOR
            ecb.AddSharedComponent(entity, new BtDebugTree {
                GraphGuid = new FixedString64Bytes(InternalGUID ?? ""),
                CompiledRevision = new FixedString64Bytes(InternalCompiledRevision ?? "")
            });
#endif

            const NodeIndex None = NodeIndex.MaxValue;
            NodeIndex leafIndex = 0;
            for (int i = 0; i < Nodes.Length; i++)
            {
                var isLeaf = Nodes[i].Id == NodeId.Leaf;
                ecb.AppendToBuffer(entity, new NodeStateElement {
                    CachedStatus = 0,
                    Cursor = 0xFF,
                    LeafStateIndex = isLeaf ? leafIndex : None,
                    MemoryCursor = 0xFF,
                    TmpA = 0,
                    TmpB = 0,
                });
                if (isLeaf) leafIndex++;
            }
            for (int i = 0; i < Leafs.Length; i++)
                ecb.AppendToBuffer(entity, new LeafStateElement {
                    IsEntered = 0,
                    BufferIndex = (ushort)i,
                    StateEntity = Entity.Null
                });
        }

        public BlobAssetReference<BehaviourTreeBlob> CreateBlob()
        {
            string error = GetCompatibilityError();
            if (error != null) throw new System.InvalidOperationException(error);
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BehaviourTreeBlob>();

            var nodes = builder.Allocate(ref root.Nodes, Nodes.Length);
            for (int i = 0; i < Nodes.Length; i++)
                nodes[i] = Nodes[i];
            var selectorNodes = builder.Allocate(ref root.SelectorNodes, SelectorNodes.Length);
            for (int i = 0; i < SelectorNodes.Length; i++)
                selectorNodes[i] = SelectorNodes[i];
            var sequenceNodes = builder.Allocate(ref root.SequenceNodes, SequenceNodes.Length);
            for (int i = 0; i < SequenceNodes.Length; i++)
                sequenceNodes[i] = SequenceNodes[i];
            var memorySelectorNodes = builder.Allocate(ref root.MemorySelectorNodes, MemorySelectorNodes.Length);
            for (int i = 0; i < MemorySelectorNodes.Length; i++)
                memorySelectorNodes[i] = MemorySelectorNodes[i];
            var memorySequenceNodes = builder.Allocate(ref root.MemorySequenceNodes, MemorySequenceNodes.Length);
            for (int i = 0; i < MemorySequenceNodes.Length; i++)
                memorySequenceNodes[i] = MemorySequenceNodes[i];
            var parallelNodes = builder.Allocate(ref root.ParallelNodes, ParallelNodes.Length);
            for (int i = 0; i < ParallelNodes.Length; i++)
                parallelNodes[i] = ParallelNodes[i];
            var leafs = builder.Allocate(ref root.Leafs, Leafs.Length);
            for (int i = 0; i < Leafs.Length; i++)
                leafs[i] = Leafs[i];
            root.RootIndex = RootIndex;

            var blob = builder.CreateBlobAssetReference<BehaviourTreeBlob>(Allocator.Persistent);
            return blob;
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
            LeafData[] leafs,
            string contextIdentity,
            LeafBinding[] bindings)
        {
            Nodes = nodes;
            RootIndex = rootIndex;
            SelectorNodes = selectorNodes;
            SequenceNodes = sequenceNodes;
            MemorySelectorNodes = memorySelectorNodes;
            MemorySequenceNodes = memorySequenceNodes;
            ParallelNodes = parallelNodes;
            Leafs = leafs;
            InternalFormatVersion = CurrentFormatVersion;
            InternalContextIdentity = contextIdentity;
            InternalLeafBindings = bindings;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
