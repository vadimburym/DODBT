// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEditor;
    using Unity.Profiling;
    using VadimBurym.DodBehaviourTree.Dots;

    internal sealed class BtEntityDebugSession : IBtDebugState, IDisposable
    {
        private static readonly ProfilerMarker ReadMarker = new("DODBT.Debugger.ObserveRead");
        internal readonly World World;
        internal readonly Entity Target;
        internal BtDebugSnapshot Snapshot { get; private set; }
        internal bool IsAlive => !_disposed && World != null && World.IsCreated && World.EntityManager.Exists(Target);
        public NodeStatus[] DebugStatus { get; }
        public List<string> DebugRunningLeafs { get; } = new();
        private readonly BtGraphAsset _graph;
        private readonly string _compiledRevision;
        private readonly BtDebugTree _targetTree;
        internal string Error { get; private set; }
        private Entity _request;
        private double _nextRefresh;
        private bool _disposed;

        internal BtEntityDebugSession(World world, Entity entity, BtGraphAsset graph)
        {
            World = world;
            Target = entity;
            _graph = graph;
            int count = ValidateTarget(world, entity, graph);
            _compiledRevision = graph.CompiledRevision;
            var manager = world.EntityManager;
            _targetTree = manager.GetSharedComponent<BtDebugTree>(entity);
            DebugStatus = new NodeStatus[count];
            // A graph window owns one observation request; no persistent native allocations are retained by the editor.
            using var query = manager.CreateEntityQuery(typeof(BtDebugTarget));
            if (!query.IsEmptyIgnoreFilter) throw new InvalidOperationException("This world already has an active BT debug request.");
            manager.AddBuffer<BtDebugNodeState>(entity).ResizeUninitialized(count);
            var states = manager.GetBuffer<BtDebugNodeState>(entity);
            for (int i = 0; i < states.Length; i++) states[i] = default;
            manager.AddComponentData(entity, default(BtDebugSnapshot));
            _request = manager.CreateEntity();
            manager.AddComponentData(_request, new BtDebugTarget { Entity = entity });
        }

        internal static int ValidateTarget(World world, Entity entity, BtGraphAsset graph)
        {
            if (world == null || !world.IsCreated || !world.EntityManager.Exists(entity))
                throw new InvalidOperationException("This entity is no longer available.");
            if (graph == null) throw new InvalidOperationException("The source graph is unavailable.");
            if (graph.NotActualCompiledVersion)
                throw new InvalidOperationException("The source graph has uncompiled edits. Compile the graph and restart Play Mode before observing it.");
            var manager = world.EntityManager;
            if (string.IsNullOrEmpty(graph.CompiledRevision) || !manager.HasComponent<BtDebugTree>(entity) ||
                manager.GetSharedComponent<BtDebugTree>(entity).CompiledRevision.ToString() != graph.CompiledRevision)
                throw new InvalidOperationException("The entity uses a different compiled graph revision. Compile the graph and restart Play Mode.");
            if (!manager.HasBuffer<NodeStateElement>(entity) || !manager.HasBuffer<LeafStateElement>(entity))
                throw new InvalidOperationException("The entity has no behaviour tree state buffers.");
            int count = manager.GetBuffer<NodeStateElement>(entity, true).Length;
            if (graph.GuidsByCompiledId == null || graph.GuidsByCompiledId.Length != count)
                throw new InvalidOperationException("The graph and entity use different compiled versions. Recompile and restart Play Mode.");
            return count;
        }

        internal bool Refresh()
        {
            if (!IsAlive) return false;
            if (_graph == null || _graph.NotActualCompiledVersion || _graph.CompiledRevision != _compiledRevision)
            {
                Error = "The source graph changed. Compile the graph and restart the agent before observing it again.";
                return false;
            }
            if (EditorApplication.timeSinceStartup < _nextRefresh) return true;
            using var profile = ReadMarker.Auto();
            _nextRefresh = EditorApplication.timeSinceStartup + .1;
            var manager = World.EntityManager;
            if (!manager.HasComponent<BtDebugTree>(Target) || !manager.HasBuffer<NodeStateElement>(Target) ||
                !manager.HasBuffer<LeafStateElement>(Target)) return false;
            var tree = manager.GetSharedComponent<BtDebugTree>(Target);
            if (tree.GraphGuid != _targetTree.GraphGuid || tree.CompiledRevision != _targetTree.CompiledRevision)
            {
                Error = "The agent's tree changed. Observe the agent again to open its current graph.";
                return false;
            }
            if (!manager.HasBuffer<BtDebugNodeState>(Target) || !manager.HasComponent<BtDebugSnapshot>(Target)) return false;
            manager.CompleteDependencyBeforeRO<BtDebugNodeState>();
            manager.CompleteDependencyBeforeRO<BtDebugSnapshot>();
            Snapshot = manager.GetComponentData<BtDebugSnapshot>(Target);
            var states = manager.GetBuffer<BtDebugNodeState>(Target, true);
            if (states.Length != DebugStatus.Length) return false;
            DebugRunningLeafs.Clear();
            for (int i = 0; i < states.Length; i++)
            {
                DebugStatus[i] = states[i].Status;
                if (states[i].Status != NodeStatus.Running) continue;
                var header = _graph.FindHeader(_graph.GuidsByCompiledId[i]);
                if (header?.Kind == BtNodeKind.Leaf) DebugRunningLeafs.Add(header.Title);
            }
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (World == null || !World.IsCreated) return;
            var manager = World.EntityManager;
            if (manager.Exists(_request)) manager.DestroyEntity(_request);
            if (!manager.Exists(Target)) return;
            if (manager.HasBuffer<BtDebugNodeState>(Target)) manager.RemoveComponent<BtDebugNodeState>(Target);
            if (manager.HasComponent<BtDebugSnapshot>(Target)) manager.RemoveComponent<BtDebugSnapshot>(Target);
        }
    }
}
