// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System.Collections.Generic;
    using System.Text;
    using Unity.Entities;
    using VadimBurym.DodBehaviourTree.Dots;

    // One map per source graph in a page snapshot. No runtime trace buffers are required.
    internal sealed class BtActiveLeafMap
    {
        internal readonly struct Summary
        {
            internal readonly string Text;
            internal readonly int Count;
            internal readonly bool IsValid;
            internal Summary(string text, int count = 0, bool valid = false)
            { Text = text; Count = count; IsValid = valid; }
        }

        private readonly List<(int NodeIndex, string Name)> _leaves = new();
        private readonly string _revision;
        private readonly int _nodeCount;
        private readonly string _unavailable;

        internal BtActiveLeafMap(BtGraphAsset graph)
        {
            if (graph == null) { _unavailable = "Source unavailable"; return; }
            if (graph.NotActualCompiledVersion) { _unavailable = "Graph has uncompiled edits"; return; }
            if (string.IsNullOrEmpty(graph.CompiledRevision) || graph.GuidsByCompiledId == null)
            { _unavailable = "Compile the source graph"; return; }
            _revision = graph.CompiledRevision;
            _nodeCount = graph.GuidsByCompiledId.Length;
            var headers = new Dictionary<string, BtNodeHeader>();
            foreach (var header in graph.Nodes)
                if (!string.IsNullOrEmpty(header.Guid)) headers[header.Guid] = header;
            for (int i = 0; i < _nodeCount; i++)
            {
                var guid = graph.GuidsByCompiledId[i];
                if (string.IsNullOrEmpty(guid) || !headers.TryGetValue(guid, out var header))
                { _unavailable = "Graph mapping unavailable"; return; }
                if (header.Kind == BtNodeKind.Leaf)
                    _leaves.Add((i, string.IsNullOrWhiteSpace(header.Title) ? $"Leaf {i}" : header.Title));
            }
        }

        internal Summary Read(in BtDebugTree tree, DynamicBuffer<NodeStateElement> nodes, DynamicBuffer<LeafStateElement> leaves)
        {
            if (_unavailable != null) return new Summary(_unavailable);
            if (tree.CompiledRevision.ToString() != _revision) return new Summary("Different graph revision");
            if (nodes.Length != _nodeCount || leaves.Length != _leaves.Count) return new Summary("State layout mismatch");
            StringBuilder text = null;
            int count = 0;
            foreach (var leaf in _leaves)
            {
                int index = nodes[leaf.NodeIndex].LeafStateIndex;
                if (index >= leaves.Length) return new Summary("State layout mismatch");
                if (leaves[index].IsEntered == 0) continue;
                if (text == null) text = new StringBuilder();
                else text.Append(", ");
                text.Append(leaf.Name);
                count++;
            }
            return new Summary(text?.ToString() ?? "No running leaves", count, true);
        }
    }
}
