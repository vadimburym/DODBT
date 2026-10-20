// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine;
    using VadimBurym.DodBehaviourTree.Dots;

    internal static class BtGraphValidation
    {
        internal static string Validate(BtGraphAsset graph)
        {
            if (graph == null) return "No graph selected.";
            if (graph.Nodes == null || graph.LeafNodes == null || graph.SequenceNodes == null || graph.SelectorNodes == null ||
                graph.MemorySequenceNodes == null || graph.MemorySelectorNodes == null || graph.ParallelNodes == null)
                return "The graph has missing node collections. Restore the asset before compiling.";
            if (graph.RootNode == null || string.IsNullOrEmpty(graph.RootNode.ChildrenGuid)) return "Root node has no output.";
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(graph)) return "The graph contains missing serialized types. Restore their scripts before compiling.";
            var headers = new Dictionary<string, BtNodeHeader>();
            foreach (var header in graph.Nodes)
            {
                if (header == null || string.IsNullOrEmpty(header.Guid)) return "A graph node has no GUID.";
                if (!headers.TryAdd(header.Guid, header)) return "Duplicate node GUID: " + header.Guid;
            }
            var pending = new Stack<(string Guid, string Parent, int AbortSlots)>();
            var visited = new HashSet<string>();
            Type contextType = BtGraphContext.Resolve(graph);
            if (contextType == null) return BtGraphContext.SelectionError(graph);
            pending.Push((graph.RootNode.ChildrenGuid, graph.RootNode.Guid, 1));
            while (pending.Count > 0)
            {
                var entry = pending.Pop();
                string guid = entry.Guid;
                if (string.IsNullOrEmpty(guid) || !headers.TryGetValue(guid, out var header)) return "A connection references a missing node.";
                if (header.ParentGuid != entry.Parent) return "Node '" + header.Title + "' has an inconsistent parent connection.";
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
                    var declaration = type.GetCustomAttribute<LeafCodeGenAttribute>();
                    if (declaration?.ContextType == null || !declaration.ContextType.IsDefined(typeof(GenerateBtRunnerAttribute), false))
                        return "Leaf '" + header.Title + "' needs [LeafCodeGen(id, typeof(Context))] and a context marked [GenerateBtRunner].";
                    if (contextType != null && contextType != declaration.ContextType)
                        return "Leaf '" + header.Title + "' uses " + declaration.ContextType.FullName + ", but this graph uses " + contextType.FullName + ". All leaves in one tree must use the same generated context.";
                    if (declaration.Id < 0) return "Leaf IDs must be nonnegative int values.";
                    contextType = declaration.ContextType;
                    try
                    {
                        if (leaf.GetCompiledData().LeafId != declaration.Id)
                            return "Leaf '" + header.Title + "' returns a LeafId that differs from its LeafCodeGen attribute.";
                    }
                    catch (Exception error) { return "Could not compile leaf '" + header.Title + "': " + error.Message; }
                    continue;
                }
                var children = graph.GetChildrenList(guid, header.Kind);
                if (children == null) return "Missing or unsupported node data: " + header.Title;
                if (children.Count == 0 || children.Count > byte.MaxValue) return "Node '" + header.Title + "' must have between 1 and 255 children.";
                if (header.Kind == BtNodeKind.Parallel)
                {
                    var data = graph.FindParallelData(guid);
                    if (!ThresholdValid(data.SuccessThreshold, children.Count) || !ThresholdValid(data.FailureThreshold, children.Count))
                        return "Parallel thresholds must be -1 (all children) or between 1 and the child count.";
                }
                int slots = header.Kind == BtNodeKind.Parallel ? entry.AbortSlots + children.Count - 1 : entry.AbortSlots;
                if (slots > new FixedList4096Bytes<int>().Capacity)
                    return "Nested parallel branches exceed the runtime abort stack capacity. Split or flatten this branch.";
                foreach (string child in children) pending.Push((child, guid, slots));
            }
            return string.Empty;
        }

        private static bool ThresholdValid(int threshold, int count) => threshold == -1 || threshold >= 1 && threshold <= count;
    }
}
