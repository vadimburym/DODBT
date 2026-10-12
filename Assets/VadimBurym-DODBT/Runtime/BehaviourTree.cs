// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VadimBurym.DodBehaviourTree
{
    public sealed class BehaviourTree<TContext, TLeafState>
        where TContext : class where TLeafState : struct
    {
        private const byte Unknown = 0;
        private const byte Failure = 0xFF;
        private const byte Success = 1;
        private const byte Running = 2;
        private const byte None = 0xFF;

        public ILeaf[] Leafs => _asset.Leafs;
        private BehaviourTreeAsset _asset;
        private ILeaf<TContext, TLeafState>[] _leafs;
        private int[] _buffer;
        private int[] _randomOrderOffsets;

        public void Construct(BehaviourTreeAsset asset)
        {
            _asset = asset;
            _leafs = new ILeaf<TContext, TLeafState>[asset.Leafs.Length];
            for (int i = 0; i < _leafs.Length; i++)
            {
#if UNITY_EDITOR
                try
                {
                    _leafs[i] = (ILeaf<TContext, TLeafState>)asset.Leafs[i];
                }
                catch (InvalidCastException)
                {
                    var actualType = asset.Leafs[i]?.GetType();
                    throw new InvalidOperationException(
                        $"[BehaviourTree] Leaf at index {i} has type '{actualType?.FullName}', " +
                        $"but BehaviourTree was constructed with <{typeof(TContext).Name}, {typeof(TLeafState).Name}>. " +
                        $"Make sure all leafs implement ILeaf<{typeof(TContext).Name}, {typeof(TLeafState).Name}>.",
                        innerException: null);
                }
#else
                _leafs[i] = (ILeaf<TContext, TLeafState>)asset.Leafs[i];
#endif
            }
            // Nested random selectors must not overwrite the order of their parents.
            // These buffers belong to the shared tree, not to individual agents.
            _randomOrderOffsets = new int[asset.MemorySelectorNodes.Length];
            int bufferSize = 0;
            for (int i = 0; i < asset.MemorySelectorNodes.Length; i++)
            {
                _randomOrderOffsets[i] = bufferSize;
                if (asset.MemorySelectorNodes[i].PickRandom)
                    bufferSize += asset.MemorySelectorNodes[i].ChildCount;
            }
            _buffer = bufferSize == 0 ? Array.Empty<int>() : new int[bufferSize];
        }
        
        public void FillInitialState(BtState<TLeafState> btState)
        {
            var nodes = _asset.Nodes;
            if (btState.NodeStates.Length < nodes.Length)
                btState.NodeStates = new NodeState[Mathf.NextPowerOfTwo(nodes.Length)];
            if (btState.LeafStates.Length < _asset.Leafs.Length)
                btState.LeafStates = new TLeafState[Mathf.NextPowerOfTwo(_asset.Leafs.Length)];
            var nodeStates = btState.NodeStates;
            for (int i = 0; i < nodes.Length; i++)
            {
                ref var nodeState = ref nodeStates[i];
                nodeState.Reset();
                var node = nodes[i];
                if (node.Id == NodeId.Leaf)
                    nodeState.LeafStateIndex = node.DataIndex;
            }
#if UNITY_EDITOR
            btState.DebugRunningLeafs.Clear();
            btState.DebugRunningLeafIndices.Clear();
            if (btState.DebugStatus.Length < nodes.Length)
                btState.DebugStatus = new NodeStatus[Mathf.NextPowerOfTwo(nodes.Length)];
            var debugStatus = btState.DebugStatus;
            for (int i = 0; i < nodes.Length; i++)
                debugStatus[i] = NodeStatus.None;
#endif
        }
        
        public void Tick(TContext context, BtState<TLeafState> state) => TickWithStatus(context, state);

        public NodeStatus TickWithStatus(TContext context, BtState<TLeafState> state)
        {
#if UNITY_EDITOR
            state.DebugRunningLeafs.Clear();
            state.DebugRunningLeafIndices.Clear();
            for (int i = 0; i < state.DebugStatus.Length; i++)
                state.DebugStatus[i] = NodeStatus.None;
#endif
            var status = TickNode(context, state, _asset.RootIndex);
#if UNITY_EDITOR
            state.DebugStatus[_asset.RootIndex] = status;
            // A parent may abort leaves that returned Running earlier in this tick.
            foreach (int index in state.DebugRunningLeafIndices)
                if (state.NodeStates[index].IsEntered)
                    state.DebugRunningLeafs.Add(DebugUtils.GetLeafName(_leafs[_asset.Nodes[index].DataIndex]));
#endif
            return status;
        }

        public void Abort(TContext context, BtState<TLeafState> state)
        {
            AbortNode(context, state, _asset.RootIndex);
#if UNITY_EDITOR
            state.DebugRunningLeafs.Clear();
            state.DebugRunningLeafIndices.Clear();
            Array.Clear(state.DebugStatus, 0, state.DebugStatus.Length);
#endif
        }
        
        private NodeStatus TickNode(TContext context, BtState<TLeafState> state, int index)
        {
            ref var node = ref _asset.Nodes[index];
            ref var nodeState = ref state.NodeStates[index];
            switch (node.Id)
            {
                case NodeId.Selector:
                    ref var selectorNode = ref _asset.SelectorNodes[node.DataIndex];
                    for (int i = 0; i < selectorNode.ChildCount; i++)
                    {
                        var status = TickNode(context, state, selectorNode.FirstChild + i);
#if UNITY_EDITOR
                        state.DebugStatus[selectorNode.FirstChild + i] = status;
#endif
                        if (status == NodeStatus.Failure) continue;
                        if (nodeState.Cursor != i && nodeState.Cursor != None)
                            AbortNode(context, state, selectorNode.FirstChild + nodeState.Cursor);
                        nodeState.Cursor = status == NodeStatus.Running ? (byte)i : None;
                        return status;
                    }
                    nodeState.Cursor = None;
                    return NodeStatus.Failure;
                
                case NodeId.Sequence:
                    ref var sequenceNode = ref _asset.SequenceNodes[node.DataIndex];
                    for (int i = 0; i < sequenceNode.ChildCount; i++)
                    {
                        var status = TickNode(context, state, sequenceNode.FirstChild + i);
#if UNITY_EDITOR
                        state.DebugStatus[sequenceNode.FirstChild + i] = status;
#endif
                        if (status == NodeStatus.Success) continue;
                        if (nodeState.Cursor != i && nodeState.Cursor != None)
                            AbortNode(context, state, sequenceNode.FirstChild + nodeState.Cursor);
                        nodeState.Cursor = status == NodeStatus.Running ? (byte)i : None;
                        return status;
                    }
                    nodeState.Cursor = None;
                    return NodeStatus.Success;
                
                case NodeId.MemorySequence:
                    ref var memorySequenceNode = ref _asset.MemorySequenceNodes[node.DataIndex];
                    var cursor = nodeState.Cursor;
                    if (cursor != None) cursor--;
                    for (; ++cursor < memorySequenceNode.ChildCount;)
                    {
                        var status = TickNode(context, state, memorySequenceNode.FirstChild + cursor);
#if UNITY_EDITOR
                        state.DebugStatus[memorySequenceNode.FirstChild + cursor] = status;
#endif
                        if (status == NodeStatus.Success) continue;
                        nodeState.Cursor = status == NodeStatus.Failure && memorySequenceNode.ResetOnFailure ? None : cursor;
                        return status;
                    }
                    nodeState.Cursor = None;
                    return NodeStatus.Success;
                
                case NodeId.MemorySelector:
                    ref var memorySelectorNode = ref _asset.MemorySelectorNodes[node.DataIndex];
                    if (nodeState.Cursor != None)
                    {
                        var status = TickNode(context, state, memorySelectorNode.FirstChild + nodeState.Cursor);
#if UNITY_EDITOR
                        state.DebugStatus[memorySelectorNode.FirstChild + nodeState.Cursor] = status;
#endif
                        if (status != NodeStatus.Failure)
                        {
                            if (status == NodeStatus.Success) nodeState.Cursor = None;
                            return status;
                        }
                    }
                    int orderOffset = _randomOrderOffsets[node.DataIndex];
                    if (memorySelectorNode.PickRandom) WarmUpBuffer(orderOffset, memorySelectorNode.ChildCount);
                    for (int i = 0; i < memorySelectorNode.ChildCount; i++)
                    {
                        var bufferCursor = memorySelectorNode.PickRandom ? _buffer[orderOffset + i] : i;
                        var status = TickNode(context, state, memorySelectorNode.FirstChild + bufferCursor);
#if UNITY_EDITOR
                        state.DebugStatus[memorySelectorNode.FirstChild + bufferCursor] = status;
#endif
                        if (status == NodeStatus.Failure) continue;
                        nodeState.Cursor = status == NodeStatus.Running ? (byte)bufferCursor : None;
                        return status;
                    }
                    nodeState.Cursor = None;
                    return NodeStatus.Failure;
                
                case NodeId.Leaf:
                    var leaf = _leafs[node.DataIndex];
                    ref var leafState = ref state.LeafStates[nodeState.LeafStateIndex];
                    if (!nodeState.IsEntered)
                    {
                        leaf.OnEnter(context, ref leafState);
                        nodeState.IsEntered = true;
                    }
                    var leafStatus = leaf.OnTick(context, ref leafState);
#if UNITY_EDITOR
                    state.DebugStatus[index] = leafStatus;
                    if (leafStatus == NodeStatus.Running) state.DebugRunningLeafIndices.Add(index);
#endif
                    if (leafStatus != NodeStatus.Running)
                    {
                        leaf.OnExit(context, ref leafState, leafStatus);
                        nodeState.IsEntered = false;
                    }
                    return leafStatus;
                
                case NodeId.Parallel:
                    ref var parallelNode = ref _asset.ParallelNodes[node.DataIndex];
                    byte success = 0; byte fails = 0;
                    for (int i = 0; i < parallelNode.ChildCount; i++)
                    {
                        ref var childState = ref state.NodeStates[parallelNode.FirstChild + i];
                        if (childState.CachedStatus == Failure)
                        {
                            fails++;
#if UNITY_EDITOR
                            state.DebugStatus[parallelNode.FirstChild + i] = NodeStatus.Failure;
#endif
                            continue;
                        }
                        if (childState.CachedStatus == Success)
                        {
                            success++;
#if UNITY_EDITOR
                            state.DebugStatus[parallelNode.FirstChild + i] = NodeStatus.Success;
#endif
                            continue;
                        }
                        var status = TickNode(context, state, parallelNode.FirstChild + i);
#if UNITY_EDITOR
                        state.DebugStatus[parallelNode.FirstChild + i] = status;
#endif
                        if (status == NodeStatus.Failure)
                        {
                            fails++;
                            childState.CachedStatus = parallelNode.CacheChildStatus ? Failure : Unknown;
                        }
                        else if (status == NodeStatus.Success)
                        {
                            success++;
                            childState.CachedStatus = parallelNode.CacheChildStatus ? Success : Unknown;
                        }
                        else
                        {
                            childState.CachedStatus = Running;
                        }
                    }
                    if (success >= parallelNode.SuccessThreshold || fails >= parallelNode.FailsThreshold)
                    {
                        for (byte i = 0; i < parallelNode.ChildCount; i++)
                        {
                            ref var childState = ref state.NodeStates[parallelNode.FirstChild + i];
                            if (childState.CachedStatus == Running) AbortNode(context, state, parallelNode.FirstChild + i);
                            childState.CachedStatus = Unknown;
                        }
                        return fails >= parallelNode.FailsThreshold ? NodeStatus.Failure : NodeStatus.Success;
                    }
                    return NodeStatus.Running;

                default:
                    return NodeStatus.Failure;
            }
        }

        private void AbortNode(TContext context, BtState<TLeafState> state, int index)
        {
#if UNITY_EDITOR
            state.DebugStatus[index] = NodeStatus.None;
#endif
            ref var node = ref _asset.Nodes[index];
            ref var nodeState = ref state.NodeStates[index];
            switch (node.Id)
            {
                case NodeId.Selector:
                    ref var selectorNode = ref _asset.SelectorNodes[node.DataIndex];
                    if (nodeState.Cursor == None) return;
                    AbortNode(context, state, selectorNode.FirstChild + nodeState.Cursor);
                    nodeState.Cursor = None;
                    return;
                
                case NodeId.Sequence:
                    ref var sequenceNode = ref _asset.SequenceNodes[node.DataIndex];
                    if (nodeState.Cursor == None) return;
                    AbortNode(context, state, sequenceNode.FirstChild + nodeState.Cursor);
                    nodeState.Cursor = None;
                    return;
                
                case NodeId.MemorySequence:
                    ref var memorySequenceNode = ref _asset.MemorySequenceNodes[node.DataIndex];
                    if (nodeState.Cursor == None) return;
                    AbortNode(context, state, memorySequenceNode.FirstChild + nodeState.Cursor);
                    if (memorySequenceNode.ResetOnAbort) nodeState.Cursor = None;
                    return;
                
                case NodeId.MemorySelector:
                    ref var memorySelectorNode = ref _asset.MemorySelectorNodes[node.DataIndex];
                    if (nodeState.Cursor == None) return;
                    AbortNode(context, state, memorySelectorNode.FirstChild + nodeState.Cursor);
                    if (memorySelectorNode.ResetOnAbort) nodeState.Cursor = None;
                    return;
                
                case NodeId.Leaf:
                    if (!nodeState.IsEntered) return;
                    var leaf = _leafs[node.DataIndex];
                    ref var leafState = ref state.LeafStates[nodeState.LeafStateIndex];
                    nodeState.IsEntered = false;
                    leaf.OnAbort(context, ref leafState);
                    return;
                
                case NodeId.Parallel:
                    ref var parallelNode = ref _asset.ParallelNodes[node.DataIndex];
                    for (int i = 0; i < parallelNode.ChildCount; i++)
                    {
                        ref var childState = ref state.NodeStates[parallelNode.FirstChild + i];
                        if (childState.CachedStatus == Running) AbortNode(context, state, parallelNode.FirstChild + i);
                        childState.CachedStatus = Unknown;
                    }
                    return;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void WarmUpBuffer(int offset, int count)
        {
            for (int i = 0; i < count; i++) 
                _buffer[offset + i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_buffer[offset + i], _buffer[offset + j]) = (_buffer[offset + j], _buffer[offset + i]);
            }
        }
    }
}
