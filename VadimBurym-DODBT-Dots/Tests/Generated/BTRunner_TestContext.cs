// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal struct BTRunner_TestContext
    {
#if DODBT_SMALL_SIZE
        private const ushort None = 0xFF;
#else
        private const ushort None = 0xFFFF;
#endif

        public NodeStatus Tick(
            ref BehaviourTreeBlob blob,
            ref Random rng,
            DynamicBuffer<NodeStateElement> nodeStates,
            DynamicBuffer<RecordingLeafState> leafStates,
            in TestContext leafContext)
        {
            var pc = blob.RootIndex;
            var childStatus = NodeStatus.Running;
            var returning = false;

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

                        var runnerState = new RunnerState_TestContext {
                            LeafState = leafState,
                            LeafData = leafData,
                            Context = leafContext
                        };

                        if (leafState.IsEntered == 0)
                        {
                            LeafTables_TestContext.EnterLeaf(leafData.LeafId, ref runnerState);
                            runnerState.LeafState.IsEntered = 1;
                        }

                        var status = LeafTables_TestContext.TickLeaf(leafData.LeafId, ref runnerState);
                        if (status != NodeStatus.Running)
                        {
                            LeafTables_TestContext.ExitLeaf(leafData.LeafId, ref runnerState);
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
                        NodeExecutor.ExecuteSequence(
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
                        NodeExecutor.ExecuteSelector(
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
                        NodeExecutor.ExecuteMemorySequence(
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
                        NodeExecutor.ExecuteMemorySelector(
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
                        NodeExecutor.ExecuteParallel(
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
                    AbortSubtree(ref blob, nodeStates, leafStates, abortingNode, leafContext);

                nodeStates[nodeStatePc] = nodeState;
            }

            return childStatus;
        }

        public void Abort(
            ref BehaviourTreeBlob blob,
            DynamicBuffer<NodeStateElement> nodeStates,
            DynamicBuffer<RecordingLeafState> leafStates,
            in TestContext leafContext)
        {
            AbortSubtree(ref blob, nodeStates, leafStates, (ushort)blob.RootIndex, leafContext);
        }

        private void AbortSubtree(
            ref BehaviourTreeBlob blob,
            DynamicBuffer<NodeStateElement> nodeStates,
            DynamicBuffer<RecordingLeafState> leafStates,
            ushort root,
            in TestContext leafContext)
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

                            var runnerState = new RunnerState_TestContext {
                                LeafState = leafState,
                                LeafData = leafData,
                                Context = leafContext
                            };

                            LeafTables_TestContext.AbortLeaf(leafData.LeafId, ref runnerState);
                            leafStates[nodeState.LeafStateIndex] = runnerState.LeafState;
                        }
                        break;
                    }

                    case NodeId.Selector:
                    {
                        NodeExecutor.AbortSelector(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }

                    case NodeId.Sequence:
                    {
                        NodeExecutor.AbortSequence(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }

                    case NodeId.MemorySelector:
                    {
                        NodeExecutor.AbortMemorySelector(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }

                    case NodeId.MemorySequence:
                    {
                        NodeExecutor.AbortMemorySequence(
                            ref stack,
                            ref nodeState,
                            ref nodeData,
                            ref blob);
                        break;
                    }

                    case NodeId.Parallel:
                    {
                        NodeExecutor.AbortParallel(
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
