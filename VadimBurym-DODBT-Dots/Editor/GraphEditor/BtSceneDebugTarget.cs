// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEditor;
    using UnityEngine;
    using VadimBurym.DodBehaviourTree.Dots;

    internal sealed class BtSceneDebugTarget : IDisposable
    {
        internal readonly World World;
        internal readonly Entity Selected;
        internal Entity Owner { get; private set; }
        internal Vector3 Position { get; private set; }
        internal string Label { get; private set; }
        internal bool HasPosition { get; private set; }
        private EntityQuery _transformDependencies;
        private bool _hasQuery;
        private bool _disposed;

        internal BtSceneDebugTarget(World world, Entity selected) { World = world; Selected = selected; }

        internal static bool TryResolveOwner(World world, Entity selected, out Entity owner)
        {
            owner = Entity.Null;
            if (world == null || !world.IsCreated || selected.Index < 0) return false;
            var manager = world.EntityManager;
            var cursor = selected;
            // Bounded traversal also rejects malformed/cyclic hierarchies without allocations.
            for (int depth = 0; depth < 64 && cursor.Index >= 0 && manager.Exists(cursor); depth++)
            {
                if (manager.HasComponent<BtDebugTree>(cursor) && manager.HasBuffer<NodeStateElement>(cursor) &&
                    manager.HasBuffer<LeafStateElement>(cursor))
                {
                    owner = cursor;
                    return true;
                }
                if (!manager.HasComponent<Parent>(cursor)) return false;
                cursor = manager.GetComponentData<Parent>(cursor).Value;
            }
            return false;
        }

        internal bool Refresh()
        {
            if (_disposed || World == null || !World.IsCreated || !World.EntityManager.Exists(Selected)) return false;
            if (!_hasQuery)
            {
                // Only used to inspect dependency fences, never to enumerate entities.
                _transformDependencies = World.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<Parent>(), ComponentType.ReadOnly<LocalToWorld>());
                _hasQuery = true;
            }
            // GetComponentData completes writers for the entire component type. Do not
            // interrupt simulation/transform jobs just to draw an editor marker.
            if (!_transformDependencies.GetDependency().IsCompleted) return true;
            if (!TryResolveOwner(World, Selected, out var owner)) return false;
            var manager = World.EntityManager;
            if (!manager.HasComponent<LocalToWorld>(owner)) return false;
            var position = manager.GetComponentData<LocalToWorld>(owner).Position;
            if (!math.all(math.isfinite(position))) return false;
            if (owner != Owner || Label == null)
            {
                string name = manager.GetName(owner);
                Label = string.IsNullOrEmpty(name) ? $"Entity {owner.Index}:{owner.Version}" : name;
            }
            Owner = owner;
            Position = position;
            HasPosition = true;
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_hasQuery && World != null && World.IsCreated) _transformDependencies.Dispose();
            _disposed = true;
            HasPosition = false;
        }

        internal bool Observe(out string error)
        {
            error = null;
            if (!Refresh()) { error = "This entity is no longer available."; return false; }
            if (!HasPosition) { error = "Waiting for this entity's transform jobs."; return false; }
            // Navigation is an explicit action: revalidate the owner even if the last
            // non-blocking sample was deferred while jobs were running.
            if (!TryResolveOwner(World, Selected, out var owner) || owner != Owner)
            { error = "The selected entity's tree changed. Select it again."; return false; }
            var tree = World.EntityManager.GetSharedComponent<BtDebugTree>(Owner);
            var graph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(AssetDatabase.GUIDToAssetPath(tree.GraphGuid.ToString()));
            if (graph == null) { error = "The source graph is unavailable. Open BT Debugger to inspect this entity."; return false; }
            try { BtEditorWindow.OpenWithEntity(graph, World, Owner); return true; }
            catch (InvalidOperationException exception) { error = exception.Message; return false; }
        }
    }
}
