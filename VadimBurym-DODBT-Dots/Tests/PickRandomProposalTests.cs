// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class PickRandomTraversalTests
    {
        [Test]
        public void Executor_CoversEveryChildExactlyOnce_ForAllSupportedSizesAndStarts()
        {
            var seen = new bool[255];
            for (int size = 1; size <= 255; size++)
            {
                using var builder = new BlobBuilder(Allocator.Temp);
                ref var data = ref builder.ConstructRoot<BehaviourTreeBlob>();
                var selectors = builder.Allocate(ref data.MemorySelectorNodes, 1);
                selectors[0] = new MemorySelectorNode { ChildCount = (byte)size, PickRandom = 1 };
                var blob = builder.CreateBlobAssetReference<BehaviourTreeBlob>(Allocator.Temp);
                try
                {
                    var node = new Node { DataIndex = 0 };
                    for (int first = 0; first < size; first++)
                    {
                        System.Array.Clear(seen, 0, seen.Length);
                        var state = new NodeStateElement { MemoryCursor = (byte)first, TmpA = (byte)first };
                        var random = new Random(12345);
                        int visits = 0;
                        while (state.MemoryCursor != byte.MaxValue)
                        {
                            if (seen[state.MemoryCursor]) Assert.Fail($"Repeated child, count {size}, start {first}.");
                            seen[state.MemoryCursor] = true;
                            visits++;
                            int pc = 0;
                            bool returning = true;
                            var status = NodeStatus.Failure;
                            NodeExecutor.ExecuteMemorySelector(ref pc, ref status, ref returning, ref state, ref node, ref blob.Value, ref random);
                        }
                        if (visits != size) Assert.Fail($"Visited {visits} of {size} children.");
                    }
                }
                finally { blob.Dispose(); }
            }
        }

        [Test]
        public void RandomTraversal_DoesNotIncreaseNodeStateSize()
        {
#if DODBT_SMALL_SIZE
            const int expected = 6;
#else
            const int expected = 8;
#endif
            Assert.That(Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<NodeStateElement>(), Is.EqualTo(expected));
        }
    }
}
