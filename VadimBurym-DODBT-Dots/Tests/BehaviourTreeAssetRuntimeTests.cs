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

using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class BehaviourTreeAssetRuntimeTests
    {
        [Test]
        public void CreateBlob_CopiesRuntimeNodeArrays_AndPreservesLeafStatusBytes()
        {
            var asset = TestTreeFactory.CreateAsset(
                TestNodeSpec.Sequence(
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Failure, NodeStatus.Success),
                    TestNodeSpec.RecordingLeaf("C", NodeStatus.Running, NodeStatus.Success)));

            using (var blob = asset.CreateBlob())
            {
                ref var value = ref blob.Value;

                Assert.That(value.RootIndex, Is.EqualTo(0));
                Assert.That(value.Nodes.Length, Is.EqualTo(4));
                Assert.That(value.SequenceNodes.Length, Is.EqualTo(1));
                Assert.That(value.SelectorNodes.Length, Is.EqualTo(0));
                Assert.That(value.MemorySelectorNodes.Length, Is.EqualTo(0));
                Assert.That(value.MemorySequenceNodes.Length, Is.EqualTo(0));
                Assert.That(value.ParallelNodes.Length, Is.EqualTo(0));
                Assert.That(value.Leafs.Length, Is.EqualTo(3));
                Assert.That(value.Leafs[0].LeafId, Is.EqualTo(RecordingLeaf.LeafId));
                Assert.That(value.Leafs[1].LeafId, Is.EqualTo(RecordingLeaf.LeafId));
                Assert.That(value.Leafs[2].LeafId, Is.EqualTo(RecordingLeaf.LeafId));
                Assert.That(value.Leafs[0].Bytes.Length, Is.EqualTo(1));
                Assert.That(value.Leafs[0].Bytes[0], Is.EqualTo((byte)NodeStatus.Success));
                Assert.That(value.Leafs[1].Bytes.Length, Is.EqualTo(2));
                Assert.That(value.Leafs[1].Bytes[0], Is.EqualTo((byte)NodeStatus.Failure));
                Assert.That(value.Leafs[1].Bytes[1], Is.EqualTo((byte)NodeStatus.Success));
                Assert.That(value.Leafs[2].Bytes.Length, Is.EqualTo(2));
                Assert.That(value.Leafs[2].Bytes[0], Is.EqualTo((byte)NodeStatus.Running));
                Assert.That(value.Leafs[2].Bytes[1], Is.EqualTo((byte)NodeStatus.Success));
            }

            Assert.That(asset.GUID, Is.Not.Null.And.Not.Empty);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void FillAgentStateBuffers_CreatesNodeAndLeafBuffers_WithExpectedDefaults()
        {
            var asset = TestTreeFactory.CreateAsset(
                TestNodeSpec.Selector(
                    TestNodeSpec.RecordingLeaf("A", NodeStatus.Failure),
                    TestNodeSpec.RecordingLeaf("B", NodeStatus.Success)));

            using (var world = new World("DODBT.Tests.Asset"))
            {
                var entityManager = world.EntityManager;
                var entity = entityManager.CreateEntity();
                entityManager.AddBuffer<NodeStateElement>(entity);
                entityManager.AddBuffer<LeafStateElement>(entity);

                using (var ecb = new EntityCommandBuffer(Allocator.Temp))
                {
                    asset.FillAgentStateBuffers(entity, ecb);
                    ecb.Playback(entityManager);
                }

                var nodeStates = entityManager.GetBuffer<NodeStateElement>(entity);
                var leafStates = entityManager.GetBuffer<LeafStateElement>(entity);

                Assert.That(nodeStates.Length, Is.EqualTo(3));
                Assert.That(leafStates.Length, Is.EqualTo(2));
                Assert.That(nodeStates[0].LeafStateIndex, Is.EqualTo(NodeIndex.MaxValue));
                Assert.That(nodeStates[1].LeafStateIndex, Is.EqualTo((ushort)0));
                Assert.That(nodeStates[2].LeafStateIndex, Is.EqualTo((ushort)1));
                Assert.That(nodeStates[0].Cursor, Is.EqualTo((byte)0xFF));
                Assert.That(nodeStates[0].MemoryCursor, Is.EqualTo((byte)0xFF));
                Assert.That(nodeStates[0].CachedStatus, Is.EqualTo((byte)0));
                Assert.That(leafStates[0].IsEntered, Is.EqualTo((byte)0));
                Assert.That(leafStates[0].BufferIndex, Is.EqualTo((ushort)0));
                Assert.That(leafStates[1].BufferIndex, Is.EqualTo((ushort)1));
                Assert.That(leafStates[0].StateEntity, Is.EqualTo(Entity.Null));
                Assert.That(leafStates[1].StateEntity, Is.EqualTo(Entity.Null));
            }

            UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
