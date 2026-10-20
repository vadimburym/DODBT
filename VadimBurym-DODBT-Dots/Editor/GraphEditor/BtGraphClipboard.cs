// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal static class BtGraphClipboard
    {
        private const string Prefix = "DODBT-DOTS-NODES-1\n";

        [Serializable]
        private sealed class Payload
        {
            public string Project;
            public string Session;
            public string Graph;
            public List<ObjectReference> References = new();
        }

        [Serializable]
        private sealed class ObjectReference
        {
            public string Path;
            public string GlobalId;
            public int InstanceId;
            public bool Persistent;
        }

        private static string SessionId()
        {
            const string key = "DODBT.Dots.Clipboard.Session";
            string id = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(id)) SessionState.SetString(key, id = Guid.NewGuid().ToString("N"));
            return id;
        }

        internal static bool CanPaste(string text) => !string.IsNullOrEmpty(text) && text.StartsWith(Prefix, StringComparison.Ordinal);

        internal static string Copy(BtGraphAsset graph, IEnumerable<string> selected)
        {
            if (graph == null) return string.Empty;
            var ids = selected.ToHashSet();
            var snapshot = Object.Instantiate(graph);
            try
            {
                foreach (var node in snapshot.Nodes.ToArray())
                    if (!ids.Contains(node.Guid)) snapshot.RemoveTypedDataByGuid(node.Guid);
                snapshot.Nodes.RemoveAll(node => !ids.Contains(node.Guid));
                if (snapshot.Nodes.Count == 0) return string.Empty;
                KeepSelectedData(snapshot, ids);
                foreach (var node in snapshot.Nodes)
                {
                    if (!ids.Contains(node.ParentGuid ?? "")) node.ParentGuid = null;
                    snapshot.GetChildrenList(node.Guid, node.Kind)?.RemoveAll(id => !ids.Contains(id));
                }
                snapshot.RootNode = null;
                snapshot.LinkedGroups.Clear();
                snapshot.CompiledAsset = null;
                snapshot.GuidsByCompiledId = Array.Empty<string>();
                var payload = new Payload { Project = Application.dataPath, Session = SessionId(), Graph = EditorJsonUtility.ToJson(snapshot) };
                using var serialized = new SerializedObject(snapshot);
                var property = serialized.GetIterator();
                var visited = new HashSet<long>();
                bool enter = true;
                while (property.Next(enter))
                {
                    enter = property.propertyType != SerializedPropertyType.ManagedReference || visited.Add(property.managedReferenceId);
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null || property.propertyPath == "m_Script") continue;
                    var value = property.objectReferenceValue;
                    payload.References.Add(new ObjectReference {
                        Path = property.propertyPath,
                        GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(value).ToString(),
                        InstanceId = value.GetInstanceID(),
                        Persistent = EditorUtility.IsPersistent(value)
                    });
                }
                return Prefix + JsonUtility.ToJson(payload);
            }
            finally { Object.DestroyImmediate(snapshot); }
        }

        internal static List<string> Paste(BtGraphAsset graph, string text, Vector2 offset)
        {
            if (graph == null || !CanPaste(text)) throw new InvalidOperationException("The clipboard does not contain compatible DODBT nodes.");
            var snapshot = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                var payload = JsonUtility.FromJson<Payload>(text.Substring(Prefix.Length));
                if (payload == null || payload.Project != Application.dataPath || string.IsNullOrEmpty(payload.Graph) || payload.References == null)
                    throw new InvalidOperationException("The clipboard belongs to another project or has an invalid format.");
                EditorJsonUtility.FromJsonOverwrite(payload.Graph, snapshot);
                if (SerializationUtility.HasManagedReferencesWithMissingTypes(snapshot))
                    throw new InvalidOperationException("Clipboard leaf types are missing. Restore their scripts before pasting.");
                using (var serialized = new SerializedObject(snapshot))
                {
                    foreach (var reference in payload.References)
                    {
                        if (reference == null || string.IsNullOrEmpty(reference.Path))
                            throw new InvalidOperationException("The clipboard contains an invalid object reference.");
                        Object value = null;
                        if (reference.Persistent && GlobalObjectId.TryParse(reference.GlobalId, out var globalId))
                            value = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                        else if (!reference.Persistent && payload.Session == SessionId())
                            value = EditorUtility.EntityIdToObject(reference.InstanceId);
                        if (value == null) throw new InvalidOperationException("A copied Unity object is no longer available: " + reference.Path);
                        var property = serialized.FindProperty(reference.Path);
                        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                            throw new InvalidOperationException("Copied field layout changed: " + reference.Path);
                        property.objectReferenceValue = value;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var remap = new Dictionary<string, string>();
                foreach (var node in snapshot.Nodes)
                {
                    if (node == null || string.IsNullOrEmpty(node.Guid) || !remap.TryAdd(node.Guid, Guid.NewGuid().ToString("N")))
                        throw new InvalidOperationException("The clipboard contains invalid or duplicate node identifiers.");
                    if (node.Kind < BtNodeKind.Leaf || node.Kind > BtNodeKind.Parallel)
                        throw new InvalidOperationException("Unsupported clipboard node type.");
                    bool hasData = node.Kind == BtNodeKind.Leaf ? snapshot.FindLeafData(node.Guid) != null : snapshot.GetChildrenList(node.Guid, node.Kind) != null;
                    if (!hasData) throw new InvalidOperationException("The clipboard is missing node settings.");
                }
                KeepSelectedData(snapshot, remap.Keys.ToHashSet());
                ValidateTopology(snapshot, offset);
                // Remap the whole snapshot before mutating the destination or recording Undo.
                foreach (var node in snapshot.Nodes)
                {
                    string old = node.Guid;
                    node.Guid = remap[old];
                    node.ParentGuid = node.ParentGuid != null && remap.TryGetValue(node.ParentGuid, out var parent) ? parent : null;
                    node.Position += offset;
                    var children = snapshot.GetChildrenList(old, node.Kind);
                    if (children != null)
                    {
                        children.RemoveAll(child => !remap.ContainsKey(child));
                        for (int i = 0; i < children.Count; i++) children[i] = remap[children[i]];
                    }
                    switch (node.Kind)
                    {
                        case BtNodeKind.Leaf: snapshot.FindLeafData(old).Guid = node.Guid; break;
                        case BtNodeKind.Sequence: snapshot.FindSequenceData(old).Guid = node.Guid; break;
                        case BtNodeKind.Selector: snapshot.FindSelectorData(old).Guid = node.Guid; break;
                        case BtNodeKind.MemorySequence: snapshot.FindMemorySequenceData(old).Guid = node.Guid; break;
                        case BtNodeKind.MemorySelector: snapshot.FindMemorySelectorData(old).Guid = node.Guid; break;
                        case BtNodeKind.Parallel: snapshot.FindParallelData(old).Guid = node.Guid; break;
                    }
                }
                if (snapshot.Nodes.Count == 0) return new List<string>();
                // The native JSON round trip produces independent managed objects, while Unity assets retain their references.
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(graph, "BT Paste Nodes");
                graph.Nodes.AddRange(snapshot.Nodes);
                graph.LeafNodes.AddRange(snapshot.LeafNodes);
                graph.SequenceNodes.AddRange(snapshot.SequenceNodes);
                graph.SelectorNodes.AddRange(snapshot.SelectorNodes);
                graph.MemorySequenceNodes.AddRange(snapshot.MemorySequenceNodes);
                graph.MemorySelectorNodes.AddRange(snapshot.MemorySelectorNodes);
                graph.ParallelNodes.AddRange(snapshot.ParallelNodes);
                graph.ReloadNodeCounts();
                graph.MarkModified();
                return snapshot.Nodes.Select(node => node.Guid).ToList();
            }
            finally { Object.DestroyImmediate(snapshot); }
        }

        private static void ValidateTopology(BtGraphAsset graph, Vector2 offset)
        {
            var headers = graph.Nodes.ToDictionary(n => n.Guid);
            var parents = new Dictionary<string, string>();
            foreach (var node in graph.Nodes)
            {
                var position = node.Position + offset;
                if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y))
                    throw new InvalidOperationException("The clipboard contains an invalid node position.");
                var children = graph.GetChildrenList(node.Guid, node.Kind);
                if (children == null) continue;
                foreach (string child in children)
                {
                    if (string.IsNullOrEmpty(child) || !headers.ContainsKey(child) || !parents.TryAdd(child, node.Guid))
                        throw new InvalidOperationException("The clipboard contains a missing child, duplicate connection, or multiple parents.");
                }
            }
            foreach (var node in graph.Nodes)
            {
                parents.TryGetValue(node.Guid, out string parent);
                if ((node.ParentGuid ?? "") != (parent ?? ""))
                    throw new InvalidOperationException("The clipboard contains inconsistent parent connections.");
            }
            var pending = new Queue<string>(headers.Keys.Where(id => !parents.ContainsKey(id)));
            int visited = 0;
            while (pending.Count > 0)
            {
                string id = pending.Dequeue();
                visited++;
                var children = graph.GetChildrenList(id, headers[id].Kind);
                if (children != null)
                    foreach (string child in children) pending.Enqueue(child);
            }
            if (visited != headers.Count) throw new InvalidOperationException("The clipboard contains a cycle.");
        }

        private static void KeepSelectedData(BtGraphAsset graph, HashSet<string> ids)
        {
            graph.LeafNodes.RemoveAll(data => !ids.Contains(data.Guid));
            graph.SequenceNodes.RemoveAll(data => !ids.Contains(data.Guid));
            graph.SelectorNodes.RemoveAll(data => !ids.Contains(data.Guid));
            graph.MemorySequenceNodes.RemoveAll(data => !ids.Contains(data.Guid));
            graph.MemorySelectorNodes.RemoveAll(data => !ids.Contains(data.Guid));
            graph.ParallelNodes.RemoveAll(data => !ids.Contains(data.Guid));
        }
    }
}
