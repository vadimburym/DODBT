// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

#if DODBT_DECORATORS
using System;

namespace VadimBurym.DodBehaviourTree.Light
{
    public sealed partial class BehaviourTree<TContext, TLeafState>
        where TContext : class where TLeafState : struct
    {
        private bool _requiresTime;
        private double _maximumDuration;

        private void InitializeDecorators()
        {
            _requiresTime = false;
            _maximumDuration = 0;
            foreach (var node in _asset.Nodes)
            {
                if (node.Id != NodeId.Timeout && node.Id != NodeId.Cooldown) continue;
                _requiresTime = true;
                _maximumDuration = Math.Max(_maximumDuration, _asset.DecoratorNodes[node.DataIndex].Duration);
            }
        }

        public void Tick(TContext context, BtState<TLeafState> state, double now) => TickWithStatus(context, state, now);

        public NodeStatus TickWithStatus(TContext context, BtState<TLeafState> state, double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || double.IsInfinity(now + _maximumDuration))
                throw new ArgumentOutOfRangeException(nameof(now), "Time and decorator deadlines must be finite.");
            if (state.HasTickTime && now < state.TickTime)
                throw new ArgumentOutOfRangeException(nameof(now), "Time cannot move backwards within an initialized agent state.");
            state.TickTime = now;
            state.HasTickTime = true;
            return TickCore(context, state);
        }

        private NodeStatus TickDecorator(TContext context, BtState<TLeafState> state, Node node)
        {
            ref var data = ref _asset.DecoratorNodes[node.DataIndex];
            ref var local = ref state.DecoratorStates[node.DataIndex];
            if (node.Id == NodeId.Timeout)
            {
                if (!local.HasDeadline)
                {
                    local.Deadline = state.TickTime + data.Duration;
                    local.HasDeadline = true;
                }
                if (state.TickTime >= local.Deadline)
                {
                    AbortDecorator(context, state, node);
                    return NodeStatus.Failure;
                }
            }
            else if (node.Id == NodeId.Cooldown && !local.Active && local.HasDeadline && state.TickTime < local.Deadline)
                return NodeStatus.Failure;

            var result = TickNode(context, state, data.Child);
#if UNITY_EDITOR
            state.DebugStatus[data.Child] = result;
#endif
            local.Active = result == NodeStatus.Running;
            if (!local.Active)
            {
                if (node.Id == NodeId.Cooldown)
                {
                    local.Deadline = state.TickTime + data.Duration;
                    local.HasDeadline = true;
                }
                else local.HasDeadline = false;
            }
            if (node.Id == NodeId.Inverter)
            {
                if (result == NodeStatus.Success) return NodeStatus.Failure;
                if (result == NodeStatus.Failure) return NodeStatus.Success;
            }
            return result;
        }

        private void AbortDecorator(TContext context, BtState<TLeafState> state, Node node)
        {
            ref var local = ref state.DecoratorStates[node.DataIndex];
            bool active = local.Active;
            local.Active = false;
            if (node.Id != NodeId.Cooldown) local.HasDeadline = false;
            if (active) AbortNode(context, state, _asset.DecoratorNodes[node.DataIndex].Child);
        }
    }
}
#endif
