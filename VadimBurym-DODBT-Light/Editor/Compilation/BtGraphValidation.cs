// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using VadimBurym.DodBehaviourTree.Light;

    internal static class BtGraphValidation
    {
        internal static string Validate(BtGraphAsset graph)
        {
            if (graph == null) return "No graph selected.";
            if (graph.Nodes == null || graph.LeafNodes == null || graph.SequenceNodes == null || graph.SelectorNodes == null ||
                graph.MemorySequenceNodes == null || graph.MemorySelectorNodes == null || graph.ParallelNodes == null || graph.DecoratorNodes == null)
                return "The graph has missing node collections. Restore the asset before compiling.";
            if (graph.RootNode == null || string.IsNullOrEmpty(graph.RootNode.ChildrenGuid)) return "Root node has no output.";
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(graph)) return "The graph contains missing serialized types. Restore their scripts before compiling.";
            var headers = new Dictionary<string, BtNodeHeader>();
            foreach (var header in graph.Nodes)
            {
                if (header == null || string.IsNullOrEmpty(header.Guid)) return "A graph node has no GUID.";
#if !DODBT_DECORATORS
                if (BtNodeKinds.IsDecorator(header.Kind)) return "This graph requires DODBT_DECORATORS. Enable the extension to compile it; saved node data is preserved.";
#endif
                if (!headers.TryAdd(header.Guid, header)) return "Duplicate node GUID: " + header.Guid;
            }
            var pending = new Stack<string>();
            var visited = new HashSet<string>();
            pending.Push(graph.RootNode.ChildrenGuid);
            while (pending.Count > 0)
            {
                string guid = pending.Pop();
                if (string.IsNullOrEmpty(guid) || !headers.TryGetValue(guid, out var header)) return "A connection references a missing node.";
                if (!visited.Add(guid)) return "The graph contains a cycle or a node with multiple parents.";
#if DODBT_SMALL_SIZE
                if (visited.Count > byte.MaxValue) return "DODBT_SMALL_SIZE supports at most 255 reachable nodes.";
#else
                if (visited.Count > ushort.MaxValue) return "The graph exceeds 65535 reachable nodes.";
#endif
                if (header.Kind == BtNodeKind.Leaf)
                {
                    var leaf = graph.FindLeafData(guid)?.Leaf;
                    if (leaf == null) return "Leaf '" + header.Title + "' has no ILeaf implementation.";
                    var type = leaf.GetType();
                    if (!type.IsClass || !type.IsSerializable || leaf is UnityEngine.Object || type.ContainsGenericParameters)
                        return "Leaf '" + header.Title + "' is not a Unity-serializable managed class.";
                    if (!type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILeaf<,>)))
                        return "Leaf '" + header.Title + "' must implement ILeaf<TContext, TLeafState>.";
                    if (BtGraphContext.Resolve(graph) == null || BtGraphContext.ResolveState(graph) == null)
                        return BtGraphContext.SelectionError(graph);
                    if (!BtGraphContext.AcceptsLeaf(graph, type))
                        return "Leaf '" + header.Title + "' does not match the graph context and leaf state.";
                    continue;
                }
                var children = graph.GetChildrenList(guid, header.Kind);
                if (children == null) return "Missing or unsupported node data: " + header.Title;
                if (BtNodeKinds.IsDecorator(header.Kind))
                {
                    if (children.Count != 1) return "Decorator '" + header.Title + "' must have exactly one child.";
                    double duration = graph.FindDecoratorData(guid).Duration;
                    if (header.Kind != BtNodeKind.Inverter && (double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0))
                        return "Decorator duration must be finite and nonnegative.";
                }
                if (children.Count == 0 || children.Count > byte.MaxValue) return "Node '" + header.Title + "' must have between 1 and 255 children.";
                if (header.Kind == BtNodeKind.Parallel)
                {
                    var data = graph.FindParallelData(guid);
                    if (!ThresholdValid(data.SuccessThreshold, children.Count) || !ThresholdValid(data.FailureThreshold, children.Count))
                        return "Parallel thresholds must be -1 (all children) or between 1 and the child count.";
                }
                foreach (string child in children) pending.Push(child);
            }
            return string.Empty;
        }

        private static bool ThresholdValid(int threshold, int count) => threshold == -1 || threshold >= 1 && threshold <= count;
    }
}
