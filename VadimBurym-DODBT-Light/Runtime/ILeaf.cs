// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light
{
    public interface ILeaf {}
    public interface ILeaf<in TContext, TLeafState> : ILeaf
        where TContext : class where TLeafState : struct
    {
        NodeStatus OnTick(TContext context, ref TLeafState state);
        void OnEnter(TContext context, ref TLeafState state);
        void OnExit(TContext context, ref TLeafState state, NodeStatus exitStatus);
        void OnAbort(TContext context, ref TLeafState state);
    }
}