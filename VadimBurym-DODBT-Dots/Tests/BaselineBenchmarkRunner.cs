// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
// Reference runner from ce37bfb for comparative performance tests.

using VadimBurym.DodBehaviourTree.Dots.Tests;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace VadimBurym.DodBehaviourTree.Dots.Generated
{
    public struct BaselineBenchmarkRunner
    {
#if DODBT_SMALL_SIZE
        private const ushort None = 0xFF;
#else
        private const ushort None = 0xFFFF;
#endif

        public NodeStatus Tick(
            Entity entity,
            ref BehaviourTreeBlob blob,
            ref Random rng,
            DynamicBuffer<NodeStateElement> nodeStates,
            DynamicBuffer<LeafStateElement> leafStates,
            in BenchmarkContext leafContext,
            int sortKey)
        {
            var pc = blob.RootIndex;
            var childStatus = NodeStatus.Running;
            bool returning = false;

            while (pc != None)
            {
                var nodeData = blob.Nodes[pc];
                var nodeStatePc = pc;
                var nodeState = nodeStates[nodeStatePc];
                ushort abortingNode = None;
                switch (nodeData.Id)
                {
                    case NodeId.Leaf:
                    {
                        var leafState = leafStates[nodeState.LeafStateIndex];
                        var leafData = blob.Leafs[nodeData.DataIndex];

                        var runnerState = new RunnerState_BenchmarkContext {
                            Agent = entity,
                            Context = leafContext,
                            LeafData = leafData,
                            LeafState = leafState,
                            Random = rng,
                            SortKey = sortKey
                        };

                        if (leafState.IsEntered == 0)
                        {
                            LeafTables_BenchmarkContext.EnterLeaf(leafData.LeafId, ref runnerState);
                            runnerState.LeafState.IsEntered = 1;
                        }
                        var status = LeafTables_BenchmarkContext.TickLeaf(leafData.LeafId, ref runnerState);
                        if (status != NodeStatus.Running)
                        {
                            LeafTables_BenchmarkContext.ExitLeaf(leafData.LeafId, ref runnerState);
                            runnerState.LeafState.IsEntered = 0;
                        }

                        leafStates[nodeState.LeafStateIndex] = runnerState.LeafState;
                        childStatus = status;
                        returning = true;
                        pc = nodeData.ParentIndex;
                        break;
                    }
                    case NodeId.Sequence:
                    {
                        BaselineNodeExecutor.ExecuteSequence(
                            ref pc,
                            ref childStatus,
                            ref returning,
                            ref abortingNode,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }
                    case NodeId.Selector:
                    {
                        BaselineNodeExecutor.ExecuteSelector(
                            ref pc,
                            ref childStatus,
                            ref returning,
                            ref abortingNode,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }
                    case NodeId.MemorySequence:
                    {
                        BaselineNodeExecutor.ExecuteMemorySequence(
                            ref pc,
                            ref childStatus,
                            ref returning,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }
                    case NodeId.MemorySelector:
                    {
                        BaselineNodeExecutor.ExecuteMemorySelector(
                            ref pc,
                            ref childStatus,
                            ref returning,
                            ref nodeState,
                            ref nodeData,
                            ref blob,
                            ref rng);
                        break;
                    }
                    case NodeId.Parallel:
                    {
                        BaselineNodeExecutor.ExecuteParallel(
                            ref pc,
                            ref childStatus,
                            ref returning,
                            ref abortingNode,
                            ref nodeState,
                            ref nodeData,
                            ref blob,
                            ref nodeStates);
                        break;
                    }
                    default:
                    {
                        childStatus = NodeStatus.Failure;
                        returning = true;
                        pc = nodeData.ParentIndex;
                        break;
                    }
                }
                if (abortingNode != None)
                    AbortSubtree(entity, ref blob, nodeStates, leafStates, abortingNode, leafContext, sortKey);
                nodeStates[nodeStatePc] = nodeState;
            }
            return childStatus;
        }

        public void Abort(
            Entity entity,
            ref BehaviourTreeBlob blob,
            DynamicBuffer<NodeStateElement> nodeStates,
            DynamicBuffer<LeafStateElement> leafStates,
            in BenchmarkContext leafContext,
            int sortKey)
        {
            AbortSubtree(entity, ref blob, nodeStates, leafStates, (ushort)blob.RootIndex, leafContext, sortKey);
        }

        private void AbortSubtree(
            Entity entity,
            ref BehaviourTreeBlob blob,
            DynamicBuffer<NodeStateElement> nodeStates,
            DynamicBuffer<LeafStateElement> leafStates,
            ushort root,
            in BenchmarkContext leafContext,
            int sortKey)
        {
            FixedList4096Bytes<int> stack = default;
            stack.Add(root);

            while (stack.Length > 0)
            {
                var nodeIndex = stack[^1];
                stack.RemoveAt(stack.Length - 1);
                var nodeData = blob.Nodes[nodeIndex];
                var nodeState = nodeStates[nodeIndex];
                switch (nodeData.Id)
                {
                    case NodeId.Leaf:
                    {
                        var leafState = leafStates[nodeState.LeafStateIndex];

                        if (leafState.IsEntered != 0)
                        {
                            leafState.IsEntered = 0;
                            var leafData = blob.Leafs[nodeData.DataIndex];

                            var runnerState = new RunnerState_BenchmarkContext {
                                Agent = entity,
                                Context = leafContext,
                                LeafData = leafData,
                                LeafState = leafState,
                                SortKey = sortKey
                            };

                            LeafTables_BenchmarkContext.AbortLeaf(leafData.LeafId, ref runnerState);
                            leafStates[nodeState.LeafStateIndex] = runnerState.LeafState;
                        }
                        break;
                    }
                    case NodeId.Selector:
                    {
                        BaselineNodeExecutor.AbortSelector(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }
                    case NodeId.Sequence:
                    {
                        BaselineNodeExecutor.AbortSequence(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }
                    case NodeId.MemorySelector:
                    {
                        BaselineNodeExecutor.AbortMemorySelector(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }
                    case NodeId.MemorySequence:
                    {
                        BaselineNodeExecutor.AbortMemorySequence(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }
                    case NodeId.Parallel:
                    {
                        BaselineNodeExecutor.AbortParallel(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob,
                            ref nodeStates);
                        break;
                    }
                }
                nodeStates[nodeIndex] = nodeState;
            }
        }
    }
}
