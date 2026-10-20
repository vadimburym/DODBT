// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal sealed class SceneDebugTests
    {
        private World _world;
        private Entity _owner, _child;

        [SetUp] public void Setup()
        {
            _world = new World("Scene selection test", WorldFlags.Game);
            var manager = _world.EntityManager;
            _owner = manager.CreateEntity(typeof(LocalToWorld));
            manager.AddSharedComponent(_owner, new BtDebugTree());
            manager.AddBuffer<NodeStateElement>(_owner).ResizeUninitialized(1);
            manager.AddBuffer<LeafStateElement>(_owner).ResizeUninitialized(1);
            manager.SetName(_owner, "Selected agent");
            manager.SetComponentData(_owner, new LocalToWorld { Value = float4x4.Translate(new float3(12, 2, 7)) });
            _child = manager.CreateEntity(typeof(Parent));
            manager.SetComponentData(_child, new Parent { Value = _owner });
        }

        [TearDown] public void Cleanup()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<BtEditorWindow>()) window.Close();
            if (_world.IsCreated) _world.Dispose();
        }

        [Test] public void RendererSelection_FindsTreeOwnerAndTracksPositionWithoutStartingTrace()
        {
            using var target = new BtSceneDebugTarget(_world, _child);
            Assert.That(target.Refresh(), Is.True);
            Assert.That(target.Owner, Is.EqualTo(_owner));
            Assert.That(target.Position, Is.EqualTo(new Vector3(12, 2, 7)));
            Assert.That(target.Label, Is.EqualTo("Selected agent"));
            var manager = _world.EntityManager;
            manager.SetComponentData(_owner, new LocalToWorld { Value = float4x4.Translate(new float3(20, 0, 9)) });
            Assert.That(target.Refresh(), Is.True);
            Assert.That(target.Position.x, Is.EqualTo(20));
            Assert.That(manager.HasBuffer<BtDebugNodeState>(_owner), Is.False);
            using var requests = manager.CreateEntityQuery(typeof(BtDebugTarget));
            Assert.That(requests.IsEmptyIgnoreFilter, Is.True);
        }

        [Test] public void Resolver_HandlesNestedParentsAndRejectsCyclesDetachedRenderersAndStaleVersions()
        {
            var manager = _world.EntityManager;
            var grandchild = manager.CreateEntity(typeof(Parent));
            manager.SetComponentData(grandchild, new Parent { Value = _child });
            Assert.That(BtSceneDebugTarget.TryResolveOwner(_world, grandchild, out var found), Is.True);
            Assert.That(found, Is.EqualTo(_owner));
            manager.SetComponentData(_child, new Parent { Value = grandchild });
            Assert.That(BtSceneDebugTarget.TryResolveOwner(_world, grandchild, out _), Is.False);
            manager.RemoveComponent<Parent>(_child);
            Assert.That(BtSceneDebugTarget.TryResolveOwner(_world, _child, out _), Is.False);
            var stale = new Entity { Index = _owner.Index, Version = _owner.Version + 1 };
            Assert.That(BtSceneDebugTarget.TryResolveOwner(_world, stale, out _), Is.False);
        }

        [Test] public void Target_RejectsDestroyedOwnerDisposedWorldMissingBuffersAndInvalidPosition()
        {
            using var target = new BtSceneDebugTarget(_world, _child);
            var manager = _world.EntityManager;
            manager.SetComponentData(_owner, new LocalToWorld { Value = float4x4.Translate(new float3(float.NaN)) });
            Assert.That(target.Refresh(), Is.False);
            manager.RemoveComponent<LeafStateElement>(_owner);
            Assert.That(BtSceneDebugTarget.TryResolveOwner(_world, _child, out _), Is.False);
            manager.DestroyEntity(_owner);
            Assert.That(target.Refresh(), Is.False);
            _world.Dispose();
            Assert.That(target.Refresh(), Is.False);
            Assert.That(target.Observe(out var error), Is.False);
            Assert.That(error, Does.Contain("no longer"));
        }

        [Test] public void Adapter_ReadsActualInstalledEntitiesProxyAndRejectsOrdinaryObjects()
        {
            Assert.That(BtEntitySelectionAdapter.IsSupported, Is.True, "Review the selection adapter when updating Entities.");
            var type = Type.GetType("Unity.Entities.Editor.EntitySelectionProxy, Unity.Entities.Editor");
            var create = type.GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(World), typeof(Entity) }, null);
            var proxy = (Object)create.Invoke(null, new object[] { _world, _child });
            var ordinary = new GameObject("Non-entity selection");
            try
            {
                Assert.That(BtEntitySelectionAdapter.TryRead(proxy, out var world, out var entity), Is.True);
                Assert.That(world, Is.SameAs(_world));
                Assert.That(entity, Is.EqualTo(_child));
                Assert.That(BtEntitySelectionAdapter.TryRead(ordinary, out _, out _), Is.False);
                _world.EntityManager.DestroyEntity(_child);
                Assert.That(BtEntitySelectionAdapter.TryRead(proxy, out _, out _), Is.False);
            }
            finally { Object.DestroyImmediate(proxy); Object.DestroyImmediate(ordinary); }
        }

        [Test] public void Observe_UsesSourceGraphAndReleasesTraceWhenWindowCloses()
        {
            string path = "Assets/__SceneDebugTest-" + Guid.NewGuid().ToString("N") + ".asset";
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.CompiledRevision = "scene-test";
            graph.GuidsByCompiledId = new[] { "leaf" };
            graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
            graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Title = "Leaf", Kind = BtNodeKind.Leaf, ParentGuid = "root" });
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf() });
            try
            {
                AssetDatabase.CreateAsset(graph, path);
                _world.EntityManager.SetSharedComponent(_owner, new BtDebugTree {
                    GraphGuid = AssetDatabase.AssetPathToGUID(path), CompiledRevision = "scene-test" });
                using var target = new BtSceneDebugTarget(_world, _child);
                Assert.That(target.Observe(out var error), Is.True, error);
                Assert.That(_world.EntityManager.HasBuffer<BtDebugNodeState>(_owner), Is.True);
                graph.NotActualCompiledVersion = true;
                Assert.That(target.Observe(out error), Is.False);
                Assert.That(error, Does.Contain("uncompiled"));
                Assert.That(_world.EntityManager.HasBuffer<BtDebugNodeState>(_owner), Is.True, "Rejected navigation must preserve the existing trace.");
                foreach (var window in Resources.FindObjectsOfTypeAll<BtEditorWindow>()) window.Close();
                Assert.That(_world.EntityManager.HasBuffer<BtDebugNodeState>(_owner), Is.False);
                using var query = _world.EntityManager.CreateEntityQuery(typeof(BtDebugTarget));
                Assert.That(query.IsEmptyIgnoreFilter, Is.True);
            }
            finally { AssetDatabase.DeleteAsset(path); if (graph != null) Object.DestroyImmediate(graph); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DebugWindowStopsObservingWhenAgentOrWorldIsDestroyed(bool disposeWorld)
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.CompiledRevision = "scene-test";
            graph.GuidsByCompiledId = new[] { "selector" };
            graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "selector" };
            graph.Nodes.Add(new BtNodeHeader { Guid = "selector", Title = "Selector", Kind = BtNodeKind.Selector, ParentGuid = "root" });
            _world.EntityManager.SetSharedComponent(_owner, new BtDebugTree { CompiledRevision = "scene-test" });
            BtEditorWindow window = null;
            try
            {
                BtEditorWindow.OpenWithEntity(graph, _world, _owner);
                window = Resources.FindObjectsOfTypeAll<BtEditorWindow>()[0];
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var view = (BtGraphView)typeof(BtEditorWindow).GetField("_graphView", flags).GetValue(window);
                view.Bind(graph);
                view.SetDebugStatus(new[] { NodeStatus.Running });
                if (disposeWorld) _world.Dispose();
                else _world.EntityManager.DestroyEntity(_owner);
                Assert.DoesNotThrow(() => window.LateDebugUpdate());
                Assert.That(typeof(BtEditorWindow).GetField("_debugSession", flags).GetValue(window), Is.Null);
                Assert.DoesNotThrow(() => window.LateDebugUpdate());
                if (!disposeWorld)
                {
                    using var requests = _world.EntityManager.CreateEntityQuery(typeof(BtDebugTarget));
                    Assert.That(requests.IsEmptyIgnoreFilter, Is.True);
                }
            }
            finally { if (window != null) window.Close(); Object.DestroyImmediate(graph); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OpenSessionRejectsGraphEditsAndRecompilationBeforeReadingStatuses(bool recompiled)
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.CompiledRevision = "before";
            graph.GuidsByCompiledId = new[] { "leaf" };
            _world.EntityManager.SetSharedComponent(_owner, new BtDebugTree { CompiledRevision = "before" });
            try
            {
                using var session = new BtEntityDebugSession(_world, _owner, graph);
                Assert.That(session.Refresh(), Is.True);
                if (recompiled) graph.CompiledRevision = "after";
                else graph.NotActualCompiledVersion = true;
                // Even if a poll was just completed, changed metadata must invalidate the session.
                Assert.That(session.Refresh(), Is.False);
                Assert.That(session.Error, Does.Contain("source graph changed"));
            }
            finally { Object.DestroyImmediate(graph); }
            Assert.That(_world.EntityManager.HasBuffer<BtDebugNodeState>(_owner), Is.False);
            using var query = _world.EntityManager.CreateEntityQuery(typeof(BtDebugTarget));
            Assert.That(query.IsEmptyIgnoreFilter, Is.True);
        }

        [Test]
        public void OpenSessionRejectsAnAgentReboundToAnotherTree()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.CompiledRevision = "before";
            graph.GuidsByCompiledId = new[] { "leaf" };
            _world.EntityManager.SetSharedComponent(_owner, new BtDebugTree { CompiledRevision = "before" });
            try
            {
                using var session = new BtEntityDebugSession(_world, _owner, graph);
                _world.EntityManager.SetSharedComponent(_owner, new BtDebugTree { CompiledRevision = "after" });
                Assert.That(session.Refresh(), Is.False);
                Assert.That(session.Error, Does.Contain("agent's tree changed"));
            }
            finally { Object.DestroyImmediate(graph); }
        }

        private static readonly ManualResetEventSlim ReleaseWriter = new(false);

        private struct WaitingWriter : IJob
        {
            public void Execute() => ReleaseWriter.Wait(5000);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Sampling_DoesNotWaitForTransformWritersAndResumesAfterCompletion(bool parentWriter)
        {
            using var target = new BtSceneDebugTarget(_world, _child);
            Assert.That(target.Refresh(), Is.True);
            var previous = target.Position;
            _world.EntityManager.SetComponentData(_owner, new LocalToWorld { Value = float4x4.Translate(new float3(30, 0, 5)) });
            using var writerQuery = _world.EntityManager.CreateEntityQuery(
                parentWriter ? ComponentType.ReadWrite<Parent>() : ComponentType.ReadWrite<LocalToWorld>());
            ReleaseWriter.Reset();
            var writer = new WaitingWriter().Schedule();
            writerQuery.AddDependency(writer);
            JobHandle.ScheduleBatchedJobs();
            try
            {
                Assert.That(writer.IsCompleted, Is.False);
                var watch = Stopwatch.StartNew();
                Assert.That(target.Refresh(), Is.True);
                watch.Stop();
                Assert.That(writer.IsCompleted, Is.False, "Sampling waited for a writer instead of deferring the position read.");
                Assert.That(target.Position, Is.EqualTo(previous));
                Assert.That(watch.ElapsedMilliseconds, Is.LessThan(100));
                NUnit.Framework.TestContext.WriteLine($"Non-blocking sample with {(parentWriter ? "Parent" : "LocalToWorld")} writer: {watch.Elapsed.TotalMilliseconds:F3} ms");
            }
            finally { ReleaseWriter.Set(); writer.Complete(); }
            Assert.That(target.Refresh(), Is.True);
            Assert.That(target.Position, Is.EqualTo(new Vector3(30, 0, 5)));
        }

        [Test] public void Target_DisposalIsIdempotentAndStopsSampling()
        {
            var target = new BtSceneDebugTarget(_world, _child);
            Assert.That(target.Refresh(), Is.True);
            Assert.That(target.HasPosition, Is.True);
            target.Dispose();
            target.Dispose();
            Assert.That(target.Refresh(), Is.False);
            Assert.That(target.HasPosition, Is.False);
        }
    }
}
