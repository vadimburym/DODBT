// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using Unity.Collections;
using Unity.Entities;

namespace VadimBurym.DodBehaviourTree.Dots
{
    public struct BehaviourTreeBlob
    {
        public int RootIndex;
        public BlobArray<Node> Nodes;
        internal BlobArray<SelectorNode> SelectorNodes;
        internal BlobArray<SequenceNode> SequenceNodes;
        internal BlobArray<MemorySelectorNode> MemorySelectorNodes;
        internal BlobArray<MemorySequenceNode> MemorySequenceNodes;
        internal BlobArray<ParallelNode> ParallelNodes;
        public BlobArray<LeafData> Leafs;
    }
}
