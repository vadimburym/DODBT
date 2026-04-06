// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Tests
{
    internal static class TestTreeFactory
    {
        public static BehaviourTree<TestContext, TestLeafState> CreateTree(BehaviourTreeAsset asset)
        {
            var tree = new BehaviourTree<TestContext, TestLeafState>();
            tree.Construct(asset);
            return tree;
        }

        public static BtState<TestLeafState> CreateInitializedState(BehaviourTree<TestContext, TestLeafState> tree)
        {
            var state = new BtState<TestLeafState>();
            tree.FillInitialState(state);
            return state;
        }

        public static BehaviourTreeAsset CreateLeafAsset(ILeaf leaf)
        {
            return CreateAsset(
                new[] { CreateNode(NodeId.Leaf, 0) },
                Array.Empty<SelectorNode>(),
                Array.Empty<SequenceNode>(),
                Array.Empty<MemorySelectorNode>(),
                Array.Empty<MemorySequenceNode>(),
                Array.Empty<ParallelNode>(),
                new[] { leaf });
        }

        public static BehaviourTreeAsset CreateSelectorAsset(params ILeaf[] leafs)
        {
            return CreateCompositeAsset(
                NodeId.Selector,
                leafs,
                selectorNodes: new[] { new SelectorNode { FirstChild = 1, ChildCount = checked((byte)leafs.Length) } });
        }

        public static BehaviourTreeAsset CreateSequenceAsset(params ILeaf[] leafs)
        {
            return CreateCompositeAsset(
                NodeId.Sequence,
                leafs,
                sequenceNodes: new[] { new SequenceNode { FirstChild = 1, ChildCount = checked((byte)leafs.Length) } });
        }

        public static BehaviourTreeAsset CreateMemorySelectorAsset(bool pickRandom, bool resetOnAbort, params ILeaf[] leafs)
        {
            return CreateCompositeAsset(
                NodeId.MemorySelector,
                leafs,
                memorySelectorNodes: new[]
                {
                    new MemorySelectorNode
                    {
                        FirstChild = 1,
                        ChildCount = checked((byte)leafs.Length),
                        PickRandom = pickRandom,
                        ResetOnAbort = resetOnAbort
                    }
                });
        }

        public static BehaviourTreeAsset CreateMemorySequenceAsset(bool resetOnFailure, bool resetOnAbort, params ILeaf[] leafs)
        {
            return CreateCompositeAsset(
                NodeId.MemorySequence,
                leafs,
                memorySequenceNodes: new[]
                {
                    new MemorySequenceNode
                    {
                        FirstChild = 1,
                        ChildCount = checked((byte)leafs.Length),
                        ResetOnFailure = resetOnFailure,
                        ResetOnAbort = resetOnAbort
                    }
                });
        }

        public static BehaviourTreeAsset CreateParallelAsset(byte successThreshold, byte failsThreshold, bool cacheChildStatus, params ILeaf[] leafs)
        {
            return CreateCompositeAsset(
                NodeId.Parallel,
                leafs,
                parallelNodes: new[]
                {
                    new ParallelNode
                    {
                        FirstChild = 1,
                        ChildCount = checked((byte)leafs.Length),
                        SuccessThreshold = successThreshold,
                        FailsThreshold = failsThreshold,
                        CacheChildStatus = cacheChildStatus
                    }
                });
        }

        private static BehaviourTreeAsset CreateCompositeAsset(
            NodeId rootId,
            ILeaf[] leafs,
            SelectorNode[] selectorNodes = null,
            SequenceNode[] sequenceNodes = null,
            MemorySelectorNode[] memorySelectorNodes = null,
            MemorySequenceNode[] memorySequenceNodes = null,
            ParallelNode[] parallelNodes = null)
        {
            if (leafs == null || leafs.Length == 0)
                throw new ArgumentException("At least one leaf is required.", nameof(leafs));

            var nodes = new Node[leafs.Length + 1];
            nodes[0] = CreateNode(rootId, 0);
            for (var i = 0; i < leafs.Length; i++)
                nodes[i + 1] = CreateNode(NodeId.Leaf, i);

            return CreateAsset(
                nodes,
                selectorNodes ?? Array.Empty<SelectorNode>(),
                sequenceNodes ?? Array.Empty<SequenceNode>(),
                memorySelectorNodes ?? Array.Empty<MemorySelectorNode>(),
                memorySequenceNodes ?? Array.Empty<MemorySequenceNode>(),
                parallelNodes ?? Array.Empty<ParallelNode>(),
                leafs);
        }

        private static BehaviourTreeAsset CreateAsset(
            Node[] nodes,
            SelectorNode[] selectorNodes,
            SequenceNode[] sequenceNodes,
            MemorySelectorNode[] memorySelectorNodes,
            MemorySequenceNode[] memorySequenceNodes,
            ParallelNode[] parallelNodes,
            ILeaf[] leafs)
        {
            var asset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            asset.InternalGUID = Guid.NewGuid().ToString("N");
            asset.RootIndex = 0;
            asset.Nodes = nodes;
            asset.SelectorNodes = selectorNodes;
            asset.SequenceNodes = sequenceNodes;
            asset.MemorySelectorNodes = memorySelectorNodes;
            asset.MemorySequenceNodes = memorySequenceNodes;
            asset.ParallelNodes = parallelNodes;
            asset.Leafs = leafs;
            asset.ChildBufferSize = Math.Max(1, leafs.Length);
            return asset;
        }

        private static Node CreateNode(NodeId id, int dataIndex)
        {
            var node = new Node
            {
                Id = id
            };

#if DODBT_SMALL_SIZE
            node.DataIndex = checked((byte)dataIndex);
#else
            node.DataIndex = checked((ushort)dataIndex);
#endif
            return node;
        }
    }
}