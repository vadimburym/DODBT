// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Dots.Generated;
using Random = Unity.Mathematics.Random;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    [GenerateBtRunner]
    public struct AuditContext
    {
        public NativeArray<int> Events;
        [ReadOnly] public NativeArray<NodeStatus> Outcomes;
        public NativeArray<uint> RandomValues;
        public byte UseRandom;
    }

    [Serializable, LeafCodeGen(0, typeof(AuditContext))]
    public sealed class AuditLeaf : ILeaf
    {
        public LeafData GetCompiledData() => default;
        public static void OnEnter(ref RunnerState_AuditContext state)
        {
            state.Context.Events[state.LeafState.BufferIndex * 4]++;
            if ((state.Context.UseRandom & 2) != 0) state.Random.NextUInt();
        }
        public static NodeStatus OnTick(ref RunnerState_AuditContext state)
        {
            int i = state.LeafState.BufferIndex;
            state.Context.Events[i * 4 + 1]++;
            if ((state.Context.UseRandom & 1) != 0) state.Context.RandomValues[i] = state.Random.NextUInt();
            return state.Context.Outcomes[i];
        }
        public static void OnExit(ref RunnerState_AuditContext state)
        {
            state.Context.Events[state.LeafState.BufferIndex * 4 + 2]++;
            if ((state.Context.UseRandom & 4) != 0) state.Random.NextUInt();
        }
        public static void OnAbort(ref RunnerState_AuditContext state)
        {
            state.Context.Events[state.LeafState.BufferIndex * 4 + 3]++;
            if ((state.Context.UseRandom & 8) != 0) state.Context.RandomValues[state.LeafState.BufferIndex] = state.Random.NextUInt();
        }
    }

    [Serializable, LeafCodeGen(65536, typeof(AuditContext))]
    public sealed class WideIdAuditLeaf : ILeaf
    {
        public LeafData GetCompiledData() => new LeafData { LeafId = 65536 };
        public static void OnEnter(ref RunnerState_AuditContext state) => AuditLeaf.OnEnter(ref state);
        public static NodeStatus OnTick(ref RunnerState_AuditContext state) => AuditLeaf.OnTick(ref state);
        public static void OnExit(ref RunnerState_AuditContext state) => AuditLeaf.OnExit(ref state);
        public static void OnAbort(ref RunnerState_AuditContext state) => AuditLeaf.OnAbort(ref state);
    }

    internal sealed class ProductionFixture : IDisposable
    {
        internal readonly World World = new World("DODBT Production Tests");
        internal Entity Entity;
        internal BlobAssetReference<BehaviourTreeBlob> Blob;
        internal AuditContext Context;
        internal Random Random = new Random(12345);
        internal DynamicBuffer<NodeStateElement> Nodes => World.EntityManager.GetBuffer<NodeStateElement>(Entity);
        internal DynamicBuffer<LeafStateElement> Leafs => World.EntityManager.GetBuffer<LeafStateElement>(Entity);
        internal DynamicBuffer<BtDebugNodeState> Trace => World.EntityManager.GetBuffer<BtDebugNodeState>(Entity);

        internal ProductionFixture(TestNodeSpec spec, bool trace = false, int leafId = 0)
        {
            var asset = TestTreeFactory.CreateAsset(spec);
            try
            {
                Entity = World.EntityManager.CreateEntity();
                World.EntityManager.AddBuffer<NodeStateElement>(Entity);
                World.EntityManager.AddBuffer<LeafStateElement>(Entity);
                for (int i = 0; i < asset.Leafs.Length; i++) asset.Leafs[i].LeafId = leafId;
                TestTreeFactory.SetBindings(asset, typeof(AuditContext), leafId == 0 ? typeof(AuditLeaf) : typeof(WideIdAuditLeaf), leafId);
                asset.InternalCompiledRevision = "test-revision";
                Blob = asset.CreateBlob();
                using var commands = new EntityCommandBuffer(Allocator.Temp);
                asset.FillAgentStateBuffers(Entity, commands);
                commands.Playback(World.EntityManager);
                int count = asset.Leafs.Length;
                Context = new AuditContext {
                    Events = new NativeArray<int>(count * 4, Allocator.Persistent),
                    Outcomes = new NativeArray<NodeStatus>(count, Allocator.Persistent),
                    RandomValues = new NativeArray<uint>(count, Allocator.Persistent)
                };
                for (int i = 0; i < count; i++) Context.Outcomes[i] = NodeStatus.Success;
                if (trace) World.EntityManager.AddBuffer<BtDebugNodeState>(Entity).ResizeUninitialized(asset.Nodes.Length);
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        internal NodeStatus Tick(bool trace = false)
        {
            var runner = new BTRunner_AuditContext();
            return trace ? runner.TickWithDebug(Entity, ref Blob.Value, ref Random, Nodes, Leafs, Context, 0, Trace) :
                runner.Tick(Entity, ref Blob.Value, ref Random, Nodes, Leafs, Context, 0);
        }

        public void Dispose()
        {
            if (World.IsCreated) { World.EntityManager.CompleteAllTrackedJobs(); World.Dispose(); }
            if (Blob.IsCreated) Blob.Dispose();
            if (Context.Events.IsCreated) Context.Events.Dispose();
            if (Context.Outcomes.IsCreated) Context.Outcomes.Dispose();
            if (Context.RandomValues.IsCreated) Context.RandomValues.Dispose();
        }
    }

    public sealed class ProductionRunnerTests
    {
        [Test]
        public void WideLeafId_DispatchesCorrectCallbacksInBurstJob()
        {
            Assert.That(BurstCompiler.IsEnabled, Is.True);
            using var f = new ProductionFixture(Leaf("wide"), leafId: 65536);
            using var result = new NativeArray<NodeStatus>(1, Allocator.TempJob);
            new AuditJob { Entity = f.Entity, Blob = f.Blob, Context = f.Context, Nodes = f.Nodes, Leafs = f.Leafs, Result = result }.Schedule().Complete();
            Assert.That(result[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Context.Events.ToArray(), Is.EqualTo(new[] { 1, 1, 1, 0 }));
        }

        private static TestNodeSpec Leaf(string name) => TestNodeSpec.RecordingLeaf(name, NodeStatus.Success);

        [Test]
        public void GeneratedRunner_ExecutesLifecycleAndReturnsRootStatus()
        {
            using var f = new ProductionFixture(TestNodeSpec.Sequence(Leaf("A"), Leaf("B")));
            Assert.That(f.Tick(), Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Context.Events.ToArray(), Is.EqualTo(new[] { 1, 1, 1, 0, 1, 1, 1, 0 }));
            Assert.That(f.Leafs[0].IsEntered, Is.Zero);
        }

        [Test]
        public void EditorTrace_PreservesRuntimeResultsAndState()
        {
            var spec = TestNodeSpec.MemorySequence(true, true, Leaf("A"), Leaf("B"));
            using var normal = new ProductionFixture(spec);
            using var debug = new ProductionFixture(spec, true);
            for (int tick = 0; tick < 3; tick++)
            {
                normal.Context.Outcomes[1] = debug.Context.Outcomes[1] = tick == 0 ? NodeStatus.Running : NodeStatus.Success;
                Assert.That(debug.Tick(true), Is.EqualTo(normal.Tick()));
                using var debugNodes = debug.Nodes.ToNativeArray(Allocator.Temp);
                using var normalNodes = normal.Nodes.ToNativeArray(Allocator.Temp);
                using var debugLeafs = debug.Leafs.ToNativeArray(Allocator.Temp);
                using var normalLeafs = normal.Leafs.ToNativeArray(Allocator.Temp);
                Assert.That(debugNodes.ToArray(), Is.EqualTo(normalNodes.ToArray()));
                Assert.That(debugLeafs.ToArray(), Is.EqualTo(normalLeafs.ToArray()));
                Assert.That(debug.Context.Events.ToArray(), Is.EqualTo(normal.Context.Events.ToArray()));
                Assert.That(debug.Trace[0].Status, Is.EqualTo(tick == 0 ? NodeStatus.Running : NodeStatus.Success));
            }
        }

        [Test]
        public void EditorTrace_ClearsAbortedRunningBranchAndKeepsCompletedResults()
        {
            using var f = new ProductionFixture(TestNodeSpec.Parallel(1, 2, false, Leaf("A"), Leaf("B")), true);
            f.Context.Outcomes[0] = NodeStatus.Running;
            Assert.That(f.Tick(true), Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Trace[0].Status, Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Trace[1].Status, Is.EqualTo(NodeStatus.None));
            Assert.That(f.Trace[2].Status, Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Context.Events[3], Is.EqualTo(1));
        }

        [Test]
        public void GeneratedRunner_CompilesWithBurstAndRunsInJob()
        {
            Assert.That(BurstCompiler.IsEnabled, Is.True, "This validation requires Burst to be enabled.");
            using var f = new ProductionFixture(TestNodeSpec.Sequence(Leaf("A"), Leaf("B")));
            using var result = new NativeArray<NodeStatus>(1, Allocator.TempJob);
            new AuditJob { Entity = f.Entity, Blob = f.Blob, Context = f.Context, Nodes = f.Nodes, Leafs = f.Leafs, Result = result }.Schedule().Complete();
            Assert.That(result[0], Is.EqualTo(NodeStatus.Success));
            Assert.That(f.Context.Events[5], Is.EqualTo(1));
        }

        [Test]
        public void EntityDebugSession_RemovesRequestAndTraceOnDispose()
        {
            using var f = new ProductionFixture(Leaf("A"));
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.GuidsByCompiledId = new[] { "leaf" };
            graph.CompiledRevision = "test-revision";
            graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Title = "A", Kind = BtNodeKind.Leaf });
            try
            {
                using (var session = new BtEntityDebugSession(f.World, f.Entity, graph))
                {
                    Assert.That(f.World.EntityManager.HasBuffer<BtDebugNodeState>(f.Entity), Is.True);
                    f.Tick(true);
                    Assert.That(session.Refresh(), Is.True);
                    Assert.That(session.DebugStatus[0], Is.EqualTo(NodeStatus.Success));
                }
                Assert.That(f.World.EntityManager.HasBuffer<BtDebugNodeState>(f.Entity), Is.False);
                using var query = f.World.EntityManager.CreateEntityQuery(typeof(BtDebugTarget));
                Assert.That(query.IsEmptyIgnoreFilter, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(graph); }
        }

        [Test]
        public void EntityDebugSession_RejectsRecompiledGraphWithTheSameNodeCount()
        {
            using var f = new ProductionFixture(Leaf("A"));
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.GuidsByCompiledId = new[] { "leaf" };
            graph.CompiledRevision = "new-revision";
            try
            {
                Assert.Throws<InvalidOperationException>(() => new BtEntityDebugSession(f.World, f.Entity, graph));
                Assert.That(f.World.EntityManager.HasBuffer<BtDebugNodeState>(f.Entity), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(graph); }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct AuditJob : IJob
        {
            public Entity Entity;
            public BlobAssetReference<BehaviourTreeBlob> Blob;
            public AuditContext Context;
            public DynamicBuffer<NodeStateElement> Nodes;
            public DynamicBuffer<LeafStateElement> Leafs;
            public NativeArray<NodeStatus> Result;
            public void Execute()
            {
                var random = new Random(12345);
                Result[0] = new BTRunner_AuditContext().Tick(Entity, ref Blob.Value, ref random, Nodes, Leafs, Context, 0);
            }
        }
    }
}
