// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace VadimBurym.DodBehaviourTree.Dots
{
    // Called directly by generated runners in the consuming assembly.
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct NodeExecutor
    {
        private const byte Unknown = 0;
        private const byte Failure = 1;
        private const byte Success = 2;
        private const byte Running = 3;
        private const byte None = 0xFF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AbortSelector(
            ref FixedList4096Bytes<int> stack,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob)
        {
            if (nodeState.MemoryCursor == None)
                return;
            var selectorData = blob.SelectorNodes[nodeData.DataIndex];
            stack.Add(selectorData.FirstChild + nodeState.MemoryCursor);
            nodeState.MemoryCursor = None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AbortSequence(
            ref FixedList4096Bytes<int> stack,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob)
        {
            if (nodeState.MemoryCursor == None)
                return;
            var sequenceData = blob.SequenceNodes[nodeData.DataIndex];
            stack.Add(sequenceData.FirstChild + nodeState.MemoryCursor);
            nodeState.MemoryCursor = None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AbortMemorySequence(
            ref FixedList4096Bytes<int> stack,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob)
        {
            if (nodeState.MemoryCursor == None)
                return;
            var memorySequenceData = blob.MemorySequenceNodes[nodeData.DataIndex];
            stack.Add(memorySequenceData.FirstChild + nodeState.MemoryCursor);
            nodeState.MemoryCursor = memorySequenceData.ResetOnAbort != 0 ? None : nodeState.MemoryCursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AbortMemorySelector(
            ref FixedList4096Bytes<int> stack,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob)
        {
            if (nodeState.MemoryCursor == None)
                return;
            var memorySelectorData = blob.MemorySelectorNodes[nodeData.DataIndex];
            stack.Add(memorySelectorData.FirstChild + nodeState.MemoryCursor);
            nodeState.MemoryCursor = memorySelectorData.ResetOnAbort != 0 ? None : nodeState.MemoryCursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExecuteSequence(
            ref int pc,
            ref NodeStatus childStatus,
            ref bool returning,
            ref ushort abortingNode,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob)
        {
            var sequenceData = blob.SequenceNodes[nodeData.DataIndex];
            if (!returning)
            {
                nodeState.Cursor = 0;
            }
            else
            {
                switch (childStatus)
                {
                    case NodeStatus.Success:
                        nodeState.Cursor++;
                        break;
                    case NodeStatus.Running:
                    {
                        if (nodeState.MemoryCursor != None &&
                            nodeState.MemoryCursor > nodeState.Cursor)
                        {
                            abortingNode = (ushort)(sequenceData.FirstChild + nodeState.MemoryCursor);
                        }
                        nodeState.MemoryCursor = nodeState.Cursor;
                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                    }
                    default: //Failure
                    {
                        if (nodeState.MemoryCursor != None &&
                            nodeState.MemoryCursor > nodeState.Cursor)
                        {
                            abortingNode = (ushort)(sequenceData.FirstChild + nodeState.MemoryCursor);
                        }
                        nodeState.MemoryCursor = None;
                        nodeState.Cursor = None;
                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                    }
                }
            }
            if (nodeState.Cursor >= sequenceData.ChildCount)
            {
                nodeState.Cursor = None;
                nodeState.MemoryCursor = None;
                returning = true;
                childStatus = NodeStatus.Success;
                pc = nodeData.ParentIndex;
                return;
            }
            pc = sequenceData.FirstChild + nodeState.Cursor;
            returning = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExecuteSelector(
            ref int pc,
            ref NodeStatus childStatus,
            ref bool returning,
            ref ushort abortingNode,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob)
        {
            var selectorData = blob.SelectorNodes[nodeData.DataIndex];
            if (!returning)
            {
                nodeState.Cursor = 0;
            }
            else
            {
                switch (childStatus)
                {
                    case NodeStatus.Failure:
                        nodeState.Cursor++;
                        break;
                    case NodeStatus.Running:
                    {
                        if (nodeState.MemoryCursor != None &&
                            nodeState.MemoryCursor > nodeState.Cursor)
                        {
                            abortingNode = (ushort)(selectorData.FirstChild + nodeState.MemoryCursor);
                        }
                        nodeState.MemoryCursor = nodeState.Cursor;
                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                    }
                    default: //Success
                    {
                        if (nodeState.MemoryCursor != None &&
                            nodeState.MemoryCursor > nodeState.Cursor)
                        {
                            abortingNode = (ushort)(selectorData.FirstChild + nodeState.MemoryCursor);
                        }
                        nodeState.MemoryCursor = None;
                        nodeState.Cursor = None;

                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                    }
                }
            }

            if (nodeState.Cursor >= selectorData.ChildCount)
            {
                nodeState.Cursor = None;
                nodeState.MemoryCursor = None;
                returning = true;
                childStatus = NodeStatus.Failure;
                pc = nodeData.ParentIndex;
                return;
            }
            pc = selectorData.FirstChild + nodeState.Cursor;
            returning = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExecuteMemorySequence(
            ref int pc,
            ref NodeStatus childStatus,
            ref bool returning,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob)
        {
            var memorySequenceData = blob.MemorySequenceNodes[nodeData.DataIndex];
            if (!returning)
            {
                if (nodeState.MemoryCursor == None)
                    nodeState.MemoryCursor = 0;
            }
            else
            {
                switch (childStatus)
                {
                    case NodeStatus.Success:
                        nodeState.MemoryCursor++;
                        break;
                    case NodeStatus.Running:
                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                    default: // Failure
                    {
                        if (memorySequenceData.ResetOnFailure != 0) nodeState.MemoryCursor = None;
                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                    }
                }
            }
            if (nodeState.MemoryCursor >= memorySequenceData.ChildCount)
            {
                nodeState.MemoryCursor = None;
                returning = true;
                childStatus = NodeStatus.Success;
                pc = nodeData.ParentIndex;
                return;
            }
            pc = memorySequenceData.FirstChild + nodeState.MemoryCursor;
            returning = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExecuteMemorySelector(
            ref int pc,
            ref NodeStatus childStatus,
            ref bool returning,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob,
            ref Random rng)
        {
            var memorySelectorData = blob.MemorySelectorNodes[nodeData.DataIndex];
            if (!returning)
            {
                if (nodeState.MemoryCursor == None)
                {
                    if (memorySelectorData.PickRandom != 0)
                    {
                        nodeState.MemoryCursor = (byte)rng.NextInt(0, memorySelectorData.ChildCount);
                        nodeState.TmpA = nodeState.MemoryCursor;
                    }
                    else
                        nodeState.MemoryCursor = 0;
                }
            }
            else
            {
                switch (childStatus)
                {
                    case NodeStatus.Failure:
                        nodeState.MemoryCursor++;
                        if (memorySelectorData.PickRandom != 0)
                        {
                            if (nodeState.MemoryCursor == memorySelectorData.ChildCount) nodeState.MemoryCursor = 0;
                            if (nodeState.MemoryCursor == nodeState.TmpA) nodeState.MemoryCursor = None;
                        }
                        break;
                    case NodeStatus.Running:
                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                    default:
                        nodeState.MemoryCursor = None;
                        returning = true;
                        pc = nodeData.ParentIndex;
                        return;
                }
            }
            if (nodeState.MemoryCursor >= memorySelectorData.ChildCount)
            {
                nodeState.MemoryCursor = None;
                returning = true;
                childStatus = NodeStatus.Failure;
                pc = nodeData.ParentIndex;
                return;
            }
            pc = memorySelectorData.FirstChild + nodeState.MemoryCursor;
            returning = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExecuteParallel(
            ref int pc,
            ref NodeStatus childStatus,
            ref bool returning,
            ref ushort abortingNode,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob,
            ref DynamicBuffer<NodeStateElement> nodeStates)
        {
            var parallelData = blob.ParallelNodes[nodeData.DataIndex];
            if (!returning)
            {
                nodeState.Cursor = 0;
                nodeState.TmpA = 0;
                nodeState.TmpB = 0;
            }
            else
            {
                if (childStatus == NodeStatus.Failure)
                {
                    nodeState.TmpB++;
                    if (parallelData.CacheChildStatus == 1)
                    {
                        var cachedState = nodeStates[parallelData.FirstChild + nodeState.Cursor];
                        cachedState.CachedStatus = Failure;
                        nodeStates[parallelData.FirstChild + nodeState.Cursor] = cachedState;
                    }
                }
                else if (childStatus == NodeStatus.Success)
                {
                    nodeState.TmpA++;
                    if (parallelData.CacheChildStatus == 1)
                    {
                        var cachedState = nodeStates[parallelData.FirstChild + nodeState.Cursor];
                        cachedState.CachedStatus = Success;
                        nodeStates[parallelData.FirstChild + nodeState.Cursor] = cachedState;
                    }
                }
                nodeState.Cursor++;
            }

            if (nodeState.Cursor < parallelData.ChildCount && parallelData.CacheChildStatus == 1)
            {
                var nextNodeState = nodeStates[parallelData.FirstChild + nodeState.Cursor];
                while (nextNodeState.CachedStatus != Unknown)
                {
                    if (nextNodeState.CachedStatus == Success)
                        nodeState.TmpA++;
                    else if (nextNodeState.CachedStatus == Failure)
                        nodeState.TmpB++;
                    nodeState.Cursor++;
                    if (nodeState.Cursor >= parallelData.ChildCount)
                        break;
                    nextNodeState = nodeStates[parallelData.FirstChild + nodeState.Cursor];
                }
            }

            if (nodeState.Cursor >= parallelData.ChildCount)
            {
                childStatus = NodeStatus.Running;
                if (nodeState.TmpA >= parallelData.SuccessThreshold)
                    childStatus = NodeStatus.Success;
                if (nodeState.TmpB >= parallelData.FailsThreshold)
                    childStatus = NodeStatus.Failure;
                if (childStatus != NodeStatus.Running)
                {
                    abortingNode = (ushort)pc;
                }
                returning = true;
                pc = nodeData.ParentIndex;
                return;
            }
            pc = parallelData.FirstChild + nodeState.Cursor;
            returning = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AbortParallel(
            ref FixedList4096Bytes<int> stack,
            ref NodeStateElement nodeState,
            ref Node nodeData,
            ref BehaviourTreeBlob blob,
            ref DynamicBuffer<NodeStateElement> nodeStates)
        {
            var parallelData = blob.ParallelNodes[nodeData.DataIndex];
            for (int i = 0; i < parallelData.ChildCount; i++)
            {
                var childIdx = parallelData.FirstChild + i;
                stack.Add(childIdx);
                var childState = nodeStates[childIdx];
                childState.CachedStatus = Unknown;
                nodeStates[childIdx] = childState;
            }
            nodeState.Cursor = None;
            nodeState.TmpA = 0;
            nodeState.TmpB = 0;
        }
    }
}
