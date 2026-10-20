// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{

    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using VadimBurym.DodBehaviourTree.Dots;

    internal static class BtCompiler
    {
        private static readonly Queue<BtNodeHeader> _nodesQueue = new();
        private static readonly List<Node> _nodes = new();
        private static readonly List<SelectorNode> _selectorNodes = new();
        private static readonly List<SequenceNode> _sequenceNodes = new();
        private static readonly List<MemorySelectorNode> _memorySelectorNodes = new();
        private static readonly List<MemorySequenceNode> _memorySequenceNodes = new();
        private static readonly List<ParallelNode> _parallelNodes = new();
        private static readonly List<LeafData>  _leafs = new();
        private static readonly List<string> _guidsByCompiled = new();
        private static readonly Dictionary<int, string> _leafBindings = new();
        private static string _contextIdentity;

        internal static string TryCompileAsset(BtGraphAsset asset, BehaviourTreeAsset compiled)
        {
            var validation = BtGraphValidation.Validate(asset);
            if (!string.IsNullOrEmpty(validation)) return validation;
            if (compiled == null) return "No compiled asset selected.";
            var root = asset.FindHeader(asset.RootNode.ChildrenGuid);
            if (root == null) return "There is no entry point in the graph (Root node has no outputs).";
            ClearBuffers();
            _nodesQueue.Enqueue(root);
            while (_nodesQueue.Count > 0)
            {
                var node = _nodesQueue.Dequeue();
                _guidsByCompiled.Add(node.Guid);
                var error = CompileNode(node, asset);
                if (!string.IsNullOrEmpty(error)) return error;
            }

            var indices = new Dictionary<string, int>();
            for (int i = 0; i < _guidsByCompiled.Count; i++) indices.Add(_guidsByCompiled[i], i);
            for (int i = 0; i < _guidsByCompiled.Count; i++)
            {
                var guid = _guidsByCompiled[i];
                var node = asset.FindHeader(guid);
                var parent = node.ParentGuid != null && indices.TryGetValue(node.ParentGuid, out var index) ? index : -1;
                var nodeData = _nodes[i];
                nodeData.ParentIndex = Convert(parent != -1 ? parent : -1);
                _nodes[i] = nodeData;
            }

            var bindings = _leafBindings.OrderBy(p => p.Key)
                .Select(p => new LeafBinding { Id = p.Key, TypeIdentity = p.Value }).ToArray();
            var compatibilityError = LeafBindingRegistry.Validate(_contextIdentity, bindings, _leafs.ToArray());
            if (compatibilityError != null) return compatibilityError;
            BtGraphContext.BindLegacy(asset);
            compiled.InternalCompiledRevision = System.Guid.NewGuid().ToString("N");
            compiled.SetupCompiledTree(
                _nodes.ToArray(),
                0,
                _selectorNodes.ToArray(),
                _sequenceNodes.ToArray(),
                _memorySelectorNodes.ToArray(),
                _memorySequenceNodes.ToArray(),
                _parallelNodes.ToArray(),
                _leafs.ToArray(),
                _contextIdentity,
                bindings);
            if (string.IsNullOrEmpty(compiled.InternalGUID))
            {
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
                compiled.InternalGUID = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            }
            asset.GuidsByCompiledId = _guidsByCompiled.ToArray();
            asset.CompiledRevision = compiled.InternalCompiledRevision;
            asset.CompiledAsset = compiled;
            asset.SetupNewCompiledVersion();
            UnityEditor.EditorUtility.SetDirty(compiled);
            UnityEditor.AssetDatabase.SaveAssets();
            return string.Empty;
        }

        private static string CompileNode(BtNodeHeader nodeData, BtGraphAsset asset)
        {
            switch (nodeData.Kind)
            {
                case BtNodeKind.Selector:
                    var selectorData = asset.FindSelectorData(nodeData.Guid);
                    var selectorChildren = selectorData.ChildrenGuids;
                    if (selectorChildren.Count == 0) return "One of your Selectors does not have children.";
                    _nodes.Add(new Node {
                        Id = NodeId.Selector,
                        DataIndex = Convert(_selectorNodes.Count) });
                    _selectorNodes.Add(new SelectorNode {
                        FirstChild = Convert(_nodes.Count + _nodesQueue.Count),
                        ChildCount = (byte)selectorChildren.Count });
                    for (int i = 0; i < selectorChildren.Count; i++)
                        _nodesQueue.Enqueue(asset.FindHeader(selectorChildren[i]));
                    return string.Empty;
                case BtNodeKind.MemorySelector:
                    var memorySelectorData = asset.FindMemorySelectorData(nodeData.Guid);
                    var memorySelectorChildren = memorySelectorData.ChildrenGuids;
                    if (memorySelectorChildren.Count == 0) return "One of your MemorySelectors does not have children.";
                    _nodes.Add(new Node {
                        Id = NodeId.MemorySelector,
                        DataIndex = Convert(_memorySelectorNodes.Count) });
                    _memorySelectorNodes.Add(new MemorySelectorNode {
                        FirstChild = Convert(_nodes.Count + _nodesQueue.Count),
                        ChildCount = (byte)memorySelectorChildren.Count,
                        PickRandom = memorySelectorData.PickRandom ? (byte)1 : (byte)0,
                        ResetOnAbort = memorySelectorData.ResetOnAbort ? (byte)1 : (byte)0 });
                    for (int i = 0; i < memorySelectorChildren.Count; i++)
                        _nodesQueue.Enqueue(asset.FindHeader(memorySelectorChildren[i]));
                    return string.Empty;
                case BtNodeKind.MemorySequence:
                    var memorySequenceData = asset.FindMemorySequenceData(nodeData.Guid);
                    var memorySequenceChildren = memorySequenceData.ChildrenGuids;
                    if (memorySequenceChildren.Count == 0) return "One of your MemorySequences does not have children.";
                    _nodes.Add(new Node {
                        Id = NodeId.MemorySequence,
                        DataIndex = Convert(_memorySequenceNodes.Count)
                    });
                    _memorySequenceNodes.Add(new MemorySequenceNode {
                        FirstChild = Convert(_nodes.Count + _nodesQueue.Count),
                        ChildCount = (byte)memorySequenceChildren.Count,
                        ResetOnAbort = memorySequenceData.ResetOnAbort ? (byte)1 : (byte)0,
                        ResetOnFailure = memorySequenceData.ResetOnFailure ? (byte)1 : (byte)0 });
                    for (int i = 0; i < memorySequenceChildren.Count; i++)
                        _nodesQueue.Enqueue(asset.FindHeader(memorySequenceChildren[i]));
                    return string.Empty;
                case BtNodeKind.Sequence:
                    var sequenceData = asset.FindSequenceData(nodeData.Guid);
                    var sequenceChildren = sequenceData.ChildrenGuids;
                    if (sequenceChildren.Count == 0) return "One of your Sequences does not have children.";
                    _nodes.Add(new Node {
                        Id = NodeId.Sequence,
                        DataIndex = Convert(_sequenceNodes.Count) });
                    _sequenceNodes.Add(new SequenceNode {
                        FirstChild = Convert(_nodes.Count + _nodesQueue.Count),
                        ChildCount = (byte)sequenceChildren.Count });
                    for (int i = 0; i < sequenceChildren.Count; i++)
                        _nodesQueue.Enqueue(asset.FindHeader(sequenceChildren[i]));
                    return string.Empty;
                case BtNodeKind.Leaf:
                    var leafData = asset.FindLeafData(nodeData.Guid);
                    _nodes.Add(new Node {
                        Id = NodeId.Leaf,
                        DataIndex = Convert(_leafs.Count) });
                    if (leafData.Leaf == null) return "One of your Leafs does not have an ILeaf implementation.";
                    var type = leafData.Leaf.GetType();
                    var declaration = type.GetCustomAttribute<LeafCodeGenAttribute>();
                    var data = leafData.Leaf.GetCompiledData();
                    if (data.LeafId < 0 || data.LeafId != declaration.Id) return "Invalid compiled LeafId on " + type.FullName;
                    var identity = LeafBindingRegistry.Identity(type);
                    if (_leafBindings.TryGetValue(data.LeafId, out var existing) && existing != identity)
                        return "Different leaf classes use the same ID: " + data.LeafId;
                    _leafBindings[data.LeafId] = identity;
                    _contextIdentity = LeafBindingRegistry.Identity(declaration.ContextType);
                    _leafs.Add(data);
                    return string.Empty;
                case BtNodeKind.Parallel:
                    var parallelData = asset.FindParallelData(nodeData.Guid);
                    var parallelChildren = parallelData.ChildrenGuids;
                    if (parallelChildren.Count == 0) return "One of your Parallels does not have children.";
                    _nodes.Add(new Node {
                        Id = NodeId.Parallel,
                        DataIndex = Convert(_parallelNodes.Count) });
                    _parallelNodes.Add(new ParallelNode {
                        FirstChild = Convert(_nodes.Count + _nodesQueue.Count),
                        ChildCount = (byte)parallelChildren.Count,
                        CacheChildStatus = parallelData.CacheChildStatus ? (byte)1 : (byte)0,
                        FailsThreshold = parallelData.FailureThreshold == -1 ? (byte)parallelChildren.Count : (byte)parallelData.FailureThreshold,
                        SuccessThreshold = parallelData.SuccessThreshold == -1 ? (byte)parallelChildren.Count : (byte)parallelData.SuccessThreshold });
                    for (int i = 0; i < parallelChildren.Count; i++)
                        _nodesQueue.Enqueue(asset.FindHeader(parallelChildren[i]));
                    return string.Empty;
            }
            return "Unknown Node Type";
        }

        private static void ClearBuffers()
        {
            _leafs.Clear();
            _leafBindings.Clear();
            _contextIdentity = null;
            _memorySequenceNodes.Clear();
            _memorySelectorNodes.Clear();
            _sequenceNodes.Clear();
            _selectorNodes.Clear();
            _parallelNodes.Clear();
            _nodesQueue.Clear();
            _nodes.Clear();
            _guidsByCompiled.Clear();
        }
#if DODBT_SMALL_SIZE
        private static byte Convert(int value) => (byte)value;
#else
        private static ushort Convert(int value) => (ushort)value;
#endif
    }
}
