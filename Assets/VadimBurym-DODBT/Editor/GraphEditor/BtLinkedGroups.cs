using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
internal sealed class BtLinkedGroup
{
    public string Id;
    public string Name;
    public List<BtGroupInstance> Instances = new();
}

[Serializable]
internal sealed class BtGroupInstance
{
    public string RootGuid;
    public List<BtGroupMember> Members = new();
}

[Serializable]
internal sealed class BtGroupMember
{
    public string Key;
    public string Guid;
}

// Groups own editor metadata only. Every instance is an ordinary, independent branch.
internal static class BtLinkedGroups
{
    internal static BtGroupInstance Find(BtGraphAsset graph, string guid) => graph?.LinkedGroups
        .SelectMany(g => g.Instances).FirstOrDefault(i => i.Members.Any(m => m.Guid == guid));

    internal static BtLinkedGroup Family(BtGraphAsset graph, BtGroupInstance instance) =>
        graph.LinkedGroups.FirstOrDefault(g => g.Instances.Contains(instance));

    internal static List<string> Branch(BtGraphAsset graph, string root)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            string id = pending.Pop();
            var node = graph.FindHeader(id);
            if (node == null || !seen.Add(id)) throw new InvalidOperationException("The branch contains a missing node, cycle, or shared child.");
            result.Add(id);
            var children = graph.GetChildrenList(id, node.Kind);
            if (children != null)
                for (int n = children.Count - 1; n >= 0; n--) pending.Push(children[n]);
        }
        return result;
    }

    internal static BtLinkedGroup Create(BtGraphAsset graph, string root)
    {
        var ids = Branch(graph, root);
        if (ids.Any(id => Find(graph, id) != null))
            throw new InvalidOperationException("Linked groups cannot overlap or contain another linked group.");
        Undo.RegisterCompleteObjectUndo(graph, "BT Create Linked Group");
        var group = new BtLinkedGroup { Id = Guid.NewGuid().ToString("N"), Name = graph.FindHeader(root).Title };
        var instance = new BtGroupInstance { RootGuid = root };
        instance.Members.AddRange(ids.Select(id => new BtGroupMember { Guid = id, Key = Guid.NewGuid().ToString("N") }));
        group.Instances.Add(instance);
        graph.LinkedGroups.Add(group);
        EditorUtility.SetDirty(graph);
        return group;
    }

    internal static void Unlink(BtGraphAsset graph, BtGroupInstance instance)
    {
        var family = Family(graph, instance);
        if (family == null) return;
        Undo.RegisterCompleteObjectUndo(graph, "BT Unlink Group");
        family.Instances.Remove(instance);
        if (family.Instances.Count == 0) graph.LinkedGroups.Remove(family);
        EditorUtility.SetDirty(graph);
    }

    internal static BtGroupInstance Duplicate(BtGraphAsset graph, BtGroupInstance source, Vector2 offset)
    {
        EnsureTypesAvailable(graph);
        var group = Family(graph, source) ?? throw new InvalidOperationException("The linked group no longer exists.");
        Undo.RegisterCompleteObjectUndo(graph, "BT Duplicate Group");
        var target = new BtGroupInstance();
        CopyInstance(graph, source, target, graph.FindHeader(source.RootGuid).Position + offset);
        group.Instances.Add(target);
        graph.ReloadNodeCounts();
        graph.MarkModified();
        return target;
    }

    internal static string ConnectionError(BtGraphAsset graph, string parent, string child)
    {
        var from = Find(graph, parent);
        var to = Find(graph, child);
        if (to != null && from != to && (from != null || child != to.RootGuid))
            return "Connection blocked: linked groups cannot overlap. Unlink the branch first.";
        if (from != null)
        {
            foreach (string id in Branch(graph, child))
            {
                var memberOf = Find(graph, id);
                if (memberOf != null && memberOf != from)
                    return "Connection blocked: this branch contains another linked group.";
            }
        }
        return null;
    }

    // Call inside the originating edit's Undo transaction, after the edit is complete.
    internal static void Synchronize(BtGraphAsset graph, BtGroupInstance source)
    {
        var group = Family(graph, source);
        if (group == null) return;
        // A single selection may delete roots and edit other instances of the same family.
        group.Instances.RemoveAll(i => graph.FindHeader(i.RootGuid) == null);
        if (group.Instances.Count == 0)
        {
            graph.LinkedGroups.Remove(group);
            return;
        }
        if (!group.Instances.Contains(source)) return;
        EnsureTypesAvailable(graph);
        var ids = Branch(graph, source.RootGuid).ToHashSet();
        if (ids.Any(id => Find(graph, id) is BtGroupInstance other && other != source))
            throw new InvalidOperationException("Linked groups cannot overlap.");
        var removed = source.Members.Where(m => !ids.Contains(m.Guid)).ToArray();
        foreach (var target in group.Instances.Where(i => i != source))
        {
            var deleted = removed.Where(m => graph.FindHeader(m.Guid) == null).Select(m => m.Key).ToHashSet();
            DeleteNodes(graph, target.Members.Where(m => deleted.Contains(m.Key)).Select(m => m.Guid).ToHashSet());
            var detached = target.Members.Where(m => removed.Any(r => r.Key == m.Key)).Select(m => m.Guid).ToHashSet();
            foreach (string id in detached)
            {
                var node = graph.FindHeader(id);
                if (node != null && !detached.Contains(node.ParentGuid ?? "")) node.ParentGuid = null;
            }
            target.Members.RemoveAll(m => detached.Contains(m.Guid));
        }
        source.Members.RemoveAll(m => !ids.Contains(m.Guid));
        foreach (string id in ids)
            if (!source.Members.Any(m => m.Guid == id))
                source.Members.Add(new BtGroupMember { Guid = id, Key = Guid.NewGuid().ToString("N") });
        foreach (var target in group.Instances.Where(i => i != source).ToArray())
            CopyInstance(graph, source, target, graph.FindHeader(target.RootGuid).Position);
        graph.ReloadNodeCounts();
        graph.MarkModified();
    }

    private static void CopyInstance(BtGraphAsset graph, BtGroupInstance source, BtGroupInstance target, Vector2 origin)
    {
        var snapshot = Object.Instantiate(graph);
        try
        {
            var root = snapshot.FindHeader(source.RootGuid);
            var offset = origin - root.Position;
            string externalParent = graph.FindHeader(target.RootGuid)?.ParentGuid;
            var map = source.Members.ToDictionary(m => m.Guid, m =>
                target.Members.FirstOrDefault(t => t.Key == m.Key)?.Guid ?? Guid.NewGuid().ToString("N"));
            foreach (var member in source.Members)
            {
                var node = snapshot.FindHeader(member.Guid);
                string old = node.Guid;
                string newId = map[old];
                var children = snapshot.GetChildrenList(old, node.Kind);
                if (children != null)
                    for (int n = 0; n < children.Count; n++) children[n] = map[children[n]];
                graph.Nodes.RemoveAll(n => n.Guid == newId);
                graph.RemoveTypedDataByGuid(newId);
                switch (node.Kind)
                {
                    case BtNodeKind.Leaf:
                        var leaf = snapshot.FindLeafData(old); leaf.Guid = newId; graph.LeafNodes.Add(leaf); break;
                    case BtNodeKind.Sequence:
                        var sequence = snapshot.FindSequenceData(old); sequence.Guid = newId; graph.SequenceNodes.Add(sequence); break;
                    case BtNodeKind.Selector:
                        var selector = snapshot.FindSelectorData(old); selector.Guid = newId; graph.SelectorNodes.Add(selector); break;
                    case BtNodeKind.MemorySequence:
                        var ms = snapshot.FindMemorySequenceData(old); ms.Guid = newId; graph.MemorySequenceNodes.Add(ms); break;
                    case BtNodeKind.MemorySelector:
                        var mr = snapshot.FindMemorySelectorData(old); mr.Guid = newId; graph.MemorySelectorNodes.Add(mr); break;
                    case BtNodeKind.Parallel:
                        var parallel = snapshot.FindParallelData(old); parallel.Guid = newId; graph.ParallelNodes.Add(parallel); break;
                    default:
                        var decorator = snapshot.FindDecoratorData(old); decorator.Guid = newId; graph.DecoratorNodes.Add(decorator); break;
                }
                node.Position += offset;
                node.ParentGuid = old == source.RootGuid ? externalParent : map[node.ParentGuid];
                node.Guid = newId;
                graph.Nodes.Add(node);
            }
            target.RootGuid = map[source.RootGuid];
            target.Members = source.Members.Select(m => new BtGroupMember { Key = m.Key, Guid = map[m.Guid] }).ToList();
        }
        finally { Object.DestroyImmediate(snapshot); }
    }

    private static void EnsureTypesAvailable(BtGraphAsset graph)
    {
        if (SerializationUtility.HasManagedReferencesWithMissingTypes(graph))
            throw new InvalidOperationException("Restore missing serialized types before editing linked groups. Saved data is preserved.");
    }

    internal static void DeleteNodes(BtGraphAsset graph, HashSet<string> ids)
    {
        graph.Nodes.RemoveAll(n => ids.Contains(n.Guid));
        foreach (string id in ids) graph.RemoveTypedDataByGuid(id);
        if (graph.RootNode != null && ids.Contains(graph.RootNode.ChildrenGuid ?? "")) graph.RootNode.ChildrenGuid = null;
        foreach (var node in graph.Nodes)
        {
            if (ids.Contains(node.ParentGuid ?? "")) node.ParentGuid = null;
            graph.GetChildrenList(node.Guid, node.Kind)?.RemoveAll(ids.Contains);
        }
    }

    internal static void Move(BtGraphAsset graph, IReadOnlyDictionary<string, Vector2> positions)
    {
        var affected = positions.Keys.Select(id => Find(graph, id)).Where(i => i != null).Distinct().ToList();
        var changedLayout = affected.Where(instance =>
        {
            Vector2 delta = positions.TryGetValue(instance.RootGuid, out var origin)
                ? origin - graph.FindHeader(instance.RootGuid).Position : Vector2.zero;
            return instance.Members.Any(m => m.Guid != instance.RootGuid && positions.TryGetValue(m.Guid, out var p) &&
                p != graph.FindHeader(m.Guid).Position + delta);
        }).ToHashSet();
        foreach (var instance in affected)
        {
            if (!positions.TryGetValue(instance.RootGuid, out var origin)) continue;
            var delta = origin - graph.FindHeader(instance.RootGuid).Position;
            foreach (var member in instance.Members) graph.FindHeader(member.Guid).Position += delta;
        }
        foreach (var pair in positions)
            if (graph.FindHeader(pair.Key) is BtNodeHeader node) node.Position = pair.Value;
        SortChildren(graph);
        // One source per family prevents a multiple-instance drag from applying the same delta twice.
        foreach (var source in affected.GroupBy(i => Family(graph, i)).Select(g => g.FirstOrDefault(changedLayout.Contains)).Where(i => i != null))
            Synchronize(graph, source);
        SortChildren(graph);
    }

    internal static void SortChildren(BtGraphAsset graph)
    {
        foreach (var node in graph.Nodes)
        {
            var children = graph.GetChildrenList(node.Guid, node.Kind);
            if (children == null) continue;
            // Stable ties retain equivalent order between instances despite their distinct GUIDs.
            var sorted = children.OrderBy(id => graph.FindHeader(id)?.Position.x ?? 0)
                .ThenBy(id => graph.FindHeader(id)?.Position.y ?? 0).ToArray();
            children.Clear(); children.AddRange(sorted);
        }
    }
}
