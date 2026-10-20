// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

#if DODBT_SMALL_SIZE
using NodeIndex = System.Byte;
#else
using NodeIndex = System.UInt16;
#endif
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal static class TestTreeFactory
    {
        private const NodeIndex None = NodeIndex.MaxValue;

        public static TestTreeRunner CreateRunner(TestNodeSpec rootSpec, int randomSeed = 1)
        {
            if (rootSpec == null)
                throw new ArgumentNullException(nameof(rootSpec));

            BuildLayout(
                rootSpec,
                out var nodes,
                out var selectorNodes,
                out var sequenceNodes,
                out var memorySelectorNodes,
                out var memorySequenceNodes,
                out var parallelNodes,
                out var leafSpecs);

            var asset = BuildAsset(
                nodes,
                selectorNodes,
                sequenceNodes,
                memorySelectorNodes,
                memorySequenceNodes,
                parallelNodes,
                leafSpecs);

            var blob = asset.CreateBlob();
            UnityEngine.Object.DestroyImmediate(asset);

            var leafNames = new string[leafSpecs.Count];
            for (var i = 0; i < leafSpecs.Count; i++)
                leafNames[i] = leafSpecs[i].Name;

            return new TestTreeRunner(blob, new TestContext(leafNames), randomSeed);
        }

        public static BehaviourTreeAsset CreateAsset(TestNodeSpec rootSpec)
        {
            if (rootSpec == null)
                throw new ArgumentNullException(nameof(rootSpec));

            BuildLayout(
                rootSpec,
                out var nodes,
                out var selectorNodes,
                out var sequenceNodes,
                out var memorySelectorNodes,
                out var memorySequenceNodes,
                out var parallelNodes,
                out var leafSpecs);

            var asset = BuildAsset(
                nodes,
                selectorNodes,
                sequenceNodes,
                memorySelectorNodes,
                memorySequenceNodes,
                parallelNodes,
                leafSpecs);

            return asset;
        }

        private static BehaviourTreeAsset BuildAsset(
            List<Node> nodes,
            List<SelectorNode> selectorNodes,
            List<SequenceNode> sequenceNodes,
            List<MemorySelectorNode> memorySelectorNodes,
            List<MemorySequenceNode> memorySequenceNodes,
            List<ParallelNode> parallelNodes,
            List<RecordingLeafSpec> leafSpecs)
        {
            var asset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            asset.InternalGUID = Guid.NewGuid().ToString("N");
            asset.RootIndex = 0;
            asset.Nodes = nodes.ToArray();
            asset.SelectorNodes = selectorNodes.ToArray();
            asset.SequenceNodes = sequenceNodes.ToArray();
            asset.MemorySelectorNodes = memorySelectorNodes.ToArray();
            asset.MemorySequenceNodes = memorySequenceNodes.ToArray();
            asset.ParallelNodes = parallelNodes.ToArray();

            var result = new LeafData[leafSpecs.Count];
            for (var i = 0; i < leafSpecs.Count; i++)
            {
                var leafData = new LeafData {
                    LeafId = RecordingLeaf.LeafId,
                    Bytes = default };

                for (var j = 0; j < leafSpecs[i].Statuses.Length; j++)
                    leafData.Bytes.Add((byte)leafSpecs[i].Statuses[j]);

                result[i] = leafData;
            }
            asset.Leafs = result;
            string context = LeafBindingRegistry.Identity(typeof(TestContext));
            string leaf = LeafBindingRegistry.Identity(typeof(RecordingLeaf));
            LeafBindingRegistry.Register(context, id => id == 0 ? leaf : null);
            SetBindings(asset, typeof(TestContext), typeof(RecordingLeaf));
            return asset;
        }

        internal static void SetBindings(BehaviourTreeAsset asset, Type context, Type leaf, int id = 0)
        {
            asset.InternalFormatVersion = BehaviourTreeAsset.CurrentFormatVersion;
            asset.InternalContextIdentity = LeafBindingRegistry.Identity(context);
            asset.InternalLeafBindings = new[] { new LeafBinding { Id = id, TypeIdentity = LeafBindingRegistry.Identity(leaf) } };
        }

        private static void BuildLayout(
            TestNodeSpec rootSpec,
            out List<Node> nodes,
            out List<SelectorNode> selectorNodes,
            out List<SequenceNode> sequenceNodes,
            out List<MemorySelectorNode> memorySelectorNodes,
            out List<MemorySequenceNode> memorySequenceNodes,
            out List<ParallelNode> parallelNodes,
            out List<RecordingLeafSpec> leafSpecs)
        {
            nodes = new List<Node>();
            selectorNodes = new List<SelectorNode>();
            sequenceNodes = new List<SequenceNode>();
            memorySelectorNodes = new List<MemorySelectorNode>();
            memorySequenceNodes = new List<MemorySequenceNode>();
            parallelNodes = new List<ParallelNode>();
            leafSpecs = new List<RecordingLeafSpec>();
            var queue = new Queue<(TestNodeSpec Spec, NodeIndex Parent)>();
            queue.Enqueue((rootSpec, None));
            while (queue.Count > 0)
            {
                var (spec, parent) = queue.Dequeue();
                var index = checked((NodeIndex)nodes.Count);
                nodes.Add(default);
                var node = new Node { ParentIndex = parent };
                NodeIndex first = checked((NodeIndex)(nodes.Count + queue.Count));
                switch (spec)
                {
                    case RecordingLeafSpec leaf:
                        node.Id = NodeId.Leaf; node.DataIndex = checked((NodeIndex)leafSpecs.Count);
                        leafSpecs.Add(leaf);
                        break;
                    case SelectorSpec selector:
                        node.Id = NodeId.Selector; node.DataIndex = checked((NodeIndex)selectorNodes.Count);
                        selectorNodes.Add(new SelectorNode { FirstChild = first, ChildCount = checked((byte)selector.Children.Length) });
                        break;
                    case SequenceSpec sequence:
                        node.Id = NodeId.Sequence; node.DataIndex = checked((NodeIndex)sequenceNodes.Count);
                        sequenceNodes.Add(new SequenceNode { FirstChild = first, ChildCount = checked((byte)sequence.Children.Length) });
                        break;
                    case MemorySelectorSpec selector:
                        node.Id = NodeId.MemorySelector; node.DataIndex = checked((NodeIndex)memorySelectorNodes.Count);
                        memorySelectorNodes.Add(new MemorySelectorNode { FirstChild = first, ChildCount = checked((byte)selector.Children.Length),
                            PickRandom = selector.PickRandom ? (byte)1 : (byte)0, ResetOnAbort = selector.ResetOnAbort ? (byte)1 : (byte)0 });
                        break;
                    case MemorySequenceSpec sequence:
                        node.Id = NodeId.MemorySequence; node.DataIndex = checked((NodeIndex)memorySequenceNodes.Count);
                        memorySequenceNodes.Add(new MemorySequenceNode { FirstChild = first, ChildCount = checked((byte)sequence.Children.Length),
                            ResetOnFailure = sequence.ResetOnFailure ? (byte)1 : (byte)0, ResetOnAbort = sequence.ResetOnAbort ? (byte)1 : (byte)0 });
                        break;
                    case ParallelSpec parallel:
                        node.Id = NodeId.Parallel; node.DataIndex = checked((NodeIndex)parallelNodes.Count);
                        parallelNodes.Add(new ParallelNode { FirstChild = first, ChildCount = checked((byte)parallel.Children.Length),
                            SuccessThreshold = parallel.SuccessThreshold, FailsThreshold = parallel.FailsThreshold,
                            CacheChildStatus = parallel.CacheChildStatus ? (byte)1 : (byte)0 });
                        break;
                    default: throw new ArgumentException("Unknown test node.", nameof(spec));
                }
                nodes[index] = node;
                if (spec is CompositeSpec composite)
                    foreach (var child in composite.Children) queue.Enqueue((child, index));
            }
        }
    }
}
