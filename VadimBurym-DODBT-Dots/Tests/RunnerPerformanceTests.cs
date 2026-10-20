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
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using VadimBurym.DodBehaviourTree.Dots.Generated;
using Random = Unity.Mathematics.Random;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    [GenerateBtRunner] public struct BenchmarkContext { }
    public struct BenchmarkRandomState : IComponentData { public uint Value; }

    [Serializable, LeafCodeGen(0, typeof(BenchmarkContext))]
    public sealed class BenchmarkLeaf : ILeaf
    {
        public LeafData GetCompiledData() => default;
        public static NodeStatus OnTick(ref RunnerState_BenchmarkContext state) => NodeStatus.Success;
        public static void OnEnter(ref RunnerState_BenchmarkContext state) { }
        public static void OnExit(ref RunnerState_BenchmarkContext state) { }
        public static void OnAbort(ref RunnerState_BenchmarkContext state) { }
    }

    [DisableAutoCreation]
    public partial class RunnerBenchmarkSystem : SystemBase
    {
        internal BlobAssetReference<BehaviourTreeBlob> Blob;
        internal int Iterations = 5;
        protected override void OnUpdate() { }
        internal void Run(bool baseline)
        {
            var query = GetEntityQuery(ComponentType.ReadWrite<NodeStateElement>(), ComponentType.ReadWrite<LeafStateElement>());
            var nodes = GetBufferTypeHandle<NodeStateElement>();
            var leafs = GetBufferTypeHandle<LeafStateElement>();
            var entities = GetEntityTypeHandle();
            if (baseline)
                new BaselineJob { Blob = Blob, Nodes = nodes, Leafs = leafs, Entities = entities, Iterations = Iterations }.ScheduleParallel(query, default).Complete();
            else
                new GeneratedJob { RandomStates = GetComponentTypeHandle<BenchmarkRandomState>(), Blob = Blob, Nodes = nodes, Leafs = leafs, Entities = entities, Iterations = Iterations }.ScheduleParallel(query, default).Complete();
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct BaselineJob : IJobChunk
        {
            [ReadOnly] public BlobAssetReference<BehaviourTreeBlob> Blob;
            public BufferTypeHandle<NodeStateElement> Nodes;
            public BufferTypeHandle<LeafStateElement> Leafs;
            [ReadOnly] public EntityTypeHandle Entities;
            public int Iterations;
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var nodes = chunk.GetBufferAccessor(ref Nodes);
                var leafs = chunk.GetBufferAccessor(ref Leafs);
                var entities = chunk.GetNativeArray(Entities);
                var random = new Random(12345);
                var context = new BenchmarkContext();
                var runner = new BaselineBenchmarkRunner();
                for (int repeat = 0; repeat < Iterations; repeat++)
                    for (int i = 0; i < chunk.Count; i++)
                        runner.Tick(entities[i], ref Blob.Value, ref random, nodes[i], leafs[i], context, unfilteredChunkIndex);
            }
        }
        [BurstCompile(CompileSynchronously = true)]
        private struct GeneratedJob : IJobChunk
        {
            [ReadOnly] public BlobAssetReference<BehaviourTreeBlob> Blob;
            public BufferTypeHandle<NodeStateElement> Nodes;
            public BufferTypeHandle<LeafStateElement> Leafs;
            [ReadOnly] public EntityTypeHandle Entities;
            public int Iterations;
            public ComponentTypeHandle<BenchmarkRandomState> RandomStates;
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var nodes = chunk.GetBufferAccessor(ref Nodes);
                var leafs = chunk.GetBufferAccessor(ref Leafs);
                var entities = chunk.GetNativeArray(Entities);
                var randomStates = chunk.GetNativeArray(ref RandomStates);
                var context = new BenchmarkContext();
                var runner = new BTRunner_BenchmarkContext();
                for (int repeat = 0; repeat < Iterations; repeat++)
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var random = new Random { state = randomStates[i].Value };
                        runner.Tick(entities[i], ref Blob.Value, ref random, nodes[i], leafs[i], context, unfilteredChunkIndex);
                        randomStates[i] = new BenchmarkRandomState { Value = random.state };
                    }
            }
        }

    }

    public sealed class RunnerPerformanceTests
    {
        [TestCase(false), TestCase(true), Explicit("Comparative Burst/Jobs benchmark; run without other profiling or build workloads.")]
        public void TenThousandAgents_CompareGeneratedRunnerAgainstOriginal(bool randomSelectors)
        {
            Assert.That(BurstCompiler.IsEnabled, Is.True);
            const int count = 10000;
            var branches = Enumerable.Range(0, 3).Select(b => {
                var children = Enumerable.Range(0, 10).Select(i => TestNodeSpec.RecordingLeaf(b + ":" + i, NodeStatus.Success)).ToArray();
                return randomSelectors ? TestNodeSpec.MemorySelector(true, true, children) : TestNodeSpec.MemorySequence(true, true, children);
            }).ToArray();
            var asset = TestTreeFactory.CreateAsset(TestNodeSpec.Parallel(3, 1, false, branches));
            for (int i = 0; i < asset.Leafs.Length; i++) asset.Leafs[i].LeafId = 0;
                TestTreeFactory.SetBindings(asset, typeof(BenchmarkContext), typeof(BenchmarkLeaf));
            var blob = asset.CreateBlob();
            using var world = new World("DODBT Benchmark");
            try
            {
                var manager = world.EntityManager;
                var archetype = manager.CreateArchetype(typeof(NodeStateElement), typeof(LeafStateElement), typeof(BenchmarkRandomState));
                using var entities = manager.CreateEntity(archetype, count, Allocator.Temp);
                foreach (var entity in entities)
                {
                    manager.SetComponentData(entity, new BenchmarkRandomState { Value = (uint)(entity.Index + 1) * 12345u });
                    var nodes = manager.GetBuffer<NodeStateElement>(entity);
                    nodes.ResizeUninitialized(asset.Nodes.Length);
                    NodeIndex leaf = 0;
                    for (int i = 0; i < nodes.Length; i++) nodes[i] = new NodeStateElement {
                        Cursor = byte.MaxValue, MemoryCursor = byte.MaxValue,
                        LeafStateIndex = asset.Nodes[i].Id == NodeId.Leaf ? leaf++ : NodeIndex.MaxValue
                    };
                    var leafs = manager.GetBuffer<LeafStateElement>(entity);
                    leafs.ResizeUninitialized(asset.Leafs.Length);
                    for (int i = 0; i < leafs.Length; i++) leafs[i] = new LeafStateElement { BufferIndex = (ushort)i };
                }
                var system = world.GetOrCreateSystemManaged<RunnerBenchmarkSystem>();
                system.Blob = blob;
                for (int i = 0; i < 8; i++) { system.Run(true); system.Run(false); }
                var oldTimes = new double[25];
                var newTimes = new double[25];
                var watch = new Stopwatch();
                double Measure(bool baseline)
                {
                    watch.Restart(); system.Run(baseline); watch.Stop();
                    return watch.Elapsed.TotalMilliseconds / system.Iterations;
                }
                for (int i = 0; i < oldTimes.Length; i++)
                {
                    if (i % 2 == 0) { oldTimes[i] = Measure(true); newTimes[i] = Measure(false); }
                    else { newTimes[i] = Measure(false); oldTimes[i] = Measure(true); }
                }
                Array.Sort(oldTimes); Array.Sort(newTimes);
                double baseline = oldTimes[oldTimes.Length / 2];
                double generated = newTimes[newTimes.Length / 2];
                string report = $"Random selectors: {randomSelectors}\nAgents: {count}\nNodes: {asset.Nodes.Length}\nLeaves: {asset.Leafs.Length}\nBurst enabled: {BurstCompiler.IsEnabled}\n" +
                    $"Baseline median: {baseline:F4} ms\nGenerated median: {generated:F4} ms\nRatio: {generated / baseline:F4}\n" +
                    $"Baseline p90: {oldTimes[22]:F4} ms\nGenerated p90: {newTimes[22]:F4} ms\n" +
                    "Constant-success leaves; 34 visits for sequences or 7 visits for random selectors. Includes scheduling/completion and generated per-agent RNG persistence. No gameplay systems, rendering, or tracing.\n";
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/Dots-Runner-Benchmark-" + (randomSelectors ? "Random" : "Full") + ".txt", report);
                NUnit.Framework.TestContext.WriteLine(report);
                Assert.That(generated, Is.LessThan(baseline * 1.15), "Generated runner regressed beyond the benchmark noise allowance.");
            }
            finally { blob.Dispose(); UnityEngine.Object.DestroyImmediate(asset); }
        }
    }
}
